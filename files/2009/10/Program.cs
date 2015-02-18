using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Neovolve.TfsSupport.NamespaceRename
{
    /// <summary>
    /// The program.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// The find name.
        /// </summary>
        private const String FindName = "Product.Subproduct";

        /// <summary>
        /// The replace name.
        /// </summary>
        private const String ReplaceName = "Company.Product.Subproduct";

        /// <summary>
        /// The root path from which changes are made.
        /// </summary>
        private const String RootPath = @"C:\Dev\ProductWorkspace";

        /// <summary>
        /// The tf path.
        /// </summary>
        private const String TfPath = @"C:\Program Files\Microsoft Visual Studio 9.0\Common7\IDE\tf.exe";

        /// <summary>
        /// Stores the directories that should not be processed
        /// </summary>
        private static readonly List<String> DirectoryExclusions = new List<String>
                                                                   {
                                                                       "bin", 
                                                                       "obj", 
                                                                       "TestResults"
                                                                   };

        /// <summary>
        /// Stores the file extensions that should not be processed.
        /// </summary>
        private static readonly List<String> FileExtensionExclusions = new List<String>
                                                                       {
                                                                           ".dll", 
                                                                           ".exe", 
                                                                           ".pdb"
                                                                       };

        /// <summary>
        /// Stores the log path
        /// </summary>
        private static readonly String LogPath = Assembly.GetExecutingAssembly().Location + "."
                                                 + DateTime.Now.ToString("yyyyMMdd-hhmmss") + ".log";

        /// <summary>
        /// Stores the expression used to find and replace text.
        /// </summary>
        private static Regex _expression;

        /// <summary>
        /// Checks out the file from TFS.
        /// </summary>
        /// <param name="file">
        /// The file.
        /// </param>
        private static void CheckOutFile(FileInfo file)
        {
            WriteMessage("Checking out file " + file.FullName);

            Debug.Assert(file.Directory != null, "No parent directory available for the file");

            ProcessStartInfo info = new ProcessStartInfo(TfPath);

            info.Arguments = "checkout \"" + file.FullName + "\"";
            info.CreateNoWindow = false;
            info.RedirectStandardOutput = true;
            info.UseShellExecute = false;

            Process renameProcess = Process.Start(info);

            Debug.Assert(renameProcess != null, "No process was started");

            String output = renameProcess.StandardOutput.ReadToEnd();

            renameProcess.WaitForExit();

            WriteMessage(output);
        }

        /// <summary>
        /// Determines the find replace expression.
        /// </summary>
        /// <param name="findText">
        /// The find text.
        /// </param>
        /// <param name="replaceText">
        /// The replace text.
        /// </param>
        /// <returns>
        /// A <see cref="Regex"/> instance.
        /// </returns>
        private static Regex DetermineFindReplaceExpression(String findText, String replaceText)
        {
            String pattern;

            if (replaceText.EndsWith(findText, StringComparison.Ordinal))
            {
                // The rename is to add a prefix
                String prefix = replaceText.Substring(0, replaceText.Length - findText.Length);
                pattern = "(?<!" + Regex.Escape(prefix) + ")" + Regex.Escape(FindName);

                WriteMessage("Rename with new prefix detected");
            }
            else if (replaceText.StartsWith(findText, StringComparison.Ordinal))
            {
                // The rename is to add a suffix
                String suffix = replaceText.Substring(findText.Length);
                pattern = Regex.Escape(FindName) + "(?!" + Regex.Escape(suffix) + ")";

                WriteMessage("Rename with new suffix detected");
            }
            else if (replaceText.Contains(findText))
            {
                // The rename is to add a prefix and a suffix
                String prefix = replaceText.Substring(0, replaceText.IndexOf(findText));
                String suffix = replaceText.Substring(replaceText.LastIndexOf(findText) + findText.Length);
                pattern = "(?<!" + Regex.Escape(prefix) + ")" + Regex.Escape(FindName) + "(?!" + Regex.Escape(suffix) + ")";

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

        /// <summary>
        /// Mains the specified args.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private static void Main(String[] args)
        {
            _expression = DetermineFindReplaceExpression(FindName, ReplaceName);

            WriteMessage("Processed started at " + DateTime.Now);

            RunRename(new DirectoryInfo(RootPath));

            WriteMessage("Processed completed at " + DateTime.Now);
            WriteMessage("Log file located at " + LogPath);

            Console.ReadKey();
        }

        /// <summary>
        /// Renames the directory in TFS.
        /// </summary>
        /// <param name="directory">
        /// The directory.
        /// </param>
        private static void RenameDirectory(DirectoryInfo directory)
        {
            WriteMessage("Renaming directory " + directory.FullName);

            Debug.Assert(directory.Parent != null, "No parent directory available for the directory");

            ProcessStartInfo info = new ProcessStartInfo(TfPath);
            String newName = _expression.Replace(directory.Name, ReplaceName);
            String newPath = Path.Combine(directory.Parent.FullName, newName);

            info.Arguments = "rename \"" + directory.FullName + "\" \"" + newPath + "\"";
            info.CreateNoWindow = false;
            info.RedirectStandardOutput = true;
            info.UseShellExecute = false;

            Process renameProcess = Process.Start(info);

            Debug.Assert(renameProcess != null, "No process was started");

            String output = renameProcess.StandardOutput.ReadToEnd();

            renameProcess.WaitForExit();

            WriteMessage(output);
        }

        /// <summary>
        /// Renames the file in TFS.
        /// </summary>
        /// <param name="file">
        /// The file.
        /// </param>
        private static void RenameFile(FileInfo file)
        {
            WriteMessage("Renaming file " + file.FullName);

            Debug.Assert(file.Directory != null, "No parent directory available for the file");

            ProcessStartInfo info = new ProcessStartInfo(TfPath);
            String newName = _expression.Replace(file.Name, ReplaceName);
            String newPath = Path.Combine(file.Directory.FullName, newName);

            info.Arguments = "rename \"" + file.FullName + "\" \"" + newPath + "\"";
            info.CreateNoWindow = false;
            info.RedirectStandardOutput = true;
            info.UseShellExecute = false;

            Process renameProcess = Process.Start(info);

            Debug.Assert(renameProcess != null, "No process was started");

            String output = renameProcess.StandardOutput.ReadToEnd();

            renameProcess.WaitForExit();

            WriteMessage(output);
        }

        /// <summary>
        /// Runs the rename against the specified directory.
        /// </summary>
        /// <param name="directory">
        /// The directory.
        /// </param>
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
                // Recurse into child directorys
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
                String contents = File.ReadAllText(file.FullName, System.Text.Encoding.UTF8);

                if (_expression.IsMatch(contents))
                {
                    contents = _expression.Replace(contents, ReplaceName);

                    CheckOutFile(file);

                    WriteMessage("Updating file contents for " + file.FullName);

                    // Ensure that the file is not readonly
                    // This will occur when the file was not under source control
                    if (file.IsReadOnly)
                    {
                        file.IsReadOnly = false;
                    }

                    File.WriteAllText(file.FullName, contents, System.Text.Encoding.UTF8);
                }
                else
                {
                    WriteMessage("No change required to file contents for " + file.FullName);
                }

                // Check if the file needs to be renamed
                if (_expression.IsMatch(file.Name))
                {
                    RenameFile(file);
                }
                else
                {
                    WriteMessage("No rename required for " + file.FullName);
                }
            }

            if (_expression.IsMatch(directory.Name))
            {
                RenameDirectory(directory);
            }
            else
            {
                WriteMessage("No rename required for " + directory.FullName);
            }
        }

        /// <summary>
        /// Writes the message.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        private static void WriteMessage(String message)
        {
            Console.WriteLine(message);
            File.AppendAllText(LogPath, message + Environment.NewLine);
        }
    }
}