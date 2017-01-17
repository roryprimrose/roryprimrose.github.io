---
title: Control registrations from web.config
categories: .Net, IT Related
tags: ASP.Net
date: 2006-05-16 09:24:00 +10:00
---

[Scott Guthrie][0] posted some ASP.Net tips several weeks ago. There were heaps of great ideas that he put into his presentation, one of which was about registering controls for aspx pages.

User controls and custom controls that are used on a page need to be registered. The control registration allows a tag prefix to be defined and identifies the location where the control can be found. These control registrations are normally placed at the top of the aspx markup along with the page directive. If you drag and drop an unregistered control onto the page, the registration will be added for you (with the exception of dragging user controls onto the markup view).

If you happen to come across a situation where controls change location, assembly or namespace, then every registration for the controls affected will need to be changed. This means that, when using the typical control registration method, each aspx page that uses those controls needs to be changed. This maintenance problem is mostly solved by Scott's tip of putting the control registrations into the web.config file by adding add elements under system.web/pages/controls.

Edit - Removed comment about this not working for master pages. Something must have gone wrong with my build as it was throwing compile errors with registrations missing from the master page. After emailing Scott and retesting this, I have found that it is fine and works with master pages as expected.

[0]: http://weblogs.asp.net/scottgu/archive/2006/04/03/441787.aspx
