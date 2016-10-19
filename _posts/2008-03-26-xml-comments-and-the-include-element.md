---
title: Xml comments and the include element
categories: .Net, Applications
tags: Sandcastle, WCF
date: 2008-03-26 23:57:47 +10:00
---

I have been doing a lot of work over the last week writing xml comment documentation. I have been compiling the xml output into chm files using SandCastle, SHFB and my own SHFB wrapper application. I have been increasingly been finding that I am writing remarks that I want to reuse across different methods and properties of several classes in an assembly. Today, I had a particular property scattered among several data contracts for a WCF service that are used for the same purpose and have the same xml comments.

For several days I have been doing the very bad practice of writing the documentation and copying what I need to the other locations. I remembered reading about the &lt;include /&gt; element for xml comments and read up on it in more detail (see [here][0] and [here][1]). After a bit of experimentation today, I can say that the &lt;include /&gt; element is very powerful for three reasons:

1. You can include the entire xml comment for an item
1. You can include part of the xml comment for an item
1. You can include include elements and they will be recursively resolved

Lets look some examples. Here is my initial code:

<!--more-->

{% highlight csharp %}
using System.Diagnostics;
     
namespace ClassLibrary1
{
    public class Class1
    {
        /// <summary>
        /// Does something random.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is a common remark.
        /// </para>
        /// </remarks>
        public void SomeRandomMethod()
        {
            Debug.WriteLine("Do something.");    
        }
     
        /// <summary>
        /// Generates a checksum value.
        /// </summary>
        /// <param name="someData">Some data.</param>
        /// <returns>A byte array.</returns>
        /// <remarks>
        /// <para>
        /// This is a paragraph about design considerations related to checksum generation.
        /// </para>
        /// <para>
        /// This is a paragraph about some general information about checksum values.
        /// Lets imagine that this is a really long paragraph that...
        /// </para>
        /// <para>
        /// extends for many
        /// </para>
        /// <para>
        /// many paragraphs.
        /// </para>
        /// <para>
        /// This is a common remark.
        /// </para>
        /// </remarks>
        public byte[] GenerateChecksum(byte[] someData)
        {
            Debug.WriteLine("Generate a checksum using the data supplied.");
     
            return null;
        }
     
        /// <summary>
        /// Gets or sets the checksum.
        /// </summary>
        /// <value>The checksum.</value>
        /// <remarks>
        /// <para>
        /// This is a paragraph about some general information about checksum values.
        /// Lets imagine that this is a really long paragraph that...
        /// </para>
        /// <para>
        /// extends for many
        /// </para>
        /// <para>
        /// many paragraphs.
        /// </para>
        /// </remarks>
        public byte[] Checksum
        {
            get;
            set;
        }
    }
     
    public class Class2
    {
        /// <summary>
        /// Gets or sets the checksum.
        /// </summary>
        /// <value>The checksum.</value>
        /// <remarks>
        /// <para>
        /// This is a paragraph about some general information about checksum values.
        /// Lets imagine that this is a really long paragraph that...
        /// </para>
        /// <para>
        /// extends for many
        /// </para>
        /// <para>
        /// many paragraphs.
        /// </para>
        /// </remarks>
        public byte[] Checksum
        {
            get;
            set;
        }
    }
}
{% endhighlight %}

There is some duplication here that we can take care of with &lt;include /&gt;.

## Reason 1: You can include the entire xml comment for an item

To cover the first reason above, there is duplication of the comment for the Checksum properties in the two classes. The documentation in its entirety is the same. Excellent candidate for pushing out into a common xml file. Add an xml file to the project called something like CommonDocumentation.xml and come up with an appropriate schema. I put together something like this:

{% highlight xml %}
<?xml version="1.0" encoding="utf-8" ?>
<CommonDocumentation>
    <Properties>
    <Property name="Checksum">
        <summary>
        Gets or sets the checksum.
        </summary>
        <value>The checksum.</value>
        <remarks>
        <para>
            This is a paragraph about some general information about checksum values.
            Lets imagine that this is a really long paragraph that...
        </para>
        <para>
            extends for many
        </para>
        <para>
            many paragraphs.
        </para>
        </remarks>
    </Property>
    </Properties>
</CommonDocumentation>
{% endhighlight %}

We can now update each of those properties to something like this:

{% highlight xml %}
<?xml version="1.0" encoding="utf-8" ?>
<CommonDocumentation>
    <Properties>
    <Property name="Checksum">
        <summary>
        Gets or sets the checksum.
        </summary>
        <value>The checksum.</value>
        <remarks>
        <para>
            This is a paragraph about some general information about checksum values.
            Lets imagine that this is a really long paragraph that...
        </para>
        <para>
            extends for many
        </para>
        <para>
            many paragraphs.
        </para>
        </remarks>
    </Property>
    </Properties>
</CommonDocumentation>    
{% endhighlight %}

 **Reason 2: You can include part of the xml comment for an item**

You can mix &lt;include /&gt; elements with other xml comments on the same item. This means you are not constrained to having the entire xml comment pushed out to an external file. If there is partial content being duplicated, this works too. Let's update the common documentation file to include the common remarks.

{% highlight xml %}
<?xml version="1.0" encoding="utf-8" ?>
<CommonDocumentation>
    <Properties>
    <Property name="Checksum">
        <summary>
        Gets or sets the checksum.
        </summary>
        <value>The checksum.</value>
        <remarks>
        <para>
            This is a paragraph about some general information about checksum values.
            Lets imagine that this is a really long paragraph that...
        </para>
        <para>
            extends for many
        </para>
        <para>
            many paragraphs.
        </para>
        </remarks>
    </Property>
    </Properties>
    <Remarks>
    <Remark name="CommonRemark">
        <para>
        This is a common remark.
        </para>
    </Remark>
    </Remarks>
</CommonDocumentation>    
{% endhighlight %}

We can now update the SomeRandomMethod() comments to be the following:

{% highlight csharp %}
/// <summary>
/// Does something random.
/// </summary>
/// <remarks>
/// <include file="CommonDocumentation.xml" path="CommonDocumentation/Remarks/Remark[@name='CommonRemark']/*" />
/// </remarks>
public void SomeRandomMethod()
{
    Debug.WriteLine("Do something.");
}
{% endhighlight %}

## Reason 3: You can include include elements and they will be recursively resolved

This is where things get really useful. Now that we have pushed out common documentation to an external file, what if there is duplication within that file. No problem. The compiler will recursively resolve all &lt;include /&gt; elements, even if the xml comment itself is in the external file.

In the examples so far, there is an extensive amount of comments in the remarks section of several properties and methods. We have already moved the Checksum comments out to the external file, but we can also remove duplication in the remarks of the GenerateChecksum method which is also common to the Checksum properties. We can update the external xml file to include the duplicate remarks about the checksum and use the same include element against the GenerateChecksum method.

The final xml file is:

{% highlight xml %}
<?xml version="1.0" encoding="utf-8" ?>
<CommonDocumentation>
    <Properties>
    <Property name="Checksum">
        <summary>
        Gets or sets the checksum.
        </summary>
        <value>The checksum.</value>
        <remarks>
        <include file="CommonDocumentation.xml"
                path="CommonDocumentation/Remarks/Remark[@name='ChecksumDescription']/*" />
        </remarks>
    </Property>
    </Properties>
    <Remarks>
    <Remark name="CommonRemark">
        <para>
        This is a common remark.
        </para>
    </Remark>
    <Remark name="ChecksumDescription">
        <para>
        This is a paragraph about some general information about checksum values.
        Lets imagine that this is a really long paragraph that...
        </para>
        <para>
        extends for many
        </para>
        <para>
        many paragraphs.
        </para>
    </Remark>
    </Remarks>
</CommonDocumentation>    
{% endhighlight %}

The final class file is:

{% highlight csharp %}
using System.Diagnostics;
     
namespace ClassLibrary1
{
    public class Class1
    {
        /// <summary>
        /// Does something random.
        /// </summary>
        /// <remarks>
        /// <include file="CommonDocumentation.xml" path="CommonDocumentation/Remarks/Remark[@name='CommonRemark']/*" />
        /// </remarks>
        public void SomeRandomMethod()
        {
            Debug.WriteLine("Do something.");    
        }
     
        /// <summary>
        /// Generates a checksum value.
        /// </summary>
        /// <param name="someData">Some data.</param>
        /// <returns>A byte array.</returns>
        /// <remarks>
        /// <include file="CommonDocumentation.xml" path="CommonDocumentation/Remarks/Remark[@name='ChecksumDescription']/*" />
        /// <include file="CommonDocumentation.xml" path="CommonDocumentation/Remarks/Remark[@name='CommonRemark']/*" />
        /// </remarks>
        public byte[] GenerateChecksum(byte[] someData)
        {
            Debug.WriteLine("Generate a checksum using the data supplied.");
     
            return null;
        }
     
        /// <include file="CommonDocumentation.xml" path="CommonDocumentation/Properties/Property[@name='Checksum']/*" />
        public byte[] Checksum
        {
            get;
            set;
        }
    }
     
    public class Class2
    {
        /// <include file="CommonDocumentation.xml" path="CommonDocumentation/Properties/Property[@name='Checksum']/*" />
        public byte[] Checksum
        {
            get;
            set;
        }
    }
}    
{% endhighlight %}

## Issues

Now, there are some hazards with this. 

The first mistake I made was not putting /* at the end of the xpath queries of the &lt;include /&gt; element. When the compiler runs, it processes the include elements by running the xpath query against the file specified and replaces the include element with the xml elements found. If you don't include /*, the only element found is the container of the external comments, not the comments themselves. This is guaranteed to break your compiled xml file.

The second issue is that because the external xml data is being injected into your file, if the external xml comments refer to a type that exists in a namespace that is not referenced in the class, then the compiler will throw errors. For example, I added the comment _&lt;see cref=&quot;SHA1CryptoServiceProvider&quot;/&gt;_ into the external file. When I recompiled, I got the following error (BTW, I treat warnings as errors):

> Error    2    Warning as Error: XML comment on 'ClassLibrary1.Class1.GenerateChecksum(byte[])' has cref attribute 'SHA1CryptoServiceProvider' that could not be resolved    C:\Users\[Account]\AppData\Local\Temporary Projects\ClassLibrary1\Class1.cs    18    13    ClassLibrary1

The third issue is the complexity of the external xml file. The more include elements you use for the sake of maintainability due to removing duplication, the less maintainable your comments become. So use wisely.

Enjoy.

[0]: http://msdn2.microsoft.com/en-us/library/5ast78ax(VS.71).aspx
[1]: http://msdn2.microsoft.com/en-us/library/9h8dy30z(VS.71).aspx
