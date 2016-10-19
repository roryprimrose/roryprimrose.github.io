---
title: LogLevel NAnt Task
categories: .Net, Applications
tags: NAnt
date: 2008-01-16 15:53:27 +10:00
---

An ongoing issue that I am having with NAnt scripts is bloat in the log records being written to the screen and to disk. Tasks like [xmlpeek][0] and [regex][1] log messages that I am not interested in for the given logging threshold. It would be nice to not have these unwanted messages logged but for everything else to be logged as expected given the defined log threshold.

After doing some searching, I came across [this post][2] from [Jay Flowers][3]. Will Buttitta posted another version of the same idea in a comment against that post. Using tasks to change the logging threshold at runtime for a particular part of the script is a great idea. Unfortunately, I have found that neither of these implementations work.

After going through Reflector, I have found that classes that are derived from Task and call the Log method end up invoking Element.Log() which calls straight down to Project.Log(). The Project.Log method does not check the logging threshold before writing the log entry. What I haven't been able to figure out is why Task.Log is not invoked. If it was, then I think the two solutions in Jay's post would probably work as Task.Log() checks the current logging threshold before calling down to the base class.

<!--more-->

There is however another way that will correctly modify the logging threshold at a much lower level. 

Project.Log() invokes OnMessageLogged(). After searching for what is hooked up to the MessageLogged event, I found that a default listener is created and hooked up via Project.CreateDefaultLogger(). That pointed me to look at the Project.BuildListeners property and the IBuildLogger interface. The IBuildLogger has its own Threshold property.

This opens up a very simple solution. Regardless of how logging is invoked higher up the call chain, this implementation will always correctly define the log threshold at runtime because it is done directly on the loggers. This is of course assuming the logger implementation checks the threshold assigned to it.

```csharp
using NAnt.Core;
using NAnt.Core.Attributes;
     
namespace Neovolve.NAnt.Tasks
{
    /// <summary>
    /// The <see cref="LogLevelTask"/>
    /// class is a NAnt task that is used to determine the logging level used to execute
    /// a set of child tasks.
    /// </summary>
    /// <remarks>This code was inspired from the blog post found at http://jayflowers.com/WordPress/?p=133</remarks>
    [TaskName("loglevel")]
    public class LogLevelTask : TaskContainer
    {
        #region Declarations
     
        /// <summary>
        /// Stores the LogLevel value.
        /// </summary>
        private Level _logLevel;
     
        #endregion
     
        #region Methods
     
        /// <summary>
        /// Executes the task.
        /// </summary>
        protected override void ExecuteTask()
        {
            Level OldLevel = Project.Threshold;
            AssignLogLevel(LogLevel);
    
            base.ExecuteTask();
    
            AssignLogLevel(OldLevel);
        }
     
        /// <summary>
        /// Assigns the log level.
        /// </summary>
        /// <param name="newLevel">The new level.</param>
        private void AssignLogLevel(Level newLevel)
        {
            // Loop through each logger
            foreach (IBuildListener listener in Project.BuildListeners)
            {
                IBuildLogger logger = listener as IBuildLogger;
     
                // Assign the new threshold
                if (logger != null)
                {
                    logger.Threshold = newLevel;
                }
            }
        }
     
        #endregion
     
        #region Properties
     
        /// <summary>
        /// Gets or sets the log level.
        /// </summary>
        /// <value>The log level.</value>
        [TaskAttribute("level", Required = true)]
        public Level LogLevel
        {
            get
            {
                return _logLevel;
            }
            set
            {
                _logLevel = value;
            }
        }
     
        #endregion
    }
}    
```

To change the logging level, add the loglevel task around other tasks and set the level value. For example:

```xml
<loglevel level="None">
     
    <!-- Determine whether the template is a project or item related template -->
    <foreach item="Line"
            in="${Path.ProjectFileExpressions}"
            delim=","
            property="pathExpression">
     
        <if test="${ProjectTemplateFile == ''}">
            <regex pattern="${pathExpression}"
                input="${childFile}"
                options="IgnoreCase"
                failonerror="false" />
        </if>
    </foreach>
</loglevel>
```

[0]: http://nant.sourceforge.net/release/0.85-rc1/help/tasks/xmlpeek.html
[1]: http://nant.sourceforge.net/nightly/latest/help/tasks/regex.html
[2]: http://jayflowers.com/WordPress/?p=133
[3]: http://jayflowers.com/
