---
title: Generic Func IEqualityComparer
categories: .Net
date: 2014-05-22 06:35:00 +10:00
---

Developers generally like the LINQ syntax with all its lambda goodness. It is fluent and easy to write. Then you do something like dataSet.Intersect(otherData, OHNO!).

Signatures like the LINQ Intersect function seems to just get in the way of productive development. With so many things in a lambda syntax, we are now forced back into the world of IEqualityComparer. The easy fix is to drop in something like a generic equality comparer that will support a Func.

<!--more-->

{% highlight csharp %}
public class PredicateComparer<T> : IEqualityComparer<T>
{
    private readonly Func<T, T, bool> _comparer;
    
    public PredicateComparer(Func<T, T, bool> comparer)
    {
        _comparer = comparer;
    }
    
    public bool Equals(T x, T y)
    {
        return _comparer(x, y);
    }
    
    public int GetHashCode(T obj)
    {
        // We don't want to use hash code comparison
        // Return zero to force usage of Equals
        return 0;
    }
}    
{% endhighlight %}

This little helper doesn’t totally fix the syntax problem, but does limit how big your coding speed bumps are. For example:

{% highlight csharp %}
var matchingEntities = allEntities.Intersect(
    subsetOfEntities,
    new PredicateComparer<MyEntityType>((x, y) => x.Id == y.Id));
{% endhighlight %}

Easy.


