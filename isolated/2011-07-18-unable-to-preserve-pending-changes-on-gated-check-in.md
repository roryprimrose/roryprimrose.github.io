---
title: Unable to preserve pending changes on gated check in
categories : .Net
tags : TFS
date: 2011-07-18 10:02:00 +10:00
---

Gated check in with TFS 2010 is a brilliant feature. Unfortunately the &ldquo;Preserve my pending changes locally&rdquo; checkbox is sometimes disabled and it isn&rsquo;t clear from the UI why this is the case.![image][0]

There is an MSDN forum thread ([here][1]) that provides some reasons why this happens. The checkbox will be disabled if there are locks on some of the files in the changeset, the changeset contains binary files or exclusive check-out option is used on the team project.

The one that has been hitting me a lot recently is a docx deployment guide that is disabling this checkbox.

[0]: //blogfiles/image_121.png
[1]: http://social.msdn.microsoft.com/Forums/en-US/tfsbuild/thread/60afe44b-c75d-4e9c-91a1-c8c4f8b77ed4/
