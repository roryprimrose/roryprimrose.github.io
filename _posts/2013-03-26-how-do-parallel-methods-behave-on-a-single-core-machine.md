---
title: How do Parallel methods behave on a single core machine?
categories : .Net
tags : Performance
date: 2013-03-26 20:09:36 +10:00
---

I’ve been wondering about this for a long time. None of the reading that I’ve done has conclusively answered this question. It just so happens that I’ve been developing in a VM for the last couple of months. I got the chance tonight to downgrade the VM specs to a single core to run some tests. The results were very pleasing, yet completely unexpected.

My test code is very unscientific but achieves the objective.{% highlight csharp linenos %}
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
    
namespace ConsoleApplication1
{
    class Program
    {
        private static ConcurrentBag<int> _taskThreadIds =new ConcurrentBag<int>();
        private static ConcurrentBag<int> _parallelThreadIds =new ConcurrentBag<int>();
    
        static void Main(string[] args)
        {
            Console.WriteLine("Starting task array on {0}", Thread.CurrentThread.ManagedThreadId);
            Stopwatch watch = Stopwatch.StartNew();
    
            Task[] tasks = new Task[100];
    
            for (int i = 0; i < 100; i++)
            {
                tasks[i] = Task.Factory.StartNew(TaskAction, i);
            }
    
            Task.WaitAll(tasks);
    
            watch.Stop();
    
            OutputResults(_taskThreadIds, watch, "task array");
    
            Console.WriteLine("Starting parallel loop on {0}", Thread.CurrentThread.ManagedThreadId);
            watch = Stopwatch.StartNew();
    
            Parallel.For(0, 100, ParallelAction);
    
            watch.Stop();
    
            OutputResults(_parallelThreadIds, watch, "parallel");
    
            Console.WriteLine("Press key to close");
            Console.ReadKey();
        }
    
        private static void OutputResults(ConcurrentBag<int> ids, Stopwatch watch, string testType)
        {
            var allIds = ids.ToList();
            var uniqueIds = allIds.Distinct().ToList();
    
            Console.WriteLine("Completed {0} on {1} in {2} millseconds using {3} threads", testType,
                                Thread.CurrentThread.ManagedThreadId, watch.ElapsedMilliseconds, uniqueIds.Count);
    
            for (int i = 0; i < uniqueIds.Count; i++)
            {
                Console.WriteLine("Thread {0} was used {1} times", uniqueIds[i], allIds.Count(x => x == uniqueIds[i]));
            }
        }
    
        private static void TaskAction(object x)
        {
            _taskThreadIds.Add(Thread.CurrentThread.ManagedThreadId);
    
            //Console.WriteLine("{0}: Starting on {1}", x, Thread.CurrentThread.ManagedThreadId);
            Thread.Sleep(500);
            //Console.WriteLine("{0}: Completing on {1}", x, Thread.CurrentThread.ManagedThreadId);
        }
    
        private static void ParallelAction(int x)
        {
            _parallelThreadIds.Add(Thread.CurrentThread.ManagedThreadId);
    
            //Console.WriteLine("{0}: Starting on {1}", x, Thread.CurrentThread.ManagedThreadId);
            Thread.Sleep(500);
            //Console.WriteLine("{0}: Completing on {1}", x, Thread.CurrentThread.ManagedThreadId);
        }
    }
}
{% endhighlight %}

And here is the answer.![image][0]

The obvious observation is that Task.WaitAll on an array of tasks is almost three times slower. It uses a lower, but similar number of threads for the execution compared to Parallel. The most interesting outcome is the distribution of thread usage. This is where the real performance kicks in.

I was honestly expecting Parallel to not execute asynchronously in a single core environment as the documentation seems to be completely geared towards multi-core systems. I am really happy with this outcome, especially because the code flow makes better use of generics.

[0]: //blogfiles/image_154.png
