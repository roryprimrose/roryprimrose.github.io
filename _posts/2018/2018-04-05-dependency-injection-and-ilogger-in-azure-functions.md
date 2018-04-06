---
title: Dependency Injection and ILogger in Azure Functions
categories: .Net
tags: 
date: 2018-04-05 23:05:00 +10:00
---

Azure Functions is a great platform for running small quick workloads. I have been migrating some code over to Azure Functions where the code was written with dependency injection and usages of ```ILogger<T>``` in the lower level dependencies. This post will go through how to support these two requirements in Azure Functions.

<!--more-->

## Dependency Injection

There are already several posts around that provide a solution for dependency injection in Azure Functions v2. They use an extensibility point in Azure Functions for binding values to the parameters on the static entry point of the function. The code below is based on [this post][0].

It starts with the creation of a marker attribute.

```csharp
[Binding]
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public class InjectAttribute : Attribute
{
    public InjectAttribute(Type type)
    {
        Type = type;
    }

    public Type Type { get; }
}
```

The extension point uses ```IExtensionConfigProvider``` to register a binder for function parameters that are resolved using an IoC container. The bulk of this class is creating the Autofac container in a thread safe way to make sure that the container is only created once.

```csharp
public class InjectConfiguration : IExtensionConfigProvider
{
    private static readonly object _syncLock = new object();
    private static IContainer _container;

    public void Initialize(ExtensionConfigContext context)
    {
        InitializeContainer(context);

        context
            .AddBindingRule<InjectAttribute>()
            .BindToInput<dynamic>(i => _container.Resolve(i.Type));
    }

    private void InitializeContainer(ExtensionConfigContext context)
    {
        if (_container != null)
        {
            return;
        }

        lock (_syncLock)
        {
            if (_container != null)
            {
                return;
            }

            _container = ContainerConfig.BuildContainer(context.Config.LoggerFactory);
        }
    }
}
```

Creating the container is where there starts to be custom code based on the project. This is a fairly general configuration of a container. In this case the types of the current assembly are registered in the container using their interfaces. Then a logger factory and module are registered (more on this later).

```csharp
public static class ContainerConfig
{
    public static IContainer BuildContainer(ILoggerFactory factory)
    {
        var builder = new ContainerBuilder();

        var assemblyTypes = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.GetInterfaces().Any()).ToArray();

        builder.RegisterTypes(assemblyTypes).AsImplementedInterfaces();

        builder.RegisterInstance(factory).As<ILoggerFactory>();
        builder.RegisterModule<LoggerModule>();

        return builder.Build();
    }
}
```

The way this works is that Azure Functions will invoke the extension point in order to bind values to the parameters of the static entry point of the function. The binder will then resolve the parameter value using the Autofac container.

This simple setup provides great flexibility for developing Azure Functions with dependency injection. It is less than ideal that Azure Functions uses a static member as the entry point so this is as good as it gets for now. It is possible that this will [change in the future][1].

## ILogger

The default Azure Functions template in Visual Studio uses ```TraceWriter``` for logging. This works well when developing locally with Visual Studio and also writes log entries out to the Azure Portal log section for the function when deployed to Azure. I have already been using ```ILogger``` and ```ILogger<T>``` in my code so prefer it over ```TraceWriter```.

Azure Functions natively support binding to a ```ILogger``` parameter on the static method of the function however there are currently some disadvantages. It does not support binding ```ILogger<T>``` or ```ILoggerFactory``` parameters, [does not write entries to the local dev console][2] and does not write to the Azure Portal log section. It does however write the log entries to Application Insights in production and that is enough for me.

I have been using an Autofac module to dynamically create logger instances for the target types being created with the logger dependency. Originally this module would create log4net log instances and now creates ```ILogger``` and ```ILogger<T>``` instances.

```csharp
public class LoggerModule : Module
{
    private static readonly ConcurrentDictionary<Type, object> _logCache = new ConcurrentDictionary<Type, object>();

    private interface ILoggerWrapper
    {
        object Create(ILoggerFactory factory);
    }

    protected override void AttachToComponentRegistration(
        IComponentRegistry componentRegistry,
        IComponentRegistration registration)
    {
        // Handle constructor parameters.
        registration.Preparing += OnComponentPreparing;
    }

    private static object GetGenericTypeLogger(IComponentContext context, Type declaringType)
    {
        return _logCache.GetOrAdd(
            declaringType,
            x =>
            {
                var wrapper = typeof(LoggerWrapper<>);
                var specificWrapper = wrapper.MakeGenericType(declaringType);
                var instance = (ILoggerWrapper)Activator.CreateInstance(specificWrapper);

                var factory = context.Resolve<ILoggerFactory>();

                return instance.Create(factory);
            });
    }

    private static object GetLogger(IComponentContext context, Type declaringType)
    {
        return _logCache.GetOrAdd(
            declaringType,
            x =>
            {
                var factory = context.Resolve<ILoggerFactory>();

                return factory.CreateLogger(declaringType);
            });
    }

    private static void OnComponentPreparing(object sender, PreparingEventArgs e)
    {
        var t = e.Component.Activator.LimitType;

        if (t.FullName.IndexOf(nameof(FunctionApp1), StringComparison.OrdinalIgnoreCase) == -1)
        {
            return;
        }

        if (t.FullName.EndsWith("[]", StringComparison.OrdinalIgnoreCase))
        {
            // Ignore IEnumerable types
            return;
        }

        e.Parameters = e.Parameters.Union(
            new[]
            {
                new ResolvedParameter((p, i) => p.ParameterType == typeof(ILogger), (p, i) => GetLogger(i, t)),
                new ResolvedParameter(
                    (p, i) => p.ParameterType.GenericTypeArguments.Any() &&
                                p.ParameterType.GetGenericTypeDefinition() == typeof(ILogger<>),
                    (p, i) => GetGenericTypeLogger(i, t))
            });
    }

    private class LoggerWrapper<T> : ILoggerWrapper
    {
        public object Create(ILoggerFactory factory)
        {
            return factory.CreateLogger<T>();
        }
    }
}
```

The line of code to note above is the filter against the namespace of the target type requested of the Autofac container. This module will only create log instances for target types that exist under the specified base namespace. This is hard-coded against a namespace using the ```nameof(FunctionApp1)``` call and will need to be updated if you use this in your own projects. This could be refactored to use the base namespace of the assembly containing the module but this will not always catch all scenarios. 

This module requires an ```ILoggerFactory``` to be already registered in the container. The module must resolve this dependency to create logger instances. Thankfully this is available in the ```InjectConfiguration.InitializeContainer``` extensibilty point via ```context.Config.LoggerFactory``` which is then passed to the ```ContainerConfig``` class. The factory is then registered in the container making it available to the ```LoggerModule```.

All this now works as expected because of Autofac tying everything together. This means for example that the following code works perfectly.

```csharp
public static class Function1
{
    [FunctionName("Function1")]
    public static void Run(
        [TimerTrigger("0 */1 * * * *")]TimerInfo myTimer,
        [Inject(typeof(ISomething))]ISomething something,
        ILogger log)
    {
        log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

        var value = something.GetSomething();

        log.LogInformation("Found value " + value);
    }
}

public interface ISomething
{
    string GetSomething();
}

public class Something : ISomething
{
    private readonly ILogger<Something> _logger;

    public Something(ILogger<Something> logger)
    {
        _logger = logger;
    }

    public string GetSomething()
    {
        _logger.LogInformation("Hey, we are doing something");

        return Guid.NewGuid().ToString();
    }
}
```

[0]: https://blog.wille-zone.de/post/azure-functions-dependency-injection/
[1]: https://github.com/Azure/Azure-Functions/issues/299#issuecomment-378384724
[2]: https://github.com/Azure/azure-functions-core-tools/issues/130
