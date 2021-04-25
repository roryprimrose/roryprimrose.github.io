---
title: NSubstitute and FluentAssertions Redux
categories: .Net
tags: Unit Testing
date: 2021-04-25 10:00:00 +10:00
---

Way back in 2014 I posted about [Bridging between NSubstitute and FluentAssertions][0]. The code sample provides a way of using [FluentAssertions][2] checks in [NSubstitute][1] arguments. There have been some slight changes in these packages over the last seven years. Here is an updated version of how to get this to work with the current versions of those packages.

<!--more-->

An additional change in this sample is a rename from `Verify.That` to `Match.On` which reads a little better.

```csharp
using System;
using System.Diagnostics;
using System.Linq;
using FluentAssertions.Execution;
using NSubstitute.Core.Arguments;

public static class Match
{
    public static T On<T>(Action<T> action)
    {
        return ArgumentMatcher.Enqueue(new AssertionMatcher<T>(action));
    }

    private class AssertionMatcher<T> : IArgumentMatcher<T>
    {
        private readonly Action<T> _assertion;

        public AssertionMatcher(Action<T> assertion)
        {
            _assertion = assertion;
        }

        public bool IsSatisfiedBy(T argument)
        {
            using var scope = new AssertionScope();

            _assertion(argument);

            var failures = scope.Discard().ToList();

            if (failures.Count == 0)
            {
                return true;
            }

            failures.ForEach(x => Trace.WriteLine(x));

            return false;
        }
    }
}
```

As in the 2014 post, this can be used like the following:

```csharp
validationStore.Received()
    .AddOutstandingVerification(
        Match.On<OutstandingVerification>(
            y => y.Expires.Should().BeCloseTo(DateTimeOffset.UtcNow.Add(verificationExpiry), 2000)));
```

[0]: /2014/10/07/bridging-between-nsubstitute-and-fluentassertions/
[1]: http://nsubstitute.github.io/
[2]: http://www.fluentassertions.com/