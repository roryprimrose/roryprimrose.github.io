---
title: Integration testing with Azure development storage
categories : .Net
tags : Azure
date: 2012-01-12 22:43:19 +10:00
---

I’ve been working on some classes that write data to Azure table storage. These classes of course need to be tested. Unfortunately the development fabric only spins up when you F5 an Azure project. This is a little problematic when the execution is from a unit test framework.

Some quick searching brought up [this post][0] which provides 99% of the answer. The only hiccup with this solution is that it is targeting the 1.0 version of the Azure SDK. I have updated this code to work with the 1.6 version of the SDK.

{% highlight csharp linenos %}
namespace MyProduct.Server.DataAccess.Azure.IntegrationTests
{
    using System;
    using System.Diagnostics;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    
    /// <summary&gt;
    /// The <see cref=&quot;Initialization&quot;/&gt;
    ///   class is used to run assembly initialization work for the test assembly.
    /// </summary&gt;
    [TestClass]
    public class Initialization
    {
        #region Setup/Teardown
    
        /// <summary&gt;
        /// The assembly initialize.
        /// </summary&gt;
        /// <param name=&quot;context&quot;&gt;
        /// The context.
        /// </param&gt;
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            StartAzureDevelopmentStorage();
        }
    
        #endregion
    
        #region Static Helper Methods
    
        /// <summary&gt;
        /// The start azure development storage.
        /// </summary&gt;
        private static void StartAzureDevelopmentStorage()
        {
            Int32 count = Process.GetProcessesByName(&quot;DSService&quot;).Length;
    
            if (count == 0)
            {
                ProcessStartInfo start = new ProcessStartInfo
                                            {
                                                Arguments = &quot;/devstore:start&quot;,
                                                FileName = @&quot;C:\Program Files\Windows Azure Emulator\emulator\csrun.exe&quot;
                                            };
                Process proc = new Process
                                {
                                    StartInfo = start
                                };
    
                proc.Start();
                proc.WaitForExit();
            }
        }
    
        #endregion
    }
}
{% endhighlight %}

Dropping this class into the test assembly will ensure that the Azure development storage is running before the integration tests are executed.

[0]: http://searchwindevelopment.techtarget.com/tip/Use-Azure-development-storage-from-unit-tests
