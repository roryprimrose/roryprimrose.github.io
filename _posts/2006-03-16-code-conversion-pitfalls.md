---
title: Code conversion pitfalls
categories : .Net, IT Related
date: 2006-03-16 23:06:00 +10:00
---

<p>After doing four days of training and meetings in Brisbane, I am now back in the office reviewing some code. I had originally developed some web custom controls in VB (the organisations language of choice) that were then put into an architecture framework. This required that the code be converted from VB to C# which is the language of the framework involved. This work was done by another person while I was back in my prior UI design team. </p>  <p>Doing a code review of C# that is based on my VB code has put me in a new situation. Not having done this before, I am trying to think of scenarios that look safe, but are trouble under the surface. Turns out I found one very quickly. In this case, string comparisons are the danger. The VB code often had statements like this: </p>  <div style="padding-bottom: 0px; margin: 0px; padding-left: 0px; padding-right: 0px; display: inline; float: none; padding-top: 0px" id="scid:812469c5-0cb0-4c63-8c15-c81123a09de7:26259b00-1fdc-43ac-8699-1d32dc0bd47a" class="wlWriterEditableSmartContent">{% highlight vbnet linenos %}If sSomeValue = String.Empty Then

    ' Do something here

End If{% endhighlight %}</div>
<!--EndFragment-->

<p>VB is very forgiving with its string comparisons and it attempts to cover all the possibilities. If a straight language conversion is done, the same result does not occur in C#. To test this, I came up with a program in VB and in C#. </p>

<p>Here is the VB version of the program:</p>

<div style="padding-bottom: 0px; margin: 0px; padding-left: 0px; padding-right: 0px; display: inline; float: none; padding-top: 0px" id="scid:812469c5-0cb0-4c63-8c15-c81123a09de7:da588adc-18a1-4a39-9c78-ebc86050c214" class="wlWriterEditableSmartContent">{% highlight vbnet linenos %}Public Class Form1 
  
    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click 
  
        RunTests("""""", "") 
        RunTests("String.Empty", String.Empty) 
        RunTests("Nothing", Nothing) 
        RunTests("ThisValue", "ThisValue") 
  
    End Sub 
  
    Private Sub RunTests(ByVal sTestName As String, ByVal sTest As String) 
  
        Debug.WriteLine(New String("_"c, 50)) 
        Debug.WriteLine(String.Empty) 
        Debug.WriteLine("Testing string value " &amp; sTestName) 
        Debug.WriteLine(String.Empty) 
  
        If (sTest = "") Then 
            Debug.WriteLine("Success: " &amp; sTestName &amp; " is equal to """"") 
        Else 
            Debug.WriteLine("Failed: " &amp; sTestName &amp; " is not equal to """"") 
        End If 
  
        If (sTest = Nothing) Then 
            Debug.WriteLine("Success: " &amp; sTestName &amp; " is equal to Nothing") 
        Else 
            Debug.WriteLine("Failed: " &amp; sTestName &amp; " is not equal to Nothing") 
        End If 
  
        If (sTest = String.Empty) Then 
            Debug.WriteLine("Success: " &amp; sTestName &amp; " is equal to String.Empty") 
        Else 
            Debug.WriteLine("Failed: " &amp; sTestName &amp; " is not equal to String.Empty") 
        End If 
  
        If String.IsNullOrEmpty(sTest) = True Then 
            Debug.WriteLine("Success: IsNullOrEmpty returns true for " &amp; sTestName) 
        Else 
            Debug.WriteLine("Failed: IsNullOrEmpty returns false for " &amp; sTestName) 
        End If 
  
    End Sub 
  
End Class 
{% endhighlight %}</div>

<p>I have missed out a test using vbNullString because it gets compiled as Nothing which is tested. </p>

<p>When this program is run, these are the results: </p>

<div style="border-bottom: windowtext 1pt solid; border-left: windowtext 1pt solid; padding-bottom: 5px; margin-top: 10px; padding-left: 5px; padding-right: 5px; font-family: courier new; margin-bottom: 10px; background: white; color: black; font-size: 10pt; border-top: windowtext 1pt solid; border-right: windowtext 1pt solid; padding-top: 5px">
  <p style="margin: 0px">__________________________________________________ </p>

  <p style="margin: 0px">&#160; </p>

  <p style="margin: 0px">Testing string value &quot;&quot; </p>

  <p style="margin: 0px">&#160; </p>

  <p style="margin: 0px">Success: &quot;&quot; is equal to &quot;&quot; </p>

  <p style="margin: 0px">Success: &quot;&quot; is equal to Nothing </p>

  <p style="margin: 0px">Success: &quot;&quot; is equal to String.Empty </p>

  <p style="margin: 0px">Success: IsNullOrEmpty returns true for &quot;&quot; </p>

  <p style="margin: 0px">__________________________________________________ </p>

  <p style="margin: 0px">&#160; </p>

  <p style="margin: 0px">Testing string value String.Empty </p>

  <p style="margin: 0px">&#160; </p>

  <p style="margin: 0px">Success: String.Empty is equal to &quot;&quot; </p>

  <p style="margin: 0px">Success: String.Empty is equal to Nothing </p>

  <p style="margin: 0px">Success: String.Empty is equal to String.Empty </p>

  <p style="margin: 0px">Success: IsNullOrEmpty returns true for String.Empty </p>

  <p style="margin: 0px">__________________________________________________ </p>

  <p style="margin: 0px">&#160; </p>

  <p style="margin: 0px">Testing string value Nothing </p>

  <p style="margin: 0px">&#160; </p>

  <p style="margin: 0px">Success: Nothing is equal to &quot;&quot; </p>

  <p style="margin: 0px">Success: Nothing is equal to Nothing </p>

  <p style="margin: 0px">Success: Nothing is equal to String.Empty </p>

  <p style="margin: 0px">Success: IsNullOrEmpty returns true for Nothing </p>

  <p style="margin: 0px">__________________________________________________ </p>

  <p style="margin: 0px">&#160; </p>

  <p style="margin: 0px">Testing string value ThisValue </p>

  <p style="margin: 0px">&#160; </p>

  <p style="margin: 0px">Failed: ThisValue is not equal to &quot;&quot; </p>

  <p style="margin: 0px">Failed: ThisValue is not equal to Nothing </p>

  <p style="margin: 0px">Failed: ThisValue is not equal to String.Empty </p>

  <p style="margin: 0px">Failed: IsNullOrEmpty returns false for ThisValue </p>
</div>
<!--EndFragment-->

<p>VB has successfully evaluated whether a string has a value or not, regardless of whether the empty value is defined as a literal empty string, String.Empty or Nothing/vbNullString. </p>

<p>Here is the C# version of the program:</p>

<div style="padding-bottom: 0px; margin: 0px; padding-left: 0px; padding-right: 0px; display: inline; float: none; padding-top: 0px" id="scid:812469c5-0cb0-4c63-8c15-c81123a09de7:27bd603b-b70f-4d90-a525-efef7366dc1c" class="wlWriterEditableSmartContent">{% highlight csharp linenos %}using System; 
using System.Collections.Generic; 
using System.ComponentModel; 
using System.Data; 
using System.Diagnostics; 
using System.Drawing; 
using System.Text; 
using System.Windows.Forms; 
  
namespace WindowsApplication1 
{ 
    public partial class Form1 : Form 
    { 
        public Form1() 
        { 
            InitializeComponent(); 
        } 
  
        private void button1_Click(object sender, EventArgs e) 
        { 
            RunTests("\"\"", ""); 
            RunTests("String.Empty", String.Empty); 
            RunTests("Nothing", null); 
            RunTests("ThisValue", "ThisValue"); 
        } 
  
        private void RunTests(String sTestName, String sTest) 
        { 
            Debug.WriteLine(new String('_', 50)); 
            Debug.WriteLine(String.Empty); 
            Debug.WriteLine("Testing string value " + sTestName); 
            Debug.WriteLine(String.Empty); 
  
            if (sTest == "") 
            { 
                Debug.WriteLine("Success: " + sTestName + " is equal to \"\""); 
            } 
            else 
            { 
                Debug.WriteLine("Failed: " + sTestName + " is not equal to \"\""); 
            } 
  
            if (sTest == null) 
            { 
                Debug.WriteLine("Success: " + sTestName + " is equal to null"); 
            } 
            else 
            { 
                Debug.WriteLine("Failed: " + sTestName + " is not equal to null"); 
            } 
  
            if (sTest == String.Empty) 
            { 
                Debug.WriteLine("Success: " + sTestName + " is equal to String.Empty"); 
            } 
            else 
            { 
                Debug.WriteLine("Failed: " + sTestName + " is not equal to String.Empty"); 
            } 
  
            if (String.IsNullOrEmpty(sTest) == true) 
            { 
                Debug.WriteLine("Success: IsNullOrEmpty returns true for " + sTestName); 
            } 
            else 
            { 
                Debug.WriteLine("Failed: IsNullOrEmpty returns false for " + sTestName); 
            } 
        } 
    } 
} 
{% endhighlight %}</div>

<p>When this program is run, these are the results: </p>

<div style="border-bottom: windowtext 1pt solid; border-left: windowtext 1pt solid; padding-bottom: 5px; margin-top: 10px; padding-left: 5px; padding-right: 5px; font-family: courier new; margin-bottom: 10px; background: white; color: black; font-size: 10pt; border-top: windowtext 1pt solid; border-right: windowtext 1pt solid; padding-top: 5px">
  <p style="margin: 0px">__________________________________________________ </p>

  <p style="margin: 0px">&#160; </p>

  <p style="margin: 0px">Testing string value &quot;&quot; </p>

  <p style="margin: 0px">&#160; </p>

  <p style="margin: 0px">Success: &quot;&quot; is equal to &quot;&quot; </p>

  <p style="margin: 0px">Failed: &quot;&quot; is not equal to null </p>

  <p style="margin: 0px">Success: &quot;&quot; is equal to String.Empty </p>

  <p style="margin: 0px">Success: IsNullOrEmpty returns true for &quot;&quot; </p>

  <p style="margin: 0px">__________________________________________________ </p>

  <p style="margin: 0px">&#160; </p>

  <p style="margin: 0px">Testing string value String.Empty </p>

  <p style="margin: 0px">&#160; </p>

  <p style="margin: 0px">Success: String.Empty is equal to &quot;&quot; </p>

  <p style="margin: 0px">Failed: String.Empty is not equal to null </p>

  <p style="margin: 0px">Success: String.Empty is equal to String.Empty </p>

  <p style="margin: 0px">Success: IsNullOrEmpty returns true for String.Empty </p>

  <p style="margin: 0px">__________________________________________________ </p>

  <p style="margin: 0px">&#160; </p>

  <p style="margin: 0px">Testing string value Nothing </p>

  <p style="margin: 0px">&#160; </p>

  <p style="margin: 0px">Failed: Nothing is not equal to &quot;&quot; </p>

  <p style="margin: 0px">Success: Nothing is equal to null </p>

  <p style="margin: 0px">Failed: Nothing is not equal to String.Empty </p>

  <p style="margin: 0px">Success: IsNullOrEmpty returns true for Nothing </p>

  <p style="margin: 0px">__________________________________________________ </p>

  <p style="margin: 0px">&#160; </p>

  <p style="margin: 0px">Testing string value ThisValue </p>

  <p style="margin: 0px">&#160; </p>

  <p style="margin: 0px">Failed: ThisValue is not equal to &quot;&quot; </p>

  <p style="margin: 0px">Failed: ThisValue is not equal to null </p>

  <p style="margin: 0px">Failed: ThisValue is not equal to String.Empty </p>

  <p style="margin: 0px">Failed: IsNullOrEmpty returns false for ThisValue </p>
</div>

<p>Definately a different result. The reason for the difference is how the VB and C# code has been compiled down to IL. </p>

<p>Reflector shows the following for the VB program:</p>

<div style="padding-bottom: 0px; margin: 0px; padding-left: 0px; padding-right: 0px; display: inline; float: none; padding-top: 0px" id="scid:812469c5-0cb0-4c63-8c15-c81123a09de7:eb36ba12-562e-4219-a808-0f477e4209b7" class="wlWriterEditableSmartContent">{% highlight vbnet linenos %}Private Sub RunTests(ByVal sTestName As String, ByVal sTest As String)

    Debug.WriteLine(New String("_"c, 50))
    Debug.WriteLine(String.Empty)
    Debug.WriteLine(("Testing string value " &amp; sTestName))
    Debug.WriteLine(String.Empty)

    If (Operators.CompareString(sTest, "", False) = 0) Then
        Debug.WriteLine(("Success: " &amp; sTestName &amp; " is equal to """""))
    Else
        Debug.WriteLine(("Failed: " &amp; sTestName &amp; " is not equal to """""))
    End If

    If (Operators.CompareString(sTest, Nothing, False) = 0) Then
        Debug.WriteLine(("Success: " &amp; sTestName &amp; " is equal to Nothing"))
    Else
        Debug.WriteLine(("Failed: " &amp; sTestName &amp; " is not equal to Nothing"))
    End If

    If (Operators.CompareString(sTest, String.Empty, False) = 0) Then
        Debug.WriteLine(("Success: " &amp; sTestName &amp; " is equal to String.Empty"))
    Else
        Debug.WriteLine(("Failed: " &amp; sTestName &amp; " is not equal to String.Empty"))
    End If

    If String.IsNullOrEmpty(sTest) Then
        Debug.WriteLine(("Success: IsNullOrEmpty returns true for " &amp; sTestName))
    Else
        Debug.WriteLine(("Failed: IsNullOrEmpty returns false for " &amp; sTestName))
    End If

End Sub{% endhighlight %}</div>

<p>Reflector shows the following for the C# program: </p>

<div style="padding-bottom: 0px; margin: 0px; padding-left: 0px; padding-right: 0px; display: inline; float: none; padding-top: 0px" id="scid:812469c5-0cb0-4c63-8c15-c81123a09de7:6b862374-8c1c-4f3d-acf1-8a0b7dedd6eb" class="wlWriterEditableSmartContent">{% highlight csharp linenos %}private void RunTests(string sTestName, string sTest)
{
    Debug.WriteLine(new string('_', 50));
    Debug.WriteLine(string.Empty);
    Debug.WriteLine("Testing string value " + sTestName);
    Debug.WriteLine(string.Empty);

    if (sTest == "")
    {
        Debug.WriteLine("Success: " + sTestName + " is equal to \"\"");
    }
    else
    {
        Debug.WriteLine("Failed: " + sTestName + " is not equal to \"\"");
    }

    if (sTest == null)
    {
        Debug.WriteLine("Success: " + sTestName + " is equal to null");
    }
    else
    {
        Debug.WriteLine("Failed: " + sTestName + " is not equal to null");
    }

    if (sTest == string.Empty)
    {
        Debug.WriteLine("Success: " + sTestName + " is equal to String.Empty");
    }
    else
    {
        Debug.WriteLine("Failed: " + sTestName + " is not equal to String.Empty");
    }

    if (string.IsNullOrEmpty(sTest))
    {
        Debug.WriteLine("Success: IsNullOrEmpty returns true for " + sTestName);
    }
    else
    {
        Debug.WriteLine("Failed: IsNullOrEmpty returns false for " + sTestName);
    }
}{% endhighlight %}</div>
The outcome of this is that for the &quot;same&quot; code to behave the same across both languages, using String.IsNullOrEmpty() is the safest way of determining whether a string as a value.


