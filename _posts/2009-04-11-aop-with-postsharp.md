---
title: AOP with PostSharp
categories : .Net
tags : AOP, PostSharp
date: 2009-04-11 14:18:00 +10:00
---

PostSharp is a very power AOP framework. It changes the IL emitted from the compiler according to the aspects that it finds. The way PostSharp can be implemented ranges from very easy to very complex. 

The demos in this series will look at a very simple scenario. PostSharp will be used to aspect the RunTest() method in the following code. The aspect used will output console messages surrounding the RunTest method. 

<!--more-->

{% highlight csharp %}
using System; 
    
namespace ConsoleApplication1 
{ 
    internal class Program 
    { 
        private static void Main(string[] args) 
        { 
            RunTest(); 
            Console.ReadKey(); 
        } 
        private static void RunTest() 
        { 
            Console.WriteLine("Running the test method"); 
        } 
    } 
}    
{% endhighlight %}

Two different implementations of the demo will be run. One with PostSharp.Laos (the easy part) and the other with PostSharp.Core (the hard part). The outcome of each demo will look at the: 

* references required by the project
* difficulty of code required
* impact on the IL of the final assembly
* references required on the final assembly
* debugging experience
