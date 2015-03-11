---
title: Code check in procedure
categories : .Net, IT Related
tags : TFS
date: 2013-12-20 14:14:40 +10:00
---

I’ve been running this check in procedure for several years with my development teams. The intention here is for developers to get their code into an acceptable state before submitting it to source control. It attempts to avoid some classic bad habits around source control, such as:

* Check in changes at the end of each day
* Missing changeset comments
* Using the build system as point of compiler/quality validation
* Big bang changesets
* Cross purpose changesets

<!--more-->

**Changeset Contents**

Changesets need to be related to a particular set of **related** changes. A changeset should not include changes or functionality from unrelated pieces of work. This makes reviewing changesets and work tracking very difficult. If you do need to work on unrelated pieces of work, shelve the prior work (undoing local changes) and start working on the new piece of work. Once a piece of work is checked in according to the procedure below, the previous shelveset can be brought back down to your local workspace and you can continue to work on it.

**Check In Procedure**

The following set of actions must be taken in order to check in changes to source control.

1. Pre- Check-in 
  * Code is functioning correctly and to spec.
  * All code comments are correct and well formatted
  * Code has been cleaned up and is consistent to team standards
1. Run get latest on the solution 
  * Fix any merge issues
1. Undo any files that haven't changed - see [Quick tip for undoing unchanged TFS checkouts][0]
1. Switch to Release build
1. Rebuild solution (not just build) 
  * Fix any compilation errors
  * Fix any compilation warnings that can be addressed
1. Deploy database projects to local machine as required
1. Run all tests 
  * They must all pass
1. Write a comment that describes the changeset
1. Assign a work item to the changeset
1. Raise a code review request if the changeset contains code changes 
  * Minor changesets that do not change code or have any functional change do not require a review
1. Verify that no other check ins have occurred since doing #1
1. Check in
1. Wait for build to complete (you can do other work during this process) 
  * Verify build successful or investigate any failures


[0]: /2009/09/09/quick-tip-for-undoing-unchanged-tfs-checkouts/
