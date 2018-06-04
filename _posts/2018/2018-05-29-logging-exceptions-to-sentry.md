---
title: Logging exceptions to Sentry
categories: .Net
tags: 
date: 2018-05-29 22:07:00 +10:00
---

Exception reporting and alerting has always been important for software delivery. I've seen companies use many solutions for this over the years from using email as an error logging system (that story does not end well) to the Windows Event Log that people usually do not monitor. On the other end of the spectrum are more mature systems like [Raygun.com][0] and [Sentry.io][1]. 

In the classic argument of don't build something that someone else can do better and cheaper, I've been using Raygun and Sentry for years. Over this time I've been looking at easier ways of integrating with these systems while also reducing coupling in the application. This post looks at how to integrate with Sentry via ```ILogger```.

<!--more-->

Using ```ILogger``` as the integration point works well because logging is almost always a cross-cutting concern. Well defined systems are likely to already have logging integrated throughout the code-base so reporting errors to Sentry via ```ILogger``` can be achieved with very little effort. Coupling application code to ```ILogger``` is preferable to coupling your code to ```IRavenClient```. With enough integration smarts, most of the application code should not have to understand how to talk to Sentry in order to send errors their way.

The Divergic.Logging.Sentry NuGet packages add support this integration ([open-source on GitHub][6]). The design of the packages has several goals to achieve.

- Easy installation
- Sole dependency in "application" code is ```ILogger```
- Minimal bootstrapping required in "host" code
- Support custom exception properties
- Support logging context data
- Prevent duplicate reports
- Support Autofac
- Support special serialization (NodaTime)

## Installing

There are several NuGet packages to give flexibility around how applications can use this feature.

- ```Install-Package Divergic.Logging.Sentry``` [on NuGet.org][2] contains the ```ILogger``` provider for sending exceptions to Sentry
- ```Install-Package Divergic.Logging.Sentry.Autofac``` [on NuGet.org][3] contains a helper module to registering ```RavenClient``` in Autofac
- ```Install-Package Divergic.Logging.Sentry.NodaTime``` [on NuGet.org][4] contains an extension to the base package to support serialization of NodaTime data types
- ```Install-Package Divergic.Logging.Sentry.All``` [on NuGet.org][5] is a meta package that contains all the above packages

## Bootstrapping

Applications often have some kind of bootstrapping code when they start up which is often where logging is configured. The Divergic.Logging.Sentry package requires access to an ```ILoggerFactory``` instance to configure logging to Sentry. An ASP.Net core application for example will provide this in the Configure method. Adding the Divergic.Logging.Sentry package will give access to the ```AddSentry``` method on ```ILoggerFactory```.

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

Exceptions in .Net often contain additional information that is not included in the ```Exception.Message``` property. Some good examples of this are ```SqlException``` and the Azure ```StorageException```. This information is additional metadata about the error which is critical to identifying the error. Unfortunately most logging systems will only log the exception stacktrace and message. The result is an error report that is not actionable.

One of the advantages of ```RavenClient``` is that ```System.Exception.Data``` is serialized and sent in the error report to Sentry. What is missing is the ability to record any other custom exception properties. The Divergic.Logging.Sentry package caters for this by adding each custom exception property to the Data property so that it will be sent to Sentry.

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
   "culprit":"Divergic.Logging.Sentry.IntegrationTests.SentryLoggerTests+<RunFailure>d__5 in MoveNext",
   "message":"Exception of type 'Divergic.Logging.Sentry.IntegrationTests.CustomPropertyException' was thrown.",
   "metadata":{
      "type":"CustomPropertyException",
      "value":"Exception of type 'Divergic.Logging.Sentry.IntegrationTests.CustomPropertyException' was thrown."
   },
   "type":"error",
   "version":"7"
}
```

Capturing the custom properties on the exception for the Sentry report now allows the error report to have much richer information.

```json
{
   "culprit":"Divergic.Logging.Sentry.IntegrationTests.SentryLoggerTests+<RunFailure>d__5 in MoveNext",
   "message":"Exception of type 'Divergic.Logging.Sentry.IntegrationTests.CustomPropertyException' was thrown.",
   "extra":{
      "CustomPropertyException.Value":2309842,
      "CustomPropertyException.Company":"{\"Address\":\"839 Anniversary Drive\",\"Name\":\"87756871-2987-414b-a545-81ed564e327b\",\"Owner\":{\"Email\":\"glori.mcileen@ravenjs.com\",\"FirstName\":\"Glori\",\"LastName\":\"McIleen\"}}"
   },
   "metadata":{
      "type":"CustomPropertyException",
      "value":"Exception of type 'Divergic.Logging.Sentry.IntegrationTests.CustomPropertyException' was thrown."
   },
   "type":"error",
   "version":"7"
}
```

## Logging context data

A common issue when dealing with error reports is not having enough information to repond to the error. Logging just the exception information may not provide enough information.

Consider the following scenario.

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
   },
   "type":"error",
   "version":"7"
}
``` 

In this scenario, the message of the exception may be written for the purposes of sending the text to a user interface. That does not help in resolving the issue when the error is reported to Sentry. Perhaps we want to know what was being processed at the time. This can be achieved in a similar way to reporting custom exception properties as above by using ```LogErrorWithContext``` and ```LogCriticalWithContext``` extension methods.

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
   "message":"Failed to process complete transaction at the payment gateway.",
   "extra":{
      "ContextData": "{\"invoiceId\":\"239349asfd-234234\",\"amountInCents\":3995}",
   },
   "metadata":{
      "type":"PaymentGatewayException",
      "value":"Failed to process complete transaction at the payment gateway."
   },
   "type":"error",
   "version":"7"
}
``` 
The Sentry issue now contains the contextual information that can assist with troubleshooting the error.

## Preventing duplicate reports

Exceptions can be caught, logged and then thrown again. A catch block higher up the callstack may then log the same exception again. This scenario is handled by ensuring that any particular exception instance is only reported to Sentry once.

## Supporting Autofac

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

Sending custom exception properties and exception context data to Sentry is really useful, but only as useful as how the data can be understood. ```NodaTime.Instant``` is a good example of this. The native JSON serialization of an Instant value will be a number rather than a readable date/time value.

Consider the following exception.

```csharp
public class CustomPropertyException : Exception
{
	public Instant Point { get; set; }
}
```

Logging this exception to Sentry without NodaTime would send something like the following to Sentry.

```json
{
   "culprit":"Divergic.Logging.Sentry.IntegrationTests.SentryLoggerTests+<RunFailure>d__5 in MoveNext",
   "message":"Exception of type 'Divergic.Logging.Sentry.IntegrationTests.CustomPropertyException' was thrown.",
   "extra":{
      "CustomPropertyException.Point":"{}"
   },
   "metadata":{
      "type":"CustomPropertyException",
      "value":"Exception of type 'Divergic.Logging.Sentry.IntegrationTests.CustomPropertyException' was thrown."
   },
   "type":"error",
   "version":"7"
}
```

Registering the provider with ```ILoggerFactory``` using ```AddSentryWithNodaTime``` will configure correct JSON serialization of NodaTime types when sending errors to Sentry. Sentry would receive something like the following instead.

```json
{
   "culprit":"Divergic.Logging.Sentry.IntegrationTests.SentryLoggerTests+<RunFailure>d__5 in MoveNext",
   "message":"Exception of type 'Divergic.Logging.Sentry.IntegrationTests.CustomPropertyException' was thrown.",
   "extra":{
        "CustomPropertyException.Point": "\"2018-06-03T13:34:42.2533212Z\"",
   },
   "metadata":{
      "type":"CustomPropertyException",
      "value":"Exception of type 'Divergic.Logging.Sentry.IntegrationTests.CustomPropertyException' was thrown."
   },
   "type":"error",
   "version":"7"
}
```

[0]: https://raygun.com/
[1]: https://sentry.io/welcome/
[2]: https://www.nuget.org/packages/Divergic.Logging.Sentry
[3]: https://www.nuget.org/packages/Divergic.Logging.Sentry.Autofac
[4]: https://www.nuget.org/packages/Divergic.Logging.Sentry.NodaTime
[5]: https://www.nuget.org/packages/Divergic.Logging.Sentry.All
[6]: https://github.com/Divergic/Divergic.Logging.Sentry