---
title: Code conversion pitfalls
categories : .Net, IT Related
date: 2006-03-16 23:06:00 +10:00
---

After doing four days of training and meetings in Brisbane, I am now back in the office reviewing some code. I had originally developed some web custom controls in VB (the organisations language of choice) that were then put into an architecture framework. This required that the code be converted from VB to C# which is the language of the framework involved. This work was done by another person while I was back in my prior UI design team.

Doing a code review of C# that is based on my VB code has put me in a new situation. Not having done this before, I am trying to think of scenarios that look safe, but are trouble under the surface. Turns out I found one very quickly. In this case, string comparisons are the danger. The VB code often had statements like this: 

{% highlight vbnet %}
If sSomeValue = String.Empty Then

' Do something here

End If
{% endhighlight %}

VB is very forgiving with its string comparisons and it attempts to cover all the possibilities. If a straight language conversion is done, the same result does not occur in C#. To test this, I came up with a program in VB and in C#.

Here is the VB version of the program:

{% highlight vbnet %}
Public Class Form1 
  
    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click 
  
        RunTests("""""", "") 
        RunTests("String.Empty", String.Empty) 
        RunTests("Nothing", Nothing) 
        RunTests("ThisValue", "ThisValue") 
  
    End Sub 
  
    Private Sub RunTests(ByVal sTestName As String, ByVal sTest As String) 
  
        Debug.WriteLine(New String("_"c, 50)) 
        Debug.WriteLine(String.Empty) 
        Debug.WriteLine("Testing string value " & sTestName) 
        Debug.WriteLine(String.Empty) 
  
        If (sTest = "") Then 
            Debug.WriteLine("Success: " & sTestName & " is equal to """"") 
        Else 
            Debug.WriteLine("Failed: " & sTestName & " is not equal to """"") 
        End If 
  
        If (sTest = Nothing) Then 
            Debug.WriteLine("Success: " & sTestName & " is equal to Nothing") 
        Else 
            Debug.WriteLine("Failed: " & sTestName & " is not equal to Nothing") 
        End If 
  
        If (sTest = String.Empty) Then 
            Debug.WriteLine("Success: " & sTestName & " is equal to String.Empty") 
        Else 
            Debug.WriteLine("Failed: " & sTestName & " is not equal to String.Empty") 
        End If 
  
        If String.IsNullOrEmpty(sTest) = True Then 
            Debug.WriteLine("Success: IsNullOrEmpty returns true for " & sTestName) 
        Else 
            Debug.WriteLine("Failed: IsNullOrEmpty returns false for " & sTestName) 
        End If 
  
    End Sub 
  
End Class 
{% endhighlight %}

I have missed out a test using vbNullString because it gets compiled as Nothing which is tested.

When this program is run, these are the results:

{% highlight text %}
  Testing string value ""

  Success: "" is equal to "" 
  Success: "" is equal to Nothing 
  Success: "" is equal to String.Empty 
  Success: IsNullOrEmpty returns true for "" 

  Testing string value String.Empty 

  Success: String.Empty is equal to "" 
  Success: String.Empty is equal to Nothing 
  Success: String.Empty is equal to String.Empty 
  Success: IsNullOrEmpty returns true for String.Empty 

  Testing string value Nothing 

  Success: Nothing is equal to "" 
  Success: Nothing is equal to Nothing 
  Success: Nothing is equal to String.Empty 
  Success: IsNullOrEmpty returns true for Nothing 

  Testing string value ThisValue 

  Failed: ThisValue is not equal to "" 
  Failed: ThisValue is not equal to Nothing 
  Failed: ThisValue is not equal to String.Empty 
  Failed: IsNullOrEmpty returns false for ThisValue 
{% endhighlight %}

VB has successfully evaluated whether a string has a value or not, regardless of whether the empty value is defined as a literal empty string, String.Empty or Nothing/vbNullString.

Here is the C# version of the program:

{% highlight csharp %}
using System; 
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
{% endhighlight %}

When this program is run, these are the results:

{% highlight text %}

  Testing string value "" 

  Success: "" is equal to "" 
  Failed: "" is not equal to null 
  Success: "" is equal to String.Empty 
  Success: IsNullOrEmpty returns true for "" 

  Testing string value String.Empty 

  Success: String.Empty is equal to "" 
  Failed: String.Empty is not equal to null 
  Success: String.Empty is equal to String.Empty 
  Success: IsNullOrEmpty returns true for String.Empty 

  Testing string value Nothing 

  Failed: Nothing is not equal to "" 
  Success: Nothing is equal to null 
  Failed: Nothing is not equal to String.Empty 
  Success: IsNullOrEmpty returns true for Nothing 

  Testing string value ThisValue 

  Failed: ThisValue is not equal to "" 
  Failed: ThisValue is not equal to null 
  Failed: ThisValue is not equal to String.Empty 
  Failed: IsNullOrEmpty returns false for ThisValue 
{% endhighlight %}

Definately a different result. The reason for the difference is how the VB and C# code has been compiled down to IL.

Reflector shows the following for the VB program:

{% highlight vbnet %}
Private Sub RunTests(ByVal sTestName As String, ByVal sTest As String)

    Debug.WriteLine(New String("_"c, 50))
    Debug.WriteLine(String.Empty)
    Debug.WriteLine(("Testing string value " & sTestName))
    Debug.WriteLine(String.Empty)

    If (Operators.CompareString(sTest, "", False) = 0) Then
        Debug.WriteLine(("Success: " & sTestName & " is equal to """""))
    Else
        Debug.WriteLine(("Failed: " & sTestName & " is not equal to """""))
    End If

    If (Operators.CompareString(sTest, Nothing, False) = 0) Then
        Debug.WriteLine(("Success: " & sTestName & " is equal to Nothing"))
    Else
        Debug.WriteLine(("Failed: " & sTestName & " is not equal to Nothing"))
    End If

    If (Operators.CompareString(sTest, String.Empty, False) = 0) Then
        Debug.WriteLine(("Success: " & sTestName & " is equal to String.Empty"))
    Else
        Debug.WriteLine(("Failed: " & sTestName & " is not equal to String.Empty"))
    End If

    If String.IsNullOrEmpty(sTest) Then
        Debug.WriteLine(("Success: IsNullOrEmpty returns true for " & sTestName))
    Else
        Debug.WriteLine(("Failed: IsNullOrEmpty returns false for " & sTestName))
    End If

End Sub
{% endhighlight %}

Reflector shows the following for the C# program:

{% highlight csharp %}
private void RunTests(string sTestName, string sTest)
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
}
{% endhighlight %}

The outcome of this is that for the "same" code to behave the same across both languages, using String.IsNullOrEmpty() is the safest way of determining whether a string as a value.

