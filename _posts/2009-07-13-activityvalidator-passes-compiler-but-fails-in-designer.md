---
title: ActivityValidator passes compiler but fails in designer
categories : .Net
tags : WF
date: 2009-07-13 11:11:01 +10:00
---

I have a custom WF activity that uses a type converter to convert from a source object to a destination object. I added an ActivityValidator to the activity to check that the TypeConverterType property specified to use for the conversion derives from TypeConverter. 

The code originally looked like the following.

{% highlight csharp linenos %}
if (converterActivity.TypeConverterType != null) 
{ 
    // The type must be a type converter 
    if (typeof(TypeConverter).IsAssignableFrom(converterActivity.TypeConverterType) == false) 
    { 
        errors.Add(new ValidationError("Property 'TypeConverterType' is not a TypeConverter", TypeConverterTypeIsInvalidId));   
    } 
}
{% endhighlight %}

The compiler successfully passed the validation and the workflow designer that used the activity was clear of any error indicators. If I rebuild the solution then the compiler would still pass but now the workflow designer indicated the error condition from the above code. After adding other validation errors into the designer for debugging there didn’t seem to be a good reason why the Type.IsAssignableFrom method should fail. The TypeConverterType property value was a type that derived from TypeConverter. With no other options, the code was changed to the following:

{% highlight csharp linenos %}
if (converterActivity.TypeConverterType != null) 
{ 
    // Rebuilds of the solution will cause this error to be added 
    // The Type.IsAssignableFrom check passes the compiler but the designer still seems to fail the check 
    // We need to manually check the inheritance chain of the type converter type 
    Boolean converterFound = false; 
    
    for (Type converterType = converterActivity.TypeConverterType; converterType != null; converterType = converterType.BaseType ) 
    { 
        if (converterType.Equals(typeof(TypeConverter))) 
        { 
            converterFound = true; 
    
            break; 
        } 
    } 
    
    // The type must be a type converter 
    if (converterFound == false) 
    { 
        errors.Add( 
            new ValidationError( 
                "Property 'TypeConverterType' is not a TypeConverter", 
                TypeConverterTypeIsInvalidId, 
                false, 
                TypeConverterActivity.TypeConverterTypeProperty.Name)); 
    } 
}
    
{% endhighlight %}

This now works for the compiler and the workflow designer.


