---
title: Readable testable code
categories: .Net
tags: Performance
date: 2008-06-02 13:14:26 +10:00
---

I read [Patrick Smacchia's][0] post this morning about [a simple trick to code better and to increase testability][1] and found it to be a great argument. These are the kinds of posts that I really enjoy reading and thinking about. They often have me hacking up test projects to flesh out ideas or kicking off Reflector to see what the BCL is doing.

This was a good post because it got me to challenge why I am coding in the particular style that I have. I have always been a fan of combining if tests when it doesn't result in overly complex logic. In this case, Patrick is arguing that such a style of coding increases nesting depth and makes testing harder.

Take this test case based on his post:

<!--more-->

```csharp
using System;
using System.Collections.Generic;
using System.Diagnostics;
     
namespace ConsoleApplication1
{
    internal class Program
    {
        public static void MethodA(List<string> list)
        {
            foreach (String item in list)
            {
                if (item == null || item.Length == 0
                    || item.Contains("a"))
                {
                    continue;
                }
     
                Console.Write(String.Empty);
            }
        }
     
        public static void MethodB(List<string> list)
        {
            foreach (String item in list)
            {
                if (item == null)
                {
                    continue;
                }
     
                if (item.Length == 0)
                {
                    continue;
                }
     
                if (item.Contains("a"))
                {
                    continue;
                }
     
                Console.Write(String.Empty);
            }
        }
     
        private static void Main(String[] args)
        {
            List<String> items = new List<string>(100);
     
            for (Int32 index = 0; index < 100; index++)
            {
                items.Add(index.ToString());
            }
     
            Stopwatch watch = new Stopwatch();
     
            watch.Start();
     
            for (Int32 firstLoop = 0; firstLoop < 100000; firstLoop ++)
            {
                MethodA(items);
            }
     
            watch.Stop();
     
            Int64 firstTickCount = watch.ElapsedTicks;
     
            watch.Reset();
     
            watch.Start();
     
            for (Int32 secondLoop = 0; secondLoop < 100000; secondLoop++)
            {
                MethodB(items);
            }
     
            watch.Stop();
     
            Int64 secondTickCount = watch.ElapsedTicks;
     
            Console.WriteLine(firstTickCount);
            Console.WriteLine(secondTickCount);
            Console.WriteLine(secondTickCount - firstTickCount);
            Console.ReadKey();
        }
    }
}
```

MethodA contains the combined if tests while MethodB separates them out into individual tests. While I'm not sure about the nesting depth argument due to compiler optimizations, I totally agree about the increase in testability. I also think that such a coding style is much more readable. The risk of combined if tests is that they quickly become complex and difficult to understand.

The first question I had about this was what is the impact on compiled code? Does this change impact performance? After writing up the above test case, I found that it doesn't (as unscientific as the test case is). The next step was to check out the difference in the IL. Turns out that both methods have exactly the same IL. This would explain the similar performance results. I say similar because there would be many outside influences that would result in slightly different outcomes.

So performance isn't a problem as the compiler optimizes the code. The code is more readable and there is no risk of getting confused about the combination of if statements. It is also, as Patrick argues, more testable. The individual continue statements will not get covered if the related scenario of an if statement is not tested for. On this point however, Visual Studio will highlight the line as partially tested rather than 100% covered, but doesn't help in telling you which part of the statement wasn't covered. This makes this style more testable as it is completely clear what hasn't been tested.

I'm no IL expert, but it is interesting that the compiler seems to choose the more complex version (at least to Reflectors interpretation). The following is Reflectors decompilation of the release build interpreted in C#.

```csharp
public static void MethodB(List<string> list)
{
    foreach (string item in list)
    {
        if (((item != null) && (item.Length != 0)) && !item.Contains("a"))
        {
            Console.Write(string.Empty);
        }
    }
}
```

I think I have a new change to my coding style.

[0]: http://codebetter.com/blogs/patricksmacchia/
[1]: http://codebetter.com/blogs/patricksmacchia/archive/2008/03/07/a-simple-trick-to-code-better-and-to-increase-testability.aspx
