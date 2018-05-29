Exception reporting and alerting has always been important for software delivery. I've seen companies use many solutions to this over the years like using email as an error logging system (that story does not end well) and the Windows Event Log that people usually do not monitor. On the other end of the spectrum are more mature systems like [Raygun.com][0] and [Sentry.io][1]. 

In the classic argument of don't build something that someone else can do better and cheaper, I've been using Raygun and Sentry for years. Over this time I've been looking at easier ways of integrating with these systems while also reducing coupling in the application. This post looks at how to integrate with Sentry in .Net via ```ILogger```.

<!--more-->

Using ```ILogger``` as the integration point works well because logging is almost always a cross-cutting concern. Well defined systems are likely to already have logging integrated throughout the code-base. This means that reporting errors to Sentry via ```ILogger``` can be achieved with very little effort. Additionally, the coupling to a logging framework is typically expected as it is cross-cutting more so than coupling your code to ```IRavenClient```. With enough integration smarts, most of the application code should not have to understand how to talk to Sentry in order to send errors their way.

I have created Divergic.Logging.Sentry NuGet packages to support this integration which are [open-source on GitHub][6]. The design of the packages has several goals to achieve.

- Easy installation
- Sole dependency in "application" code is ```ILogger```
- Minimal bootstrapping required in "host" code
- Support custom exception properties
- Support logging context data
- Prevent duplicate reports
- Support Autofac
- Support special serialization (NodaTime)

# Installing

There are several NuGet packages to give flexibility around how applications can use this feature.

- ```Install-Package Divergic.Logging.Sentry``` [on NuGet.org][2] contains the core logic for sending errors to Sentry
- ```Install-Package Divergic.Logging.Sentry.Autofac``` [on NuGet.org][3] contains a helper module to registering ```RavenClient``` in Autofac
- ```Install-Package Divergic.Logging.Sentry.NodaTime``` [on NuGet.org][4] contains an extension to the base package to support serialization of NodaTime data types
- ```Install-Package Divergic.Logging.Sentry.All``` [on NuGet.org][5] is a meta package that contains all the above packages

# Bootstrapping

Applications often have some kind of bootstrapping code when they start up which is often where logging is configured. The Divergic.Logging.Sentry package requires access to an ```ILoggerFactory``` instance to configure logging to Sentry. 

An ASP.Net core application for example will provide this in the Configure method. With the Divergic.Logging.Sentry package 

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

Once the logger factory has been configured with Sentry, any ILogger instance created with the factory will include the integration for sending errors to Sentry. The way this work is that any log message that contains a ```System.Exception``` parameter will be sent to Sentry. All other log messages will be ignored.

#Custom properties

Exceptions in .Net often contain additional information that is not included in the ```Message``` property. Some good examples of this are ```SqlException``` and the Azure ```StorageException```. This information is additional metadata about the error which is critical to identifying the error. The intent of the design is sound, but falls down quickly when most logging systems will only log the exception stacktrace and message. The result is an error report that is not actionable.

One of the advantages of ```RavenClient``` is that ```System.Exception.Data``` is serialized and sent in the error report to Sentry. What is missing is the ability to record any other custom exception property. The Divergic.Logging.Sentry package will cater for this by adding each custom property to the Data property using the exception type and property name as the dictionary key. The value will be added as is for value types and strings. All other reference types will be JSON encoded when stored in the Data property. All these additional information will now be sent to Sentry.

Consider the following exception.

```csharp
public class Person
{
	public string Email { get; set; }

	public string FirstName { get; set; }

	public string LastName { get; set; }

	public string Mobile { get; set; }
}

public class Company
{
	public string Address { get; set; }

	public string Name { get; set; }

	public Person Owner { get; set; }
}

public class PaymentFailedException : Exception
{
	public Company Company { get; set; }
}
```

If this exception was to be thrown and logged, the Sentry record would contain something like the following report.

```csharp


```

The following information can now be captured when reporting custom property data.

```csharp


```

# Logging context data

A common issue with dealing with error reports is having enough information so that staff can repond to the issues. Logging just the exception information may not provide enough information.

Consider the following scenario.

```csharp
public class PaymentGatewayException: Exception
{
	public PaymentGatewayException() : base("Failed to process complete transaction at the payment gateway.")
	{
	}
}
```

In this scenario, the message of the exception may be written for the purposes of sending the text to a user interface. That does not help in resolving the issue when the error is reported to Sentry. Perhaps we want to know what was being processed at the time. 

This can be achieved in a similar way to reporting custom exception properties as above.

```csharp
public async Task ProcessPayment(string invoiceId, int amountInCents, Person customer, CancellationToken cancellationToken)
{
	try
	{
		await _gateway.ProcessPayment(invoiceId, amountInCents, customer.Email, cancellationToken).ConfigureAwait(false);
	}
	catch (PaymentGatewayException ex)
	{
		dynamic paymentDetails = new {
			invoiceId,
			amountInCents
		};
		
		_logger.LogErrorWithContext(ex, paymentDetails);
	}
}
```

This additional context data is then sent to Sentry.

# Preventing duplicate reports

Consider a scenario where a low level component throws an exception. The caller could capture that exception and log it. The error is then sent to Sentry. The exception is then thrown up the stake and potentially logged again. The Divergic.Logging.Sentry package caters for this by tracking the Sentry error report id against the exceptions Data property. The exception is not sent to Sentry again if this value is found on the exception.

# Supporting Autofac

The Divergic.Logging.Sentry.Autofac package contains a module to assist with setting up Sentry in an Autofac container. The requirement is that the application bootstrapping has already been able to create a ```SentryConfig``` class and registered it in Autofac. The Autofac module registers a new RavenClient instance using that configuration.

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
		Version = "0.1.0
	}

    var container = BuildContainer(sentryConfig);
	
	var loggerFactory = services.BuildServiceProvider().GetService<ILoggerFactory>();
	var ravenClient = container.Resolve<IRavenClient>();	
	
	loggerFactory.AddSentry(ravenClient);
	
	// Create the IServiceProvider based on the container.
	return new AutofacServiceProvider(container);
}
```

# Support NodaTime serialization

Sending custom exception properties and exception context data to Sentry is really useful, but only as useful as how the data can be understood. ```NodaTime.Instant``` is a good example of this. The native JSON serialization of an Instant value will be a number rather than a readable date/time value.



[0]: https://raygun.com/
[1]: https://sentry.io/welcome/
[2]: https://www.nuget.org/packages/Divergic.Logging.Sentry
[3]: https://www.nuget.org/packages/Divergic.Logging.Sentry.Autofac
[4]: https://www.nuget.org/packages/Divergic.Logging.Sentry.NodaTime
[5]: https://www.nuget.org/packages/Divergic.Logging.Sentry.All
[6]: https://github.com/Divergic/Divergic.Logging.Sentry
