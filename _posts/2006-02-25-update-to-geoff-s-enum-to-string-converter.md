---
title: Update to Geoff's enum to string converter
categories: .Net, IT Related
date: 2006-02-25 03:43:00 +10:00
---

When I get some spare time here and there, I am developing a template driven code generator which uses a database schema as a data source. In doing this, I want the templates to specify what type their output is. To support this, I have defined an enum TemplateTypes with the values of TSQL, VisualBasic, CSharp and Unknown. I want to be able to convert the names of the enum values to more friendly values so I can use them in things like the tooltip text of nodes in a treeview. I remembered that [Geoff][0] had previously [posted about using type converters with enums][1] to achieve this. 

To avoid reinventing the wheel, I took his code and ran. What I found though, is that when the enum value name is converted to a nice string, it adds a space each time that it encountered an uppercase character that wasn't the first character in the name. This means that TSQL was converted to T S Q L. I made minor changes to his EnumToString function so that it only adds a space when it finds that the previous character is lowercase. I also added a conversion of _ to spaces as well. 

The function now looks like this: 

<!--more-->

 {% highlight vbnet %}
'given the passed in enum, convert to a string,
'but inject spaces in font of all capital letters.
'a regex might work best here, but for now, let's do it the long way
Private Function EnumToString(ByVal poSource As Object) As String

    'convert the value to a string
    Dim sVal As String = System.Enum.GetName(moEnumType, poSource)
    Dim sNew As New System.Text.StringBuilder

    ' Replace _ with spaces
    sVal = sVal.Replace("_"c, " "c)

    'loop through each char, looking for spaces.
    For i As Int32 = 0 To sVal.Length - 1

        If i <> 0 _
            AndAlso System.Char.IsUpper(sVal.Chars(i)) _
            AndAlso System.Char.IsUpper(sVal.Chars(i - 1)) = False Then

            sNew.Append(" "c)

        End If

        sNew.Append(sVal.Chars(i))

    Next

    Return sNew.ToString

End Function
{% endhighlight %}

I ran the following test cases: 

* T (no change)
* TSQL (no change)
* CSharp becomes CSharp (no change)
* VisualBasic becomes Visual Basic
* TWhyAmIDoingThisT becomes TWhy Am IDoing This T. This one is kinda close, but I can't really make a decision about whether multiple upper-case characters should be split.
* T_WhyAmI_DoingThisT becomes T Why Am I Doing This T

Updated: Corrected results of the CSharp test case - Thanks Geoff. 

[0]: http://codebetter.com/blogs/geoff.appleby/default.aspx
[1]: http://codebetter.com/blogs/geoff.appleby/archive/2004/11/18/32533.aspx
