---
title: MVC bundling and line comments at the end of files
categories: .Net
tags: ASP.Net
date: 2013-03-12 07:51:08 +10:00
---

Recently the bundling and minification support in ASP.Net MVC4 have been causing grief with JavaScript's having unexpected tokens. The minification process is failing to process the bundle of scripts correctly, although it does kindly add a failure message to the top of the bundle output.

<!--more-->

{% highlight javascript %}
/* Minification failed. Returning unminified contents.
(5,2-3): run-time warning JS1195: Expected expression: *
(11,60-61): run-time warning JS1004: Expected ';': {
(395,2-3): run-time warning JS1195: Expected expression: )
(397,21-22): run-time warning JS1004: Expected ';': {
(397,4590-4591): run-time warning JS1195: Expected expression: )
(398,28-29): run-time warning JS1195: Expected expression: )
(398,84-85): run-time warning JS1002: Syntax error: }
(402,44-45): run-time warning JS1195: Expected expression: )
(408,1-2): run-time warning JS1002: Syntax error: }
(393,5-22): run-time warning JS1018: 'return' statement outside of function: return Modernizr;
(404,5,406,16): run-time warning JS1018: 'return' statement outside of function: return !!('placeholder' in (Modernizr.input || document.createElement('input')) &&
               'placeholder' in (Modernizr.textarea || document.createElement('textarea'))
             );
 */
{% endhighlight %}

The issues have been found when bundling the jQuery set of files that end with a _//@ sourceMappingURL=_ inline comment. The [StackOverflow post here][0] explains why this is happening. The short story is that the contents of each file is directly appended onto the previous file. If the previous file ended in an inline comment, then the following file is also partially commented out.

The post suggests that you can either remove the line comment from all of the js files or change the line comment to a block comment. I don’t like either of these solutions because many of the scripts are sourced from Nuget and these solutions would cause upgrade pain. We can solve this problem by using some custom bundling logic.

{% highlight csharp %}
namespace MyNamespace
{
    using System.Web.Optimization;
    public class NewLineScriptBundle : ScriptBundle
    {
        public NewLineScriptBundle(string virtualPath) : base(virtualPath)
        {
            Builder = new NewLineBundleBuilder();
        }
        public NewLineScriptBundle(string virtualPath, string cdnPath) : base(virtualPath, cdnPath)
        {
            Builder = new NewLineBundleBuilder();
        }
    }
}
{% endhighlight %}

This is the class that you should use instead of ScriptBundle. It simply uses a custom bundle builder.

{% highlight csharp %}
namespace MyNamespace
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Web.Optimization;
    using Microsoft.Ajax.Utilities;
    public class NewLineBundleBuilder : IBundleBuilder
    {
        public string BuildBundleContent(Bundle bundle, BundleContext context, IEnumerable<FileInfo> files)
        {
            var content = new StringBuilder();
            foreach (var fileInfo in files)
            {
                var contents = Read(fileInfo);
                var parser = new JSParser(contents);
                var bundleValue = parser.Parse(parser.Settings).ToCode();
                content.Append(bundleValue);
                content.AppendLine(";");
            }
            return content.ToString();
        }
        private virtual string Read(FileInfo file)
        {
            using (var reader = file.OpenText())
            {
                return reader.ReadToEnd();
            }
        }
    }
}
{% endhighlight %}

This custom bundle class reads the script files and separates them with a ; and a new line. This then allows the minification engine to correctly process the bundle because any line comment on the end of a file will no longer affect the script in the next file of the bundle.

[0]: http://stackoverflow.com/questions/14402741/jquery-1-9-0-and-modernizr-cannot-be-minified-with-the-asp-net-web-optimization