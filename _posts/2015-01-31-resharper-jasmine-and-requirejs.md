---
title: ReSharper Jasmine and RequireJS
categories : Development
date: 2015-01-31 09:22:00 +10:00
---

I’ve been converting my code over to RequireJS and have been battling how to get this to work with the ReSharper test running which uses Jasmine. The main issue is that Jasmine runs the page on load of the browser document (the spec runner) before RequireJS has loaded dependencies. The reason for this is that RequireJS loads dependencies asynchronously. By the time they have loaded, the test run has already completed.  

<!--more-->

The fix for this is to use the Jasmine beforeEach function with the done parameter. This is something new from Jasmine 2.0 onwards. Previous versions did not use support the done callback.  

The syntax looks something like this:

{% highlight javascript %}
/// <reference path='/../../MyWebsite/Scripts/require.js'/>
/// <reference path='/../../MyWebsite/Scripts/Custom/ResponseParser.js'/>

describe('ResponseParser', function () {

    var target;

    beforeEach(function (done) {
        if (target) {
            done();

            return;
        }

        require(['ResponseParser'], function (parser) {
            target = parser;
            done();
        });
    });

    it('parse - returns empty set when data is null', function () {
        console.log('describe');

        var actual = target.parse(null);

        expect(actual).not.toBeNull();
        expect(actual.length).toBe(0);
    });

});
{% endhighlight %}

What happens here is before the first test is run, RequireJS is told to resolve the SUT. The done callback is only invoked once RequireJS has provided this value at which point it is stored at a scope available for the tests to run. All other tests in the describe block will call done straight away because the SUT has already been resolved.
