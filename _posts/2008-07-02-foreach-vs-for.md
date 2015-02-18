---
title: foreach vs for
categories : .Net
tags : Performance
date: 2008-07-02 10:31:54 +10:00
---

I've just hit the foreach code coverage issue again in one of my unit tests (see my [Code coverage doesn't like foreach loops][0] post). To ensure that my tests were correctly covering all possibilities, I had to change the foreach loop into a for loop and run the test again. 

The issue in this case is that the collection object I was referencing was ConfigurationElementCollection which implements ICollection. Unfortunately, this type doesn't expose an indexer property. In order to test the code coverage metrics using a for loop, I first had to create an array of the appropriate length and copy across the items across.

After running the test again, I had 100% code coverage. I have now confirmed that the missing blocks in coverage are a result of the foreach statement rather than an issue with my unit test. Now the question is do I remove the additional code and redundant array copy ? The reasons to convert the code back to the foreach loop are:

* Cleaner code 
  * Less bloat
  * More understandable
* Code coverage shouldn't define coding style

The second point is guaranteed to get some people in a flap. To be clear, I am not saying that testability shouldn't have a bearing on coding style, I'm saying that code coverage shouldn't define coding style. While this post is about code coverage, I should also point out that code coverage doesn't really mean much by itself. It is just an indicator.

In this case, the unit tests were valid and covered all the angles, so the question is purely whether a code coverage metric is important enough to modify the code. 

Another thing also comes to mind. What about performance? By using the array, I have the additional overhead of creating an array and populating it from the ICollection, but then I have the performance gain from not using the IEnumerator invoked by the foreach statement.

A quick test gave me a suitable answer.

    {% highlight csharp linenos %}
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Threading;
     
    namespace ConsoleApplication1
    {
        internal class Program
        {
            private static void Main(String[] args)
            {
                Collection<String&gt; items = new Collection<String&gt;();
     
                for (Int32 index = 0; index < 10; index++)
                {
                    items.Add(Guid.NewGuid().ToString());
                }
     
                Stopwatch watch = new Stopwatch();
                const Int32 Iterations = 10000;
     
                Thread.Sleep(1000);
     
                watch.Start();
     
                for (Int32 index = 0; index < Iterations; index++)
                {
                    foreach (String item in items)
                    {
                        Debug.WriteLine(item);
                    }
                }
     
                watch.Stop();
     
                Console.WriteLine("foreach took {0} ticks", watch.ElapsedTicks);
     
                watch.Reset();
                watch.Start();
     
                for (Int32 index = 0; index < Iterations; index++)
                {
                    String[] newItems = new String[items.Count];
     
                    items.CopyTo(newItems, 0);
     
                    for (Int32 count = 0; count < newItems.Length; count++)
                    {
                        Debug.WriteLine(newItems[count]);
                    }
                }
     
                watch.Stop();
     
                Console.WriteLine("for took {0} ticks", watch.ElapsedTicks);
     
                Console.ReadKey();
            }
        }
    }
    {% endhighlight %}

Each time I run this test, the for loop runs at 70% of the time of the foreach loop, even with the array copy.

While code coverage by itself is not enough to make me change my coding style, a performance improvement and more accurate code coverage is a good enough reason.

[0]: /archive/2008/04/04/code-coverage-doesn-t-like-foreach-loops.aspx
