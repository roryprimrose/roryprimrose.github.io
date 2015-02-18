---
title: Boosting integration testing with Azure development fabric
categories : .Net
tags : Azure
date: 2012-03-18 22:24:49 +10:00
---

I [posted previously][0] about manually spinning up Azure storage emulator in the development fabric so that it can be used with integration tests. Ever since then I have been using a vastly updated version of the code I previously published. 

This updated one might be helpful for others to leverage as well. This version allows for starting and stopping both the storage emulator and the compute emulator. It makes its best attempt at automatically finding the Azure project service directory and the service configuration for the current build configuration. If this does not work for your scenario, then you can also manually provide this information.{% highlight csharp linenos %}
namespace Neovolve.Toolkit.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    
    /// <summary>
    /// The <see cref="AzureEmulator"/> class is used to provide functionality for starting and stopping Azure storage and compute emulators.
    /// </summary>
    public static class AzureEmulator
    {
        /// <summary>
        ///   Stores the CS run path.
        /// </summary>
        public const String AzureRunPath = @"C:\Program Files\Windows Azure Emulator\emulator\csrun.exe";
    
        /// <summary>
        ///   Defines the default build type to search for.
        /// </summary>
#if DEBUG
        private const String DefaultBuildType = "Debug";
#else
        private const String DefaultBuildType = "Release";
#endif
    
        /// <summary>
        /// Starts the compute.
        /// </summary>
        public static void StartCompute()
        {
            StartCompute(null, null);
        }
    
        /// <summary>
        /// Starts the compute.
        /// </summary>
        /// <param name="serviceDirectory">
        /// The service directory. 
        /// </param>
        /// <param name="configurationPath">
        /// The configuration path. 
        /// </param>
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
                throw new InvalidOperationException("Azure service directory does not exist.");
            }
    
            if (File.Exists(configurationPath) == false)
            {
                throw new InvalidOperationException("Azure service configuration does not exist.");
            }
    
            String arguments = "/run:\"" + serviceDirectory + "\";\"" + configurationPath + "\"";
    
            Contract.Assume(String.IsNullOrWhiteSpace(arguments) == false);
    
            ExecuteAzureEmulator(arguments);
        }
    
        /// <summary>
        /// Starts the storage.
        /// </summary>
        public static void StartStorage()
        {
            const String Arguments = "/devstore:start";
    
            ExecuteAzureEmulator(Arguments);
        }
    
        /// <summary>
        /// Stops the compute.
        /// </summary>
        public static void StopCompute()
        {
            const String Arguments = "/devfabric:shutdown";
    
            ExecuteAzureEmulator(Arguments);
        }
    
        /// <summary>
        /// Stops the storage.
        /// </summary>
        public static void StopStorage()
        {
            const String Arguments = "/devstore:shutdown";
    
            ExecuteAzureEmulator(Arguments);
        }
    
        /// <summary>
        /// Finds the service configuration.
        /// </summary>
        /// <param name="context">
        /// The context. 
        /// </param>
        /// <param name="buildType">
        /// Type of the build. 
        /// </param>
        /// <returns>
        /// A <see cref="String"/> instance. 
        /// </returns>
        private static String FindServiceConfiguration(this TestContext context, String buildType)
        {
            Contract.Ensures(Contract.Result<String>() != null);
    
            return FindServiceConfiguration(context, buildType, String.Empty);
        }
    
        /// <summary>
        /// Finds the service configuration.
        /// </summary>
        /// <param name="context">
        /// The context. 
        /// </param>
        /// <param name="buildType">
        /// Type of the build. 
        /// </param>
        /// <param name="projectName">
        /// Name of the project. 
        /// </param>
        /// <returns>
        /// A <see cref="String"/> instance. 
        /// </returns>
        private static String FindServiceConfiguration(this TestContext context, String buildType, String projectName)
        {
            Contract.Ensures(Contract.Result<String>() != null);
    
            if (String.IsNullOrWhiteSpace(buildType))
            {
                buildType = DefaultBuildType;
            }
    
            String solutionDirectory = context.FindSolutionDirectory();
            String searchPattern = projectName + @"\bin\" + buildType + @"\ServiceConfiguration.cscfg";
    
            IEnumerable<String> enumerateFiles = Directory.EnumerateFiles(
                solutionDirectory, "*", SearchOption.AllDirectories);
    
            Contract.Assume(enumerateFiles != null);
    
            IEnumerable<String> matchingFiles = from x in enumerateFiles
                                                where x.EndsWith(searchPattern, StringComparison.OrdinalIgnoreCase)
                                                select x;
    
            if (matchingFiles == null)
            {
                throw new InvalidOperationException("Failed to find any service configuration files.");
            }
    
            List<String> serviceConfigurations = matchingFiles.ToList();
    
            if (serviceConfigurations.Count == 0)
            {
                throw new InvalidOperationException("No service configuration was found.");
            }
    
            if (serviceConfigurations.Count > 1)
            {
                throw new InvalidOperationException("Multiple service configurations were found.");
            }
    
            Contract.Assume(String.IsNullOrWhiteSpace(serviceConfigurations[0]) == false);
    
            return serviceConfigurations[0];
        }
    
        /// <summary>
        /// Finds the service directory.
        /// </summary>
        /// <param name="context">
        /// The context. 
        /// </param>
        /// <param name="buildType">
        /// Type of the build. 
        /// </param>
        /// <returns>
        /// A <see cref="String"/> instance. 
        /// </returns>
        private static String FindServiceDirectory(this TestContext context, String buildType)
        {
            Contract.Ensures(Contract.Result<String>() != null);
    
            return FindServiceDirectory(context, buildType, String.Empty);
        }
    
        /// <summary>
        /// Finds the service directory.
        /// </summary>
        /// <param name="context">
        /// The context. 
        /// </param>
        /// <param name="buildType">
        /// Type of the build. 
        /// </param>
        /// <param name="projectName">
        /// Name of the project. 
        /// </param>
        /// <returns>
        /// A <see cref="String"/> instance. 
        /// </returns>
        private static String FindServiceDirectory(this TestContext context, String buildType, String projectName)
        {
            Contract.Ensures(Contract.Result<String>() != null);
    
            if (String.IsNullOrWhiteSpace(buildType))
            {
                buildType = DefaultBuildType;
            }
    
            String solutionDirectory = context.FindSolutionDirectory();
            String searchPattern = projectName + @"\csx\" + buildType;
    
            IEnumerable<String> enumerateDirectories = Directory.EnumerateDirectories(
                solutionDirectory, "*", SearchOption.AllDirectories);
    
            Contract.Assume(enumerateDirectories != null);
    
            IEnumerable<String> matchingDirectories = from x in enumerateDirectories
                                                        where
                                                            x.EndsWith(searchPattern, StringComparison.OrdinalIgnoreCase)
                                                        select x;
    
            if (matchingDirectories == null)
            {
                throw new InvalidOperationException("No failed to identify any service directories.");
            }
    
            List<String> serviceDirectories = matchingDirectories.ToList();
    
            if (serviceDirectories.Count == 0)
            {
                throw new InvalidOperationException("No service directory was found.");
            }
    
            if (serviceDirectories.Count > 1)
            {
                throw new InvalidOperationException("Multiple service directories were found.");
            }
    
            Contract.Assume(String.IsNullOrWhiteSpace(serviceDirectories[0]) == false);
    
            return serviceDirectories[0];
        }
    
        /// <summary>
        /// Executes the azure emulator.
        /// </summary>
        /// <param name="arguments">
        /// The arguments. 
        /// </param>
        private static void ExecuteAzureEmulator(String arguments)
        {
            Contract.Requires<ArgumentNullException>(String.IsNullOrWhiteSpace(arguments) == false);
    
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
}
{% endhighlight %}

This makes use of an extension method on the TestContext class.{% highlight csharp linenos %}
namespace Neovolve.Toolkit.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    
    /// <summary>
    /// The <see cref="TestContextExtensions"/> class is used to provide extension methods to the <see cref="TestContext"/> class.
    /// </summary>
    public static class TestContextExtensions
    {
        /// <summary>
        /// Finds the solution directory.
        /// </summary>
        /// <param name="context">
        /// The context. 
        /// </param>
        /// <returns>
        /// A <see cref="String"/> instance. 
        /// </returns>
        public static String FindSolutionDirectory(this TestContext context)
        {
            Contract.Ensures(String.IsNullOrWhiteSpace(Contract.Result<String>()) == false);
    
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
                    "No reference point was determined in order to search for the solution directory.");
            }
    
            DirectoryInfo directory = new DirectoryInfo(startPath);
    
            while (directory.Exists)
            {
                IEnumerable<FileInfo> solutionFiles = directory.EnumerateFiles("*.sln", SearchOption.TopDirectoryOnly);
    
                Contract.Assume(solutionFiles != null);
    
                if (solutionFiles.Any())
                {
                    Contract.Assume(String.IsNullOrWhiteSpace(directory.FullName) == false);
    
                    // We have found the first parent directory that a the solution file
                    return directory.FullName;
                }
    
                if (directory.Parent == null)
                {
                    throw new InvalidOperationException("Failed to identify the solution directory.");
                }
    
                directory = directory.Parent;
            }
    
            throw new InvalidOperationException("Failed to identify the solution directory.");
        }
    }
}
{% endhighlight %}

Next up, how to provide a similar implementation for IISExpress.

[0]: /post/2012/01/12/Integration-testing-with-Azure-development-storage.aspx
