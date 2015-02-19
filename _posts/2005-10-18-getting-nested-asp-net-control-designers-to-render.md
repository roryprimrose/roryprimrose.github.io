---
title: Getting nested ASP.Net control designers to render
categories : .Net
tags : ASP.Net
date: 2005-10-18 23:18:00 +10:00
---

 Last week, [I posted][0] about how nested control designers in ASP.Net were not rendering. I have progressed a lot in the last week regarding getting good support for controls in the designer. No doubt some articles will follow. 

 With regard to nested designers getting called, this will only happen when the control related to the designer is hosted in an editable region. 

 The page for example is the classic editable region. You can drag and drop controls onto the page. You can select the control and change its properties through the property grid. Controls themselves may also have editable regions if their designer has been developed to support them. 

 There is an easy set of steps to demonstrate this. 

1. Open a webform in the designer
1. Drag on a Label control and clear its Text property. The designer for the Label control will render [ID Property] for the control so that the control remains visible in the designer while there isn't a Text property value.
1. Drag a Panel control onto the designer.
1. Drag the Label control into the Panel control. This is the first indication that the Panel control has an editable region. You can drag and drop controls into it and you can select, modify and delete those child controls. Notice that the Label is still being rendered with [ID Property] if its Text property is empty.
1. Drag a Placeholder control onto the designer.
1. Drag the Label control into the Placeholder control. You will notice that you can't drop the Label control onto the Placeholder control. The designer for the Placeholder control doesn't render an editable region.
1. Go into the Source view.
1. Manually move the declaration for the Label control from inside the Panel to inside the Placeholder control.
1. Go into the Design view. You will notice that now that the Label control is no longer rendered. Its designer is not working anymore.
1. Go into the Source view and add a Text property value to the Label control.
1. Go into the Design view again. You will now notice that the Label control is rendering again.

 The other interesting thing to notice is that if a control isn't clickable (ie not in an editable region), then it is also not available through the dropdown list in the property grid. The only way to modify the properties of the control is manually in the Source view. 

 Another thing to note about editable regions is that the only controls that are selectable and can be modified or deleted are the immediate children that are placed into the editable region. If the control hierarchy went Panel -&gt; Placeholder -&gt; Label, then the Placeholder would be selectable in the editable region of the Panel, but the Label would not be. To achieve that, the Placeholder's designer would also need to support an editable region. 

[0]: /2005/10/10/nested-whidbey-asp-net-control-designers/
