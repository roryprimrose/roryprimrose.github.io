---
title: Is your timer not firing?
categories : .Net
date: 2005-12-06 02:18:00 +10:00
---

 I have been developing an RSS aggregator for a while now because I am not happy with any of the apps around. My little project has also meant that I get really good experience developing in VS2005 and SQL2005. 

 I made a change late last week that the timer used to check for pending feeds was set up to run at the end of a call that loads the user and feed information from the database. The change I made was to have that method run on a different thread so that the UI wasn't tied up waiting for the database that it may or may not be able to connect to. Today, I am noticing that my timer just isn't firing. 

 I was thinking that the only thing that has changed with regard to the timer is that it is now getting initialised and started from a thread that is not the GUI thread. Why should this matter? Perhaps I am showing a little bit of ignorance here, but it shouldn't matter. 

 Although the timer is getting fired from a WM_TIMER message on a window rather than a callback, I wouldn't consider the mechanism involved (the windows message queue) to be a UI thing. Even if the windows message queue is considered UI based (as far as threading issues go), the call to set up the timer is the SetTimer API under the hood. Why would this need to be done from the GUI thread? 

 So with my guesswork in place, I start writing a delegate and associated method. Hang on; there is no Invoke method or InvokeRequired property on the Timer. Now I am thinking that my timer not firing is not to do with it being set up on a non-GUI thread. My reasoning is that surely if the timer was required to be set on the GUI thread, the control would expose this method and property. 

 With nothing else working after some more playing around, I wrote the delegate and its associated method anyway. I am now calling Invoke and InvokeRequired on the form as a thread reference point. What do you know, now the timer fires? 

 So now I know that the timer needs to be started from the GUI thread. If I had read the MSDN documentation first, I probably would have come across this: 

> _A **> Timer**>  is used to raise an event at user-defined intervals. This Windows timer is designed for a single-threaded environment where UI threads are used to perform processing. It requires that the user code have a UI message pump available and always operate from the same thread, or marshal the call onto another thread._

 You win some, you lose some. 

 On a side note, I find it interesting that if you try to do GUI updates from a non-GUI thread, an exception is now thrown under VS2005. A timer being set from a non-GUI thread doesn't have this behavior. It just doesn't do anything. Sneaky huh? 


