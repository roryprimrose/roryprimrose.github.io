---
title: Cleaning a VS2010 solution with a sledgehammer
categories : .Net, My Software
date: 2010-08-02 12:11:00 +10:00
---

My workplace has been having issues with VS2010 picking up old assemblies since we have been using VS2010 on a complex services solution. The issue usually pops up when executing test runs with the built in support for MSTest. This occasionally happened in VS2008 but is much more prevalent in 2010. The scenario seems to be that somewhere between the IDE and MSTest is picking up assemblies from prior TestResults directories if the assembly can't be found in bin\Debug or bin\Release directories. This means that the normal clean operation under the build menu is not sufficient to get rid of this problem.

Enter the sledgehammer. I wrote a little utility that will recursively go through each bin, obj and TestResults folder under the solution path and delete everything it can (including read-only files). Any exceptions encountered will be output to the console. The code itself is really simple.

{% highlight csharp %}
namespace Neovolve.SolutionCleaner
{
    using System;
    using System.IO;
     
    internal class Program
    {
        private static void DeleteDirectory(String directoryPath)
        {
            String[] directories = Directory.GetDirectories(directoryPath);
    
            for (int index = 0; index < directories.Length; index++)
            {
                String childDirectory = directories[index];
    
                DeleteDirectory(childDirectory);
            }
    
            Console.WriteLine("Cleaning directory " + directoryPath);
    
            String[] files = Directory.GetFiles(directoryPath);
    
            for (Int32 index = 0; index < files.Length; index++)
            {
                String file = files[index];
    
                Console.WriteLine("Deleting file " + file);
    
                try
                {
                    FileInfo fileInfo = new FileInfo(file);
    
                    if (fileInfo.IsReadOnly)
                    {
                        fileInfo.IsReadOnly = false;
                    }
    
                    fileInfo.Delete();
                }
                catch (IOException ex)
                {
                    Console.WriteLine(ex);
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine(ex);
                }
            }
    
            Console.WriteLine("Deleting directory " + directoryPath);
    
            try
            {
                Directory.Delete(directoryPath, true);
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine(ex);
            }
        }
    
        private static void Main(String[] args)
        {
            foreach (String argument in args)
            {
                if (Directory.Exists(argument))
                {
                    ProcessDirectory(argument);
                }
            }
    
            Console.WriteLine("Completed");
        }
    
        private static void ProcessChildDirectory(String directoryPath, String childDirectory)
        {
            String[] directories = Directory.GetDirectories(directoryPath, childDirectory, SearchOption.AllDirectories);
    
            foreach (String directory in directories)
            {
                DeleteDirectory(directory);
            }
        }
    
        private static void ProcessDirectory(String directoryPath)
        {
            ProcessChildDirectory(directoryPath, "bin");
            ProcessChildDirectory(directoryPath, "obj");
            ProcessChildDirectory(directoryPath, "TestResults");
        }
    }
}
{% endhighlight %}

The best way to hook this up is via the Visual Studio External Tools dialog by passing in the current solutions directory path.![image][0]

This method also allows you to output the console results to the Output window for a more integrated experience.

**NOTE:**It's a good idea to run a Visual Studio clean from the build menu before running this tool to get Visual Studio to release any locks it has on files in these directories. We haven't had any assembly version conflicts since running this tool.

You can either compile the above yourself or grab the executable below.

[Neovolve.SolutionCleaner.zip (2.61 kb)][1]

[0]: /files/image_22.png
[1]: /files/2010/8/Neovolve.SolutionCleaner.zip
