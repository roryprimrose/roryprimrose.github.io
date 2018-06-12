---
title: Configuration Autofac Module
categories: .Net
tags: 
date: 2018-06-12 15:30:00 +10:00
---

I'm quite a fan of the JSON configuration support that is available in ASP.Net core. Even better is that using it is not restricted to just ASP.Net and I use it on all my projects. One of the big benefits of this new configuration system is being able to represent configuration as a hierarchy of information rather than a flat list of key value pairs.

I use a lot of dependency injection and it is very common that components in a system require some kind of configuration. My preference is to define an interface for that configuration and then represent that configuration in a JSON settings file. 

This post looks at a little Autofac module that can help with loading configuration data into a set of classes and register each of them in an Autofac container.

<!--more-->
## Installation

The [Divergic.Configuration.Autofac](https://www.nuget.org/packages/Divergic.Configuration.Autofac) NuGet package provides an Autofac module for registering nested configuration types. The package can be installed from NuGet using ```Install-Package Divergic.Configuration.Autofac```.

## Scenario

I use the structure of JSON settings data to identify related configuration values. These are normally grouped to a component that requires that configuration.

Consider the following JSON configuration.

```json
{
  "Storage": {
    "Database": "database connection string",
    "BlobStorage": "blob storage connection",
    "TableStorage": "table storage connection"
  },
  "FirstJob": {
    "Name": "My job",
    "TriggerInSeconds": 60
  }
}
```

Each component that requires the configuration would have a constructor parameter of an interface that represents the configuration. 

```csharp
public interface IStorage
{
    string BlobStorage { get; }
    string Database { get; }
    string TableStorage { get; }
}

public interface IFirstJob
{
    string Name { get; }
    TimeSpan Trigger { get; }
}
```

The interfaces only define get accessors because the configuration should be immutable for any component that consumes it. You may also notice a mismatch between the JSON data and the ```IFirstJob.Trigger``` interface definition above. I like that the interface can remain clean (a ```TimeSpan``` in this case) while the configuration glue can provide a translation from the source configuration value.

```csharp
public class Storage : IStorage
{
    public string BlobStorage { get; set; }
    public string Database { get; set; }
    public string TableStorage { get; set; }
}

public class FirstJob : IFirstJob
{
    public string Name { get; set; }
    public TimeSpan Trigger => TimeSpan.FromSeconds(TriggerInSeconds);
    public int TriggerInSeconds { get; set; }
}
```

The ```FirstJob``` class implements ```IFirstJob``` and provides the conversion of the ```TriggerInSeconds``` value loaded from JSON data to the ```Trigger``` property that the interface defines. This works because loading the JSON data into a class maps the JSON data directly to the class properties.

## Configuration Resolution

The module relies on an ```IConfigurationResolver``` to load the root configuration class. This class must provide a structure that maps to the JSON data.

```csharp
public interface IConfig
{
    FirstJob FirstJob { get; }
    Storage Storage { get; }
}

public class Config : IConfig
{
    public FirstJob FirstJob { get; set; }
    public Storage Storage { get; set; }
}

```

The module will recursively register all properties found on the root configuration using ```AsSelf``` as well as ```AsImplementedInterfaces``` where implemented interfaces are found.

## Json Resolution

The provided ```JsonResolver<T>``` resolver will load the root configuration (of type ```T```) from an appsettings.json file by default. This can be used by ```ConfigurationModule``` like the following.

```csharp
var builder = new ContainerBuilder();

builder.RegisterModule<ConfigurationModule<JsonResolver<Config>>>();

var container = builder.Build();
```

The ```JsonResolver``` class can accept a different filename if the default appsettings.json is not suitable.

```csharp
var builder = new ContainerBuilder();
var resolver = new JsonResolver<Config>("hostsettings.json");
var module = new ConfigurationModule(resolver);

builder.RegisterModule(module);

var container = builder.Build();
```

Need environment configuration override support? This would work in ASP.Net core.

```csharp
var env = builderContext.HostingEnvironment;
var builder = new ContainerBuilder();

var resolver = new EnvironmentJsonResolver<Config>("appsettings.json", $"appsettings.{env.EnvironmentName}.json");
var module = new ConfigurationModule(resolver);

builder.RegisterModule(module);

var container = builder.Build();
```

## Custom Resolution

Need to resolve a root configuration object from something other than a JSON file? The ```ConfigurationModule``` will accept any ```IConfigurationResolver```. You can write any custom resolver and provide it to the module.

## Outcome

The module will register each of the nested properties on the resolved root configuration class with the Autofac container. Given the scenario above, the following resolutions on the container will be valid.

```csharp
var builder = new ContainerBuilder();

builder.RegisterModule<ConfigurationModule<JsonResolver<Config>>>();

var container = builder.Build();

container.Resolve<IConfig>();
container.Resolve<Config>();
container.Resolve<IStorage>();
container.Resolve<Storage>();
container.Resolve<IFirstJob>();
container.Resolve<FirstJob>();
```

Each of these resolutions can now participate in dependency injection for any other class.