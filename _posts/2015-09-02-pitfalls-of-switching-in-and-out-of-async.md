---
title: Pitfalls of switching in and out of async
categories: .Net
tags: 
date: 2015-09-02 09:34:00 +10:00
---

A couple of years ago I posted a [second version of recommended reading for developers][0]. At the time I was training up my team on some of the newer development features, namely the new async support in .Net. Since then I have come across two pitfalls that can catch developers off guard.

For some background, we need to understand what the compiler does when it processes the async and await keywords. The async keyword only affects the local method, not its callers or callee's. The compiler adds a state machine into the method so that it can track what happens before and after hitting an awaited task. For example, all variables (and parameters) declared before the awaited task must be stored so that the continuation has access to them. The continuation is the code that executes after the awaited task. The continuation may or may not execute on the thread that originally invoked the method.

One thing to note here is that using the async keyword on a method that doesn't have a continuation adds a performance hit because the compiler is adding an unnecessary state machine into the method. There would be no continuation either because there is no awaited task or no code blocks after the only awaited task. To avoid the performance hit of the state machine, a simple pass-through style method should not use async.

<!--more-->

Below is a task based method, but does not have a continuation. It therefore does not need the async keyword.

{% highlight csharp %}

public Task<Something> GetSomethingAsync(Guid id, CancellationToken cancellationToken)
{
    return _repository.GetTheThingAsync(id, cancellationToken);
}

{% endhighlight %}

The next example however does have a continuation. There is a second call and a return statement after the GetTheThingAsync call.

{% highlight csharp %}

public async Task<Something> GetSomethingAsync(Guid id, CancellationToken cancellationToken)
{
    var thing = await _repository.GetTheThingAsync(id, cancellationToken).ConfigureAwait(false);
	
	await _processor.ProcessThingAsync(thing, continuationToken).ConfigureAwait(false);
	
	return thing;
}

{% endhighlight %}

I tend to write plain task based methods and only add in async once I have a continuation as the code evolves. The two pitfalls below come into play when forgetting to add the async keyword into the mix. Both scenarios are a problem because of when the code after the task gets executed.

**Pitfall #1 - Working with locally disposable resources**

One scenario I came across was when a locally disposable resource was added to a Task based method with a using statement.

{% highlight csharp %}

public Task<Something> GetSomethingAsync(Guid id, CancellationToken cancellationToken)
{
    using (var stream = new MemoryStream())
	{
        return _repository.GetTheThingAsync(id, stream, cancellationToken);
	}
}

{% endhighlight %}

This does not work because the end of the using block is a continuation. What happens here is that the repository is called and the task returned, then the stream is disposed. Most likely the task from the repository is executed after this method has completed and the using block disposes the stream before it is used.

By making the method async, the task from the repository is executed before the end of the using block at which point the stream is still valid.

{% highlight csharp %}

public async Task<Something> GetSomethingAsync(Guid id, CancellationToken cancellationToken)
{
    using (var stream = new MemoryStream())
	{
        return await _repository.GetTheThingAsync(id, stream, cancellationToken).ConfigureAwait(false);
	}
}

{% endhighlight %}

The good thing about this example is that you will know straight away that this method requires an async keyword as you will get an ObjectDisposedException when you attempt to use the stream.

**Pitfall #2 - Catching exceptions**

Another scenario I came across was when a plain Task based method was updated to include a catch block. This suffers from exactly the same problem as the first pitfall.

{% highlight csharp %}

public Task<Something> GetSomethingAsync(Guid id, CancellationToken cancellationToken)
{
    try
	{
        return _repository.GetTheThingAsync(id, cancellationToken);
	}
	catch (TimeoutException ex)
	{
	    // Do something important
		
		throw;
	}
}

{% endhighlight %}

Like the first pitfall, this catch block will never be executed. If the task was to throw a TimeoutException, this catch block would not execute because the task has already been returned to the caller and this method is no longer on the call stack.

By making the method async, the task from the repository is evaluated within the scope of the try/catch block.

{% highlight csharp %}

public Task<Something> GetSomethingAsync(Guid id, CancellationToken cancellationToken)
{
    try
	{
        return await _repository.GetTheThingAsync(id, cancellationToken).ConfigureAwait(false);
	}
	catch (TimeoutException ex)
	{
	    // Do something important
		
		throw;
	}
}

{% endhighlight %}

Unlike the first pitfall, this one is a little harder to identify. With good unit test coverage of this code however you will quickly find this issue and resolve it.

[0]: /2013/10/16/recommended-reading-for-developers-v2/
