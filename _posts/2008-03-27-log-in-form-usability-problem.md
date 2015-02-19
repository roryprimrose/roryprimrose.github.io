---
title: Log in form usability problem
categories : IT Related
date: 2008-03-27 13:37:59 +10:00
---

I've just realised a usability problem with website log in forms. The "remember my password" checkbox is almost always below the log in button. For keyboard support, the sequence goes like this:

1. Give focus to the username field (any good app would do this for you)
1. Type in username
1. Tab to focus on password field
1. Type in password
1. Tab to focus on the log in button (with perhaps another tab to get over a cancel button)
1. Tab to focus on the checkbox
1. Space to check the checkbox
1. Shift-Tab to give focus back to the log in button with perhaps another tab if you need to skip over a cancel button
1. Space to fire the log in button

If the forms where changed to simply have the checkbox above the log in button, then the keyboard sequence would be:

1. Give focus to the username field (any good app would do this for you)
1. Type in username
1. Tab to focus on password field
1. Type in password
1. Tab to focus on the checkbox
1. Space to check the checkbox
1. Tab to focus on the log in button
1. Space (to fire the Log in button)

This would read better as the user only traverses down the screen rather than down and then up. It removes unnecessary actions and makes more sense.


