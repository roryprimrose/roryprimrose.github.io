---
title: Testing IErrorHandler
categories : .Net
tags : Unit Testing, WCF
date: 2009-02-19 16:00:00 +10:00
---

I have previously posted ([here][0], [here][1] and [here][2]) about using IErrorHandler to provide error handling and exception shielding in WCF services. What I haven't discussed is how to test an implementation of this interface. 

The reason for posting this is that I recently found that I had a bug in a service where un-handled exceptions weren't being shielded from clients. This was purely because my unit tests were not validating the messages being generated for clients by the error handler.

The following method came about after a bit of research (mainly from [here][3]) and playing with code to make the solution work and easy to use. This method will assist unit testing the output of ProvideFault as it provides an easy way to extract a Fault from a Message returned by the ProvideFault method. This fault can then be tested for expected outcomes of the unit test.

{% highlight csharp linenos %}private static T ReadFaultDetail<T&gt;(Message reply) where T : class { const String DetailElementName = "Detail"; using (XmlDictionaryReader reader = reply.GetReaderAtBodyContents()) { // Find the <soap:Detail&gt; element while (reader.Read()) { if (reader.NodeType == XmlNodeType.Element && reader.LocalName == DetailElementName) { break; } } // Check that the reader is at the detail element now that we are outside the loop if (reader.NodeType != XmlNodeType.Element || reader.LocalName != DetailElementName) { return null; } // Read again to move the reader into the contents of the details element if (!reader.Read()) { return null; } // Deserialize the fault DataContractSerializer serializer = new DataContractSerializer(typeof(T)); return serializer.ReadObject(reader) as T; } }{% endhighlight %}

[0]: /page/WCF-service-contract-design.aspx
[1]: /post/2008/04/07/implementing-ierrorhandler.aspx
[2]: /post/2008/11/07/Strict-IErrorHandler-usage.aspx
[3]: http://www.olegsych.com/2008/07/simplifying-wcf-using-exceptions-as-faults/
