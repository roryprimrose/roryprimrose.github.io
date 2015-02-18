---
title: Cache Expiration Policies
date: 2008-10-08 11:57:24 +10:00
---

 This article aims to provide an overview about caching expiration policies and how they can be used. While the concepts are technology agnostic, the article references the [System.Web.Caching.Cache][0] exposed from [HttpRuntime][1] for code examples and also some references to the [Caching Application Block][2] in EntLib. 

**What is a cache expiration policy?**

 A cache expiration policy is a combination of concepts which define when a cache entry expires. Once a cache entry has expired, it may be removed from a cache. The policy is typically assigned when data is added to the cache and is normally custom to a single cached entry based on characteristics of the entry. 

 The specific implementation of a cache expiration policy depends on the caching framework and the requirements of the cache data. Typically, a cache expiration policy is defined by using one or more of the following concepts: 

* Absolute expiration
* Sliding expiration
* Cache dependency
* Cache priority

 If more than one of these concepts are used for a cache entry, then the entry is likely to expire because of one part of the expiration policy has been processed before another part of the policy is required to expire the entry. As each part of the policy is evaluated, the first part that requires an expiration of the cache entry will cause the expiration to occur regardless of whether other parts of the policy are outstanding. 

**Absolute expiration**

 Absolute expiration refers to a specific point in time when the cache entry will expire. Once that point in time has elapsed, the cache entry is expired and can be removed from the cache. 

 In the following example, an entry is added to the cache with a cache expiration policy that defines an absolute expiration of 25th December 2008 at 4:30pm. Once 4:30pm rolls around on that day, the entry will be expired. Assuming the item is added to the cache on 25th December 2008 at 4:00pm, this policy defines that the cache entry will only be alive for 15 minutes. 

 using System; 

 using System.Web; 

 using System.Web.Caching; 

 namespace ConsoleApplication1 

 { 

 internal  class  Program

 { 

 private  static  void Main( String [] args) 

 { 

 DateTime absoluteExpiration = new  DateTime (2008, 12, 25, 16, 15, 00); 

 HttpRuntime .Cache.Add( 

 &quot;AbsoluteCacheKey&quot; , 

 &quot;This is the data to cache&quot; , 

 null , // No cache dependency defined

 absoluteExpiration, 

 Cache .NoSlidingExpiration, 

 CacheItemPriority .Normal, 

 null ); // No callback defined

 } 

 } 

 } 

**Sliding expiration**

 Sliding expiration refers to a span of time in which the cache entry must be retrieved from the cache in order to prevent expiration. 

 In the following example, an entry is added to the cache with a cache expiration policy that defines a sliding expiration of 5 minutes. The entry will stay in the cache as long as it is read within 5 minutes of the previous read. As soon as 5 minutes elapse without a read of that item from the cache, the entry will be expired. 

 using System; 

 using System.Web; 

 using System.Web.Caching; 

 namespace ConsoleApplication1 

 { 

 internal  class  Program

 { 

 private  static  void Main( String [] args) 

 { 

 TimeSpan slidingExpiration = new  TimeSpan (0, 5, 0); 

 HttpRuntime .Cache.Add( 

 &quot;SlidingCacheKey&quot; , 

 &quot;This is the data to cache&quot; , 

 null , // No cache dependency defined

 Cache .NoAbsoluteExpiration, 

 slidingExpiration, 

 CacheItemPriority .Normal, 

 null ); // No callback defined

 } 

 } 

 } 

**Cache dependency**

 Cache dependencies are references to other information about the cache entry. The dependency might be on a file or database record. When the dependency has changed, the cache entry is expired. The most common scenario of cache dependencies is a dependency on a file path for data loaded from that file. 

 In the following example, file data is read from disk and added to the cache with a cache expiration policy that defines a dependency on the file path. When the file is updated (usually out of process), the file change event detected by the cache dependency will cause the entry to be expired. 

 using System; 

 using System.IO; 

 using System.Web; 

 using System.Web.Caching; 

 namespace ConsoleApplication1 

 { 

 internal  class  Program

 { 

 private  static  void Main( String [] args) 

 { 

 const  string filename = @&quot;C:\test.xml&quot; ; 

 CacheDependency dependency = new  CacheDependency (filename); 

 String contents = File .ReadAllText(filename); 

 HttpRuntime .Cache.Add( 

 &quot;DependencyCacheKey&quot; , 

 contents, 

 dependency, 

 Cache .NoAbsoluteExpiration, 

 Cache .NoSlidingExpiration, 

 CacheItemPriority .Normal, 

 null ); // No callback defined

 } 

 } 

 } 

**Cache priority**

 Cache priority indicates the importance of the data relative to other cache entries. This is used to determine which items to expire in the cache first when system resources become scarce. 

 In the following example, a low cache priority is defined. This cache entry will be expired before other entries that have a higher priority. 

 using System; 

 using System.IO; 

 using System.Web; 

 using System.Web.Caching; 

 namespace ConsoleApplication1 

 { 

 internal  class  Program

 { 

 private  static  void Main( String [] args) 

 { 

 const  string filename = @&quot;C:\test.xml&quot; ; 

 CacheDependency dependency = new  CacheDependency (filename); 

 String contents = File .ReadAllText(filename); 

 HttpRuntime .Cache.Add( 

 &quot;DependencyCacheKey&quot; , 

 contents, 

 dependency, 

 Cache .NoAbsoluteExpiration, 

 Cache .NoSlidingExpiration, 

 CacheItemPriority .Low, 

 null ); // No callback defined

 } 

 } 

 } 

**When are items actually flushed from the cache?**

 Most caching frameworks will only remove expired items from the cache when system resources are scarce or when the cache is referenced. This means that a cache entry that has expired due to an absolute or sliding expiration may not be removed from the cache until some future time which may be well after the entry actually expired. 

 This is done for performance. The caching frameworks normally use a scavenging algorithm that looks for expired entries and removes them. This is typically invoked when the cache is referenced rather than when the items actually expire. This allows the cache framework to avoid having to constantly track time based events to know when to remove items from the cache. 

**HttpRuntime.Cache vs Caching Application Block**

 There are two main differences between these caching frameworks. Firstly, the Caching Application Block in EntLib allows you to define both an absolute expiration and a sliding expiration for an expiration policy while HttpRuntime.Cache only supports one or the other. Secondly, EntLib requires a decent amount of configuration whereas HttpRuntime.Cache works out of the box. 

 Which one to use? Well it depends. Here are some things to consider: 

* Advice from [Scott Guthrie][3] and his team is that HttpRuntime.Cache may be used in non-ASP.Net scenarios.
* Do you need the flexibility of both absolute and sliding expiration? 
  * If you do, prefer EntLib.
  * If you don't, probably prefer HttpRuntime.Cache.
* Is your code conducive to configuration requirements? 
  * For example, framework type components you write that use caching probably shouldn't bundle a requirement on consumers of the assembly to put specific configuration in their application config files. In these cases, prefer HttpRuntime.Cache.

**Policy suggestions**

 What should you use for your policy? It typically depends on answers to the following questions: 

1. How often is the data read?
 A cache entry that is read very often will suit a sliding expiration. This allows for fluctuations in the frequency of reads. If the frequency reduces (say overnight), then it will expire. 

 A cache entry that is not read very often will suit an absolute expiration. This allows the data to be around for a while, just in case it is referenced, but forces it to expire at some point. 

1. How often is the data changed?
 A cache entry that is changed often will suit a cache dependency if supported. A custom cache dependency may be required in this case. Without a cache dependency, a short absolute expiration would be appropriate. 

 A cache entry that is not changed often will suit either a sliding or absolute expiration depending on the answers to the other questions. Ideally, both a sliding and absolute expiration would be used. 

1. How large is the data?
 A cache entry that is a large amount of data will suit a low priority. This will allow the system to expire the cache entry when it has scarce resources, namely RAM. A sliding expiration would also be a good combination with large data. If it is not referenced for a while, it is good to release this memory. 

 A cache entry that is not a large amount of data may use a high priority if it is read more often than other entries. It is best to leave the cache priority as the default value unless the entry has different characteristics compared to other entries based on answers to the above questions. 


 Ideally a combination of these concepts will be used to build a policy. A good combination is sliding expirations that also have an absolute expiration. Priorities should only be assigned for cache entries that are different to the others (frequency of reads, importance of data or the size of it). Cache dependencies are good, but are not always appropriate. The combinations available are also restricted to the caching framework used. 

[0]: http://msdn.microsoft.com/en-us/library/system.web.httpruntime.cache.aspx
[1]: http://msdn.microsoft.com/en-us/library/system.web.httpruntime.aspx
[2]: http://msdn.microsoft.com/en-us/library/cc511588.aspx
[3]: http://weblogs.asp.net/Scottgu/
