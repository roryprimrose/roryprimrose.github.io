---
title: Neovolve.Schema.Extensions 1.0 released
categories : .Net, IT Related, My Software
tags : ASP.Net, Schema Extensions
date: 2006-10-24 16:25:00 +10:00
---

Neovolve.Schema.Extensions is a project that will do entity mapping for web reference code generations from a WSDL. 

When you update a web reference in the Visual Studio IDE, it will get the latest version of the WSDL and generate code to access the web service. As part of this process, it will generate any object types that are exposed by the web service. If you have access to those object definitions on the consumer end point, you will have problems with these object types because the code generated Reference.cs class will use it's own code generated versions of your object types rather than the ones you really want to use. 

The schema extensions get around this problem. When the IDE updates a web reference, it will check against the schema extensions to ask whether the extension understands the object type. The extension has the opportunity to return a different object type, include namespaces and also include assembly references. 

This project is configuration driven so that when a web service changes, the configuration can be changed to support the new entity mappings. 

After the package is installed, add your object mappings to the configuration file. The configuration for the mappings looks like this:

{% highlight xml linenos %}
<MapperItem key="MyService.XmlNodeKey"
            xmlName="XmlNode"
            xmlNamespace="http://www.myservice.com/project" />
    
<MapperItem key="MyService.XmlNodeCollectionKey"
            xmlName="ArrayOfXmlNode"
            xmlNamespace="http://www.myservice.com/project"
            name="List<XmlNode>">
    <AssemblyDependencies>
        <AssemblyDependency assemblyName="SomeAssembly.dll" />
        <AssemblyDependency assemblyName="Another.dll" />
    </AssemblyDependencies>
    <NamespaceDependencies>
        <NamespaceDependency namespace="System.Collections.Generic" />
        <NamespaceDependency namespace="System.Xml" />
    </NamespaceDependencies>
</MapperItem>
{% endhighlight %}

Check the log file for the project (found through the start menu) to identify the object types that have not been successfully mapped when web references are updated (assuming logging is enabled in the configuration file). The start menu also has a shortcut to the schema for the configuration file. Because the IDE doesn't restart when a configuration file changes, the IDE must be restarted before changes to the configuration will be reflected in updates to web references. 

Download: [Neovolve.Schema.Extensions.Setup.msi (485.50 kb)][0]

[0]: /files/2008/9/Neovolve.Schema.Extensions.Setup.msi
