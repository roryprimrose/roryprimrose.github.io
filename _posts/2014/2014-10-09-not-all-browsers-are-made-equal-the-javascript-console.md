---
title: Not all browsers are made equal - The JavaScript console
categories: .Net
tags: JavaScript
date: 2014-10-09 09:17:00 +10:00
---

While this is stating the obvious, my JavaScript skills sometimes fail to appreciate that what works in one browser does not work in another. Classic example here is logging information to the console. I recently noticed that IE doesn't play very nice with a script that does something like console.log. While it might seem strange that console output across browsers might be different, that is just how it is.  

The primary issue here however is that scripts will still use the console but the script might not execute in a browser the supports it. This script might be something that you control, but could also be a third party script you import. In either case, there are two things that should happen here. Firstly and most importantly, the script shouldn't fail because the browser doesn't support console logging. Secondly, the console functionality should do its best attempt at getting console logging to work when at all possible. 

<!--more-->

A quick search pulled up [a StackOverflow article][0] which provides a very good workaround to prevent JavaScript errors being thrown. This script handles the first scenario by preventing errors if a function is undefined for a given browser. Unfortunately it completely hides information that could otherwise be logged. Here is my take on that script.

```javascript
(function () {
    if (!window.console) {
        window.console = {};
    }

    var outputFunctions = ["log", "trace", "info", "debug", "warn", "error"];
    var defaultFunction = function () { };

    // Find the first available default function
    for (var searchIndex = 0; searchIndex < outputFunctions.length; searchIndex++) {
        if (window.console[outputFunctions[searchIndex]]) {
            defaultFunction = window.console[outputFunctions[searchIndex]];

            break;
        }
    }

    // union of Chrome, FF, IE, and Safari console methods
    var m = [
      "log", "trace", "info", "debug", "warn", "error", "dir", "group",
      "groupCollapsed", "groupEnd", "table", "time", "timeEnd", "profile", "profileEnd",
      "dirxml", "assert", "count", "markTimeline", "timeStamp", "clear"
    ];
    // define undefined methods as noops to prevent errors
    for (var i = 0; i < m.length; i++) {
        if (!window.console[m[i]]) {
            window.console[m[i]] = defaultFunction;
        }
    }
})();
```

Instead of pointing undefined logging functions to a no-op function, it first attempts to find a default message logging function that is declared on the browser with a fall-back to a no-op function. This default function is then assigned to any console function (across all the browsers) that is not defined. By doing this, a script that calls console.error would be piped into console.log where the browser supports the log function, but not the error function. Worst case, the browser doesn't support the console at all, then all the console messages are swallowed by the no-op function.

**Update: Added table as per comment from Josh

[0]: http://stackoverflow.com/a/13817235