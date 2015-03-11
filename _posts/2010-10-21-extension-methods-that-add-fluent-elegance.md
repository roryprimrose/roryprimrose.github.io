---
title: Extension methods that add fluent elegance
categories : .Net
date: 2010-10-21 09:52:59 +10:00
---

Last night I was updating some integration tests for one of my projects. I have many test methods that configure a directory path from several sources of information and found that path concatenation often results in ugly unreadable code. 

Consider the scenario where you have a base path to which you want to add several other directory names.

<!--more-->

{% highlight csharp %}
namespace ConsoleApplication1
{
    using System;
    using System.IO;
    
    class Program
    {
        static void Main(String[] args)
        {
            String basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            const String ProductName = &quot;SomeProductName&quot;;
            String instanceName = Guid.NewGuid().ToString();
            const String dataName = &quot;DataStore&quot;;
    
            String hardToReadEvaluation = Path.Combine(Path.Combine(Path.Combine(basePath, ProductName), instanceName), dataName);
    
            Console.WriteLine(hardToReadEvaluation);
            Console.ReadKey();
        }
    }
}
{% endhighlight %}

The statement with multiple Path.Combine evaluations is very difficult to read. A simple extension method can turn this into a fluent API design to achieve the same result and allow the intention of the code to be crystal clear.

{% highlight csharp %}
namespace ConsoleApplication1
{
    using System;
    using System.IO;
    
    public static class Extensions
    {
        public static String AppendPath(this String basePath, String appendValue)
        {
            return Path.Combine(basePath, appendValue);
        }
    }
    
    class Program
    {
        static void Main(String[] args)
        {
            String basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            const String ProductName = &quot;SomeProductName&quot;;
            String instanceName = Guid.NewGuid().ToString();
            const String dataName = &quot;DataStore&quot;;
    
            String fluentEvaluation = basePath.AppendPath(ProductName).AppendPath(instanceName).AppendPath(dataName);
    
            Console.WriteLine(fluentEvaluation);
            Console.ReadKey();
        }
    }
}
{% endhighlight %}

The code is now much more readable. You no longer need to backtrack along the line of code to figure out which variables related to which operation in the evaluation as the code now reads fluently from left to right.


