---
title: Load tests don't like code coverage
categories : .Net
date: 2008-06-02 16:13:00 +10:00
---

<p>
I have been pulling my hair out this afternoon trying to figure out why my code is performing so badly in a load test. After playing with the code and running a lot of tests, I didn't have any answers until I looked at my testrunconfig file. Code coverage was the first thing I looked at. 
</p>
<p>
With code coverage turned on, the report looked like this. 
</p>
<p>
<a href="//blogfiles/WindowsLiveWriter/Loadtestsdontlikecodecoverage_14C6D/LoadTestWithCodeCoverage.jpg"><img style="border: 0px" src="//blogfiles/WindowsLiveWriter/Loadtestsdontlikecodecoverage_14C6D/LoadTestWithCodeCoverage_thumb.jpg" border="0" alt="Load Test With Code Coverage" width="491" height="484" /></a> 
</p>
<p>
After I turned code coverage off, I was much happier. 
</p>
<p>
<a href="//blogfiles/WindowsLiveWriter/Loadtestsdontlikecodecoverage_14C6D/LoadTestWithoutCodeCoverage.jpg"><img style="border: 0px" src="//blogfiles/WindowsLiveWriter/Loadtestsdontlikecodecoverage_14C6D/LoadTestWithoutCodeCoverage_thumb.jpg" border="0" alt="Load Test Without Code Coverage" width="526" height="484" /></a> 
</p>
<p>
The Test Response Time graphs (top right) highlight the difference between the test run performance as does the the CPU usage (red line in the bottom right graph). Of the four tests, the one that is doing the most work was averaging 6.76 seconds per test with code coverage which is atrocious. Without code coverage, this test was running an average of 0.00024 seconds per test. Just a little different. 
</p>
<p>
Updated: Changed images so they are more readable and changed the scale of the Test Response Time graph so the difference of the tests can be seen. 
</p>

