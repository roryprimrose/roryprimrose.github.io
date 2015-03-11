---
title: Bridging the gap between Font Awesome, Twitter Bootstrap, MVC and Nuget
categories : .Net
tags : ASP.Net
date: 2013-03-14 22:56:55 +10:00
---

I’m working on an MVC project which pulls in lots of Nuget packages. [Font Awesome][0] was the latest package but there is a discrepancy in the locations for the font files between the css url references and the location that the Nuget package uses for the MVC project. The css files use a reference to font/ relative to the current resource. The Nuget package puts all the resources into Content/font/. 

I don’t want to change either the css file from the Nuget package or the location of the fonts used by the Nuget package. Doing so will just cause upgrade pain when a new version of Font Awesome gets released to Nuget.

I looked at custom routing options but these just seemed to cause more problems than they solved. It then dawned on me that I could use an IBundleBuilder implementation now that I know [how they work][1].

<!--more-->

This is what I have for my current Font Awesome StyleBundle configuration.

{% highlight csharp %}
bundles.Add(
    new StyleBundle("~/css/fontawesome")
        .Include("~/Content/font-awesome.css"));
{% endhighlight %}

The contents of the css file need to be adjusted when the bundle is created so that they include the Content directory, but also make the resource reference relative to the application root. Enter the ReplaceContentsBundleBuilder.

{% highlight csharp %}
namespace MyNamespace
{
    using System.Collections.Generic;
    using System.IO;
    using System.Web.Optimization;
    using Seterlund.CodeGuard;
    
    public class ReplaceContentsBundleBuilder : IBundleBuilder
    {
        private readonly string _find;
    
        private readonly string _replaceWith;
    
        private readonly IBundleBuilder _builder;
    
        public ReplaceContentsBundleBuilder(string find, string replaceWith)
            : this(find, replaceWith, new DefaultBundleBuilder())
        {
        }
    
        public ReplaceContentsBundleBuilder(string find, string replaceWith, IBundleBuilder builder)
        {
            Guard.That(() => find).IsNotNullOrEmpty();
            Guard.That(() => replaceWith).IsNotNullOrEmpty();
            Guard.That(() => builder).IsNotNull();
    
            _find = find;
            _replaceWith = replaceWith;
            _builder = builder;
        }
    
        public string BuildBundleContent(Bundle bundle, BundleContext context, IEnumerable<FileInfo> files)
        {
            string contents = _builder.BuildBundleContent(bundle, context, files);
    
            return contents.Replace(_find, _replaceWith);
        }
    }
}
{% endhighlight %}

This class makes it super easy to modify the bundle of the fly to give me the translation that I require without having to tweek anything coming from Nuget. The bundle config is now:

{% highlight csharp %}
bundles.Add(
    new StyleBundle("~/css/fontawesome")
    {
        Builder = new ReplaceContentsBundleBuilder("url('font/", "url('/Content/font/")
    }.Include("~/Content/font-awesome.css"));
{% endhighlight %}

This will replace any content in the bundle that has a relative reference to font/ with a reference to /Content/font/ which is relative to the site root.

Easy done.

[0]: http://fortawesome.github.com/Font-Awesome/
[1]: /2013/03/11/mvc-bundling-and-line-comments-at-the-end-of-files/
