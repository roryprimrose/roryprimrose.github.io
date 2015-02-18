---
title: Running an asynchronous workflow with impersonation
categories : .Net
tags : WF
date: 2007-05-29 19:59:28 +10:00
---

I encountered a bit of a curly one today with my workflows. 

Generally, I am executing WF workflows synchronously because I am using them as my business layer implementation for distributed services. This means that in order to return a value from a service call, the workflow needs to complete first. Because WF executes asynchronously by default, I am using the ManualWorkflowSchedulerService to execute workflows on the same thread as the calling process.

This became a problem when I actually want a combination of synchronous and asynchronous workflow executions for a service call. With the DefaultWorkflowSchedulerService and ManualWorkflowSchedulerService in WF, this wouldn't be supported in the one workflow runtime instance.

My first solution was to host two runtimes, one using the ManualWorkflowSchedulerService for executing workflows synchronously, and one using the DefaultWorkflowSchedulerService for executing the asynchronous workflows. The hitch is that I also need impersonation, but this doesn't appear to be possible when executing workflows using the DefaultWorkflowSchedulerService as the impersonated credentials get lost.

Second solution was to use just the one runtime that uses the ManualWorkflowSchedulerService, but execute the asynchronous workflow by calling the runtime from a new background thread. The problem here is that it doesn't appear to be possible to set up impersonation on the new thread.

Third solution was to use a delegate by calling BeginInvoke. This worked, but then I realised that I can use a thread and manually call for impersonation from inside its execution. To do this, I need to pass the WindowsIdentity along with my other parameter to a thread wrapper. My solution now looks like this:

    {% highlight csharp linenos %}
    using System;
    using System.Collections.Generic;
    using System.Security.Principal;
    using System.Threading;
    using Neovolve.Framework.Workflow;
    using Neovolve.Jabiru.Sessions;
    using Neovolve.Jabiru.Transfer.Service.BusinessWorkflows;
     
    namespace Neovolve.Jabiru.Transfer.Service
    {
        /// <summary&gt;
        /// The <see cref="Neovolve.Jabiru.Transfer.Service.ServerItemSearcher"/&gt;
        /// class is used to search for items on the server.
        /// </summary&gt;
        internal class ServerItemSearcher
        {
            #region Declarations
     
            /// <summary&gt;
            /// Stores the session reference.
            /// </summary&gt;
            private TransferSession _session;
     
            /// <summary&gt;
            /// Stores the executing identity.
            /// </summary&gt;
            private WindowsIdentity _identity;
     
            /// <summary&gt;
            /// Stores the search thread.
            /// </summary&gt;
            private Thread _searchThread;
     
            #endregion
     
            #region Constructors
     
            /// <summary&gt;
            /// Initializes a new instance of the 
            /// <see cref="Neovolve.Jabiru.Transfer.Service.ServerItemSearcher"/&gt; class.
            /// </summary&gt;
            /// <param name="session"&gt;The session.</param&gt;
            /// <param name="identity"&gt;The identity.</param&gt;
            public ServerItemSearcher(TransferSession session, WindowsIdentity identity)
            {
                _session = session;
                _identity = identity;
            }
     
            #endregion
     
            #region Methods
     
            /// <summary&gt;
            /// Runs the search.
            /// </summary&gt;
            public void RunSearch()
            {
                // Create and start the thread
                _searchThread = new Thread(new ThreadStart(RunSearchInternal));
                _searchThread.IsBackground = true;
                _searchThread.Start();
            }
     
            /// <summary&gt;
            /// Runs the search internal.
            /// </summary&gt;
            private void RunSearchInternal()
            {
                using (WindowsImpersonationContext context = _identity.Impersonate())
                {
                    Dictionary<String, Object&gt; searchParameters = new Dictionary<String, Object&gt;();
                    searchParameters.Add("Session", _session);
     
                    // Invoke the server item search workflow asynchronously
                    SynchronousWorkflowRuntime.Current.ExecuteWorkflow(typeof(SearchNewServerItemsWorkflow), searchParameters);
                }
            }
     
            #endregion
        }
    }
    
    {% endhighlight %}


