---
title: Locking/Thread Synchronization Performance Question
categories : .Net
tags : Performance
date: 2007-12-19 12:48:00 +10:00
---

Here is my scenario:

I have a static method which checks some static generic dictionaries to determine whether keys exist. If keys don't exist, they will get added along with a value.

Rough &quot;metrics&quot; will be:

* the static method will get called a lot
* items will rarely get added to the dictionary
* items are never removed from the dictionary
* the dictionary will only contain a few items
* as many times as the method is called, the dictionary will be referenced using the ContainsKey property (and the indexer property where ContainsKey == true)

In order to ensure thread safety of adding a new item to the dictionary, a lock is required. Two options I see are as follows:

**#1 - This is what people would normally do**

{% highlight csharp linenos %}
lock (MyDictionary)
{
    // Check if a value needs to be stored
    if (MyDictionary.ContainsKey(myKey) == false)
    {
        // Store the value
        MyDictionary.Add(myKey, myValue);
    }
}
{% endhighlight %}

**#2 - I think this would perform better**

{% highlight csharp linenos %}
// Check if a value needs to be stored
if (MyDictionary.ContainsKey(myKey) == false)
{
    lock (MyDictionary)
    {
        // Check if a value needs to be stored
        if (MyDictionary.ContainsKey(myKey) == false)
        {
            // Store the value
            MyDictionary.Add(myKey, myValue);
        }
    }
}
{% endhighlight %}

The reason I think the second solution will perform better is that the first if statement gets executed as part of the normal code flow of the method. This means lots of executions on multiple threads. It will first check that an item needs to be added before causing the lock. This results in the lock only happening in rare cases. Inside the lock, it ensures that another thread isn't about to do the same thing by making a defensive call to ContainsKey.

In contrast, the first solution will lock out all other threads from checking to see if an item needs to be added causing a major bottleneck. 

Keeping in mind the metrics outlined above, is this a good solution? It seems like the best trade-off between thread safety and avoiding bottlenecks with the sacrifice being another call to ContainsKey.

Thoughts?


