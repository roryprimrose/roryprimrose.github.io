---
title: Integration testing with Azure development fabric and IISExpress
categories : .Net
tags : Azure
date: 2012-03-18 22:39:46 +10:00
---

This post puts together the code posted in the previous two posts ([here][0] and [here][1]).

The following code is what I am using the spin up the Azure storage emulator, Azure compute emulator and IISExpress so that I can run my system through its integration tests.

    namespace MySystem.Server.Web.IntegrationTests
    {
        using System;
        using System.IO;
        using System.Threading.Tasks;
        using Microsoft.VisualStudio.TestTools.UnitTesting;
        using Neovolve.Toolkit.Azure;
        using Neovolve.Toolkit.TestSupport;
        using WatiN.Core;
    
        /// <summary&gt;
        /// The <see cref=&quot;Initialization&quot;/&gt; class is used to run assembly initialization work for the test assembly.
        /// </summary&gt;
        [TestClass]
        public static class Initialization
        {
            /// <summary&gt;
            ///   Stores the IIS Express reference entity.
            /// </summary&gt;
            private static IisExpress _iisExpress;
    
            #region Setup/Teardown
    
            /// <summary&gt;
            /// Cleans up after running the unit tests in an assembly.
            /// </summary&gt;
            [AssemblyCleanup]
            public static void AssemblyCleanup()
            {
                Task iisExpressTask = Task.Factory.StartNew(
                    () =&gt;
                        {
                            _iisExpress.Dispose();
                            _iisExpress = null;
                        });
    
                Task storageTask = Task.Factory.StartNew(AzureEmulator.StopStorage);
                Task computeTask = Task.Factory.StartNew(AzureEmulator.StopCompute);
    
                Task.WaitAll(iisExpressTask, storageTask, computeTask);
            }
    
            /// <summary&gt;
            /// Initializes the assembly for running unit tests.
            /// </summary&gt;
            /// <param name=&quot;context&quot;&gt;
            /// The context. 
            /// </param&gt;
            [AssemblyInitialize]
            public static void AssemblyInitialize(TestContext context)
            {
                // Set WatiN to not move the mouse
                Settings.Instance.AutoMoveMousePointerToTopLeft = false;
    
                Task iisExpressTask = Task.Factory.StartNew(
                    () =&gt;
                        {
                            String solutionDirectory = context.FindSolutionDirectory();
                            String stsProjectDirectory = Path.Combine(solutionDirectory, &quot;[TheNameOfMySTSProject]&quot;);
    
                            _iisExpress = new IisExpress();
    
                            _iisExpress.Start(stsProjectDirectory, 35026);
                        });
    
                Task storageTask = Task.Factory.StartNew(AzureEmulator.StartStorage);
                Task computeTask = Task.Factory.StartNew(AzureEmulator.StartCompute);
    
                Task.WaitAll(iisExpressTask, storageTask, computeTask);
            }
    
            #endregion
        }
    }{% endhighlight %}

Enjoy

[0]: /post/2012/03/18/Boosting-integration-testing-with-Azure-development-fabric.aspx
[1]: /post/2012/03/18/Spinning-up-IISExpress-for-integration-testing.aspx
