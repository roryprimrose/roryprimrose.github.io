---
title: Law Of Instance Ownership
categories : .Net, Software Design
tags : Unity
date: 2010-06-15 10:41:53 +10:00
---

I’ve been writing a custom Unity extension for disposing build trees when a container is asked to tear down an instance. This has brought up some interesting ideas about the conditions in which an instance should be destroyed. This has lead to me come up with the Law of Instance Ownership.

**Law of Instance Ownership**

> _An instance is owned by the highest stack frame that holds a direct reference to an instance, or the application domain for globally held instances._

A common misconception is that the member that creates an instance is responsible for its lifetime management. The scenario that quickly breaks this idea is when a member returns an instance that requires lifetime management (such as IDisposable instance). In this case, the method that created the instance can’t destroy it because its usage is outside the scope of the member that created it. 

Take System.IO.File.Open() method for example.{% highlight csharp linenos %}
public static FileStream Open(string path, FileMode mode, FileAccess access, FileShare share)
{
    return new FileStream(path, mode, access, share);
}
{% endhighlight %}

This method returns a Stream which must be disposed when it is no longer required. While the File.Open method created the stream instance, it is up to the member that owns the stream to dispose of it.

What about instances that require lifetime management that are stored as fields on a type? 

The impact here is that the class that defines the field (the owning class) will itself require lifetime management. For disposable types this will mean that the owning class will need to implement IDisposable. The lifetime management of the owning class is then the responsibility of the member that holds an instance of it. When that member disposes the owning class, the owning class will in turn dispose of its field instance.

This law should be used to determine who is responsible for lifetime management of an object instance. Only the owner of the instance should be responsible for handling this lifetime management.


