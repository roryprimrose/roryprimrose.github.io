---
title: Code coverage not available when debugging unit tests
categories : .Net
date: 2008-03-17 08:54:00 +10:00
---

 Yep, this one bit me last week. 

 I had been writing some unit tests and debugging them. When the tests were finished, I kept wanting to look at the code coverage. All I would see was the message &quot;_Code coverage is not enabled for this test run_&quot;. After trying lots of things and wasting 30 minutes, it turns out that code coverage is not available when debugging unit tests, even though code coverage is enabled through the testrunconfig file and that the build configuration is set Debug. 

 To avoid this mistake in the future, you can enable a warning message that specifically highlights the problem. Go to _Tools_, _Options_, expand the _Test Tools_ node and select _Default Dialog Box Action_. There is an option called &quot;_When starting a remote test run or a run with code coverage under the debugger:_&quot;. Set this value to &quot;_Always prompt&quot;_. The next time that you run a unit test with the debugger attached, you will get a warning message saying &quot;_Debugging tests running on a remote computer or with code coverage enabled is not supported. The tests will run under the debugger locally and without code coverage enabled._&quot;. 

 No more confusion. 


