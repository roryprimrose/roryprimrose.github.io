---
title: Executing build tasks without a build server – Implementation
categories : Personal
tags : Extensibility, MEF, TFS, WiX
date: 2011-07-03 14:40:05 +10:00
---

[My previous post][0] outlines my design requirements for a set of build task actions. I want to execute these tasks for my personal projects that do not have the backing of a fully functional TFS deployment. This post will look at how to implement these requirements using a console application.

Neovolve.BuildTaskExecutor (or BTE) is the application that will execute specific tasks based on command line parameters. It implements MEF for the extensibility support so that additional tasks can be added at any time with zero impact on the application itself. 

**Extensibility**

The main interface for BTE extensibility is the ITask interface. It provides the ability for BTE to identify the command line names associated with the task, validate command line arguments, obtain help information about the task and to execute the task.

{% highlight csharp %}
namespace Neovolve.BuildTaskExecutor.Extensibility
{
    using System;
    using System.Collections.Generic;
    
    public interface ITask
    {
        Boolean Execute(IEnumerable<String> arguments);
    
        Boolean IsValidArgumentSet(IEnumerable<String> arguments);
    
        String CommandLineArgumentHelp
        {
            get;
        }
    
        String Description
        {
            get;
        }
    
        IEnumerable<String> Names
        {
            get;
        }
    }
}
{% endhighlight %}

The IEventWriter interface supports writing event messages. BTE provides an implementation of this interface that writes messages to the console. Custom implementations can be provided to output event messages to other locations.

{% highlight csharp %}
namespace Neovolve.BuildTaskExecutor.Extensibility
{
    using System;
    using System.Diagnostics;
    
    public interface IEventWriter
    {
        void WriteMessage(TraceEventType eventType, String message, params Object[] arguments);
    }
}
{% endhighlight %}

The IVersionManager interface provides the ability to read and write version information from a file path. BTE provides three implementations of this class. They manage version information for C# AssemblyInfo.cs style files, Wix projects and binary files. These three implementations can be added to custom tasks by using name specified imports such as _[Import(VersionManagerExport.Wix)] IVersionManager versionAction_.

{% highlight csharp %}
namespace Neovolve.BuildTaskExecutor.Extensibility
{
    using System;
    
    public interface IVersionManager
    {
        Version ReadVersion(String filePath);
    
        void WriteVersion(String filePath, Version newVersion);
    }
}
{% endhighlight %}

**Services**

BTE provides several service classes that execute tasks and provide additional service support to tasks. 

The TaskExecutor and the TaskResolver classes are the core of the application. They identify which task to execute based on the first command line argument and then execute the resolved task with the remaining command line arguments.

These classes are listed below with their method bodies removed for brevity.

{% highlight csharp %}
namespace Neovolve.BuildTaskExecutor.Services
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Linq;
    using Neovolve.BuildTaskExecutor.Extensibility;
    using Neovolve.BuildTaskExecutor.Properties;
    
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class TaskResolver
    {
        [ImportingConstructor]
        public TaskResolver(EventWriter writer)
        {
        }
    
        public ITask ResolveTask(String taskName)
        {
        }
    
        [ImportMany]
        public IEnumerable<ITask> Tasks
        {
            get;
            private set;
        }
    
        private EventWriter Writer
        {
            get;
            set;
        }
    }
}
{% endhighlight %}

{% highlight csharp %}
namespace Neovolve.BuildTaskExecutor.Services
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Linq;
    using Neovolve.BuildTaskExecutor.Extensibility;
    using Neovolve.BuildTaskExecutor.Properties;
    using Neovolve.BuildTaskExecutor.Tasks;
    
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class TaskExecutor
    {
        [ImportingConstructor]
        public TaskExecutor(EventWriter writer)
        {
        }
    
        public Boolean Execute(IEnumerable<String> arguments)
        {
        }
    
        [Import]
        private TaskResolver Resolver
        {
            get;
            set;
        }
    
        private EventWriter Writer
        {
            get;
            set;
        }
    }
}
{% endhighlight %}

The EventWriter class is another service available in BTE. It wraps all the available IEventWriter implementations for an easy way to write event messages. It also manages the logic around the event writing level that can be configured on the command line.

{% highlight csharp %}
namespace Neovolve.BuildTaskExecutor.Services
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Linq;
    using Neovolve.BuildTaskExecutor.Extensibility;
    
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class EventWriter
    {
        [ImportingConstructor]
        public EventWriter([ImportMany] IEventWriter[] eventWriters, TraceEventType eventLevel)
        {
            if (eventWriters == null)
            {
                throw new ArgumentNullException("eventWriters");
            }
    
            EventWriters = eventWriters.ToList();
            EventLevel = eventLevel;
        }
    
        public void WriteMessage(TraceEventType eventType, String message, params Object[] arguments)
        {
            if (eventType > EventLevel)
            {
                return;
            }
    
            EventWriters.ForEach(x => x.WriteMessage(eventType, message, arguments));
        }
    
        public TraceEventType EventLevel
        {
            get;
            private set;
        }
    
        private List<IEventWriter> EventWriters
        {
            get;
            set;
        }
    }
}
{% endhighlight %}

**Task Execution**

Finally there is the Program class that is the entry point for BTE. It resolves the TaskExector from an internal ServiceManager and starts processing the command line arguments that are also resolved via MEF.

{% highlight csharp %}
namespace Neovolve.BuildTaskExecutor
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Neovolve.BuildTaskExecutor.Properties;
    using Neovolve.BuildTaskExecutor.Services;
    
    internal class Program
    {
        private static Int32 Main()
        {
            using (ServiceManager manager = new ServiceManager())
            {
                Lazy<EventWriter> writerService = manager.GetService<EventWriter>();
                Lazy<TaskExecutor> executorService = manager.GetService<TaskExecutor>();
    
                try
                {
                    EventWriter writer = writerService.Value;
    
                    if (writer == null)
                    {
                        throw new InvalidOperationException("Failed to resolve writer");
                    }
    
                    WriteApplicationInfo(writer);
    
                    TaskExecutor taskExecutor = executorService.Value;
    
                    if (taskExecutor == null)
                    {
                        throw new InvalidOperationException("Failed to resolve executor");
                    }
    
                    Lazy<IEnumerable<String>> arguments = manager.GetService<IEnumerable<String>>();
    
                    Boolean success = taskExecutor.Execute(arguments.Value);
    
                    if (success)
                    {
                        return 0;
                    }
    
                    return 1;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
    
                    return 1;
                }
                finally
                {
                    manager.ReleaseService(writerService);
                    manager.ReleaseService(executorService);
    
#if DEBUG
                    Console.ReadKey();
#endif
                }
            }
        }
    
        private static void WriteApplicationInfo(EventWriter writer)
        {
            String assemblyPath = typeof(Program).Assembly.Location;
            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(assemblyPath);
    
            writer.WriteMessage(TraceEventType.Information, Resources.Executor_ApplicationInformation, versionInfo.ProductVersion);
            writer.WriteMessage(TraceEventType.Information, String.Empty);
        }
    }
}
{% endhighlight %}

This post has outlined the core implementation of BTE. The core of BTE and the tasks already in the application satisfy all the design requirements from the previous post. The next post will look at how the application looks when it is invoked on the command line.

[0]: /2011/07/01/executing-build-tasks-without-a-build-server-design/