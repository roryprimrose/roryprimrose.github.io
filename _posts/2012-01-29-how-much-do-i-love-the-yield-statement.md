---
title: How much do I love the yield statement?
categories : .Net, Software Design
date: 2012-01-29 22:15:09 +10:00
---

Quite simply, a lot. The yield statement seems to be such a simple part of C# yet it can provide such amazing power (being delayed enumeration). Outside of that power however, it can provide beautiful simplicity.

Take the following abstract class for example:

    namespace MyApplication.Diagnostics
    {
        using System;
        using System.Collections.Generic;
    
        public abstract class DiagnosticTask
        {
            public abstract IEnumerable<DiagnosticTaskResult&gt; ExecuteAll();
    
            public abstract DiagnosticTaskResult ExecuteStep(Guid id);
    
            public virtual String Description
            {
                get
                {
                    return String.Empty;
                }
            }
    
            public abstract Guid Id
            {
                get;
            }
    
            public abstract IEnumerable<DiagnosticTaskStep&gt; Steps
            {
                get;
            }
    
            public virtual Boolean SupportsSingleStepExecution
            {
                get
                {
                    return false;
                }
            }
    
            public abstract String Title
            {
                get;
            }
        }
    }{% endhighlight %}

The design of this abstract class allows for a diagnostic task to have multiple steps. My first implementation of this class however is one that only had a single hard-coded step. Enter the wonderful yield return statement.

    public override IEnumerable<DiagnosticTaskStep&gt; Steps
    {
        get
        {
            yield return _validateStep;
        }
    }{% endhighlight %}

This is so much more elegant than creating a new collection to return the single predetermined item. I can’t say how much I love the yield statement.


