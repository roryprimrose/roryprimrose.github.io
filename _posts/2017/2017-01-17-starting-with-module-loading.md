---
title: Starting with module loading
categories: JavaScript
tags: 
date: 2017-01-17 12:45:00 +10:00
---

I have been slowly working on rebuilding a web application from ASP.Net MVC into a SPA application over the last six months. The timescale of this work has been a blessing and a curse. It is a blessing because there has been an amazing churn in the client frameworks, build pipelines and current best practices so I get to learn a lot of new things. This is directly linked to the curse however as it makes it very difficult to rewrite an existing system and keep up with that rate of change.

I want to share what I have learnt about module loaders as part of this journey. It is new to me so please shout out any inaccuracies in the comments.

<!--more-->

I originally started my rewrite using Angular 1.5 and its subsequent patch releases. While I have used Angular before and really like it as a framework, I started to dislike the constraints it was putting on my implementation. At the time, I was waiting for Angular 2 to be released and then Aurelia just snuck across the line before it. The upskill required to go to either of these frameworks seemed quite immense.

One thing that was clear in my Angular 1.5 rewrite was that I need to start to understand and use a module loader. Module loaders essentially let your class file indicate what it requires such that the runtime will then load it for you. Lacking a module loader lead to my code being fragile during a build because the ordering of dependencies in the bundler was critical.

The latest rewrite of my web application is currently a combination of Vue.js and TypeScript for the front-end and Node.js for the back-end. It is using Webpack as the compiler/bundler to put together both parts of the system. 

The most common module loaders are CommonJS, SystemJS and AMD. I chose CommonJS as my module loader due to the similarities to the node module loader and the consumption of front-end and back-end libraries from NPM which are mostly targeting Node.js.

CommonJS works with the concept of exporting from a module which is really just a file. Other modules/files then import them for use in their own module. From what I can understand, there are several ways you can import a module. Keep in mind that I'm using TypeScript (see [here][0] for more information).

## Single import
```javascript
import otherModuleA from "./otherModuleA";
```

This is the simplest import. You use it when there is a single export from the exported module. There is nothing to distinguish in terms of exported artefacts as there is only one.

## Multiple import
```javascript
import { otherModuleB, otherModuleC } from "otherModules";
```

Modules are often more complex than just exporting a single class, function or data object. This module import syntax allows you to define which items you want to bring in from the external module. I use this syntax if I need to import both an interface and a class that implements it.

## Wildcard import
```javascript
import * as otherModuleD from "./otherModuleD";
```

This syntax imports everything exported from the external module and pins each against the ```otherModuleD``` variable. I generally avoid this syntax as it is not descriptive about the items you are importing.

I normally use this syntax if the prior two have not worked. A great example of this is importing the excellent [iziToast library][1]. Neither the single or multiple import syntax worked for this library, however the wildcard import did work. I dug into the iziToast source to try to understand why but didn't get very far. I suspect the reason is because the export from iziToast is a function that exposes other functions rather than exporting a class like in TypeScript. Smarter people that me will hopefully pitch in here.

## Require
```javascript
let otherModuleE = require("./otherModuleE");
```

I'm still confused with this one as I use it in different circumstances. My compilation process using Webpack is all traditional JavaScript that uses ```module.exports``` syntax which is pulled in via the require syntax. My build process exports a combination of objects, functions and data for the build process.

In the context of TypeScript, this syntax is used when importing a module that explicitly defines an export rather than exporting interface and class definitions. See [the documentation][2] for a good explanation of this.

One scenario I have used this is exporting a function in TypeScript rather than a class definition. In this example, I have a function that wraps an async/await function for a callback parameter.

```
import "es6-promise/auto";

export function runAsync(fn) {
    return async (done) => {
        try {
            await fn();
            done();
        } catch (err) {
            done.fail(err);
        }
    };
};
```

I use this in Jasmine unit tests when I am testing an async function. It makes the unit test class code cleaner rather than importing a class to get access to this function. 

```
const core = require("../../tests/core");

describe("send", () => {
    it("sends message via service when validation passes", core.runAsync(async () => {
        spyOn(service, "sendMessage");

        await sut.send();

        expect(service.sendMessage).toHaveBeenCalledWith(sut.request);
    }));
});
```

I guess the equivalent would be exporting this function as a static in a class that is then imported using the single import syntax above.

## Conclusion
I think I have covered the main ways to do module importing with CommonJS. There is also default exports you can read about in the [TypeScript documentation][3]. I generally stick with the import syntax as it is more aligned with TypeScript and then fall back on wildcard and require import syntaxes for anything that is out of the ordinary.

[0]: https://www.typescriptlang.org/docs/handbook/modules.html
[1]: http://izitoast.marcelodolce.com/
[2]: https://www.typescriptlang.org/docs/handbook/modules.html#export--and-import--require
[3]: https://www.typescriptlang.org/docs/handbook/modules.html#default-exports