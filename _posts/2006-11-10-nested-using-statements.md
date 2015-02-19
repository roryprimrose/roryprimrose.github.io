---
title: Nested using statements
categories : .Net, IT Related
date: 2006-11-10 09:21:18 +10:00
---

The using statement in the .Net framework is a really good way of neatly using and disposing of an object in your code. It is certainly much more elegant than having Try/Catch/Finally blocks where you manually dispose of your objects. I do however have a minor issue with nested using statements. I can't put my finger on it, but it just doesn't seem that 'right' to me. Maybe it just looks ugly.

The following is an example taken from [John Papa's][0] latest [Data Points][1] [MSDN Magazine][2] article:

{% highlight csharp linenos %}
using (TransactionScope ts = new TransactionScope())    
{
    using (SqlConnection cn2005 = new SqlConnection(cnString))
    {
        SqlCommand cmd = new SqlCommand(updateSql1, cn2005);
        cn2005.Open();
        cmd.ExecuteNonQuery();
    }
    
    ts.Complete();
}
{% endhighlight %}

As far as the messy look of it goes, I know that you can also do the following.

{% highlight csharp linenos %}
using (TransactionScope ts = new TransactionScope())
using (SqlConnection cn2005 = new SqlConnection(cnString))
{
    SqlCommand cmd = new SqlCommand(updateSql1, cn2005);
    cn2005.Open();
    cmd.ExecuteNonQuery();
}
    
ts.Complete();
{% endhighlight %}

I have some vague recollection that someone had a problem with this way of coding the using statement.

Has anyone come across any best practices with regard to nested using statements?

[0]: http://codebetter.com/blogs/john.papa/archive/2006/10/15/System.Transactions-Revisited-2-Years-Later.aspx
[1]: http://msdn.microsoft.com/msdnmag/issues/06/11/DataPoints/default.aspx
[2]: https://msdn.microsoft.com/en-us/magazine/
