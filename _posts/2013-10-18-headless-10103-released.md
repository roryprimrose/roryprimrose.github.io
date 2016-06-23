---
title: Headless 1.0.103 Released
categories: .Net, My Software
date: 2013-10-18 23:16:19 +10:00
---

I’m super excited to announce the first release of Headless, a HTTP acceptance testing framework. It makes GET and POST calls and provides a framework for interacting with HTML pages. Oh, and it is super fast.

I have completed converting the acceptance test suite an enterprise web application from WatiN to Headless. The test suite has around 360 tests of which only about 6 could not be converted to Headless. The execution time of the test suite dropped from 1:04 hours down to just over 7 minutes. Yeah, it’s fast.

<!--more-->

The speed of Headless over WatiN or Microsoft’s CUIT is achieved because it doesn’t use an actual browser. It simulates the actions of a browser for elements like buttons and links. This also has a side benefit that you can execute the acceptance tests on a build server without requiring an interactive process and you can continue to work while your test suite is executing. This does however mean that it does not execute JavaScript or download other resources like CSS and images.

Some of the features of Headless are:

* HTML element inspection
* Forms support
* Hyperlink support
* Manual, page or dynamic programming models
* Location and status code validation
* Extensible

**Examples**

Headless can use a page model class that describes the model, location and behaviour of a page.

{% highlight csharp %}
using (var browser = new Browser())
{
    var homePage = browser.GoTo<HomePage>();
    
    var contactPage = homePage.Contact.Click<ContactPage>();
    
    contactPage.Email.Value = "my@address.com";
    contactPage.Name.Value = "My Name";
    contactPage.Comment.Value = "Hi there!";
    
    var thankYouPage = contactPage.Submit.Click<ThankYouPage>();
}
{% endhighlight %}

It also supports the dynamic keyword if you don’t want to use a model. You can also jump between these two models if that suits.

{% highlight csharp %}
using (var browser = new Browser())
{
    var homePage = browser.GoTo(new Uri("http://mysite"));
    
    var contactPage = homePage.Contact.Click();
    
    contactPage.Email.Value = "my@address.com";
    contactPage.Name.Value = "My Name";
    contactPage.Comment.Value = "Hi there!";
    
    var thankYouPage = contactPage.Submit.Click();
}
{% endhighlight %}

Headless is open source on GitHub at [https://github.com/roryprimrose/Headless][0] and you can get it from NuGet at [http://www.nuget.org/packages/Headless][1]. 

The NuGet package comes with a CHM for the API documentation and xmldoc for intellisense. Further information is available on the wiki at [https://github.com/roryprimrose/Headless/wiki][2] and the API documentation will hopefully be available soon on [http://www.nudoq.org/#!/Headless][3]. 

[0]: https://github.com/roryprimrose/Headless
[1]: http://www.nuget.org/packages/Headless
[2]: https://github.com/roryprimrose/Headless/wiki
[3]: http://www.nudoq.org/#!/Headless
