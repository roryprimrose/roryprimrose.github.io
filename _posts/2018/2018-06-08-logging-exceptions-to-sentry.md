---
title: ILogger for Sentry.io
categories: .Net
tags: 
date: 2018-06-08 08:07:00 +10:00
---

Exception reporting and alerting has always been important for software delivery. I've seen companies use many solutions for this over the years, from using email as an error logging system (that story does not end well....twice) to the Windows Event Log that people usually do not monitor. On the other end of the spectrum are more mature systems like [Raygun.com](https://raygun.com/) and [Sentry.io](https://sentry.io/). 

Aligning with the idea of "don't build something that someone else can do better and cheaper", I've been using Raygun and Sentry for years. Over this time I've been looking at easier ways of integrating with these systems while also reducing coupling in the application code. This post looks at how to integrate with Sentry via ```ILogger```.

<!--more-->

Using ```ILogger``` as the integration point works well because logging is almost always a cross-cutting concern. Well defined systems are likely to already have logging integrated throughout the code so reporting exceptions to Sentry via ```ILogger``` can be achieved with very little effort. Coupling application code to ```ILogger``` is also preferable to coupling code to Sentry's ```IRavenClient```. With enough integration smarts, most of the application code should not have to understand how to talk to Sentry in order to send exceptions their way.

I've published a suite of [Divergic.Logging NuGet packages](https://www.nuget.org/packages?q=Divergic.Logging) to add support for this integration ([open-source on GitHub](https://github.com/Divergic/Divergic.Logging.Sentry)). The design of the packages achieve several goals.

- Easy installation
- Sole dependency in "application" code is ```ILogger```
- Minimal bootstrapping required in "host" code
- Support custom exception properties
- Support logging context data
- Prevent duplicate reports
- Support Autofac
- Support special serialization (NodaTime)
- Clean stacktrace reporting

## Installing

There are several NuGet packages to give flexibility around how applications can use this feature.

- ```Install-Package Divergic.Logging.Sentry``` [on NuGet.org](https://www.nuget.org/packages/Divergic.Logging.Sentry) contains the ```ILogger``` provider for sending exceptions to Sentry
- ```Install-Package Divergic.Logging.Sentry.Autofac``` [on NuGet.org](https://www.nuget.org/packages/Divergic.Logging.Sentry.Autofac) extends the base package to provide an Autofac module to register ```RavenClient``` in Autofac
- ```Install-Package Divergic.Logging.NodaTime``` [on NuGet.org](https://www.nuget.org/packages/Divergic.Logging.NodaTime) extends the inherited Divergic.Logging base package to support serialization of NodaTime data types

All the above packages depend on the Divergic.Logging package that provides ```ILogger``` extension methods for adding context information to an exception.

## Bootstrapping

Applications often have some kind of bootstrapping code when they start up which is where logging is typically configured. The Divergic.Logging.Sentry package requires access to an ```ILoggerFactory``` instance on which it provides the ```AddSentry``` extension method. An ASP.Net core application for example will provide ```ILoggerFactory``` in the Configure method.

```csharp
public void Configure(
    IApplicationBuilder app,
    IHostingEnvironment env,
    ILoggerFactory loggerFactory,
    IApplicationLifetime appLifetime)
{
    // You will need to create the RavenClient before configuring the logging integration
    var ravenClient = BuildRavenClient();
    
    loggerFactory.AddSentry(ravenClient);
}
```

Any ILogger instance created with the factory will now send exceptions to Sentry. Any log message without an exception will be ignored by this logging provider.

## Custom properties

Exceptions in .Net often contain additional information that is not included in the ```Exception.Message``` property. Some good examples of this are ```SqlException```, ```ReflectionTypeLoadException ``` and the Azure ```StorageException```. The ReflectionTypeLoadException message for example will log the following message.

> ReflectionTypeLoadException: Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.

These properties contain additional metadata about the exception that is critical to identifying the error. Unfortunately most logging systems will only log the exception stacktrace and message. The result is an error report that is unactionable.

One of the advantages of ```RavenClient``` is that the ```System.Exception.Data``` property is sent in the error report to Sentry. What is missing is the ability to record any other custom exception properties. The Divergic.Logging.Sentry package caters for this by adding each custom exception property to the Data property so that it will be sent to Sentry.

Consider the following exception.

```csharp
public class Person
{
    public string Email { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }
}

public class Company
{
    public string Address { get; set; }

    public string Name { get; set; }

    public Person Owner { get; set; }
}

public class CustomPropertyException : Exception
{
    public Company Company { get; set; }

    public int Value { get; set; }
}
```

If this exception was to be thrown and logged without custom property data, the Sentry record would contain something like the following report.

```json
{
   "message":"Exception of type 'Divergic.Logging.Sentry.IntegrationTests.CustomPropertyException' was thrown.",
   "metadata":{
      "type":"CustomPropertyException",
      "value":"Exception of type 'Divergic.Logging.Sentry.IntegrationTests.CustomPropertyException' was thrown."
   }
}
```

Capturing the custom properties on the exception for the Sentry report now allows the error report to have much richer information.

```json
{
   "message":"Exception of type 'Divergic.Logging.Sentry.IntegrationTests.CustomPropertyException' was thrown.",
   "extra":{
      "CustomPropertyException.Value":2309842,
      "CustomPropertyException.Company":"{\"Address\":\"839 Anniversary Drive\",\"Name\":\"McIleen Inc\",\"Owner\":{\"Email\":\"glori.mcileen@ravenjs.com\",\"FirstName\":\"Glori\",\"LastName\":\"McIleen\"}}"
   },
   "metadata":{
      "type":"CustomPropertyException",
      "value":"Exception of type 'Divergic.Logging.Sentry.IntegrationTests.CustomPropertyException' was thrown."
   }
}
```

## Logging context data

A common issue when dealing with error reports is not having enough information to repond to the error. Logging just the exception data may not be sufficient.

Consider the following exception.

```csharp
public class PaymentGatewayException: Exception
{
    public PaymentGatewayException() : base("Failed to process complete transaction at the payment gateway.")
    {
    }
}
```

Sending this exception to Sentry would produce an error report with something like the following.

```json
{
   "message":"Failed to process complete transaction at the payment gateway.",
   "metadata":{
      "type":"PaymentGatewayException",
      "value":"Failed to process complete transaction at the payment gateway."
   }
}
``` 

In this scenario, the message of the exception may be written for the purposes of sending the text to a user interface. That does not help in resolving the issue when the exception is reported to Sentry. We may need to know what was being processed at the time. The inherited Divergic.Logging package uses the same technique as reporting custom exception properties above by providing ```LogErrorWithContext``` and ```LogCriticalWithContext``` extension methods on ```ILogger```.

```csharp
public async Task ProcessPayment(string invoiceId, int amountInCents, Person customer, CancellationToken cancellationToken)
{
    try
    {
        await _gateway.ProcessPayment(invoiceId, amountInCents, customer.Email, cancellationToken).ConfigureAwait(false);
    }
    catch (PaymentGatewayException ex)
    {
        var paymentDetails = new {
            invoiceId,
            amountInCents
        };
        
        _logger.LogErrorWithContext(ex, paymentDetails);
    }
}
```

This additional context data is then sent to Sentry.

```json
{
   "extra":{
      "ContextData": "{\"invoiceId\":\"239349asfd-234234\",\"amountInCents\":3995}",
   },
   "metadata":{
      "type":"PaymentGatewayException",
      "value":"Failed to process complete transaction at the payment gateway."
   }
}
``` 

## Preventing duplicate reports

Exceptions can be caught, logged and then thrown again. A catch block higher up the callstack may then log the same exception a second time. This scenario is handled by ensuring that any particular exception instance is only reported to Sentry once.

## Supporting Autofac

The Divergic.Logging.Sentry.Autofac package contains ```SentryModule``` to assist with setting up Sentry in an Autofac container. The requirement is that the application bootstrapping has already been able to create an ```ISentryConfig``` instance and registered it in Autofac. ```SentryModule``` registers a new RavenClient instance using that configuration.

Here is an example for ASP.Net core.

```csharp
internal static IContainer BuildContainer(ISentryConfig sentryConfig)
{
    var builder = new ContainerBuilder();
    
    builder.AddInstance(sentryConfig).As<ISentryConfig>();
    builder.AddModule<SentryModule>();
    
    return builder.Build();
}

public IServiceProvider ConfigureServices(IServiceCollection services)
{
    // Load this from a configuration, this is just an example
    var sentryConfig = new SentryConfig
    {
        Dsn = "Sentry DSN value here",
        Environment = "Local",
        Version = "0.1.0"
    }

    var container = BuildContainer(sentryConfig);
    
    var loggerFactory = services.BuildServiceProvider().GetService<ILoggerFactory>();
    var ravenClient = container.Resolve<IRavenClient>();    
    
    loggerFactory.AddSentry(ravenClient);
    
    // Create the IServiceProvider based on the container.
    return new AutofacServiceProvider(container);
}
```

## Support NodaTime serialization

Sending custom exception properties and exception context data to Sentry is only useful if the data can be understood. ```NodaTime.Instant``` is a good example of this. The native JSON serialization of an Instant value will be ```{}``` rather than a readable date/time value.

Consider the following exception.

```csharp
public class CustomPropertyException : Exception
{
    public Instant Point { get; set; }
}
```

Logging this exception to Sentry without NodaTime JSON serialization support would send the following to Sentry.

```json
{
   "message":"Exception of type 'Divergic.Logging.Sentry.IntegrationTests.CustomPropertyException' was thrown.",
   "extra":{
      "CustomPropertyException.Point":"{}"
   },
   "metadata":{
      "type":"CustomPropertyException",
      "value":"Exception of type 'Divergic.Logging.Sentry.IntegrationTests.CustomPropertyException' was thrown."
   }
}
```

The Divergic.Logging.NodaTime package provides  a ```UsingNodaTimeTypes``` extension method on ```ILoggerFactory```. This will configure the correct JSON serialization of NodaTime types when sending errors to Sentry. 

```csharp
public void Configure(
    IApplicationBuilder app,
    IHostingEnvironment env,
    ILoggerFactory loggerFactory,
    IApplicationLifetime appLifetime)
{
    loggerFactory.UsingNodaTimeTypes();
}
```

Sentry would then receive something like the following for the above example.

```json
{
   "message":"Exception of type 'Divergic.Logging.Sentry.IntegrationTests.CustomPropertyException' was thrown.",
   "extra":{
        "CustomPropertyException.Point": "\"2018-06-03T13:34:42.2533212Z\"",
   },
   "metadata":{
      "type":"CustomPropertyException",
      "value":"Exception of type 'Divergic.Logging.Sentry.IntegrationTests.CustomPropertyException' was thrown."
   }
}
```

## Clean stacktrace reporting

A long standing complaint with .Net has been how stacktraces are difficult to read when async/await is involved. While this should be [improved in .Net core 2.1](https://www.ageofascent.com/2018/01/26/stack-trace-for-exceptions-in-dotnet-core-2.1/), all prior .Net versions suffer from this issue. As part of sending exceptions to Sentry, the Divergic.Logging.Sentry package will also include a ```CleanedException``` value in the error report. This value will contain the ```Exception.ToString``` of the exception after it has been through ```Demystify``` via the ```Ben.Demystifier``` NuGet package. Read more about it [here](https://github.com/benaadams/Ben.Demystifier).

## Conclusion

These packages aim to provide a simple and elegant method of sending exceptions to Sentry with the minimum amount of effort. Now you can go forth and fail in style and clarity.