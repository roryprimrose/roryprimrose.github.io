---
title: That beginners feeling - Episode 23
categories: .Net
date: 2006-01-27 08:19:00 +10:00
---

Like most things in life, if you haven't needed to do a particular task before, then you probably don't know a lot about it, let alone how to actually do the task.

Today, I felt like a novice programmer. Something very simple, but also simply misunderstood, was causing me grief. The issue was caused by multi-dimensional arrays. I simply haven't had a need for multi-dimensional arrays, probably since uni. To me, multi-dimensional arrays are about relationships between data and that normally screams databases to me, not arrays.

Each time I need to loop though an array, I will loop while the index is less than MyArray.Length. Until today, this has always been fine. Now I have a multi-dimensional array, Length is returning a much larger number than I expected. I initially thought that the Length property was returning the length of the first dimension x the number of dimensions. I thought this because I had a [24, 2] array declaration and Length was returning 48. Logical conclusion right?

<!--more-->

No problem I thought. A little messy, but I changed my code to loop while the index was less than (MyArray.Length / MyArray.Rank). This worked a treat until I found a need to change my array declaration from [24, 2] to [24, 3]. Without thinking enough about the change I was assuming that this was now a three dimensional array (yeah, so let's just ignore that little tidbit of information).

Now the code fails again. Because I was incorrect about the number of dimensions, (MyArray.Length / MyArray.Rank) is now producing a result of 36, which errors when I reference index 24 of the first dimension because it only has a length of 24 (23 being the last index).

Knowing that I am obviously making silly beginner mistakes, I read the manual. The Length property actually returns the sum of array entries available across all dimensions. The Rank property returns the number of dimensions. Neither of these properties are what I should be looking at.

The key here is the GetLength function. I originally misunderstood this function to be related to the Length property because I missed the fact that it had a parameter. GetLength is the one to call, passing it the index of the dimension you want to know about. In this case, GetLength(0) would return 24 while GetLength(1) would return 3.

Now in future, my code will be more defensive if I code my loops using MyArray.GetLength(0) instead of MyArray.Length.

Maybe this post should be titled "Arrays 101". Perhaps "Read the manual" would be a better title.

What can I say. Oops. Live and learn...


