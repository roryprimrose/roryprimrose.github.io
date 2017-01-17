---
title: Hashing on the fly
categories: .Net
date: 2007-10-02 12:10:32 +10:00
---

I have been a fan of hashing data for a while, especially for calculating SHA1 checksum values. Until now, I have always had access to the entire set of data that needs to be hashed. Using the SHA1CryptoServiceProvider makes it so easy to build a hash value. You just throw a stream or a byte array at the ComputeHash method.

What if you don't have all the data available? Streaming is the classic example here, especially when you are dealing with a stupid amount of data. It is not wise to hold an entire data set in memory if the amount of data is large. In addition to memory issues, buffering the entire data set while streaming in order to calculate a checksum destroys the point of streaming your data.

Using Reflector to checkout the HashAlgorithm implementation, I thought that there wasn't a way around it as the ComputeHash methods call down to internal methods. After thinking for a while that it wasn't possible to create hash values from chunks of data, I went back and had a look again. Turns out there are publicly available methods to support this. TransformBlock and TransformBlockFinal are the methods to call. I found the naming misleading which is why I previously overlooked them.

With these methods, you can now stream your data in chunks and build a hash value on the fly without needing the entire data set.


