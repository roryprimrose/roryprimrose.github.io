---
title: Crisis of Code Contracts faith
tags: Azure, TFS
date: 2012-11-08 15:31:00 +10:00
---

I really like Microsoft's Code Contracts. The concept is absolutely great, but I'm starting to wonder if using them is worth it. Here are the pro's and con's as I see them.

<!--more-->

Pros

* Contracts applied once for interfaces and abstract classes
* Automated documentation generation of exceptions thrown by an API
* IDE warnings about contract violations

Cons

* Becoming increasingly slow to compile with the static checker
* FxCop constantly complains that parameters are not validated
* Not supported on TFS online build (Azure build agents)

The first pro is really great and I don't want to let go of this one easily. Given that I still feel that [each implementation should be tested][0] however, I'm not really gaining any level of productivity out of this.

The second pro is really only worthwhile if you are actually generating documentation. My [Neovolve Toolkit][1] is the only solution I have in this scenario so it isn't much of a benefit.

The third pro is now irrelevant as I have turned off the static checker because compile time has become so bad (see con #1)

The second con is now polluting my code base with FxCop suppressions which is just adding white noise to my code.

On a side note, Code Contracts causes you to spend a lot of time getting your code "exactly right" to satisfy its rules. This has good and bad sides to it so I don't list it as either a pro or a con. The reality is however that the extra effort in getting code good enough for the contracts engine is not really worth it.

So things are not looking good for code contracts. I think I'm at the tipping point where I let Code Contracts go and migrate back to using traditional guard clauses. I'm just not getting enough benefit from using them, but they are bringing a lot of issues to my solutions.

Thoughts?

[0]: /2012/03/20/should-code-contracts-be-tested/
[1]: http://neovolve.codeplex.com/releases/view/53499
