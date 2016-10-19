---
title: Bridging between NSubstitute and FluentAssertions
categories: .Net
tags: Unit Testing
date: 2014-10-08 05:41:00 +10:00
---

The two go-to packages I use for unit testing are [NSubstitute][0] and [FluentAssertions][1]. NSubstitute is a wonderful package that solves all the pain points I had testing with RhinoMocks. Similarly, FluentAssertions is an awesome package for running assertions on data with very powerful evaluations. A great feature of both these packages is the really good feedback about failures. There is one scenario however that falls in the cracks of between the functionality of these two packages.

NSubstitute is really good at setting up stub behaviour using predicate or value argument matching and asserting received calls with the same argument matching. FluentAssertions is really good at asserting whether values satisfy specified criteria. The difference between these packages is that NSubstitute works with predicate expressions whereas FluentAssertions evaluates assertions and throws an exception if the assertions fail.

The gap in functionality here is where I want to use NSubstitute to evaluate received calls but the power of FluentAssertions evaluation in the argument matching. This doesn't work because NSubstitute uses Arg.Is&lt;T&gt;
    (Expression&lt;predicate&lt;T&gt;&gt;) whereas FluentAssertions will throw an exception. We need the ability to bridge these two packages so that we can have the power of FluentAssertions executing within an NSubstitute argument matcher and allowing the test code to remain readable.

<!--more-->

Getting this to work uses two key pieces, IArgumentMatcher from NSubstitute and AssertionScope from FluentAssertions.

```csharp
public static class Verify
{
    private static readonly ArgumentSpecificationQueue _queue;

    static Verify()
    {
        _queue = new ArgumentSpecificationQueue(SubstitutionContext.Current);
    }

    public static T That<T>(Action<T> action)
    {
        return _queue.EnqueueSpecFor<T>(new AssertionMatcher<T>(action));
    }

    private class AssertionMatcher<T> : IArgumentMatcher
    {
        private readonly Action<T> _assertion;

        public AssertionMatcher(Action<T> assertion)
        {
            _assertion = assertion;
        }

        public bool IsSatisfiedBy(object argument)
        {
            using (var scope = new AssertionScope())
            {
                _assertion((T)argument);

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
}
```

The Verify.That method is similar in syntax to the Arg.Is&lt;T&gt; method in NSubstitute. It takes Action&lt;T&gt; so that it can evaluate the T value using the AssertionMatcher&lt;T&gt; class. The AssertionMatcher class runs the action within an AssertionScope so that it can capture any FluentAssertions failures. At this point, AssertionMatcher essentially converts the outcome into a predicate rather than allowing FluentAssertions to throw an exception.

A key outcome of this class is to report failures. This is simply done by tracing out the FluentAssertion failures to the test result trace output. This is nice because it will then be combined with the NSubstitute failure reporting so that we know why the failure occurred and we know where.

This can be used like the following:

```csharp
validationStore.Received()
    .AddOutstandingVerification(
        Verify.That<OutstandingVerification>(
            y => y.Expires.Should().BeCloseTo(DateTimeOffset.UtcNow.Add(verificationExpiry), 2000)));
```

The failure reporting of this test would then look like the following:

```text
Debug Trace:
Expected date and time to be within 2000 ms from <2015-10-09 22:18:00.911>, but found <2014-10-09 22:18:00.895>.
Expected date and time to be within 2000 ms from <2015-10-09 22:18:00.930>, but found <2014-10-09 22:18:00.895>.
Expected date and time to be within 2000 ms from <2015-10-09 22:18:00.931>, but found <2014-10-09 22:18:00.895>.
Test method
NSubstitute.Exceptions.ReceivedCallsException: Expected to receive a call matching:
    AddOutstandingVerification(Matcher`1[OutstandingVerification])
Actually received no matching calls.
Received 1 non-matching call (non-matching arguments indicated with '*' characters):
    AddOutstandingVerification(*OutstandingVerification*)
```

Easy done.

[0]: http://nsubstitute.github.io/
[1]: http://www.fluentassertions.com/
