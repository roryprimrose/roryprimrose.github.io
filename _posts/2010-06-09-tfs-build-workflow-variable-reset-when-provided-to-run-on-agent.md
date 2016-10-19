---
title: TFS Build workflow variable reset when provided to Run On Agent
categories: .Net
tags: TeamBuild, TFS
date: 2010-06-09 09:36:00 +10:00
---

I’ve been working with TFS Build in Visual Studio 2010 over the last week. It has been a lot of fun working with the new workflow support for build automation. I did hit an interesting issue the other day though.

My build workflow attempts to determine a “build version” for the build. The build version is used to update the build number and is then injected back into the version info files for the compilation of the solution. It is determined by finding all the version files in the solution. We tend to use a common ProductInfo.cs to contain this information so there really should be only one file found in this search. The “best” version is obtained from the set of version files using a set of business rules. There is also some logic in there for automatically incrementing the Build and Revision numbers and the ability to check the version changes back into source control so the version numbers keep incrementing on subsequent builds.

<!--more-->

What I was finding was that the build version was being reset when the Run On Agent activity was executed. I added some debug messages into the build to track what was happening to the build version.

![image][0]

This resulted in the following output in the build log.

```text
If DropBuild And Build Reason is ValidateShelveset
Initial Property Values
Condition = False
Final Property Values
Condition = False
BuildVersion: (4.3.3.10)

00:34
Run On Agent (reserved build agent TFS-BUILD01 - Agent1)
Initial Property Values
MaxExecutionTime = 00:00:00
MaxWaitTime = 04:00:00
ReservationSpec = Name=*, Tags=
BuildVersion: (0.0.0.0)
00:00
Initialize Variables
```

I posted a question out to the [Build Automation][1] forum, the [OzTFS mail list][2] and emailed [Grant][3] my query. Grant sent my email on to [William Bartholomew][4] who wrote the excellent [MS Press Team Build book][5]. William suggested putting the Serializable attribute on my build version struct because this activity sends the workflow variables to the build agent. Unfortunately this didn’t stop the variable from being reset.

The only other change that I could think of was to change the build version struct into a class. This shouldn’t matter if the type is being serialized for the build agent, but there were no other ideas to work with. Unfortunately this didn’t fix the issue either.

I did some browsing of the Run On Agent (AgentScope) activity using Reflector. It seems that reflection over workflow context properties is used to provide workflow variables to the build agent. I figured that property reflection rather than serialization might also be used to get the properties of each workflow variable to provide to the build agent. My build version uses fields not properties as a left over implementation from using a struct. I changed the version information fields to properties and deployed the new assembly to the build agent. The build version is now provided to the build agent. I also removed the Serializable attribute from the class and the build version is still valid in the build agent.

The build log now contains the following.

```text
If DropBuild And Build Reason is ValidateShelveset
Initial Property Values
Condition = False
Final Property Values
Condition = False
BuildVersion: (4.3.7.10)
00:32
Run On Agent (reserved build agent TFS-BUILD01 - Agent1)
Initial Property Values
MaxExecutionTime = 00:00:00
MaxWaitTime = 04:00:00
ReservationSpec = Name=*, Tags=
BuildVersion: (4.3.7.10)
00:00
Initialize Variables
```

So it seems that information provided to the build agent uses property reflection rather than serialization. Perhaps a particular implementation of serialization is used such that the Serialization attribute alone is not enough. If property reflection alone is used then it would be an odd choice of implementation and one that is quite limiting. Either way, using classes with properties seems to be the answer to providing workflow variables to build agents.

[0]: /files/image_11.png
[1]: http://social.msdn.microsoft.com/Forums/en-US/tfsbuild/thread/e2e30422-19e8-4868-81a8-fe878860f685/
[2]: http://oztfs.com/
[3]: http://blogs.msdn.com/b/granth/
[4]: http://blogs.msdn.com/b/willbar/
[5]: http://www.microsoft.com/learning/en/us/book.aspx?ID=12999&amp;locale=en-us