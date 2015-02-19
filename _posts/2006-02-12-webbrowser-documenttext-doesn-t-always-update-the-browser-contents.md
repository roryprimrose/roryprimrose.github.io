---
title: WebBrowser.DocumentText doesn't always update the browser contents
categories : .Net, IT Related
date: 2006-02-12 05:23:00 +10:00
---

I have been developing some new software which uses the WebBrowser control. Depending on what is selected in a TreeView control, the browser will be instructed to load some HTML that has been created and manipulated in memory. 

The problem I have encountered is that once the HTML has been determined, setting the DocumentText property hasn't been updating what is displayed in the browser. It only displays the HTML that was orginally assigned to the control. The reason for this not working is hinted at in the help documentation for the Document Text property. The documentation says this:

> _Use this property when you want to manipulate the contents of an HTML page displayed in the WebBrowser control using string processing tools. You can use this property, for example, to load pages from a database or to analyze pages using regular expressions. When you set this property, the WebBrowser control automatically navigates to the about:blank URL before loading the specified text. This means that the Navigating, Navigated, and DocumentCompleted events occur when you set this property, and the value of the Url property is no longer meaningful._

For my application, I have set up the WebBrowser control to be as restricted as possible because I don't want my application to look like a browser. For example, I don't want the browsers context menu to be displayed when the user right-clicks on it. One of the properties I changed was to set AllowNavigation = false. The help description for the AllowNavigation property says this:

> _Gets or sets a value indicating whether the control can navigate to another page after its initial page has been loaded._

If you fire up reflector and find the WebBrowser.DocumentText property you will see that internally, setting the WebBrowser.DocumentText property will create a MemoryStream with the new text value and then the stream is assigned to the WebBrowser.DocumentStream property. Reflector shows the DocumentStream property code to be this:

 {% highlight csharp linenos %}
public Stream DocumentStream
{
    get
    {
        HtmlDocument document1 = this.Document;
        if (document1 == null)
        {
                return null;
        }
        UnsafeNativeMethods.IPersistStreamInit init1 = document1.DomDocument as UnsafeNativeMethods.IPersistStreamInit;
        if (init1 == null)
        {
                return null;
        }
        MemoryStream stream1 = new MemoryStream();
        UnsafeNativeMethods.IStream stream2 = new UnsafeNativeMethods.ComStreamFromDataStream(stream1);
        init1.Save(stream2, false);
        return new MemoryStream(stream1.GetBuffer(), 0, (int) stream1.Length, false);
    }
    set
    {
        this.documentStreamToSetOnLoad = value;
        try
        {
                this.webBrowserState[2] = true;
                this.Url = new Uri("about:blank");
        }
        finally
        {
                this.webBrowserState[2] = false;
        }
    }
}
{% endhighlight %}

When a new stream is assigned to DocumentStream, it will attempt to navigate to about:blank as the documentation says. The code here indicates that the stream data will be populated into the browser after it has navigated to about:blank. The hitch here is that if the navigation fails, you are none the wiser that the process has failed and the new document text won't be loaded. My problem with the code here is that the try block around the navigate swallows the exception, hence the document content doesn't change and you won't know why.

Basically, setting AllowNavigation = false cripples the DocumentText and DocumentStream properties as well as the navigation methods.


