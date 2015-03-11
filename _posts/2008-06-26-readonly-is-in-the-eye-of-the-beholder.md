---
title: Readonly is in the eye of the beholder
categories : .Net
date: 2008-06-26 11:11:04 +10:00
---

One of my most used feature in the 3.5 compiler for .Net is automatic properties. In case you are not familiar, here is a recap.

Traditionally, properties work with a backing field. For example:

<!--more-->

{% highlight csharp %}
private String _someValue;
    
public String SomeValue
{
    get
    {
        return _someValue;
    }
    set
    {
        _someValue = value;
    }
}
{% endhighlight %}

Automatic properties under the 3.5 compiler allow this code to be represented as:

{% highlight csharp %}
public String SomeValue
{
    get;
    set;
}
{% endhighlight %}

Automatic properties make code cleaner and easier to use. Additionally, they also require less testing. There isn't any point testing their logic of accessing the backing field to ensure correct operation and they appear with 100% code coverage. My biggest gripe though is that they don't truly support readonly definitions.

The [documentation on MSDN][0] says:

> _Auto-implemented properties must declare both a get and a set accessor. To create a [readonly][1] auto-implemented property, give it a [private][2] set accessor._

The implementation of automatic properties by the compiler define that readonly automatic properties are achieved by assigning a private scope to the set accessor. For example:

{% highlight csharp %}
public String SomeValue
{
    get;
    private set;
}
{% endhighlight %}

Unfortunately, I think this is a cheap bandaid hack. It doesn't make the property truly readonly. It is technically readonly, but only from the perspective of external callers. Internal to the class, the property is not readonly because the private scope makes setting the property value possible. This doesn't protect the value of the backing field which is what a true readonly property should be able to support. See [here][3] for the MSDN documentation on the readonly keyword.

I would like to see this changed. Without understanding the complexities of writing compilers, I would think that this change would not be too significant. The simple fix would be to enhance the syntax of the automatic property setter definition. If the private set statement could be enhanced to understand a [readonly keyword][3], then the compile could mark the backing field with the same readonly keyword. For example:

{% highlight csharp %}
public String SomeValue
{
    get;
    private readonly set;
}
{% endhighlight %}

This would then be interpreted by the compiler as something like this:

{% highlight csharp %}
private readonly String _someValue;
    
public String SomeValue
{
    get
    {
        return _someValue;
    }
    set
    {
        _someValue = value;
    }
}
{% endhighlight %}

I have added this as a suggestion to Microsoft Connect [here][4].

[0]: http://msdn.microsoft.com/en-us/library/bb384054.aspx
[1]: http://msdn.microsoft.com/en-us/library/acdd6hb7.aspx
[2]: http://msdn.microsoft.com/en-us/library/st6sy9xe.aspx
[3]: http://msdn.microsoft.com/en-us/library/acdd6hb7(VS.80).aspx
[4]: https://connect.microsoft.com/VisualStudio/feedback/ViewFeedback.aspx?FeedbackID=353059
