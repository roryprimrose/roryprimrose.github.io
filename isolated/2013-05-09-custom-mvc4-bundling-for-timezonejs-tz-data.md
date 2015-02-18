---
title: Custom MVC4 bundling for timezoneJS tz data
categories : .Net
tags : ASP.Net, Azure
date: 2013-05-09 13:07:00 +10:00
---


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


{% highlight javascript linenos %}
timezoneJS.timezone.zoneFileBasePath = "/script/tz";
timezoneJS.timezone.defaultZoneFile = [];
timezoneJS.timezone.init({ async: false });
{% endhighlight %}


Now comes the part were we strip out the unnecessary comments from the zone files. The TimeZoneBundleBuilder class simply strips out blank lines, lines that start with comments and the parts of lines that end in comments.

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

    
bundles.Add(
    new Bundle("~/script/tz/" + fileName)
    {
        Builder = new TimeZoneBundleBuilder()
    }.Include("~/Scripts/tz/" + fileName));
{% endhighlight %}

Fiddler confirms that the bundler is stripping the comments. The good news here is that gzip compression also comes into play. Now the gzip compressed “northamerica” file is down from 58kb to 9kb over the wire.


[0]: https://github.com/mde/timezone-js
[1]: http://nuget.org/packages/Microsoft.Web.Optimization/
[2]: /2013/03/14/Bridging-the-gap-between-Font-Awesome-Twitter-Bootstrap-MVC-and-Nuget/
[3]: /2013/03/12/MVC-bundling-and-line-comments-at-the-end-of-files/
[4]: /blogfiles/image_155.png
[5]: /blogfiles/image_156.png