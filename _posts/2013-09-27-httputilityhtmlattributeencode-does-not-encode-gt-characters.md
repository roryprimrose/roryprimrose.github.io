---
title: HttpUtility.HtmlAttributeEncode does not encode &gt; characters
categories: .Net
tags: ASP.Net, WIF
date: 2013-09-27 12:35:33 +10:00
---

I’ve been working on a side project that works with HTML responses from websites when I hit an issue when authenticating to a development STS that uses WIF. I found that WIF uses HttpUtility.HtmlAttributeEncode to dynamically generate a form that contains auth information in a hidden field. The auth information is XML but the form does not get processed correctly by some custom code because the HTML is invalid. 

So my debugging efforts seem to have uncovered an issue in HttpUtility.HtmlAttributeEncode (or more specifically the HttpEncoder class).

<!--more-->

The problem can be simply expressed by the following code.

{% highlight csharp %}
using System;
using System.Web;
namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(HttpUtility.HtmlAttributeEncode("<p/>"));
            Console.ReadKey();
        }
    }
}
{% endhighlight %}

The call to HttpUtility.HtmlAttributeEncode produces the result &lt;p/&gt; instead of &lt;p/&gt;. This is a problem in terms of putting encoded XML in a HTML attribute. It is up to the HTML parser to determine how to fix up the corrupted HTML.

For example, encode "&lt;p/&gt;" and put it into a hidden field and you then get HTML like the following:

{% highlight xml %}
<form>
    <input type="hidden" name="DataSet" value="<p/>" />
    <input type="submit" name="Submit" />
</form>
{% endhighlight %}

The interpretation of this HTML could either implicitly consider the &gt; in the attribute as &gt; or add in an implicit " before the literal &gt; to close out the attribute. The first outcome would be correct HTML and the second would continue to produce incorrect HTML. The big problem here is an assumption that all interpreters of HTML are going to make the same decision.

The workaround is to use a custom HttpEncoder that will run the current encoding logic, then fix up the encoding of the &gt; character.

{% highlight csharp %}
public class TagHttpEncoder : HttpEncoder
{
    /// <inheritdoc />
    protected override void HtmlAttributeEncode(string value, TextWriter output)
    {
        var firstWriter = new StringWriter(CultureInfo.InvariantCulture);
    
        base.HtmlAttributeEncode(value, firstWriter);
    
        var encoded = firstWriter.ToString();
    
        var fixedEncoding = encoded.Replace(">", ">");
    
        output.Write(fixedEncoding);
    }
}
{% endhighlight %}

This encoder can then be hooked up to ASP.Net in Global.asax.

{% highlight csharp %}
protected void Application_Start(object sender, EventArgs e)
{
    HttpEncoder.Current = new TagHttpEncoder();
}
{% endhighlight %}

I’ve raised this bug on [Connect][0].

[0]: https://connect.microsoft.com/VisualStudio/feedback/details/802421/httputility-htmlattributeencode-does-not-encode-characters#
