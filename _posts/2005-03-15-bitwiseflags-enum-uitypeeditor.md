---
title: Bitwise/Flags Enum UITypeEditor
date: 2005-03-15 05:49:00 +10:00
---

Over the last couple of months, I have been developing a new skinning engine for the new versions of my applications. To help me test the new skin object model, I have created a skin builder application that relies heavily on the property grid control.

I quickly found that I didn't like the default support for bitwise enum types in the property grid. For these enum types, the property grid uses the normal drop-down list and allows for only one item to be selected. This means that if you want to set multiple values, you have to manually enter them in the property grids textbox.

I thought that a better way of doing this would be to use a CheckedListBox control instead of the normal ListBox control in the UITypeEditor. True to my style, I rushed in to coding it, only to find out that several other people have done it. It was fun, but with my lack of time, I should have checked Google first. In any case, to maintain some sense of worth and satisfaction, instead of throwing away my code, I made it better than the examples I have seen so far.

<!--more-->

My version of the enum editor will auto-detect if the flags enum has been defined. It will either display a CheckedListBox if the Flags attribute is defined, or the standard ListBox if the Flags attribute isn't defined. This makes the editor more flexible as it can be used on any enum.

Hope you find this helpful.

{% highlight vb.net %}
 Public  Class EnumEditor
# Region " Declarations "
 Inherits System.Drawing.Design.UITypeEditor

 Private m_objService As IWindowsFormsEditorService

# End  Region

# Region " Sub Procedures "

 Private  Sub ItemSelected( ByVal sender As  Object , ByVal e As EventArgs)
     ' Check if the service object exists
     If  Not m_objService Is  Nothing  Then
         ' Close the drop down control
         m_objService.CloseDropDown()
     End  If  ' End checking if the service object exists
 End  Sub

# End  Region

# Region " Functions "

 Public  Overloads  Overrides  Function GetEditStyle( ByVal context As System.ComponentModel.ITypeDescriptorContext) As System.Drawing.Design.UITypeEditorEditStyle
     ' This is a drop-down editor
     Return UITypeEditorEditStyle.DropDown
 End  Function

 Public  Overloads  Overrides  Function EditValue( _
     ByVal context As ITypeDescriptorContext, _
     ByVal provider As IServiceProvider, _
     ByVal value As  Object ) As  Object

     ' Check if the required objects exist
     If  Not context Is  Nothing _
         AndAlso  Not context.Instance Is  Nothing _
         AndAlso  Not provider Is  Nothing  Then

         ' Get the editor used to display the list box
         m_objService = CType (provider.GetService( GetType (IWindowsFormsEditorService)), IWindowsFormsEditorService)

         ' Check if the service exists
         If  Not m_objService Is  Nothing  Then

             Dim objType As System.Type = context.PropertyDescriptor.PropertyType
             Dim bFlagsDefined As  Boolean
             Dim aAttributes() As  Object = objType.GetCustomAttributes( False )
             Dim nIndex As Int32
             Dim nValue As Int32 = CType (value, Int32)
             Dim nItemValue As Int32
             Dim aValues As Array = System.Enum.GetValues(objType)
             Dim nNewValue As Int32

             ' Loop through each of the attributes
             For nIndex = 0 To aAttributes.Length - 1

                 ' Check if this attribute is Flags
                 If  CType (aAttributes(nIndex), System.Attribute).GetType.Equals( GetType (System.FlagsAttribute)) Then

                     ' We have found the flags attribute
                     bFlagsDefined = True
                     ' Break

                     Exit  For

                 End  If  ' End checking if this attribute is Flags

             Next  ' Loop through each of the attributes

             ' Check if Flags is defined
             If bFlagsDefined = True  Then

                 Dim objList As  New System.Windows.Forms.CheckedListBox
                 Dim bChecked As  Boolean

                 ' Set up the ComboBox
                 objList.BorderStyle = System.Windows.Forms.BorderStyle.None
                 objList.CheckOnClick = True

                 ' Loop through all the values
                 For nIndex = 0 To aValues.Length - 1

                     ' Get the value of this item
                     nItemValue = CType (aValues.GetValue(nIndex), Int32)

                     ' Enums that are bitwise can be defined with a 0 value
                     ' For example: None = 0
                     ' These are often used for default values
                     ' We don't want to display these as a bitwise comparison is always true
                     ' Check if the item has a valid bitwise value
                     If nItemValue > 0 Then

                         ' Determine if this item is selected
                         bChecked = ((nValue And nItemValue) = nItemValue)

                         ' Add the image to the list
                         objList.Items.Add(System.Enum.Parse(objType, CType (aValues.GetValue(nIndex), String )), bChecked)

                     End  If  ' End checking if the item has a valid bitwise value

                 Next  ' Loop through all the values

                 ' Check if the listbox height is too large
                 If objList.Height > (objList.Items.Count * objList.ItemHeight) Then

                     ' Adjust the height of the list
                     objList.Height = objList.Items.Count * objList.ItemHeight

                 End  If  ' End checking if the listbox height is too large

                 ' Display the drop down list
                 m_objService.DropDownControl(objList)

                 ' Loop through each item in the listbox
                 For nIndex = 0 To objList.CheckedItems.Count - 1

                     ' Get the value of the selected item
                     nItemValue = CType (System.Enum.Parse(objType, CType (objList.CheckedItems.Item(nIndex), String )), Int32)

                     ' Add this value to the final value
                     nNewValue = nNewValue Or nItemValue

                 Next  ' Loop through each item in the listbox

                ' Store the values selected
                value = System.Enum.ToObject(objType, nNewValue)

            Else  ' Flags is not defined

                Dim objList As  New System.Windows.Forms.ListBox

                ' Set up the ComboBox
                objList.BorderStyle = System.Windows.Forms.BorderStyle.None

                ' Loop through all the values
                For nIndex = 0 To aValues.Length - 1

                ' Add the image to the list
                objList.Items.Add(System.Enum.Parse(objType, CType (aValues.GetValue(nIndex), String )))

                Next  ' Loop through all the values

                ' Preselect the current item
                objlist.SelectedIndex = objlist.Items.IndexOf(value)

                ' Hook up the IndexChanged event to hide the drop down list
                AddHandler objList.SelectedIndexChanged, AddressOf ItemSelected

                ' Check if the listbox height is too large
                If objList.Height > (objList.Items.Count * objList.ItemHeight) Then

                    ' Adjust the height of the list
                    objList.Height = objList.Items.Count * objList.ItemHeight

                End  If  ' End checking if the listbox height is too large

                ' Display the drop down list
                m_objService.DropDownControl(objList)

                ' Check if we have a value to convert
                If  Not objlist.SelectedItem Is  Nothing  Then

                    ' Store the value selected
                    value = System.Enum.Parse(objType, CType (objlist.SelectedItem, String ))

                End  If  ' End checking if we have a value to convert

            End  If  ' End checking if Flags is defined

        End  If  ' End checking if the service exists

    End  If  ' End checking if the required objects exist

    ' Return the value
    Return value

 End  Function

# End  Region

 End  Class
{% endhighlight %}


