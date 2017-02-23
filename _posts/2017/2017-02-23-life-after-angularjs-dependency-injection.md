---
title: Life after AngularJS - Dependency Injection
categories: JavaScript
tags: 
date: 2017-02-23 13:50:00 +11:00
---

I [posted][0] recently about module loading in modern JavaScript development to assist in breaking up complex scripts into individual files/modules. How do we get the benefit of external modules and also get the benefits of dependency injection for unit testing our scripts?

One of the things I really like about AngularJS as a long term [SOLID practitioner][1] in the C# world is the inbuilt support for dependency injection. This post looks at how to join module based JavaScript applications with dependency injection when AngularJS is not in the picture. The code is in TypeScript however the concept is the same regardless of script flavour.

<!--more-->

Our options are very limited as JavaScript does not natively support dependency injection. I have been using Vue.js with TypeScript recently for which there is no dependency injection support. The only way to support both runtime module loading and dependency injection for unit testing is to implement "poor man's dependency injection". 

Poor man's dependency injection does not use an inversion of control (IoC) container or dependency injection framework to create dependencies that can be provided to a class. It works by a class having knowledge about how to create an instance of a dependency as required. This means that we cannot avoid a coupling between components but we can still satisfy our runtime and testing needs.

The way this works is by providing a dependency as a constructor parameter to a class. This supports unit testing where the unit test can provide a stub/mock to the constructor to test the methods of the class. The constructor can then create an instance of its dependency (therefore coupling) if no value has been provided to the constructor. This supports the runtime implementation where the application will create an instance of this class without knowledge of its dependencies.

For example:

```javascript
import Plan from "./Plan";
import { Http, IHttp } from "../../services/http";

export interface IPricesService {
    loadPlans(): Promise<Array<Plan>>;
}

export class PricesService implements IPricesService {
    public constructor(private http: IHttp = new Http()) {
    }

    public loadPlans(): Promise<Array<Plan>> {
        return this.http.get<Array<Plan>>("plans");
    }
};
```

We can now test this class by providing the Http parameter to the constructor.

```javascript
import { PricesService } from "./pricesService";
import { IHttp } from "../../services/http";
import Plan from "./plan";
const core = require("../../tests/core");

describe("prices.service.ts", () => {

    let sut: PricesService;
    let plans: Array<Plan>;

    let http: IHttp = <IHttp>{
        get: async () => {
            return plans;
        },
        post: () => {
            return null;
        }
    };

    beforeEach(function () {
        sut = new PricesService(http);

        plans = [<Plan>{name: "Test"}];
    });

    describe("loadPlans", () => {
        it("should return plans from http", core.runAsync(async () => {
            spyOn(http, "get").and.callThrough();

            let actual = await sut.loadPlans();

            expect(actual).toEqual(plans);
            expect(http.get).toHaveBeenCalledWith("plans");
        }));
    });
});
```

[0]: /2017/01/17/starting-with-module-loading/
[1]: /2010/02/17/recommended-reading-for-developers/