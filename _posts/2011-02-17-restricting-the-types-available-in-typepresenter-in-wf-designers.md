---
title: Restricting the types available in TypePresenter in WF designers
categories : .Net
tags : WF
date: 2011-02-17 16:56:36 +10:00
---

The TypePresenter control is the UI that the WF designer displays for selecting a type. It is often seen on generics based activities and the InvokeMethod activity.![image][0]

This drop down list provides some common types and includes some of the types already found in the current activity context. Selecting “Browse for Types …” allows for all referenced types to be searched in a dialog.![image][1]

Sometimes you don’t want the TypePresenter to provide every available type. The TypePresenter has a great feature that allows you to restrict the types it displays in this list and the associated “Browse for Types …” dialog. This is done by providing a Func&lt;Type, Boolean&gt; reference on the TypePresenter’s Filter property. 

<!--more-->

In my scenario, I want to restrict the types available to those that derive from System.Exception. The first step to achieve this is to make a reference to the filter method in the xaml of the activity designer.

{% highlight xml %}
<sapv:TypePresenter HorizontalAlignment="Left"
    VerticalAlignment="Center"
    Margin="6"
    Grid.Row="0"
    Grid.Column="1"
    Filter="ExceptionTypeFilter"
    AllowNull="false"
    BrowseTypeDirectly="false"
    Label="Exception type"
    Type="{Binding Path=ModelItem.ExceptionType, Mode=TwoWay, Converter={StaticResource modelItemConverter}}"
    Context="{Binding Context}" />
{% endhighlight %}

The code behind class of the designer must contain the method defined in the Filter property (ExceptionTypeFilter in this case). This method must take a Type parameter and return a Boolean in order to satisfy the Func&lt;Type, Boolean&gt; signature. The filter method related to the xaml above is the following.

{% highlight csharp %}
public Boolean ExceptionTypeFilter(Type typeToValidate)
{
    if (typeToValidate == null)
    {
        return false;
    }
    
    if (typeof(Exception).IsAssignableFrom(typeToValidate))
    {
        return true;
    }
    
    return false;
}
{% endhighlight %}

The designer for this activity will now only display exception types in the TypePresenter.![image][2]

The associated “Browse for Types …” dialog will also use this filter value.![image][3]

Unfortunately the property grid will still use the default TypePresenter implementation.![image][4]

I haven’t figured out a way to change this behaviour and I suspect that it is not possible. 

The final piece of the puzzle is to address what happens when the developer selects an inappropriate type using the property grid. This is where activity validation using CacheMetadata comes into play.

{% highlight csharp %}
protected override void CacheMetadata(NativeActivityMetadata metadata)
{
    metadata.AddDelegate(Body);
    metadata.AddImplementationChild(_internalDelay);
    metadata.AddImplementationVariable(_attemptCount);
    metadata.AddImplementationVariable(_delayDuration);
    
    RuntimeArgument maxAttemptsArgument = new RuntimeArgument("MaxAttempts", typeof(Int32), ArgumentDirection.In, true);
    RuntimeArgument retryIntervalArgument = new RuntimeArgument("RetryInterval", typeof(TimeSpan), ArgumentDirection.In, true);
    
    metadata.Bind(MaxAttempts, maxAttemptsArgument);
    metadata.Bind(RetryInterval, retryIntervalArgument);
    
    Collection<RuntimeArgument> arguments = new Collection<RuntimeArgument>
                                            {
                                                maxAttemptsArgument, 
                                                retryIntervalArgument
                                            };
    
    metadata.SetArgumentsCollection(arguments);
    
    if (Body == null)
    {
        ValidationError validationError = new ValidationError(Resources.Retry_NoChildActivitiesDefined, true, "Body");
    
        metadata.AddValidationError(validationError);
    }
    
    if (typeof(Exception).IsAssignableFrom(ExceptionType) == false)
    {
        ValidationError validationError = new ValidationError(Resources.Retry_InvalidExceptionType, false, "ExceptionType");
    
        metadata.AddValidationError(validationError);
    }
}
{% endhighlight %}

The validation at the end of this method checks for an inappropriate type. It is marked as an error so that the activity is not able to be executed in this state. For example, it the property grid is used to assign the type of System.String, the designer will display the following.![image][5]

The workflow runtime will throw an InvalidWorkflowException if the activity is executed in this state.

We have seen here that we can restrict the types presented by TypePresenter and back this up with some activity validation.

[0]: /files/image_66.png
[1]: /files/image_67.png
[2]: /files/image_68.png
[3]: /files/image_69.png
[4]: /files/image_70.png
[5]: /files/image_71.png
