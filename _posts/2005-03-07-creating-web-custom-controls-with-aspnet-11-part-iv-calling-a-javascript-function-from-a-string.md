---
title: Creating Web Custom Controls With ASP.Net 1.1 - Part IV - Calling a JavaScript Function From a String
date: 2005-03-07 20:32:00 +10:00
---

Along my travels of creating web custom controls, I have put a lot of effort into making the controls as dynamic as possible on the client. This typically means running a JavaScript function as a result of some event, such as the page load or control click. I quickly realized that to make a control that is generic and reusable, I need to have some code that always runs on an event, such as control event specific behavior, but I also need a way of allowing the implementer of the control to run their own code based on the same event.

Take my (work in progress) ImageButton control from the previous articles for an example. When the user clicks the button, a script will need to run that will determine if the button should have a toggle behavior and how the button should render itself with its defined set of images. This code must always run as it is part of the controls intrinsic behavior.

The implementer of the ImageButton control may also want to run their own script when the button is clicked. If the implementor can't run their own code on the client, then the control will only be useful as a server control that fires a click event on the server. I want the implementor to be able to run their own function when the onclick event fires, and also allow them to stop the postback as required.

The control needs to be able to run two functions when the button is clicked. The first is the control specific code which the control. The second is the implementer code. The control will know how to call its own internal function, but how does the control know what code the implementer wants to run?

There are several ways to do this. These include but are not limited to:

* Chaining function calls in the event handler of the tag.
* Setting new function pointers to the events.
* Attaching new function pointers to events.
* Calling a function by name.

**Chaining function calls in the event handler of the tag**

We could chain the two functions required by declaring them in the event handler on the controls tag. To do this, the tag would be rendered like this:

{% highlight aspx-vb linenos %}
<DIV onclick="ControlCode();ImpelementorCode();"></DIV>
{% endhighlight %}

There are issues with this solutions such as:

* The user won't be able to change the name of the function at runtime on the client without affecting the controls internal function.
* The implementor may not have defined the function although more intelligent scripting would get around this.
* Return values might need to be returned from either function.
* The implementor may want to cancel a postback, but there isn't any interaction between the functions or return value processing.
* The control code may need to run before and after the implementors code.
* The control may also want to prevent the implementor code from running, such as where the control is disabled, but click events still fire.

These are not the only situations that make the above solution not feasible. Many of these issues can be coded around with more complex control rendering, but it is a lot of work to cover these issues using the above solution. Because of this, we don't want the implementors function call to be rendered in the controls event handler. As such, our controls will usually render a click event handler like this:

{% highlight aspx-vb linenos %}
<DIV onclick="return ControlCode();"></DIV>
{% endhighlight %}

**Setting new function pointers to the events**

If the controls internal function is called from the event handler, but the implementors function isn't, there is another way of getting the implementors function to run on the event. We can repoint the event handler to a new function pointer.

{% highlight javascript linenos %}
var objElement = document.getElementById("MyControlID");
objElement.onclick = ImplementorFunction;
{% endhighlight %}

The good news is that there is only one problem with this solution. The bad news is that it is a complete deal breaker. What this will do is when the onclick event fires, it will no longer call the ControlCode function because the event handler has been given a new function pointer. It will now only call the ImplementorCode function. The result is that the controls internal code will not run.

**Attaching new function pointers to events**

To follow on from the previous solution, instead of repointing the event handler to a new function, we can add a function pointer to the set of functions that the event handler will call. This is done like this:

{% highlight javascript linenos %}
var objElement = document.getElementById("MyControlID");
// For IE
objElement.attachEvent("onclick", ImplementorFunction);
// Or
objElement.addEventListener("onclick", ImplementorFunction);
// For Netscape
{% endhighlight %}

This solution is better because in this case, the controls click code will run, as well as the implementers code. It does however suffer from the same problems as the previous solutions, as well as:

* You can't ensure which function will fire first
* You can't pass parameters to the function

**Calling a function by name**

The previous solutions leave us with more problems than decent answers.

We don't want to force the implementor to write code in a function with a predefined name for the control to call. A function of the same name might be used for something else. Instead, we want the implementor to be able to specify a function name for the control to call.

What we need in order to support this is to declare a server-side string property that defines the name of the client-side JavaScript function to call. This property will be rendered as a custom attribute of the control so that the value is persisted on the client. From the previous articles we know that it is easy enough to get the value of that custom attribute when the controls code runs. The question of the hour is now that we have the name of the function as a string value, how do we call that function from the string representation of its name?

There are other considerations in my solution other than just calling a function from a string. The code needs to handle the following cases:

* The implementor hasn't specified a function to call.
* The function specified doesn't exist.
* There may be multiple instances of the control on the page.
* The function call must be able to pass along parameters.
* The process must be able to handle return values.

The answer to all these problems is the JavaScript Function object. With the Function object, we can create new functions and define the code they will run. We can also call the new function with the arguments that we want.

To make a function pointer from a function name string, all we have to do is generate a new function where the code calls the string value. This will look like this:

{% highlight javascript linenos %}
function InvokeFunction(Handler)
{
    var objFunc = new Function("return " + Handler + "();");

    return objFunc.apply(this.caller);
}
{% endhighlight %}

The above code will result in an anonymous function being created and called at runtime. If the function name in the Handler parameter was "TestFunc", then the code for the anonymous function when run will be:

{% highlight javascript linenos %}
function anonymous()
{
    return TestFunc();
}
{% endhighlight %}

With just this amount of code, we have been able to convert a string into a JavaScript function call, and pass back the return value.

Next we need to add support for parameters/arguments. Arguments are available to a function object through its arguments array property regardless of whether the parameters have been declared. Declaring parameters in the function definition only provides a named reference to an item in the arguments array. If an argument isn't declared by name, it will still be found in the arguments property which is a zero-based array.

My InvokeFunction function will assume that the first and only declared parameter is the name of the function to call. It will check that the value is either a string or already a function pointer. It then loops through the reset of the arguments array of the function to build a new array of arguments. This array will be a copy of the arguments passed to InvokeFunction, but offset by one to skip passing along the function name parameter.

The full function looks like this:

{% highlight javascript linenos %}
function InvokeFunction(Handler)
{
    // Calls Handler and passes any additional parameters
    try
    {
        if ((typeof Handler != "string") && (typeof Handler != "function"))
        {
            throw new Error("Invalid Handler object specified.");
        }
        else
        {
            var aArgs = new Array;
            var bBuildFunction = (typeof Handler == "string");
            var sCode = "";
          
            // Store the arguments
            for (var nCount = 1; nCount &lt; arguments.length; nCount++)
            {
                aArgs[nCount - 1] = arguments[nCount];

                if (bBuildFunction == true)
                {
                    if (nCount > 1)
                    {
                        sCode += ", ";
                    }
           
                    sCode += "arguments[" + (nCount - 1) + "]";
                }
            }
          
            var objFunc = null;

            if (typeof Handler == "string")
            {
                // Build the dynamic code to run
                sCode = "return " + Handler + "(" + sCode + ");";

                // Create a new function from the code built
                objFunc = new Function(sCode);
            }
            else if (typeof Handler == "function")
            {
                // Take a pointer to the function
                objFunc = Handler;
            }

            // Run the function with the arguments
            return objFunc.apply(this.caller, aArgs);
        }
    }
    catch(e)
    {
        throw new Error("Failed to invoke function " + Handler + ".\n\n" + e.message);
    }
}
{% endhighlight %}

We have now been able to pass parameters to a function call that is defined by either a string or a function pointer and handle its return value. To test this out, a simple test page will do the trick.

{% highlight javascript linenos %}
<HTML>
    <HEAD>
    <SCRIPT language="javascript">
    function InvokeFunction(Handler)
    {
        // Calls Handler and passes any additional parameters
        try
        {
            if ((typeof Handler != "string") && (typeof Handler != "function"))
            {
                throw new Error("Invalid Handler object specified.");
            }
            else
            {
                var aArgs = new Array;
                var bBuildFunction = (typeof Handler == "string");
               
                var sCode = "";
               
                // Store the arguments
                for (var nCount = 1; nCount &lt; arguments.length; nCount++)
                {
                    aArgs[nCount - 1] = arguments[nCount];

                    if (bBuildFunction == true)
                    {
                        if (nCount > 1)
                        {
                            sCode += ", ";
                        }
                
                        sCode += "arguments[" + (nCount - 1) + "]";
                    }
                }
               
                var objFunc = null;
               
                if (typeof Handler == "string")
                {
                    // Build the dynamic code to run
                    sCode = "return " + Handler + "(" + sCode + ");";

                    // Create a new function from the code built
                    objFunc = new Function(sCode);
                }
                else if (typeof Handler == "function")
                {
                    // Take a pointer to the function
                    objFunc = Handler;
                }

                // Run the function with the arguments
                return objFunc.apply(this.caller, aArgs);
            }
        }
        catch(e)
        {
            throw new Error("Failed to invoke function " + Handler + ".\n\n" + e.message);
        }
    }

    function DummyObject(x)
    {
        this.x = x;
    }

    function RunTest()
    {
        alert(InvokeFunction("TestFunc", new DummyObject(false), "My Test", 123, false, RunTest));
    }

    function TestFunc(objDummy)
    {
        var sMsg = "TestFunc called with " + arguments.length + " arguments.\n\n";

        for (var nCount = 0; nCount < arguments.length; nCount++)
        {
            sMsg += nCount + ". " + typeof(arguments[nCount]) + " - " + arguments[nCount] + "\n";
        }

        alert(sMsg);

        return objDummy.x;
    }
    </SCRIPT>
    </HEAD>
    <BODY onload="RunTest();">
    </BODY>
</HTML>
{% endhighlight %}

This test case results in an anonymous function being created and called. That function is this:

{% highlight javascript linenos %}
function anonymous()
{
    return TestFunc(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4]);
}
{% endhighlight %}

