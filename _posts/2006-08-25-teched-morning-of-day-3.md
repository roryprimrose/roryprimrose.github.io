---
title: TechEd - Morning of Day 3
categories : IT Related
tags : TechEd
date: 2006-08-25 13:03:08 +10:00
---

I was quite surprised that most people were up and awake after the night before. I do know of a couple of people who partied hard at the nightclub, then went on from there. They didn't quite make the first session, but managed to get there for the second session of the day.

My first session for the day was _ASP.NET 2.0 Tips and Tricks_ by Scott Guthrie. I had previously seen the PowerPoint deck and the code for this presentation from previous times Scott has presented it throughout the year. Even though I was reasonably familiar with the content, it was really good to listen to Scott as he discussed each of the items in the session. He was able to add a lot of information and background to the slides. He also answered a lot of really good questions from the audience.

<!--more-->

Session #2 was _Visual C#: Future Directions and Tips and Tricks_ that Dan Green presented. He was given the session without a whole lot of notice. I think he did a really good job and presented an amazing amount of really awesome IDE and 3rd part tools and features. The only issue I had from the session was that many of the items Dan presented were around the IDE and the framework rather than being C# specific. While this was still really good, I wanted to get deeper into more C# specific tips and tricks. I would like to get access to his presentation deck though.

The last session for the morning was _A Developer's Overview of Windows Presentation Foundation_ by [Arik Cohen][0]. Of all the cool stuff that is coming out with [NetFX][1], WPF is the one I am most passionate about. I love what user interfaces can be, but am usually left disappointed by implementations that developers have come up with. The idea that there can be a tighter working relationship between designers and developers is a situation that I am very much looking forward to. This first WPF session from Arik was a great overview of the benefits of WPF and what you can achieve with WPF.

One of the things that I don't like about Win32 UI development is that controls are almost always child windows (unless a developer has done the long hard work of rendering everything themselves). This means that the UI is restricted from using cool effects like alpha blending a controls rendering and achieving per-pixel alpha blending of the window. In Win32, there simply is no way of getting per-pixel alpha-blending on a parent window if it contains child controls that are windowed controls. I asked Arik if per-pixel alpha-blending was fully supported in WPF and he said that it is because all rendering of a WPF form is done using DirectX. We can finally enter the awesome alpha blended UI world, but people please, use your new powers for good, not evil!

I loved the demos that Arik showed. I had seen the [Healthcare demo][2] at the end of last year, but all the demos were a great demonstration of what can be easily achieved with WPF. My favourite was the treeview that was rendered as a 3D disc with items around the edge. It was a great way of displaying relationship information but not allowing the UI to be too cluttered as parent nodes in the hierarchy were shrunk and the current node was enlarged.

I think that there are people (managers of project budgets especially) who will think that WPF and all the cool stuff that goes with it (animations etc) is a waste of time, money and development resources. I really liked how Arik indirectly addressed this issue. He was talking about the animations used in the Healthcare application and the treeview control in the other demo (a CRM application?). He was saying that the animation is important because it gives visual feedback to the user:

* that they have done something;
* indicating what the affect of the action is;
* in a way that can mean something to the them.
The example of the third point was in the Healthcare demo when a different patient was selected. The UI folded over horizontally to display the selected patient. This is an action that is familiar to doctors who would traditionally have a clipboard of patient information and they would fold over the pages to look at subsequent pages.

The other thing I like is that it will be very easy to achieve these new features and the designers will have the tools to do most, if not all, of that work.

As much as I am looking forward to a world of sweet UI's, I am concerned that we are simply giving developers another method of creating really poor UI designs. Most projects I have seen have had poor UI's simply because UI designers were not used. Unfortunately, I don't think people appreciate how important a good UI is to the success of an application and the users ability to use it. I hope this changes.

[0]: http://blogs.msdn.com/arikc/default.aspx
[1]: http://www.netfx3.com
[2]: http://channel9.msdn.com/showpost.aspx?postid=109413
