---
title: Generating release notes in VSTS
categories: 
tags: 
date: 2016-10-19 16:45:00 +10:00
---

Writing release documentation is one of those tasks that you need to do, but nobody wants to do it. We want the ability to automate the generation of documentation as much as possible to remove both manual labour and human error.

My team uses VSTS to manage the planning and delivery of software and we have defined it as the source of truth about our software from requirements to release. This means that (assuming good process is followed) VSTS contains all the information about the history of the product. This is the perfect source of information to produce release notes. 

<!--more-->

There are two parts for automatically generating release notes. Firstly, we need to identify what should be in release notes. Secondly, we need to be able to export that data in a way that is suitable as a release notes document. 

We defined the following criteria for release notes contents in our project:

- Includes Product Backlog Items and Bugs
- Does not include Tasks work items
- Does not include removed work items
- Must identify the release version per PBI/Bug
- Must allow for selectively excluding a PBI/Bug

**Work Item Query**

Because the source of information for the release notes are work items, the mechanism to automate this originates with a work item query. Already we can easily address the first three of the above criteria using available work item query filters.

![Initial work item query][0]

The remaining criteria start to get a little tricky with the out of the box implementation of PBI and Bug work items. 

Identifying the release version related to a PBI or Bug could be done using the *Microsoft.VSTS.Build.IntegrationBuild* field. There are issues with this however. Currently this field is not populated with VSTS Build vNext although the old XAML build system still should. Even if this was populated by a build, the build would write to this field and associate the work item (and their parent PBI and Bugs) for the current build. Each of our code changes will participate in many automated builds before they are bundled in an actual production release version. This means that this field will contain pre-release versions which we do not want included in release notes at the point of the work item query. Filtering out pre-release versions on this field would not be easy.

Our solution was to add two additional fields to our project process. We added a field to track the release version and a field for whether the work item should be included in release notes (a Yes/No pick list). You can read about modifying work items [on MSDN][1]

![Custom work item][2]

These fields were put onto the PBI and Bug work items. From here we can then add those fields into our release notes query to satisfy the remaining criteria.

![Updated work item query][3]

The release notes query filters on "Include In Release Notes <> No" to handle work items that have not had a Yes or No value selected for the field. We defined the default value on the field as Yes, but the underlying data storage of the work item stores this as empty until a value is specifically selected and saved. This means that you need to query on <> No instead of = Yes to include both default Yes and specifically selected Yes work items.

The last filter ensures that any PBI or Bug that does not have a release version is not included in release notes.

The final touches to the query are to return all the fields required for the documentation and to define the sort order. 

The fields we want to report on are:

- Id
- Work Item Type
- Title
- Tags
- Release Version
- Acceptance Criteria

We then sort the results as:

- Release Version (Descending)
- Backlog Priority (Ascending)

Now we have a query that provides all the information we require to generate release notes. We have several repositories and products in our account so we duplicate this query and include a filter on Area Path so that each query can target a specific product.

**Documentation Generation**

Now we need to export the work item query data in a way that is suitable for release notes. There is an excellent VSTS/TFS extension on the marketplace called [Enhanced Export][4]. This allows you to take a work item query or test plan and apply the XML data against an XSLT template. The output of this is HTML which you can then download as a Word or Excel document. This suits our purpose nicely.

I took the "Requirements Specification" template that came with the extension and started tweaking it to be more appropriate for our release notes. This is a very flexible system that you can modify to output release notes that suit your business purpose.

[0]: /files/2016/10/2016-10-19 15_36_42-New-Query-1-Visual-Studio-Team-Services.png
[1]: https://www.visualstudio.com/en-us/docs/work/process/customize-process-field
[2]: /files/2016/10/2016-10-19 15_46_14-Work-Item-Type-Bug.png
[3]: /files/2016/10/2016-10-19 15_49_22-New-Query-2-Visual-Studio-Team-Services.png
[4]: https://marketplace.visualstudio.com/items?itemName=mskold.mskold-enhanced-export