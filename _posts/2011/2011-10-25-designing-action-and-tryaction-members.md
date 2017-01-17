---
title: Designing Action and TryAction members
categories: .Net, Software Design
date: 2011-10-25 13:13:26 +10:00
---

I have a pet peeve with how Action and TryAction style members are often implemented. Too often I see the following style of implementation.

<!--more-->

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
    
public class SomeClass
{
    public Stream GetSomething(String referenceData)
    {
        if (String.IsNullOrWhiteSpace(referenceData))
        {
            throw new InvalidOperationException();
        }
    
        return new MemoryStream();
    }
    
    public Boolean TryGetSomething(String referenceData, out Stream stream)
    {
        stream = null;
    
        try
        {
            stream = GetSomething(referenceData);
    
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
```

This is undesirable because there is an exception being throw in the standard Action method which the TryAction method only uses to determine the return value. It is an expensive way to identify that the TryAction method has failed to successfully do its work. The most likely reason code being written this way is because the Action method was written first with the TryAction retrofitted later.

With a trivial amount of work, this can be written as follows.

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
    
public class SomeClass
{
    public Stream GetSomething(String referenceData)
    {
        Stream data;
    
        if (TryGetSomething(referenceData, out data))
        {
            return data;
        }
    
        throw new InvalidOperationException();
    }
    
    public Boolean TryGetSomething(String referenceData, out Stream stream)
    {
        stream = null;
    
        if (String.IsNullOrWhiteSpace(referenceData))
        {
            return false;
        }
    
        stream = new MemoryStream();
    
        return true;
    }
}
```

This alternative means that the code is never catching an exception in order to determine the result of the TryAction method. Instead, the standard Action method calls down to TryAction and checks the Boolean result to determine whether an exception should be thrown. This code is avoids unnecessary exceptions and has exactly the same behaviour from the callers perspective.


