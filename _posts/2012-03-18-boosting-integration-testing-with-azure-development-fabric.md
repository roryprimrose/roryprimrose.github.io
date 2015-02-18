---
title: Boosting integration testing with Azure development fabric
categories : .Net
tags : Azure
date: 2012-03-18 22:24:49 +10:00
---

I [posted previously][0] about manually spinning up Azure storage emulator in the development fabric so that it can be used with integration tests. Ever since then I have been using a vastly updated version of the code I previously published. 

This updated one might be helpful for others to leverage as well. This version allows for starting and stopping both the storage emulator and the compute emulator. It makes its best attempt at automatically finding the Azure project service directory and the service configuration for the current build configuration. If this does not work for your scenario, then you can also manually provide this information.

    namespace Neovolve.Toolkit.Azure
    {
        using System;
        using System.Collections.Generic;
        using System.Diagnostics;
        using System.Diagnostics.Contracts;
        using System.IO;
        using System.Linq;
        using Microsoft.VisualStudio.TestTools.UnitTesting;
    
        /// <summary&gt;
        /// The <see cref=&quot;AzureEmulator&quot;/&gt; class is used to provide functionality for starting and stopping Azure storage and compute emulators.
        /// </summary&gt;
        public static class AzureEmulator
        {
            /// <summary&gt;
            ///   Stores the CS run path.
            /// </summary&gt;
            public const String AzureRunPath = @&quot;C:\Program Files\Windows Azure Emulator\emulator\csrun.exe&quot;;
    
            /// <summary&gt;
            ///   Defines the default build type to search for.
            /// </summary&gt;
    #if DEBUG
            private const String DefaultBuildType = &quot;Debug&quot;;
    #else
            private const String DefaultBuildType = &quot;Release&quot;;
    #endif
    
            /// <summary&gt;
            /// Starts the compute.
            /// </summary&gt;
            public static void StartCompute()
            {
                StartCompute(null, null);
            }
    
            /// <summary&gt;
            /// Starts the compute.
            /// </summary&gt;
            /// <param name=&quot;serviceDirectory&quot;&gt;
            /// The service directory. 
            /// </param&gt;
            /// <param name=&quot;configurationPath&quot;&gt;
            /// The configuration path. 
            /// </param&gt;
            public static void StartCompute(String serviceDirectory, String configurationPath)
            {
                if (String.IsNullOrWhiteSpace(serviceDirectory))
                {
                    // Attempt to resolve the service directory using default searching parameters
                    serviceDirectory = FindServiceDirectory(null, null);
                }
    
                if (String.IsNullOrWhiteSpace(configurationPath))
                {
                    configurationPath = FindServiceConfiguration(null, null);
                }
    
                if (Directory.Exists(serviceDirectory) == false)
                {
                    throw new InvalidOperationException(&quot;Azure service directory does not exist.&quot;);
                }
    
                if (File.Exists(configurationPath) == false)
                {
                    throw new InvalidOperationException(&quot;Azure service configuration does not exist.&quot;);
                }
    
                String arguments = &quot;/run:\&quot;&quot; + serviceDirectory + &quot;\&quot;;\&quot;&quot; + configurationPath + &quot;\&quot;&quot;;
    
                Contract.Assume(String.IsNullOrWhiteSpace(arguments) == false);
    
                ExecuteAzureEmulator(arguments);
            }
    
            /// <summary&gt;
            /// Starts the storage.
            /// </summary&gt;
            public static void StartStorage()
            {
                const String Arguments = &quot;/devstore:start&quot;;
    
                ExecuteAzureEmulator(Arguments);
            }
    
            /// <summary&gt;
            /// Stops the compute.
            /// </summary&gt;
            public static void StopCompute()
            {
                const String Arguments = &quot;/devfabric:shutdown&quot;;
    
                ExecuteAzureEmulator(Arguments);
            }
    
            /// <summary&gt;
            /// Stops the storage.
            /// </summary&gt;
            public static void StopStorage()
            {
                const String Arguments = &quot;/devstore:shutdown&quot;;
    
                ExecuteAzureEmulator(Arguments);
            }
    
            /// <summary&gt;
            /// Finds the service configuration.
            /// </summary&gt;
            /// <param name=&quot;context&quot;&gt;
            /// The context. 
            /// </param&gt;
            /// <param name=&quot;buildType&quot;&gt;
            /// Type of the build. 
            /// </param&gt;
            /// <returns&gt;
            /// A <see cref=&quot;String&quot;/&gt; instance. 
            /// </returns&gt;
            private static String FindServiceConfiguration(this TestContext context, String buildType)
            {
                Contract.Ensures(Contract.Result<String&gt;() != null);
    
                return FindServiceConfiguration(context, buildType, String.Empty);
            }
    
            /// <summary&gt;
            /// Finds the service configuration.
            /// </summary&gt;
            /// <param name=&quot;context&quot;&gt;
            /// The context. 
            /// </param&gt;
            /// <param name=&quot;buildType&quot;&gt;
            /// Type of the build. 
            /// </param&gt;
            /// <param name=&quot;projectName&quot;&gt;
            /// Name of the project. 
            /// </param&gt;
            /// <returns&gt;
            /// A <see cref=&quot;String&quot;/&gt; instance. 
            /// </returns&gt;
            private static String FindServiceConfiguration(this TestContext context, String buildType, String projectName)
            {
                Contract.Ensures(Contract.Result<String&gt;() != null);
    
                if (String.IsNullOrWhiteSpace(buildType))
                {
                    buildType = DefaultBuildType;
                }
    
                String solutionDirectory = context.FindSolutionDirectory();
                String searchPattern = projectName + @&quot;\bin\&quot; + buildType + @&quot;\ServiceConfiguration.cscfg&quot;;
    
                IEnumerable<String&gt; enumerateFiles = Directory.EnumerateFiles(
                    solutionDirectory, &quot;*&quot;, SearchOption.AllDirectories);
    
                Contract.Assume(enumerateFiles != null);
    
                IEnumerable<String&gt; matchingFiles = from x in enumerateFiles
                                                    where x.EndsWith(searchPattern, StringComparison.OrdinalIgnoreCase)
                                                    select x;
    
                if (matchingFiles == null)
                {
                    throw new InvalidOperationException(&quot;Failed to find any service configuration files.&quot;);
                }
    
                List<String&gt; serviceConfigurations = matchingFiles.ToList();
    
                if (serviceConfigurations.Count == 0)
                {
                    throw new InvalidOperationException(&quot;No service configuration was found.&quot;);
                }
    
                if (serviceConfigurations.Count &gt; 1)
                {
                    throw new InvalidOperationException(&quot;Multiple service configurations were found.&quot;);
                }
    
                Contract.Assume(String.IsNullOrWhiteSpace(serviceConfigurations[0]) == false);
    
                return serviceConfigurations[0];
            }
    
            /// <summary&gt;
            /// Finds the service directory.
            /// </summary&gt;
            /// <param name=&quot;context&quot;&gt;
            /// The context. 
            /// </param&gt;
            /// <param name=&quot;buildType&quot;&gt;
            /// Type of the build. 
            /// </param&gt;
            /// <returns&gt;
            /// A <see cref=&quot;String&quot;/&gt; instance. 
            /// </returns&gt;
            private static String FindServiceDirectory(this TestContext context, String buildType)
            {
                Contract.Ensures(Contract.Result<String&gt;() != null);
    
                return FindServiceDirectory(context, buildType, String.Empty);
            }
    
            /// <summary&gt;
            /// Finds the service directory.
            /// </summary&gt;
            /// <param name=&quot;context&quot;&gt;
            /// The context. 
            /// </param&gt;
            /// <param name=&quot;buildType&quot;&gt;
            /// Type of the build. 
            /// </param&gt;
            /// <param name=&quot;projectName&quot;&gt;
            /// Name of the project. 
            /// </param&gt;
            /// <returns&gt;
            /// A <see cref=&quot;String&quot;/&gt; instance. 
            /// </returns&gt;
            private static String FindServiceDirectory(this TestContext context, String buildType, String projectName)
            {
                Contract.Ensures(Contract.Result<String&gt;() != null);
    
                if (String.IsNullOrWhiteSpace(buildType))
                {
                    buildType = DefaultBuildType;
                }
    
                String solutionDirectory = context.FindSolutionDirectory();
                String searchPattern = projectName + @&quot;\csx\&quot; + buildType;
    
                IEnumerable<String&gt; enumerateDirectories = Directory.EnumerateDirectories(
                    solutionDirectory, &quot;*&quot;, SearchOption.AllDirectories);
    
                Contract.Assume(enumerateDirectories != null);
    
                IEnumerable<String&gt; matchingDirectories = from x in enumerateDirectories
                                                          where
                                                              x.EndsWith(searchPattern, StringComparison.OrdinalIgnoreCase)
                                                          select x;
    
                if (matchingDirectories == null)
                {
                    throw new InvalidOperationException(&quot;No failed to identify any service directories.&quot;);
                }
    
                List<String&gt; serviceDirectories = matchingDirectories.ToList();
    
                if (serviceDirectories.Count == 0)
                {
                    throw new InvalidOperationException(&quot;No service directory was found.&quot;);
                }
    
                if (serviceDirectories.Count &gt; 1)
                {
                    throw new InvalidOperationException(&quot;Multiple service directories were found.&quot;);
                }
    
                Contract.Assume(String.IsNullOrWhiteSpace(serviceDirectories[0]) == false);
    
                return serviceDirectories[0];
            }
    
            /// <summary&gt;
            /// Executes the azure emulator.
            /// </summary&gt;
            /// <param name=&quot;arguments&quot;&gt;
            /// The arguments. 
            /// </param&gt;
            private static void ExecuteAzureEmulator(String arguments)
            {
                Contract.Requires<ArgumentNullException&gt;(String.IsNullOrWhiteSpace(arguments) == false);
    
                ProcessStartInfo processStartInfo = new ProcessStartInfo
                                                    {
                                                        FileName = AzureRunPath, 
                                                        Arguments = arguments, 
                                                        RedirectStandardOutput = true, 
                                                        UseShellExecute = false, 
                                                        CreateNoWindow = true, 
                                                        WindowStyle = ProcessWindowStyle.Hidden
                                                    };
    
                using (Process process = Process.Start(processStartInfo))
                {
                    process.WaitForExit();
    
                    using (StreamReader reader = process.StandardOutput)
                    {
                        Trace.WriteLine(reader.ReadToEnd());
                    }
                }
            }
        }
    }{% endhighlight %}

This makes use of an extension method on the TestContext class.

    namespace Neovolve.Toolkit.Azure
    {
        using System;
        using System.Collections.Generic;
        using System.Diagnostics.Contracts;
        using System.IO;
        using System.Linq;
        using System.Reflection;
        using Microsoft.VisualStudio.TestTools.UnitTesting;
    
        /// <summary&gt;
        /// The <see cref=&quot;TestContextExtensions&quot;/&gt; class is used to provide extension methods to the <see cref=&quot;TestContext&quot;/&gt; class.
        /// </summary&gt;
        public static class TestContextExtensions
        {
            /// <summary&gt;
            /// Finds the solution directory.
            /// </summary&gt;
            /// <param name=&quot;context&quot;&gt;
            /// The context. 
            /// </param&gt;
            /// <returns&gt;
            /// A <see cref=&quot;String&quot;/&gt; instance. 
            /// </returns&gt;
            public static String FindSolutionDirectory(this TestContext context)
            {
                Contract.Ensures(String.IsNullOrWhiteSpace(Contract.Result<String&gt;()) == false);
    
                String startPath;
    
                if (context != null)
                {
                    startPath = context.TestDir;
                }
                else
                {
                    startPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                }
    
                if (String.IsNullOrWhiteSpace(startPath))
                {
                    throw new InvalidOperationException(
                        &quot;No reference point was determined in order to search for the solution directory.&quot;);
                }
    
                DirectoryInfo directory = new DirectoryInfo(startPath);
    
                while (directory.Exists)
                {
                    IEnumerable<FileInfo&gt; solutionFiles = directory.EnumerateFiles(&quot;*.sln&quot;, SearchOption.TopDirectoryOnly);
    
                    Contract.Assume(solutionFiles != null);
    
                    if (solutionFiles.Any())
                    {
                        Contract.Assume(String.IsNullOrWhiteSpace(directory.FullName) == false);
    
                        // We have found the first parent directory that a the solution file
                        return directory.FullName;
                    }
    
                    if (directory.Parent == null)
                    {
                        throw new InvalidOperationException(&quot;Failed to identify the solution directory.&quot;);
                    }
    
                    directory = directory.Parent;
                }
    
                throw new InvalidOperationException(&quot;Failed to identify the solution directory.&quot;);
            }
        }
    }{% endhighlight %}

Next up, how to provide a similar implementation for IISExpress.

[0]: /post/2012/01/12/Integration-testing-with-Azure-development-storage.aspx
