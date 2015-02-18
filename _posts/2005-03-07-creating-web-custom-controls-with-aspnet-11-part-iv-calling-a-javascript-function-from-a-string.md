---
title: Creating Web Custom Controls With ASP.Net 1.1 - Part IV - Calling a JavaScript Function From a String
date: 2005-03-07 20:32:00 +10:00
---

<P>Along my travels of creating web custom controls, I have put a lot of effort 
	into making the controls as dynamic as possible on the client. This typically 
	means running a JavaScript function as a result of some event, such as the page 
	load or control click. I quickly realized that to make a control that is 
	generic and reusable, I need to have some code that always runs on an event, 
	such as control event specific behavior, but I also need a way of allowing the 
	implementer of the control to run their own code based on the same event.</P>
<P>Take my (work in progress) ImageButton control from the previous articles for an 
	example. When the user clicks the button, a script will need to run that will 
	determine if the button should have a toggle behavior and how the button 
	should render itself with its defined set of images. This code must always run 
	as it is part of the controls intrinsic behavior.</P>
<P>The implementer of the ImageButton control may also want to run their own script 
	when the button is clicked. If the implementor can't run their own code on the 
	client, then the control will only be useful as a server control that fires a 
	click event on the server. I want the implementor to be able to run their own 
	function when the onclick event fires, and also allow them to stop the postback 
	as required.</P>
<P onclick="alert('test');">The control needs to be able 
	to run two functions when the button is clicked. The first is the control 
	specific code which the control. The second is the implementer code. The control will know how 
	to call its own internal function, but how does the control know what 
	code the implementer wants to run?</P>
<P>There are several ways to do this. These include but are not limited to:</P>
<UL><LI>Chaining function calls in the event handler of the tag.</LI><LI>Setting new function pointers to the events.</LI><LI>Attaching new function pointers to events.</LI><LI>Calling a function by name.</LI></UL><P><STRONG>Chaining function calls in the event handler of the tag</STRONG></P>
<P>We could chain the two functions required by declaring them in the event handler 
	on the controls tag. To do this, the tag would be rendered like this:
</P>
<DIV style="BORDER-RIGHT: windowtext 1pt solid; PADDING-RIGHT: 5px; BORDER-TOP: windowtext 1pt solid; PADDING-LEFT: 5px; FONT-SIZE: 10pt; BACKGROUND: white; PADDING-BOTTOM: 5px; BORDER-LEFT: windowtext 1pt solid; COLOR: black; PADDING-TOP: 5px; BORDER-BOTTOM: windowtext 1pt solid; FONT-FAMILY: Courier New; margin-bottom: 7px;">
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px"><SPAN style="COLOR: blue">&lt;</SPAN><SPAN style="COLOR: brown">DIV</SPAN>
		<SPAN style="COLOR: red">onclick</SPAN><SPAN style="COLOR: blue">="ControlCode();ImpelementorCode();"&gt;</SPAN><SPAN style="COLOR: blue">&lt;/</SPAN><SPAN style="COLOR: brown">DIV</SPAN><SPAN style="COLOR: blue">&gt;</SPAN></P>
	<P style="MARGIN: 0px"> </P>
</DIV>
<P>There are issues with this solutions such as:</P>
<UL><LI>The user won't be able to change the name of the function at runtime on the client without affecting the controls internal function.</LI><LI>The implementor may not have defined the function although more intelligent scripting would get around this.</LI><LI>Return values might need to be returned from either function.</LI><LI>The implementor may want to cancel a postback, but there isn't any interaction between the functions or return value processing.</LI><LI>The control code may need to run before and after the implementors code.</LI><LI>The control may also want to prevent the implementor code from running, such as where the control is disabled, but click events still fire.</LI></UL><P>These are not the only situations that make the above solution not feasible. Many of 
	these issues can be coded around with more complex control rendering, but it is a lot of 
	work to cover these issues using the above solution. Because of this, we 
	don't want the implementors function call to be rendered in the controls event 
	handler. As such, our controls will usually render a click event handler like 
	this:</P>
<DIV style="BORDER-RIGHT: windowtext 1pt solid; PADDING-RIGHT: 5px; BORDER-TOP: windowtext 1pt solid; PADDING-LEFT: 5px; FONT-SIZE: 10pt; BACKGROUND: white; PADDING-BOTTOM: 5px; BORDER-LEFT: windowtext 1pt solid; COLOR: black; PADDING-TOP: 5px; BORDER-BOTTOM: windowtext 1pt solid; FONT-FAMILY: Courier New; margin-bottom: 7px;">
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px"><SPAN style="COLOR: blue">&lt;</SPAN><SPAN style="COLOR: brown">DIV</SPAN>
		<SPAN style="COLOR: red">onclick</SPAN><SPAN style="COLOR: blue">="return ControlCode();"&gt;</SPAN><SPAN style="COLOR: blue">&lt;/</SPAN><SPAN style="COLOR: brown">DIV</SPAN><SPAN style="COLOR: blue">&gt;</SPAN></P>
	<P style="MARGIN: 0px"> </P>
</DIV>
<P><STRONG>Setting new function pointers to the events</STRONG></P>
<P>If the controls internal function is called from the event handler, but the 
	implementors function isn't, there is another way of getting the implementors 
	function to run on the event. We can repoint the event handler to a new 
	function pointer.</P>
<DIV style="BORDER-RIGHT: windowtext 1pt solid; PADDING-RIGHT: 5px; BORDER-TOP: windowtext 1pt solid; PADDING-LEFT: 5px; FONT-SIZE: 10pt; BACKGROUND: white; PADDING-BOTTOM: 5px; BORDER-LEFT: windowtext 1pt solid; COLOR: black; PADDING-TOP: 5px; BORDER-BOTTOM: windowtext 1pt solid; FONT-FAMILY: Courier New; margin-bottom: 7px;">
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px"><SPAN style="COLOR: blue">var</SPAN>
		objElement = document.getElementById("MyControlID");</P>
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px">objElement.onclick = ImplementorFunction;</P>
	<P style="MARGIN: 0px"> </P>
</DIV>
<P>The good news is that there is only one problem with this solution. The bad news 
	is that it is a complete deal breaker. What this will do is when the onclick 
	event fires, it will no longer call the ControlCode function because the event 
	handler has been given a new function pointer. It will now only call the 
	ImplementorCode function. The result is that the controls internal code will 
	not run.</P>
<P><STRONG>Attaching new function pointers to events</STRONG></P>
<P>To follow on from the previous solution, instead of repointing the event handler 
	to a new function, we can add a function pointer to the set of functions that 
	the event handler will call. This is done like this:</P>
<DIV style="BORDER-RIGHT: windowtext 1pt solid; PADDING-RIGHT: 5px; BORDER-TOP: windowtext 1pt solid; PADDING-LEFT: 5px; FONT-SIZE: 10pt; BACKGROUND: white; PADDING-BOTTOM: 5px; BORDER-LEFT: windowtext 1pt solid; COLOR: black; PADDING-TOP: 5px; BORDER-BOTTOM: windowtext 1pt solid; FONT-FAMILY: Courier New; margin-bottom: 7px;">
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px"><SPAN style="COLOR: blue">var</SPAN>
		objElement = document.getElementById("MyControlID");</P>
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px"><SPAN style="COLOR: green">// For IE</SPAN></P>
	<P style="MARGIN: 0px">objElement.attachEvent("onclick", ImplementorFunction);</P>
	<P style="MARGIN: 0px"><SPAN style="COLOR: green">// Or</SPAN></P>
	<P style="MARGIN: 0px">objElement.addEventListener("onclick", 
		ImplementorFunction);</P>
	<P style="MARGIN: 0px"><SPAN style="COLOR: green">// For Netscape</SPAN></P>
	<P style="MARGIN: 0px"> </P>
</DIV>
<P>This solution is better because in this case, the controls click code will run, 
	as well as the implementers code. It does however suffer from the same problems 
	as the previous solutions, as well as:</P>
<UL><LI>You can't ensure which function will fire first</LI><LI>You can't pass parameters to the function</LI></LI></UL><P><STRONG>Calling a function by name</STRONG></P>
<P>The previous solutions leave us with more problems than decent answers.
</P>
<P>We don't want to 
	force the implementor to write code in a function with a predefined name for the 
	control to call. A function of the same name might be used for 
	something else. Instead, we want the implementor to be able to specify a 
	function name for the control to call. </P>
<P>What we need in order to support this is to declare a server-side string 
property that defines the name of the client-side JavaScript function to call. 
This property will be rendered as a custom attribute of the control so that the 
value is persisted on the client. From the previous articles we know that it is 
easy enough to get the value of that custom attribute when the controls code 
runs. The question of the hour is now that we have the name of the function as a 
string value, how do we call that function from the string representation of its 
name?</P>
<P>There are other considerations in my solution other than just calling a function from 
	a string. The code needs to handle the following cases:</P>
<UL><LI>The implementor hasn't specified a function to call.</LI><LI>The function specified doesn't exist.</LI><LI>There may be multiple instances of the control on the page.</LI><LI>The function call must be able to pass along parameters.</LI><LI>The process must be able to handle return values.</LI></UL><P>The answer to all these problems is the JavaScript Function object. With the 
	Function object, we can create new functions and define the code they will run. 
	We can also call the new function with the arguments that we want.</P>
<P>To make a function pointer from a function name string, all we have to do is generate a new 
	function where the code calls the string value. This will look like this:</P>
<DIV style="BORDER-RIGHT: windowtext 1pt solid; PADDING-RIGHT: 5px; BORDER-TOP: windowtext 1pt solid; PADDING-LEFT: 5px; FONT-SIZE: 10pt; BACKGROUND: white; PADDING-BOTTOM: 5px; BORDER-LEFT: windowtext 1pt solid; COLOR: black; PADDING-TOP: 5px; BORDER-BOTTOM: windowtext 1pt solid; FONT-FAMILY: Courier New; margin-bottom: 7px;">
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px"><SPAN style="COLOR: blue">function</SPAN>
		InvokeFunction(Handler)</P>
	<P style="MARGIN: 0px">{</P>
	<P style="MARGIN: 0px">   
		<SPAN style="COLOR: blue">var</SPAN> objFunc =
		 
		<SPAN style="COLOR: blue">new</SPAN>
		Function("return " + Handler + "();");</P>
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px">   
		<SPAN style="COLOR: blue">return</SPAN>
		objFunc.apply(<SPAN style="COLOR: blue">this</SPAN>.caller);</P>
	<P style="MARGIN: 0px">}</P>
	<P style="MARGIN: 0px"> </P>
</DIV>
<P>The above code will result in an anonymous function being created and called at runtime. If the function 
	name in the Handler parameter was "TestFunc", then the code for the anonymous 
	function when run will be:</P>
<DIV style="BORDER-RIGHT: windowtext 1pt solid; PADDING-RIGHT: 5px; BORDER-TOP: windowtext 1pt solid; PADDING-LEFT: 5px; FONT-SIZE: 10pt; BACKGROUND: white; PADDING-BOTTOM: 5px; BORDER-LEFT: windowtext 1pt solid; COLOR: black; PADDING-TOP: 5px; BORDER-BOTTOM: windowtext 1pt solid; FONT-FAMILY: Courier New; margin-bottom: 7px;">
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px"><SPAN style="COLOR: blue">function</SPAN>
		anonymous()</P>
	<P style="MARGIN: 0px">{</P>
	<P style="MARGIN: 0px">   
		<SPAN style="COLOR: blue">return</SPAN>
		TestFunc();</P>
	<P style="MARGIN: 0px">}</P>
	<P style="MARGIN: 0px"> </P>
</DIV>
<P>With just this amount of code, we have been able to convert a string into a 
	JavaScript function call, and pass back the return value.</P>
<P>Next we need to add support for parameters/arguments. Arguments are available to 
	a function object through its arguments array property regardless of whether 
	the parameters have been declared. Declaring parameters in the function 
	definition only provides a named reference to an item in the arguments array. 
	If an argument isn't declared by name, it will still be found in the arguments 
	property which is a zero-based array.</P>
<P>My InvokeFunction function will assume that the first and only declared parameter 
	is the name of the function to call. It will check that the value is either 
	a string or already a function pointer. It then loops through the reset 
	of the arguments array of the function to build a new array of arguments. This array 
	will be a copy of the arguments passed to InvokeFunction, but offset by 
	one to skip passing along the function name parameter.</P>
<P>The full function looks like this:</P>
<DIV style="BORDER-RIGHT: windowtext 1pt solid; PADDING-RIGHT: 5px; BORDER-TOP: windowtext 1pt solid; PADDING-LEFT: 5px; FONT-SIZE: 10pt; BACKGROUND: white; PADDING-BOTTOM: 5px; OVERFLOW: auto; BORDER-LEFT: windowtext 1pt solid; COLOR: black; PADDING-TOP: 5px; BORDER-BOTTOM: windowtext 1pt solid; FONT-FAMILY: Courier New; HEIGHT: 250px; margin-bottom: 7px;">
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px"><SPAN style="COLOR: blue">function</SPAN>
		InvokeFunction(Handler)</P>
	<P style="MARGIN: 0px">{</P>
	<P style="MARGIN: 0px">   
		<SPAN style="COLOR: green">// Calls Handler and passes any 
additional parameters</SPAN></P>
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px">   
		<SPAN style="COLOR: blue">try</SPAN></P>
	<P style="MARGIN: 0px">    {</P>
	<P style="MARGIN: 0px">       
		<SPAN style="COLOR: blue">if</SPAN>
		((<SPAN style="COLOR: blue">typeof</SPAN> Handler !=  
		  "string") &amp;&amp; (<SPAN style="COLOR: blue">typeof</SPAN>
		Handler != "function"))</P>
	<P style="MARGIN: 0px">        {</P>
	<P style="MARGIN: 0px">           
		<SPAN style="COLOR: blue">throw new</SPAN>
		Error("Invalid Handler object specified.");</P>
	<P style="MARGIN: 0px">        }</P>
	<P style="MARGIN: 0px">       
		<SPAN style="COLOR: blue">else</SPAN></P>
	<P style="MARGIN: 0px">        {</P>
	<P style="MARGIN: 0px">           
		<SPAN style="COLOR: blue">var</SPAN> aArgs =
		 
		<SPAN style="COLOR: blue">new</SPAN>
		Array;</P>
	<P style="MARGIN: 0px">           
		<SPAN style="COLOR: blue">var</SPAN>
		bBuildFunction = (<SPAN style="COLOR: blue">typeof</SPAN>
		Handler == "string");</P>
	<P style="MARGIN: 0px">           
		<SPAN style="COLOR: blue">var</SPAN>
		sCode = "";</P>
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px">           
		<SPAN style="COLOR: green">// Store the arguments</SPAN></P>
	<P style="MARGIN: 0px">           
		<SPAN style="COLOR: blue">for</SPAN>
		(<SPAN style="COLOR: blue">var</SPAN>
		nCount = 1; nCount &lt; arguments.length; nCount++)</P>
	<P style="MARGIN: 0px">            {</P>
	<P style="MARGIN: 0px">            
		    aArgs[nCount - 1] = arguments[nCount];</P>
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px">            
		   
		<SPAN style="COLOR: blue">if</SPAN> (bBuildFunction ==
		 
		<SPAN style="COLOR: blue">true</SPAN>)</P>
	<P style="MARGIN: 0px">            
		    {</P>
	<P style="MARGIN: 0px">            
		       
		<SPAN style="COLOR: blue">if</SPAN>
		(nCount &gt; 1)</P>
	<P style="MARGIN: 0px">            
		        {</P>
	<P style="MARGIN: 0px">            
		            sCode += ", ";</P>
	<P style="MARGIN: 0px">            
		        }</P>
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px">            
		        sCode += "arguments[" + (nCount - 1) + 
		"]";</P>
	<P style="MARGIN: 0px">            
		    }</P>
	<P style="MARGIN: 0px">            }</P>
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px">           
		<SPAN style="COLOR: blue">var</SPAN> objFunc =
		 
		<SPAN style="COLOR: blue">null</SPAN>;</P>
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px">           
		<SPAN style="COLOR: blue">if</SPAN>
		(<SPAN style="COLOR: blue">typeof</SPAN>
		Handler == "string")</P>
	<P style="MARGIN: 0px">            {</P>
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px">            
		   
		<SPAN style="COLOR: green">// Build the dynamic code to 
run</SPAN></P>
	<P style="MARGIN: 0px">            
		    sCode = "return " + Handler + "(" + sCode + ");";</P>
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px">            
		   
		<SPAN style="COLOR: green">// Create a new function from the code 
built</SPAN></P>
	<P style="MARGIN: 0px">    
            objFunc =   
		  
		<SPAN style="COLOR: blue">new</SPAN>
		Function(sCode);</P>
	<P style="MARGIN: 0px">            }</P>
	<P style="MARGIN: 0px">           
		<SPAN style="COLOR: blue">else if</SPAN>
		(<SPAN style="COLOR: blue">typeof</SPAN>
		Handler == "function")</P>
	<P style="MARGIN: 0px">            {</P>
	<P style="MARGIN: 0px">            
		   
		<SPAN style="COLOR: green">// Take a pointer to the 
function</SPAN></P>
	<P style="MARGIN: 0px">            
		    objFunc = Handler;</P>
	<P style="MARGIN: 0px">            }</P>
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px">           
		<SPAN style="COLOR: green">// Run the function with the arguments</SPAN></P>
	<P style="MARGIN: 0px">           
		<SPAN style="COLOR: blue">return</SPAN>
		objFunc.apply(<SPAN style="COLOR: blue">this</SPAN>.caller, aArgs);</P>
	<P style="MARGIN: 0px">        }   
	</P>
	<P style="MARGIN: 0px">    }</P>
	<P style="MARGIN: 0px">   
		<SPAN style="COLOR: blue">catch</SPAN>(e)</P>
	<P style="MARGIN: 0px">    {</P>
	<P style="MARGIN: 0px">       
		<SPAN style="COLOR: blue">throw new</SPAN>
		Error("Failed to invoke function " + Handler + ".\n\n" + e.message);</P>
	<P style="MARGIN: 0px">    }</P>
	<P style="MARGIN: 0px">}</P>
	<P style="MARGIN: 0px"> </P>
</DIV>
<P>We have now been able to pass parameters to a function call that is defined by 
	either a string or a function pointer and handle its return value. To test this 
	out, a simple test page will do the trick.</P>
<DIV style="BORDER-RIGHT: windowtext 1pt solid; PADDING-RIGHT: 5px; BORDER-TOP: windowtext 1pt solid; PADDING-LEFT: 5px; FONT-SIZE: 10pt; BACKGROUND: white; PADDING-BOTTOM: 5px; OVERFLOW: auto; BORDER-LEFT: windowtext 1pt solid; COLOR: black; PADDING-TOP: 5px; BORDER-BOTTOM: windowtext 1pt solid; FONT-FAMILY: Courier New; HEIGHT: 250px; margin-bottom: 7px;">
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px"><SPAN style="COLOR: blue">&lt;</SPAN><SPAN style="COLOR: brown">HTML</SPAN><SPAN style="COLOR: blue">&gt;</SPAN></P>
	<P style="MARGIN: 0px"><SPAN style="COLOR: blue">&lt;</SPAN><SPAN style="COLOR: brown">HEAD</SPAN><SPAN style="COLOR: blue">&gt;</SPAN></P>
	<P style="MARGIN: 0px"><SPAN style="COLOR: blue">&lt;</SPAN><SPAN style="COLOR: brown">SCRIPT</SPAN> <SPAN style="COLOR: red">language</SPAN><SPAN style="COLOR: blue">="javascript"&gt;</SPAN></P>
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px"><SPAN style="COLOR: blue">function</SPAN>
		InvokeFunction(Handler)</P>
	<P style="MARGIN: 0px">{</P>
	<P style="MARGIN: 0px">   
		<SPAN style="COLOR: green">// Calls Handler and passes any 
additional parameters</SPAN></P>
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px">   
		<SPAN style="COLOR: blue">try</SPAN></P>
	<P style="MARGIN: 0px">    {</P>
	<P style="MARGIN: 0px">       
		<SPAN style="COLOR: blue">if</SPAN>
		((<SPAN style="COLOR: blue">typeof</SPAN> Handler !=  
		  "string") &amp;&amp; (<SPAN style="COLOR: blue">typeof</SPAN>
		Handler != "function"))</P>
	<P style="MARGIN: 0px">        {</P>
	<P style="MARGIN: 0px">           
		<SPAN style="COLOR: blue">throw new</SPAN>
		Error("Invalid Handler object specified.");</P>
	<P style="MARGIN: 0px">        }</P>
	<P style="MARGIN: 0px">       
		<SPAN style="COLOR: blue">else</SPAN></P>
	<P style="MARGIN: 0px">        {</P>
	<P style="MARGIN: 0px">           
		<SPAN style="COLOR: blue">var</SPAN> aArgs =
		 
		<SPAN style="COLOR: blue">new</SPAN>
		Array;</P>
	<P style="MARGIN: 0px">           
		<SPAN style="COLOR: blue">var</SPAN>
		bBuildFunction = (<SPAN style="COLOR: blue">typeof</SPAN>
		Handler == "string");</P>
	<P style="MARGIN: 0px">           
		<SPAN style="COLOR: blue">var</SPAN>
		sCode = "";</P>
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px">           
		<SPAN style="COLOR: green">// Store the arguments</SPAN></P>
	<P style="MARGIN: 0px">           
		<SPAN style="COLOR: blue">for</SPAN>
		(<SPAN style="COLOR: blue">var</SPAN>
		nCount = 1; nCount &lt; arguments.length; nCount++)</P>
	<P style="MARGIN: 0px">            {</P>
	<P style="MARGIN: 0px">            
		    aArgs[nCount - 1] = arguments[nCount];</P>
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px">            
		   
		<SPAN style="COLOR: blue">if</SPAN> (bBuildFunction ==
		 
		<SPAN style="COLOR: blue">true</SPAN>)</P>
	<P style="MARGIN: 0px">            
		    {</P>
	<P style="MARGIN: 0px">            
		       
		<SPAN style="COLOR: blue">if</SPAN>
		(nCount &gt; 1)</P>
	<P style="MARGIN: 0px">            
		        {</P>
	<P style="MARGIN: 0px">            
		            sCode += ", ";</P>
	<P style="MARGIN: 0px">            
		        }</P>
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px">            
		        sCode += "arguments[" + (nCount - 1) + 
		"]";</P>
	<P style="MARGIN: 0px">            
		    }</P>
	<P style="MARGIN: 0px">            }</P>
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px">           
		<SPAN style="COLOR: blue">var</SPAN> objFunc =
		 
		<SPAN style="COLOR: blue">null</SPAN>;</P>
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px">           
		<SPAN style="COLOR: blue">if</SPAN>
		(<SPAN style="COLOR: blue">typeof</SPAN>
		Handler == "string")</P>
	<P style="MARGIN: 0px">            {</P>
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px">            
		   
		<SPAN style="COLOR: green">// Build the dynamic code to 
run</SPAN></P>
	<P style="MARGIN: 0px">            
		    sCode = "return " + Handler + "(" + sCode + ");";</P>
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px">            
		   
		<SPAN style="COLOR: green">// Create a new function from the code 
built</SPAN></P>
	<P style="MARGIN: 0px">    
            objFunc =   
		  
		<SPAN style="COLOR: blue">new</SPAN>
		Function(sCode);</P>
	<P style="MARGIN: 0px">            }</P>
	<P style="MARGIN: 0px">           
		<SPAN style="COLOR: blue">else if</SPAN>
		(<SPAN style="COLOR: blue">typeof</SPAN>
		Handler == "function")</P>
	<P style="MARGIN: 0px">            {</P>
	<P style="MARGIN: 0px">            
		   
		<SPAN style="COLOR: green">// Take a pointer to the 
function</SPAN></P>
	<P style="MARGIN: 0px">            
		    objFunc = Handler;</P>
	<P style="MARGIN: 0px">            }</P>
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px">           
		<SPAN style="COLOR: green">// Run the function with the arguments</SPAN></P>
	<P style="MARGIN: 0px">           
		<SPAN style="COLOR: blue">return</SPAN>
		objFunc.apply(<SPAN style="COLOR: blue">this</SPAN>.caller, aArgs);</P>
	<P style="MARGIN: 0px">        }   
	</P>
	<P style="MARGIN: 0px">    }</P>
	<P style="MARGIN: 0px">   
		<SPAN style="COLOR: blue">catch</SPAN>(e)</P>
	<P style="MARGIN: 0px">    {</P>
	<P style="MARGIN: 0px">       
		<SPAN style="COLOR: blue">throw new</SPAN>
		Error("Failed to invoke function " + Handler + ".\n\n" + e.message);</P>
	<P style="MARGIN: 0px">    }</P>
	<P style="MARGIN: 0px">}</P>
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px"><SPAN style="COLOR: blue">function</SPAN>
		DummyObject(x)</P>
	<P style="MARGIN: 0px">{</P>
	<P style="MARGIN: 0px">   
		<SPAN style="COLOR: blue">this</SPAN>.x = x;</P>
	<P style="MARGIN: 0px">}</P>
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px">
		<SPAN style="COLOR: blue">function</SPAN>
		RunTest()</P>
	<P style="MARGIN: 0px">{</P>
	<P style="MARGIN: 0px">    
		alert(InvokeFunction("TestFunc",
		<SPAN style="COLOR: blue">new</SPAN>
		DummyObject(<SPAN style="COLOR: blue">false</SPAN>), "My Test", 123,
		<SPAN style="COLOR: blue">false</SPAN>, RunTest));</P>
	<P style="MARGIN: 0px">}</P>
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px">
		<SPAN style="COLOR: blue">function</SPAN>
		TestFunc(objDummy)</P>
	<P style="MARGIN: 0px">{</P>
	<P style="MARGIN: 0px">    var sMsg = "TestFunc 
		called with " + arguments.length + " arguments.\n\n";</P>
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px">   
		<SPAN style="COLOR: blue">for</SPAN>
		(<SPAN style="COLOR: blue">var</SPAN>
		nCount = 0; nCount &lt; arguments.length; nCount++)</P>
	<P style="MARGIN: 0px">    {</P>
	<P style="MARGIN: 0px">        
		sMsg += nCount + ". " +
		<SPAN style="COLOR: blue">typeof</SPAN>(arguments[nCount]) + " - " + 
		arguments[nCount] + "\n";</P>
	<P style="MARGIN: 0px">    }</P>
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px">    alert(sMsg);</P>
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px">   
		<SPAN style="COLOR: blue">return</SPAN>
		objDummy.x;</P>
	<P style="MARGIN: 0px">}</P>
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px"><SPAN style="COLOR: blue">&lt;/</SPAN><SPAN style="COLOR: brown">SCRIPT</SPAN><SPAN style="COLOR: blue">&gt;</SPAN></P>
	<P style="MARGIN: 0px"><SPAN style="COLOR: blue">&lt;/</SPAN><SPAN style="COLOR: brown">HEAD</SPAN><SPAN style="COLOR: blue">&gt;</SPAN></P>
	<P style="MARGIN: 0px"><SPAN style="COLOR: blue">&lt;</SPAN><SPAN style="COLOR: brown">BODY</SPAN> <SPAN style="COLOR: red">onload</SPAN><SPAN style="COLOR: blue">="RunTest();"&gt;</SPAN></P>
	<P style="MARGIN: 0px"><SPAN style="COLOR: blue">&lt;/</SPAN><SPAN style="COLOR: brown">BODY</SPAN><SPAN style="COLOR: blue">&gt;</SPAN></P>
	<P style="MARGIN: 0px"><SPAN style="COLOR: blue">&lt;/</SPAN><SPAN style="COLOR: brown">HTML</SPAN><SPAN style="COLOR: blue">&gt;</SPAN></P>
	<P style="MARGIN: 0px"> </P>
</DIV>
<P>This test case results in an anonymous function being created and called. 
That function is this:</P>
<DIV style="BORDER-RIGHT: windowtext 1pt solid; PADDING-RIGHT: 5px; BORDER-TOP: windowtext 1pt solid; PADDING-LEFT: 5px; FONT-SIZE: 10pt; BACKGROUND: white; PADDING-BOTTOM: 5px; BORDER-LEFT: windowtext 1pt solid; COLOR: black; PADDING-TOP: 5px; BORDER-BOTTOM: windowtext 1pt solid; FONT-FAMILY: Courier New">
	<P style="MARGIN: 0px"> </P>
	<P style="MARGIN: 0px"><SPAN style="COLOR: blue">function</SPAN>
		anonymous()</P>
	<P style="MARGIN: 0px">{</P>
	<P style="MARGIN: 0px">    <SPAN style="COLOR: blue">return</SPAN>
		TestFunc(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4]);</P>
	<P style="MARGIN: 0px">}</P>
	<P style="MARGIN: 0px"> </P>
</DIV>

