---
title: Supporting scopes with xUnit ILogger
categories: .Net
tags: 
date: 2019-03-25 18:10:00 +11:00
---

I [posted previously][0] about a [package][1] I put together to easily support rendering ```ILogger``` messages to the xUnit test output. This package now supports rendering logging scopes thanks to a pull request by [Sebastian Weber][2].

<!--more-->

Here are some samples from the [package unit tests][3] to demonstrate how the log output looks.

```csharp
namespace Divergic.Logging.Xunit.UnitTests
{
    using System;
    using global::Xunit;
    using global::Xunit.Abstractions;
    using Microsoft.Extensions.Logging;
    using ModelBuilder;

    public class ScenarioTests
    {
        private readonly ITestOutputHelper _output;

        public ScenarioTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestOutputWritesMessagesInContextOfScopes()
        {
            var sut = _output.BuildLogger();

            sut.LogCritical("Writing critical message");
            sut.LogDebug("Writing debug message");
            sut.LogError("Writing error message");
            sut.LogInformation("Writing information message");
            sut.LogTrace("Writing trace message");
            sut.LogWarning("Writing warning message");

            using (sut.BeginScope("First scope"))
            {
                sut.LogInformation("Inside first scope");

                using (sut.BeginScope("Second scope"))
                {
                    sut.LogInformation("Inside second scope");
                }

                sut.LogInformation("After second scope");
            }

            sut.LogInformation("After first scope");
        }

        [Fact]
        public void TestOutputWritesScopeBoundariesUsingObjects()
        {
            var sut = _output.BuildLogger();

            sut.LogCritical("Writing critical message");
            sut.LogDebug("Writing debug message");
            sut.LogError("Writing error message");
            sut.LogInformation("Writing information message");
            sut.LogTrace("Writing trace message");
            sut.LogWarning("Writing warning message");

            var firstPerson = Model.Create<Person>();

            using (sut.BeginScope(firstPerson))
            {
                sut.LogInformation("Inside first scope");

                var secondPerson = Model.Create<Person>();

                using (sut.BeginScope(secondPerson))
                {
                    sut.LogInformation("Inside second scope");
                }

                sut.LogInformation("After second scope");
            }

            sut.LogInformation("After first scope");
        }

        [Fact]
        public void TestOutputWritesScopeBoundariesUsingValueTypes()
        {
            var sut = _output.BuildLogger();

            sut.LogCritical("Writing critical message");
            sut.LogDebug("Writing debug message");
            sut.LogError("Writing error message");
            sut.LogInformation("Writing information message");
            sut.LogTrace("Writing trace message");
            sut.LogWarning("Writing warning message");

            using (sut.BeginScope(Guid.NewGuid()))
            {
                sut.LogInformation("Inside first scope");

                using (sut.BeginScope(Environment.TickCount))
                {
                    sut.LogInformation("Inside second scope");
                }

                sut.LogInformation("After second scope");
            }

            sut.LogInformation("After first scope");
        }

        private class Person
        {
            public DateTime DateOfBirth { get; set; }

            public string Email { get; set; }

            public string FirstName { get; set; }

            public string LastName { get; set; }
        }
    }
}
```

These tests render the following in the xUnit test results.

## TestOutputWritesMessagesInContextOfScopes
```
Critical [0]: Writing critical message
Debug [0]: Writing debug message
Error [0]: Writing error message
Information [0]: Writing information message
Trace [0]: Writing trace message
Warning [0]: Writing warning message
<Scope: First scope>
   Information [0]: Inside first scope
   <Scope: Second scope>
      Information [0]: Inside second scope
   </Scope: Second scope>
   Information [0]: After second scope
</Scope: First scope>
Information [0]: After first scope
```

## TestOutputWritesScopeBoundariesUsingObjects
```
Critical [0]: Writing critical message
Debug [0]: Writing debug message
Error [0]: Writing error message
Information [0]: Writing information message
Trace [0]: Writing trace message
Warning [0]: Writing warning message
<Scope 1>
   Scope data: 
   {
     "DateOfBirth": "1921-12-23T05:01:30.9001221Z",
     "Email": "kane.bettis@mega.co.nz",
     "FirstName": "Kane",
     "LastName": "Bettis"
   }
   Information [0]: Inside first scope
   <Scope 2>
      Scope data: 
      {
        "DateOfBirth": "1941-01-08T17:14:30.9521921Z",
        "Email": "carlyn.edwinson@sadecehosting.com",
        "FirstName": "Carlyn",
        "LastName": "Edwinson"
      }
      Information [0]: Inside second scope
   </Scope 2>
   Information [0]: After second scope
</Scope 1>
Information [0]: After first scope
```

## TestOutputWritesScopeBoundariesUsingValueTypes
```
Critical [0]: Writing critical message
Debug [0]: Writing debug message
Error [0]: Writing error message
Information [0]: Writing information message
Trace [0]: Writing trace message
Warning [0]: Writing warning message
<Scope: f220f378-8301-441a-8199-f7b8d2acfec6>
   Information [0]: Inside first scope
   <Scope: 490797234>
      Information [0]: Inside second scope
   </Scope: 490797234>
   Information [0]: After second scope
</Scope: f220f378-8301-441a-8199-f7b8d2acfec6>
Information [0]: After first scope
```

You can grab the NuGet package from [here][1].

[0]: /2018/06/01/ilogger-for-xunit/
[1]: https://nuget.org/packages/Divergic.Logging.Xunit
[2]: https://github.com/webseb/
[3]: https://github.com/Divergic/Divergic.Logging.Xunit/blob/master/Divergic.Logging.Xunit.UnitTests/ScenarioTests.cs