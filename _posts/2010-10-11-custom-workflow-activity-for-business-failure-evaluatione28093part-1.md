---
title: Custom Workflow activity for business failure evaluation–Part 1
categories : .Net
tags : WF
date: 2010-10-11 17:14:34 +10:00
---

Following on from my [series][0] about custom WF support for dependency resolution, this series of posts will look at support for business failure evaluation. 

Every project I have worked on has some kind of requirement to execute data validation and/or business rules when running a business process. Data validation could be a test that a request message contains an email address whereas a business rule may be that the provided email address is unique. Providing a custom WF activity to facilitate validation allows for rapid development of business processes.

<!--more-->

The high-level designer requirements for the custom WF support are:

* Failure identifies a code and a description
* Failure evaluation supports conditional expressions
* Code value must be a generic type
* Failures are thrown in an exception
* Exception must support multiple failures
* Adequate design time support

The remainder of this post will outline the reasons behind these requirements.

**Failure identifies a code and a description**

Descriptions are strings that often provide detailed information about a failure. Unfortunately descriptions are not sufficient as a reference point for taking action based on the failure. 

Consider a UI that invokes a component that in turn throws a failure exception. The UI could provide a good user experience by identifying the input field on the form that relates to the failure. Matching failure descriptions to achieve this is fragile because different culture settings may result in different descriptions that are logically the same but are actually different as far as strings are concerned.

Using a code value is a culture independent way of identifying the failure. A culture aware application can identify a UI field related to the culture agnostic code and display the culture aware failure description for the user.

**Failure evaluation supports conditional expressions**

The custom activity should support evaluating a conditional expression that determines whether the activity is going to result in a failure. This is important to prevent the developer having to always surround the custom activity with If statements that run the business rules for the failure.

**Code value must be a generic type**

The custom activity is a common reusable workflow activity. As such it cannot know what data type is going to be used to represent the code value. One application may use integer codes whereas another may use Guid or string values. Defining the failure with a generic code allows the developer to use a code value that is suitable to their requirements.

**Failures are thrown in an exception**

Business failures in this design are intended to be exceptional circumstances that are handled within the structured error handling design of .Net. The custom activity support should therefore throw failures up the call stack using an exception.

**Exception must support multiple failures**

Business validation is very different to system validation. System validation tends to be single issue identification whereas business validation often identifies multiple issues. 

Consider a registration request where the request contains FirstName, LastName and Email. The business validation rules for such a request may be that all fields must be provided. The system would provide a poor user experience if every submission of the registration request resulted in a new failure message. This process can be streamlined by providing one exception on the first attempt that identified all three validation failures.

**Adequate design time support**

Any custom workflow activity should provide an adequate design time experience. This custom activity support should allow the developer to manage all aspects of the failure evaluation process through the designer experience.

The next post will look at the base framework for supporting business validation.

[0]: /2010/10/01/custom-windows-workflow-activity-for-dependency-resolutione28093wrap-up/
