---
title: Custom MVC4 bundling for timezoneJS tz data
categories : .Net
tags : ASP.Net, Azure
date: 2013-05-09 13:07:00 +10:00
---

I am using the [timezoneJS][0] to provide time zone support for JavaScript running in an MVC4 website that is hosted in Azure. The JavaScript library is bundled with 38 zone files under a tz directory. My understanding is the library reads these zone files in order to correctly determine time zone offsets for combinations of geographical locations and dates.   

The library downloads zone files from the web server as it requires. The big thing to note about the zone files are that they are really heavily commented using a # character as the comment marker. The 38 zone files in the package amount to 673kb of data with the file “northamerica” being the largest at 137kb. This drops down to 232kb and 36kb respectively if comments and blank lines are striped. That’s a lot of unnecessary bandwidth being consumed. MVC4 does not understand these files so none of the OOTB bundling strip the comments. The bundling support in MVC4 (via the [Microsoft.Web.Optimization][1] package) will however allow us to strip this down to the bare data with a custom IBundleBuilder (my third custom bundler – see [here][2] and [here][3] for the others).  

**Current Implementation**  

For background, this is my current implementation. The web project structure looks like this.  

![][4]

The project was already bundling the zone files using the following logic.  

{% highlight csharp linenos %}
private static void BundleTimeZoneData(BundleCollection bundles, HttpServerUtility server)
{
    var directory = server.MapPath("~/Scripts/tz");

    if (Directory.Exists(directory) == false)
    {
        var message = string.Format(
            CultureInfo.InvariantCulture, "The directory '{0}' does not exist.", directory);

        throw new DirectoryNotFoundException(message);
    }

    var files = Directory.GetFiles(directory);

    foreach (var file in files)
    {
        var fileName = Path.GetFileName(file);

        bundles.Add(new Bundle("~/script/tz/" + fileName).Include("~/Scripts/tz/" + fileName));
    }
}
{% endhighlight %}

The timezoneJS package is configured so that it correctly references the bundle paths.

{% highlight javascript linenos %}
timezoneJS.timezone.zoneFileBasePath = "/script/tz";
timezoneJS.timezone.defaultZoneFile = [];
timezoneJS.timezone.init({ async: false });
{% endhighlight %}

**Custom Bundle Builder**

Now comes the part were we strip out the unnecessary comments from the zone files. The TimeZoneBundleBuilder class simply strips out blank lines, lines that start with comments and the parts of lines that end in comments.

{% highlight csharp linenos %}
public class TimeZoneBundleBuilder : IBundleBuilder
{
    private readonly IBundleBuilder _builder;

    public TimeZoneBundleBuilder() : this(new DefaultBundleBuilder())
    {
    }

    public TimeZoneBundleBuilder(IBundleBuilder builder)
    {
        Guard.That(() => builder).IsNotNull();

        _builder = builder;
    }

    public string BuildBundleContent(Bundle bundle, BundleContext context, IEnumerable<BundleFile> files)
    {
        var contents = _builder.BuildBundleContent(bundle, context, files);

        // The compression of the data files is down to ~30% of the original size
        var builder = new StringBuilder(contents.Length / 3);
        var lines = contents.Split(
            new[]
            {
                Environment.NewLine
            }, 
            StringSplitOptions.RemoveEmptyEntries);

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.Trim().StartsWith("#", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var hashIndex = line.IndexOf("#", StringComparison.OrdinalIgnoreCase);

            if (hashIndex == -1)
            {
                builder.AppendLine(line);
            }
            else
            {
                var partialLine = line.Substring(0, hashIndex);

                builder.AppendLine(partialLine);
            }
        }

        return builder.ToString();
    }
}
{% endhighlight %}

This is then hooked up in the bundle configuration for the tz files.
    
{% highlight csharp linenos %}
bundles.Add(
    new Bundle("~/script/tz/" + fileName)
    {
        Builder = new TimeZoneBundleBuilder()
    }.Include("~/Scripts/tz/" + fileName));
{% endhighlight %}

Fiddler confirms that the bundler is stripping the comments. The good news here is that gzip compression also comes into play. Now the gzip compressed “northamerica” file is down from 58kb to 9kb over the wire.

![image][5]

One of the key points to take away from this that you need to know what your application is serving. That includes the output you have written, but also the external packages you have included in your system.

[0]: https://github.com/mde/timezone-js
[1]: http://nuget.org/packages/Microsoft.Web.Optimization/
[2]: /2013/03/14/Bridging-the-gap-between-Font-Awesome-Twitter-Bootstrap-MVC-and-Nuget/
[3]: /2013/03/12/MVC-bundling-and-line-comments-at-the-end-of-files/
[4]: /files/image_155.png
[5]: /files/image_156.png
