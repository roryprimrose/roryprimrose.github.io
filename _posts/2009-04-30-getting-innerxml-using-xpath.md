---
title: Getting InnerXML using XPath
categories: .Net
date: 2009-04-30 13:33:05 +10:00
---

I previously posted about [XML comments and the include element][0] for documenting code in Visual Studio. I commented against that post that xpath queries should end in /*. This tells the query to select all child elements of the xml node identified. Unfortunately this does not cover all scenarios where external xml can be included in xml documentation.

Take the following xml for example.

<!--more-->

```xml
<?xml version="1.0" encoding="utf-8"?>
<CommonDocumentation>
    <Remarks>
    <Remark name="FirstRemark">
        <para>
        This is a remark in a paragraph.
        </para>
    </Remark>
    <Remark name="SecondRemark">
        This is a remark with just text.
    </Remark>
    <Remark name="ThirdRemark">
        This is a remark with text and <b>xml elements</b>.
    </Remark>
    </Remarks>
</CommonDocumentation>    
```

This xml is referenced by the include statements in the code below.

```csharp
using System.Diagnostics;
     
namespace ClassLibrary1
{
    public class Class1
    {
        /// <summary>
        /// Does something random.
        /// </summary>
        /// <remarks>
        /// <include file="CommonDocumentation.xml" path="CommonDocumentation/Remarks/Remark[@name='FirstRemark']/*" />
        /// <include file="CommonDocumentation.xml" path="CommonDocumentation/Remarks/Remark[@name='SecondRemark']/text()" />
        /// <include file="CommonDocumentation.xml" path="CommonDocumentation/Remarks/Remark[@name='ThirdRemark']/node()" />
        /// </remarks>
        public void SomeRandomMethod()
        {
            Debug.WriteLine("Do something.");    
        }
    }
}    
```

The code indicates the way in which the xpath queries will be able to correctly get the inner xml of the common documentation. Using a /* query against the SecondRemark or ThirdRemark nodes will produce incorrect results as the text will be ignored because /* only select element nodes. Using /text() against the FirstRemark and ThirdRemark nodes will also produce incorrect values as it will only select text nodes and ignore the element nodes.

The best solution is to use xpath queries that end in /node(). This will successfully select the inner xml of the target node regardless of the makeup of that xml. The xml can contain text, element nodes or a mixture.

[0]: /2008/03/26/xml-comments-and-the-include-element/
