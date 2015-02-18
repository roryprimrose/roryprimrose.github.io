---
title: Spinning up IISExpress for integration testing
categories : .Net
tags : Azure, WIF
date: 2012-03-18 22:34:51 +10:00
---

The system I am currently working uses the development fabric in the Azure SDK for working with Azure web roles and worker roles. I am also using a local WIF STS site to simulate Azure ACS. This allows me to integrate claims based security into the system without having to actually start using an Azure subscription.

The local STS is running on IISExpress. Like the [previous post][0] about running the Azure emulator for integration testing, the STS also needs to be spun up to run the system. The following class provides the wrapper logic for spinning up IISExpress.

    namespace Neovolve.Toolkit.TestSupport
    {
        using System;
        using System.Diagnostics;
        using System.Diagnostics.CodeAnalysis;
        using System.Globalization;
        using System.IO;
        using System.Threading;
    
        /// <summary&gt;
        /// The <see cref=&quot;IisExpress&quot;/&gt; class is used to manage an IIS Express instance for running integration tests.
        /// </summary&gt;
        public class IisExpress : IDisposable
        {
            /// <summary&gt;
            ///   Stores whether this instance has been disposed.
            /// </summary&gt;
            private Boolean _isDisposed;
    
            /// <summary&gt;
            ///   Stores the IIS Express process.
            /// </summary&gt;
            private Process _process;
    
            /// <summary&gt;
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary&gt;
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
    
            /// <summary&gt;
            /// Starts IIS Express using the specified directory path and port.
            /// </summary&gt;
            /// <param name=&quot;directoryPath&quot;&gt;
            /// The directory path. 
            /// </param&gt;
            /// <param name=&quot;port&quot;&gt;
            /// The port. 
            /// </param&gt;
            public void Start(String directoryPath, Int32 port)
            {
                String iisExpressPath = DetermineIisExpressPath();
                String arguments = String.Format(
                    CultureInfo.InvariantCulture, &quot;/path:\&quot;{0}\&quot; /port:{1}&quot;, directoryPath, port);
    
                ProcessStartInfo info = new ProcessStartInfo(iisExpressPath)
                                            {
                                                WindowStyle = ProcessWindowStyle.Hidden,
                                                ErrorDialog = true,
                                                LoadUserProfile = true,
                                                CreateNoWindow = false,
                                                UseShellExecute = false,
                                                Arguments = arguments
                                            };
    
                Thread startThread = new Thread(() =&gt; StartIisExpress(info))
                                         {
                                             IsBackground = true
                                         };
    
                startThread.Start();
            }
    
            /// <summary&gt;
            /// Releases unmanaged and - optionally - managed resources.
            /// </summary&gt;
            /// <param name=&quot;disposing&quot;&gt;
            /// <c&gt;true</c&gt; to release both managed and unmanaged resources; <c&gt;false</c&gt; to release only unmanaged resources. 
            /// </param&gt;
            protected virtual void Dispose(Boolean disposing)
            {
                if (_isDisposed)
                {
                    return;
                }
    
                if (disposing)
                {
                    // Free managed resources
                    if (_process.HasExited == false)
                    {
                        _process.CloseMainWindow();
                    }
    
                    _process.Dispose();
                }
    
                // Free native resources if there are any
                _isDisposed = true;
            }
    
            /// <summary&gt;
            /// Determines the IIS express path.
            /// </summary&gt;
            /// <returns&gt;
            /// A <see cref=&quot;String&quot;/&gt; instance. 
            /// </returns&gt;
            private static String DetermineIisExpressPath()
            {
                String iisExpressPath;
    
                if (Environment.Is64BitOperatingSystem)
                {
                    iisExpressPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                }
                else
                {
                    iisExpressPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                }
    
                iisExpressPath = Path.Combine(iisExpressPath, @&quot;IIS Express\iisexpress.exe&quot;);
    
                return iisExpressPath;
            }
    
            /// <summary&gt;
            /// Starts the IIS express.
            /// </summary&gt;
            /// <param name=&quot;info&quot;&gt;
            /// The info. 
            /// </param&gt;
            [SuppressMessage(&quot;Microsoft.Design&quot;, &quot;CA1031:DoNotCatchGeneralExceptionTypes&quot;,
                Justification = &quot;Required here to ensure that the instance is disposed.&quot;)]
            private void StartIisExpress(ProcessStartInfo info)
            {
                try
                {
                    _process = Process.Start(info);
    
                    _process.WaitForExit();
                }
                catch (Exception)
                {
                    Dispose();
                }
            }
        }
    }{% endhighlight %}

Unfortunately this implementation is not able to automatically resolve project information that the Azure implementation can. The code would have to make too many inappropriate assumptions in order to make this work. This implementation therefore requires the information about the site it will host to be provided to it.

[0]: /post/2012/03/18/Boosting-integration-testing-with-Azure-development-fabric.aspx
