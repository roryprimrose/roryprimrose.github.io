---
title: Outlook has a dialog open, but it doesn't
categories : IT Related
date: 2008-04-01 08:40:45 +10:00
---

I was playing with a new profile on my laptop last night as I was wanting to work with Outlook data syncing to my phone without changing the data on my normal profile. After I logged in, I found that Outlook wasn't responsive. I could use the mouse to do actions, but not the keyboard. I also couldn't close it. When I tried to add a SharePoint list Outlook would give me a message saying "A dialog is open. Close it and try again." The great thing was that there was no dialog open.

This frustrated me insanely for 30 minutes. I then remembered something from working at a small IT company about eight years ago. Back then, we had an issue with Word automation from VB6 on a server where the account running the service hadn't yet logged into the machine. Word was locking up on the dialog that appears asking for your name and initials. We had to log into the machine running the service with the service account, open Word, click Ok, log off and then the service would run the Word automation without a problem.

<!--more-->

It then occurred to me that Outlook uses Word by default as its email editor. So when Outlook runs, it also spins up Word. As this is a new profile and I am only interested in Outlook, I hadn't run Word yet.

Sure enough, after opening Word, clicking Ok on the user details dialog and closing Word, Outlook worked like a charm.

I am so surprised that Microsoft haven't covered this scenario. It's crazy to render an application useless because the user hasn't dealt with a dialog that isn't displayed.


