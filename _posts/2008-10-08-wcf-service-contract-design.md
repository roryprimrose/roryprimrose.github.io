---
title: WCF service contract design
date: 2008-10-08 23:25:00 +10:00
---

A good place to start looking at service contract design is the standards document published by [iDesign][0] (found [here][1]). This article contains some additional thoughts to add to these standards. 

These ideas have come about because of a desire to achieve service designs that have minimum impact on configuration for both the client and the server. The intention is to design service contracts that will live happily with the default limits defined in WCF. The most common reasons for tweaking the WCF default configuration values are that services are returning data that is too large, or has too many objects in the graph for serialization. 

Here are some things to think of when designing service contracts: 

**Prefer chatty service interfaces**

Chatty services tend to be ones that return simplified information and use more fine-grained operations. Chunky services tend to return complex hierarchies of information and use coarse operations. In other words, the difference is that chatty services require more calls than chunky services to return the same information but allow more flexibility to return only the information that is actually required. 

Traditional services are typically chunky services. There are still a lot of people who believe that this is the only correct service design. This is probably for two reasons. Firstly, ease of use for referring to related data and secondly because connections are expensive. 

As your service becomes chunkier, you are more likely to be returning information that is not relevant to the client. Returning redundant data is more expensive than additional connections. For example, if a service that returns information about a house, the design the House data contract may contain a collection of byte arrays that hold all the images related to the house. When looking at the service design, you would expect that all the related images would be returned when a house is retrieved. 

What if there are a large number of images or the sizes of the images is large? WCF size limits may be hit in these scenarios. Each time a house object is requested, the service design also makes an assumption that the client will always want the images. Other than the additional overhead of redundant data being processed and sent over the wire, the apparent performance of the service decreases and users get cranky. 

The alternative is to break up data contracts so that they only describe a singular entity rather than the entity and other entities that are related to it. Where it can be determined that a smaller set of information of the entity is very regularly requested, then this 'summary' information can be split out into another data contract with related operations on the service. For example, if 85% of house retrievals are interested in the address, not the other 15 properties on the House data contract, then you can create a HouseSummary data contract for greater efficiency. These metrics are normally determined through business analysis. 

**Only return array based types from an operation. Never return an array of items as a property of a data contract.**

If a data contract contains a property that returns an array based type, the service design doesn't constrain how many items will be returned. Applications don't often have business rules that say you can only have a maximum of [insert number here] related records. These relationships are normally defined as a foreign key relationship in a relational database without business rules or constraints that limit the relationships. Taking the house example, the service design doesn't define the maximum number of house images can be stored for a house and business rules are most likely not going to address this either. The result is that it would be easy to have a record that has too many other related records such that we run into the default WCF size limits. 

The exception to this rule is where there are clear business rules and/or data store constraints that define how much data is in the array. For example, an array of bytes that define a SHA1 hash value. These are always going to be 20 bytes. This is both a rule and a known maximum size limit. Knowing this helps to determine whether the data contract will break the default WCF size limits. **&#160;**

**Always support paging for operations that return array types**

This basically has the same issue as returning array based types as properties of data contracts. Without paging parameters, the design of the service operation doesn't define how many items are returned. The same size limitations come into affect and the service call may crash because too much data is being passed around. 

There is also a performance implication on this one as well. It is much quicker to return 10 items than it is to return 100 (assuming 100 items would fit within the size restrictions). Client UX is all about the experience. A slow UI is bad UX. The service design can make some safe assumptions that if 10 records are returned from an operation, the user will need to review those items before moving to the next 10 items. The user gets a better response time in getting the first 10 and while reviewing those records, the next 10 can be pulled down from the service in the background. To save bandwidth, you can force the user to manually indicate that they want the next set (think Google paging). 

To support paging in service operations, the operation needs to be passed a page index and a page size. 

**Only provide a single data contract parameter to an operation or a single value type if only one value is required.**

This isn't actually anything to do with performance or data size limits. It is more a code churn issue. Service contracts tend to evolve a lot during their first few development cycles. It is so much easier for consumers to deal with modified data contracts rather than changing service operation signatures, especially for generated client proxies. 

**Never bake your security into the service operation.**

Again this is not related to performance or size limits. This is simply good design. Don't constraint your service design to a specific security model. The most common example is passing a username and password to a service in order to access some back-end resource. There are better ways around this. With regard to the username/password example, see an alternative in [this post][2]. 

**Move your exception management and exception shielding from your service implementation into an IErrorHandler implementation.**

See [this post][3] for more information about wiring up IErrorHandler implementations using configuration. 

If your IErrorHandler implementation (or at least one of them) is doing exception shielding, you may consider applying the error handler as an attribute of the service implementation class rather than leaving it up to configuration. There is a risk with configuration that it will be missing, or incorrect configured such that you don't have exception shielding protecting your service. This may represent a security risk.

See [this post][4] for more information about wiring up IErrorHandler implementations by compiling them against the service implementation using an attribute.

**Conclusion**

These are just a few ideas. Another way of checking for appropriate service contract design is to calculate the maximum amount of bytes that a message will contain as defined by the data contract passed to or returned from a service operation. If the answer is that the amount can't be determined, then that should raise red flags. This will be a risk that needs to be managed, such as paging of array based types. If it is determined that the message is too large for WCF default limits, then the granularity of the data contracts needs to be reviewed (chatty vs chunky data contracts and operations). If it can be determined that some information is used more often that other information for an entity, then consider implementing summary data contracts. 

&#160;

**Updated**

Formatting and information about IErrorHandler has been updated.

[0]: http://www.idesign.net/
[1]: http://www.idesign.net/idesign/download/IDesign%20WCF%20Coding%20Standard.zip
[2]: /post/2008/04/07/wcf-security-getting-the-password-of-the-user.aspx
[3]: /post/2008/04/07/implementing-ierrorhandler.aspx
[4]: /post/2008/11/07/Strict-IErrorHandler-usage.aspx
