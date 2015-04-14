---
title: Namespace Renamer
categories: .Net
tags: 
date: 2015-04-14 16:27:00 +11:00
---

All the way [back in 2009][0] I wrote a little console application that assisted with renaming a Visual Studio project and/or namespace. It was handy in that it took into consideration the solution being under source control with TFS. Over the years I have been using TF source control less and Git more. On the odd occasion I have also used this script for solutions that are not bound in source control.

I have refactored the script to take out the TF integration, remove the logging that was just causing a whole lot of white noise and to add in a few more out of the box exclusions for directories to skip in the process.

<!--more-->

{% highlight csharp %}

namespace Neovolve.NamespaceRenamer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;

    internal class Program
    {
        /// <summary>
        ///     The name to find.
        /// </summary>
        private const string FindName = "Solution.Project.OldNamespace";

        /// <summary>
        ///     The name to replace.
        /// </summary>
        private const string ReplaceName = "Solution.Project.NewNamespace";

        /// <summary>
        ///     The root path from which changes are made.
        /// </summary>
        private const string RootPath = @"D:\Repos\Solution.Project";

        /// <summary>
        ///     Stores the directories that should not be processed
        /// </summary>
        private static readonly List<string> DirectoryExclusions = new List<string>
        {
            "bin", 
            "obj", 
            "$tf", 
            ".git", 
            "Packages", 
            "References", 
            "TestResults"
        };

        /// <summary>
        ///     Stores the file extensions that should not be processed.
        /// </summary>
        private static readonly List<string> FileExtensionExclusions = new List<string>
        {
            ".dll", 
            ".exe", 
            ".pdb"
        };

        private static readonly string LogPath = Assembly.GetExecutingAssembly().Location + "."
                                                 + DateTime.Now.ToString("yyyyMMdd-hhmmss") + ".log";

        private static Regex _expression;

        private static Regex DetermineFindReplaceExpression(string findText, string replaceText)
        {
            string pattern;

            if (replaceText.EndsWith(findText, StringComparison.Ordinal))
            {
                // The rename is to add a prefix
                string prefix = replaceText.Substring(0, replaceText.Length - findText.Length);
                pattern = "(?<!" + Regex.Escape(prefix) + ")" + Regex.Escape(FindName);

                WriteMessage("Rename with new prefix detected");
            }
            else if (replaceText.StartsWith(findText, StringComparison.Ordinal))
            {
                // The rename is to add a suffix
                string suffix = replaceText.Substring(findText.Length);
                pattern = Regex.Escape(FindName) + "(?!" + Regex.Escape(suffix) + ")";

                WriteMessage("Rename with new suffix detected");
            }
            else if (replaceText.Contains(findText))
            {
                // The rename is to add a prefix and a suffix
                string prefix = replaceText.Substring(0, replaceText.IndexOf(findText));
                string suffix = replaceText.Substring(replaceText.LastIndexOf(findText) + findText.Length);
                pattern = "(?<!" + Regex.Escape(prefix) + ")" + Regex.Escape(FindName) + "(?!" + Regex.Escape(suffix) +
                          ")";

                WriteMessage("Rename with new prefix and suffix detected");
            }
            else
            {
                // The replace text is not contained within the find text
                pattern = Regex.Escape(FindName);

                WriteMessage("Rename with no suffix or prefix detected");
            }

            return new Regex(pattern, RegexOptions.Compiled);
        }

        private static void Main(string[] args)
        {
            _expression = DetermineFindReplaceExpression(FindName, ReplaceName);

            WriteMessage("Processed started at " + DateTime.Now);

            RunRename(new DirectoryInfo(RootPath));

            WriteMessage("Processed completed at " + DateTime.Now);
            WriteMessage("Log file located at " + LogPath);

            Console.ReadKey();
        }

        private static void RenameDirectory(DirectoryInfo directory)
        {
            WriteMessage("Renaming directory " + directory.FullName);

            Debug.Assert(directory.Parent != null, "No parent directory available for the directory");

            string newName = _expression.Replace(directory.Name, ReplaceName);
            string newPath = Path.Combine(directory.Parent.FullName, newName);

            directory.MoveTo(newPath);
        }

        private static void RenameFile(FileInfo file)
        {
            WriteMessage("Renaming file " + file.FullName);

            Debug.Assert(file.Directory != null, "No parent directory available for the file");

            string newName = _expression.Replace(file.Name, ReplaceName);
            string newPath = Path.Combine(file.Directory.FullName, newName);

            file.MoveTo(newPath);
        }

        private static void RunRename(DirectoryInfo directory)
        {
            if (DirectoryExclusions.Contains(directory.Name))
            {
                WriteMessage("Skipping excluded directory " + directory.FullName);

                return;
            }

            // Check if the directory is not excluded
            foreach (DirectoryInfo childDirectory in directory.GetDirectories())
            {
                // Recurse into child directories
                // Recurse first before changing the current directory results in a bottom up change
                RunRename(childDirectory);
            }

            foreach (FileInfo file in directory.GetFiles())
            {
                if (FileExtensionExclusions.Contains(file.Extension))
                {
                    WriteMessage("Skipping " + file.FullName + " due to file extension exclusion");

                    continue;
                }

                // Load the contents
                string contents = File.ReadAllText(file.FullName, Encoding.UTF8);

                if (_expression.IsMatch(contents))
                {
                    contents = _expression.Replace(contents, ReplaceName);

                    WriteMessage("Updating file contents for " + file.FullName);

                    // Ensure that the file is not read-only
                    // This will occur when the file was not under source control
                    if (file.IsReadOnly)
                    {
                        file.IsReadOnly = false;
                    }

                    File.WriteAllText(file.FullName, contents, Encoding.UTF8);
                }

                // Check if the file needs to be renamed
                if (_expression.IsMatch(file.Name))
                {
                    RenameFile(file);
                }
            }

            if (_expression.IsMatch(directory.Name))
            {
                RenameDirectory(directory);
            }
        }

        private static void WriteMessage(string message)
        {
            Console.WriteLine(message);
            File.AppendAllText(LogPath, message + Environment.NewLine);
        }
    }
}
{% endhighlight %}

All you have to do is drop this into a new console project, change the three field values and off you go.

[0]: /2009/10/06/changing-a-tfs-bound-solution-namespace/
