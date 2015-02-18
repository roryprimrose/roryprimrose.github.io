---
title: Executing build tasks without a build server – Creating a custom ITask
categories : .Net, My Software
tags : Extensibility, MEF
date: 2011-07-07 15:28:00 +10:00
---

Following on from the [previous posts][0] in this series, this post will look at how to create a custom task that can then be executed via BTE.

This post will provide an example of how to create a task that will allow execution of multiple tasks from a single command line execution. The task itself is fairly meaningless as you can simply invoke BTE several times to get the same result. It does however provide a good example of how to use some of the services provided by BTE.

The first thing to do is create a new library project in Visual Studio. You will then need to add BuildTaskExecutor.exe as a reference. You will also need to add a reference to System.ComponentModel.Composition.dll to get access to MEF.![image][1]

The next step is to add a new class for the custom task. I have added a MultiTask class in this example and marked it as implementing the ITask interface. The class also needs to be decorated with the _[Export(typeof(ITask))]_ attribute so that it can be picked up by MEF.

Next thing to do is add support for importing BTE types via MEF. This can either be done using either constructor or property injection. Constructor injection requires that the constructor be decorated with the _[ImportingConstructor]_ attribute. Depending on the types being imported, constructor parameters may also need to define an Import attribute and possibly an import name. Property injection also requires the Import attribute and possibly an import name.

The MultiTask class will take the EventWriter service from BTE as a constructor import. It will also task the TaskExecutor service as a property import. The TaskExecutor cannot be imported in the constructor because it has a reference to TaskResolver which in turn as a reference too all loaded tasks. Having a task import TaskExecutor or TaskResolver in its constructor would cause a circular reference and MEF would throw a composition exception.

MultiTask defines the structure for the command line arguments so that the user can define multiple tasks and their arguments.

{% highlight csharp linenos %}
using System;
using System.Collections.Generic;
using System.Linq;
namespace Neovolve.BuildTaskExecutor.ThirdParty
{
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Text;
    using System.Text.RegularExpressions;
    using Neovolve.BuildTaskExecutor.Extensibility;
    using Neovolve.BuildTaskExecutor.Services;
    
    [Export(typeof(ITask))]
    public class MultiTask : ITask
    {
        private Regex _taskExpression = new Regex("/task\\d+:(?<taskName>.+)", RegexOptions.Singleline);
    
        [ImportingConstructor]
        public MultiTask(EventWriter writer)
        {
            Writer = writer;
        }
    
        public Boolean Execute(IEnumerable<String> arguments)
        {
            List<String> taskArguments = null;
    
            foreach (String argument in arguments)
            {
                Match taskNameMatch = _taskExpression.Match(argument);
    
                if (taskNameMatch.Success)
                {
                    // This is a new task, execute any arguments already calculated
                    if (InvokeTask(taskArguments) == false)
                    {
                        return false;
                    }
    
                    // Parse out the task name
                    String taskName = taskNameMatch.Groups["taskName"].Value;
    
                    taskArguments = new List<String>
                                    {
                                        taskName
                                    };
                }
                else if (taskArguments != null)
                {
                    taskArguments.Add(argument);   
                }
            }
                
            return InvokeTask(taskArguments);
        }
    
        private Boolean InvokeTask(List<String> taskArguments)
        {
            if (taskArguments == null)
            {
                return true;
            }
    
            String message = "Invoking";
    
            taskArguments.ForEach(x => message += " " + x);
    
            Writer.WriteMessage(TraceEventType.Verbose, message);
    
            return Executor.Execute(taskArguments);
        }
    
        public Boolean IsValidArgumentSet(IEnumerable<String> arguments)
        {
            if (arguments.Any() == false)
            {
                Writer.WriteMessage(TraceEventType.Verbose, "No command line arguments provided");
    
                return false;
            }
    
            String firstTask = arguments.First();
    
            if (_taskExpression.IsMatch(firstTask) == false)
            {
                Writer.WriteMessage(TraceEventType.Verbose, "The first argument is not in the form '/taskN:' where N is a number.");
    
                return false;
            }
    
            return true;
        }
    
        public String CommandLineArgumentHelp
        {
            get
            {
                StringBuilder builder = new StringBuilder("/task1:<task1Name> [<task1Args>] [/task2:<task2Name> [<task2Args>]] ... [/taskN:<taskNName> [<taskNArgs>]]");
    
                builder.AppendLine();
                builder.AppendLine();
                builder.AppendLine("/taskN:<taskNName>\tThe task number to execute.");
                builder.AppendLine("\t\t\tN should be sequential in the command line.");
                builder.AppendLine("<taskNArgs>\t\tThe command line arguments for the associated task.");
    
                return builder.ToString();
            }
        }
    
        public String Description
        {
            get
            {
                return "Executes multiple tasks.";
            }
        }
    
        public IEnumerable<String> Names
        {
            get
            {
                return new[]
                        {
                            "MultiTask", "mt"
                        };
            }
        }
    
        [Import]
        private TaskExecutor Executor
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

The assembly that contains this custom task needs to reside in the same directory as BuildTaskExecutor.exe for MEF to resolve the task. Running BTE with the generic help task will prove that this custom task is being picked up by MEF.![image][2]

We can then check that the task is able validate command line arguments correctly. We will turn on verbose event writing to see a bit more detail here.![image][3]

You will notice here that the help task is rendering the command line help for the custom task given that the command line arguments have failed validation.

We can also provide an invalid argument to the task to test the other validation logic of the task.![image][4]

We can now get the task to successfully execute multiple tasks. In this case the command is to output the help for the wov and tfsedit tasks.![image][5]

One final action to check is that the second task is not invoked if the first task fails.![image][6]

You can see here that creating a custom ITask implementation to be invoked by BTE is really easy.

It is now up to you to create your own tasks for any actions that you require in your own solutions. If you like, you could even contribute your tasks to the [BTE project][7] out on Codeplex.

[0]: /post/2011/07/06/Executing-build-tasks-without-a-build-server-%E2%80%93-Example-scenario.aspx
[1]: //blogfiles/image_115.png
[2]: //blogfiles/image_116.png
[3]: //blogfiles/image_117.png
[4]: //blogfiles/image_118.png
[5]: //blogfiles/image_119.png
[6]: //blogfiles/image_120.png
[7]: http://neovolve.codeplex.com/SourceControl/changeset/view/80333#1583086
