---
title: Using GhostDoc to document unit test methods
categories: .Net, Applications
date: 2009-03-18 11:15:00 +10:00
---

GhostDoc is one of the coolest tools around for Visual Studio. Sometimes it needs a little help though and unit test methods are a classic example. The format of my unit test method names usually identifies the method name, parameter list, parameter value conditions, expected outcome and end in "Test". The default rule that GhostDoc applies to this format is fairly ugly.

For example,

<!--more-->

{% highlight csharp %}
/// <summary>
/// Creates the returns cache store instance test.
/// </summary>
[TestMethod] 
[Description("Black box tests")]
public void CreateReturnsCacheStoreInstanceTest()
{
    ICacheStore actual = CacheStoreFactory.Create();
     
    Assert.IsNotNull(actual, "Create failed to return an instance");
}    
{% endhighlight %}

Creating a custom GhostDoc rule for unit test methods can assist in cleaning the documentation up a little.

First, open up the GhostDoc configuration.![GhostDoc configuration][0]

Under methods, click Add.![GhostDoc configuration dialog][1]

Click Ok.![Add Rule dialog][2]

Enter the new rule name, identify that the method name must end in "_Test_" and fill out the summary using the value "_Runs test for $(MethodName.Words.ExceptLast)._".![Edit Rule dialog][3]

Using this new rule, the example above now gets the following documentation generated.

{% highlight csharp %}
/// <summary>
/// Runs test for create returns cache store instance.
/// </summary>
[TestMethod]
[Description("Black box tests")]
public void CreateReturnsCacheStoreInstanceTest()
{
    ICacheStore actual = CacheStoreFactory.Create();
     
    Assert.IsNotNull(actual, "Create failed to return an instance");
}
{% endhighlight %}

It's not perfect, but its a lot better.

[0]: /files/WindowsLiveWriter/UsingGhostDoctodocumentUnitTestmethods_992E/image_9.png
[1]: /files/WindowsLiveWriter/UsingGhostDoctodocumentUnitTestmethods_992E/image_12.png
[2]: /files/WindowsLiveWriter/UsingGhostDoctodocumentUnitTestmethods_992E/image_8.png
[3]: /files/WindowsLiveWriter/UsingGhostDoctodocumentUnitTestmethods_992E/image_15.png
