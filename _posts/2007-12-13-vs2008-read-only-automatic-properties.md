---
title: VS2008 read-only automatic properties
categories : .Net
date: 2007-12-13 10:31:31 +10:00
---

I have started using the very nice automatic properties in VS2008. As I was using these, I was thinking about how they work when you define the property as read-only or write-only. Without a backing field, you wouldn't be able to read from or write to the backing field, rendering the property useless.

I coded an automatic property in this way and didn't get any error indication from the IDE, but I didn't actually compile it. I have since run some code analysis that suggests that my collection properties should be readonly. As these properties are automatic properties, I then removed the setter and compiled.

Boom! Now there is a compiler error CS0840 that includes the message _&quot;Automatically implemented properties must define both get and set accessors&quot;_. It is unfortunate that this wasn't indicated with those helpful squiggly red lines in the code editor, but not a major problem.

This does highlight an issue though. How can you implement read-only automatic properties? The answer is quite simple and is provided by the help description of the compiler error. It says _&quot;To create a read-only auto-implemented property, make the set accessor private_&quot;.

So the code will look something like this:

    {% highlight csharp linenos %}
    public class SomeClass
    {
        public SomeClass()
        {
            SomeProperty = new Collection<SomeOtherClass&gt;();
        }
    
        public Collection<SomeOtherClass&gt; SomeProperty
        {
            get;
            private set;
        }
    }
    {% endhighlight %}

That is very neat.


