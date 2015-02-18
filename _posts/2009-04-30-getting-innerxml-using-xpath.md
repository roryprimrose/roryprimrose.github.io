---
title: Getting InnerXML using XPath
categories : .Net
date: 2009-04-30 13:33:05 +10:00
---

I previously posted about [XML comments and the include element][0] for documenting code in Visual Studio. I commented against that post that xpath queries should end in /*. This tells the query to select all child elements of the xml node identified. Unfortunately this does not cover all scenarios where external xml can be included in xml documentation.

Take the following xml for example.

{% highlight xml linenos %}<?xml version="1.0" encoding="utf-8"?&gt; <CommonDocumentation&gt; <Remarks&gt; <Remark name="FirstRemark"&gt; <para&gt; This is a remark in a paragraph. </para&gt; </Remark&gt; <Remark name="SecondRemark"&gt; This is a remark with just text. </Remark&gt; <Remark name="ThirdRemark"&gt; This is a remark with text and <b&gt;xml elements</b&gt;. </Remark&gt; </Remarks&gt; </CommonDocumentation&gt; {% endhighlight %}

This xml is referenced by the include statements in the code below.

{% highlight csharp linenos %}using System.Diagnostics; namespace ClassLibrary1 { public class Class1 { /// <summary&gt; /// Does something random. /// </summary&gt; /// <remarks&gt; /// <include file="CommonDocumentation.xml" path="CommonDocumentation/Remarks/Remark[@name='FirstRemark']/*" /&gt; /// <include file="CommonDocumentation.xml" path="CommonDocumentation/Remarks/Remark[@name='SecondRemark']/text()" /&gt; /// <include file="CommonDocumentation.xml" path="CommonDocumentation/Remarks/Remark[@name='ThirdRemark']/node()" /&gt; /// </remarks&gt; public void SomeRandomMethod() { Debug.WriteLine("Do something."); } } } {% endhighlight %}

The code indicates the way in which the xpath queries will be able to correctly get the inner xml of the common documentation. Using a /* query against the SecondRemark or ThirdRemark nodes will produce incorrect results as the text will be ignored because /* only select element nodes. Using /text() against the FirstRemark and ThirdRemark nodes will also produce incorrect values as it will only select text nodes and ignore the element nodes.

The best solution is to use xpath queries that end in /node(). This will successfully select the inner xml of the target node regardless of the makeup of that xml. The xml can contain text, element nodes or a mixture.

[0]: post/2008/03/26/xml-comments-and-the-include-element.aspx
