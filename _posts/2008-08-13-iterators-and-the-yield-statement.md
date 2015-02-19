---
title: Iterators and the yield statement
categories : .Net, IT Related
tags : Performance, Useful Links
date: 2008-08-13 09:57:55 +10:00
---

The [yield][0] statement is one of those C# statements that is really powerful but is either not understood or is unknown to most developers. [Raymond Chen][1] just posted a very good write up on how the compiler deals with the yield statement.

[The Old New Thing : The implementation of iterators in C# and its consequences (part 1)][2]

The great feature that the yield statement brings is delayed execution in iterations. If building a collection of items to iterate through is an expensive operation on either performance or memory, getting individual items as they are requested may be a better design. Using the yield statement means that this is really easy to achieve.

Take the following code for example:

{% highlight csharp linenos %}
using System;
using System.Collections.Generic;
     
namespace ConsoleApplication1
{
    internal class Program
    {
        private static void Main(String[] args)
        {
            Console.WriteLine("Starting the iterations");
     
            foreach (Entity item in GetItems(10))
            {
                Console.WriteLine("Got item {0} - {1} - {2}", item.Id, item.FirstValue, item.SecondValue);
            }
     
            Console.ReadKey();
        }
     
        private static IEnumerable<Entity> GetItems(Int32 maxItems)
        {
            for (Int32 index = 0; index < maxItems; index++)
            {
                yield return GetItemFromStore(index);
            }
        }
     
        private static Entity GetItemFromStore(Int32 id)
        {
            Console.WriteLine("Getting item {0} from the store", id);
     
            // Simulate getting the item from a data store or service
            Entity item = new Entity();
     
            item.Id = id;
            item.FirstValue = "First" + id;
            item.SecondValue = "Second" + id;
     
            return item;
        }
    }
     
    internal class Entity
    {
        public String FirstValue
        {
            get;
            set;
        }
     
        public Int32 Id
        {
            get;
            set;
        }
     
        public String SecondValue
        {
            get;
            set;
        }
    }
}
    
{% endhighlight %}

This code iterates over a set of 10 items and outputs the details of each item encountered. The interesting bit is that each item is only requested and created for each iteration rather than calculating the entire set up front.

The output of this code is:

{% highlight text linenos %}
Starting the iterations
Getting item 0 from the store
Got item 0 - First0 - Second0
Getting item 1 from the store
Got item 1 - First1 - Second1
Getting item 2 from the store
Got item 2 - First2 - Second2
Getting item 3 from the store
Got item 3 - First3 - Second3
Getting item 4 from the store
Got item 4 - First4 - Second4
Getting item 5 from the store
Got item 5 - First5 - Second5
Getting item 6 from the store
Got item 6 - First6 - Second6
Getting item 7 from the store
Got item 7 - First7 - Second7
Getting item 8 from the store
Got item 8 - First8 - Second8
Getting item 9 from the store
Got item 9 - First9 - Second9
{% endhighlight %}

Very powerful, very easy.

[0]: http://msdn.microsoft.com/en-us/library/9k7k7cf0(VS.80).aspx
[1]: http://blogs.msdn.com/oldnewthing/default.aspx
[2]: http://blogs.msdn.com/oldnewthing/archive/2008/08/12/8849519.aspx
