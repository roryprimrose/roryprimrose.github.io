---
title: Example of coding for testability
categories : .Net, Software Design
tags : BlogEngine.Net, Dependency Injection, Unit Testing
date: 2008-08-28 12:40:41 +10:00
---

I have done it again. In order to work on a particular project, I have been sidetracked into writing a utility to help me continue with what I am actually trying to do. In my defense, I did look around the net for an application that would do what I needed so I didn't have to write it, but no application seemed appropriate.

In essence, I need an application to resolve all the links internal to a website and check their status. This will give me an initial view of the state of the site. I then want to take all of those links and replay the analysis of those resolved links using a different base address. The reason I am doing this is because I am looking at migrating my [CS] based blog to use [BE] instead. As part of that migration, I want to maintain as much of the [CS] url formats as possible so I don't lose my existing audience.

Given that this application is going to chew a lot of bandwidth (my entire site) along with the need for accurate results, I want to make sure that this utility is doing the right thing. Unit testing is critical for this to be successful. I quickly found however that my initial cut is not very testable.

Here is a simple example of how to take untestable code and make it testable. In the following examples, there is some logic in the ResourceResolver.GetResourceContents method that we want to test.

**The untestable example**

    {% highlight csharp linenos %}
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
     
    namespace ConsoleApplication1
    {
        internal class Program
        {
            private static void Main(String[] args)
            {
                ResourceResolver resolver = new ResourceResolver();
                Uri location = new Uri("http://localhost");
                String content = resolver.GetResourceContents(location);
     
                Debug.WriteLine(content);
            }
        }
     
        internal class ResourceResolver
        {
            public String GetResourceContents(Uri location)
            {
                HttpWebRequest request = HttpWebRequest.Create(location) as HttpWebRequest;
     
                request.Credentials = CredentialCache.DefaultCredentials;
     
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
     
                using (Stream contentStream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(contentStream))
                    {
                        String content = reader.ReadToEnd();
     
                        // Do something with this value 
     
     
                        return content;
                    }
                }
            }
        }
    }
    {% endhighlight %}

This is untestable as a unit test. To write a test for this code will require an integration test as requests are being made to an external resource (http in this case). What if that resource is not available or produces unknown or unmanageable results? Unit testing normally requires more flexibility as the same code paths need different data thrown at them.

This code is a classic example. The HttpWebRequest class is not easily testable. Converting this code so that mocks and stubs can be used will allow unit tests to be supported.

**The testable example**

    {% highlight csharp linenos %}
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
     
    namespace ConsoleApplication1
    {
        internal class Program
        {
            private static void Main(String[] args)
            {
                HttpResourceLoader loader = new HttpResourceLoader();
                ResourceResolver resolver = new ResourceResolver(loader);
                Uri location = new Uri("http://localhost");
                String resourceContent = resolver.GetResourceContents(location);
     
                Debug.WriteLine(resourceContent);
            }
        }
     
        public interface IResourceLoader
        {
            String GetResourceContents(Uri location);
        }
     
        public class HttpResourceLoader : IResourceLoader
        {
            public String GetResourceContents(Uri location)
            {
                HttpWebRequest request = WebRequest.Create(location) as HttpWebRequest;
     
                request.Credentials = CredentialCache.DefaultCredentials;
     
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
     
                using (Stream contentStream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(contentStream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }
     
        internal class ResourceResolver
        {
            public ResourceResolver(IResourceLoader loader)
            {
                Loader = loader;
            }
     
            public String GetResourceContents(Uri location)
            {
                String content = Loader.GetResourceContents(location);
     
                // Do something with this value
     
                return content;
            }
     
            private IResourceLoader Loader
            {
                get;
                set;
            }
        }
    }
    {% endhighlight %}

By abstracting an implementation that actually gets the resource contents (with a bit of [DI] thrown in), we now have a ResourceResolver.GetResourceContents method that can be unit tested. The unit testing involved now needs to pass in either a stub or a mocked instance of IResourceLoader and we can safely test the logic of this method without requiring http requests.

When you develop code, please think about how it is going to be tested. Even better, write unit tests when you develop the code. You will quickly find out how flexible your code is for unit testing.


