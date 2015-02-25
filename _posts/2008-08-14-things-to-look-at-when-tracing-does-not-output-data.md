---
title: Things to look at when tracing does not output data
categories : .Net
tags : Tracing
date: 2008-08-14 15:45:33 +10:00
---

There are several issues that can prevent trace data being written. Here are a few of that you might encounter.

**TraceSource names**

If using [TraceSource][0], the name provided to the TraceSource constructor is case sensitive. If the string doesn't match your configuration exactly, a default TraceSource instance is created rather than the configured one you were expecting.

**TextWriterTraceListener**

If using [TextWriterTraceListener][1] (or [XmlWriterTraceListener][2] that derives from it), there are several more issues that can occur. 

 The following code is the code in TextWriterTraceListener that causes the issues.   
  
{% highlight csharp %}
public override void WriteLine(String message)
{
    if (EnsureWriter())
    {
        if (base.NeedIndent)
        {
            WriteIndent();
        }
    
        writer.WriteLine(message);
    
        base.NeedIndent = true;
    }
}
    
internal Boolean EnsureWriter()
{
    Boolean flag = true;
    
    if (writer == null)
    {
        flag = false;
    
        if (this.fileName == null)
        {
            return flag;
        }
    
        Encoding encodingWithFallback = GetEncodingWithFallback(new UTF8Encoding(false));
        String fullPath = Path.GetFullPath(this.fileName);
        String directoryName = Path.GetDirectoryName(fullPath);
        String fileName = Path.GetFileName(fullPath);
    
        for (Int32 i = 0; i < 2; i++)
        {
            try
            {
                writer = new StreamWriter(fullPath, true, encodingWithFallback, 0x1000);
                flag = true;
                break;
            }
            catch (IOException)
            {
                fileName = Guid.NewGuid() + fileName;
                fullPath = Path.Combine(directoryName, fileName);
            }
            catch (UnauthorizedAccessException)
            {
                break;
            }
            catch (Exception)
            {
                break;
            }
        }
    
        if (!flag)
        {
            this.fileName = null;
        }
    }
    
    return flag;
}
{% endhighlight %}

When writing a record, it ensures that the writer is ready. The biggest problem with this implementation is that the logic in EnsureWriter() swallows any exception. If an exception is encountered, a second attempt is made which is likely to fail for the same reason as the first attempt. This causes WriteLine() to skip out without throwing an exception.
       
I find this code very poor. If there is a problem with using the configuration values, the developer (or operations staff) need to know what the problem is so they can fix it. This implementation simply just ignores the request to write the trace record when an exception is encountered. This is the primary reason that troubleshooting tracing problems is so difficult.

Some of the problems that can be encountered with the TextWriterTraceListener and derived classes are:

* The configured directory doesn't exist. I would have expected that this code would ensure that the directory is created, but unfortunately it doesn't.
* Lack of permissions. If the identity running the trace methods doesn't have permissions to write to the configured location, this will obviously fail.
* The path is too long. The .Net framework doesn't support paths more than 260 characters. See [here][3].
* The drive specified in the path doesn't exist
    
Hope this helps.

[0]: http://msdn.microsoft.com/en-us/library/system.diagnostics.tracesource.aspx
[1]: http://msdn.microsoft.com/en-us/library/system.diagnostics.textwritertracelistener.aspx
[2]: http://msdn.microsoft.com/en-us/library/system.diagnostics.xmlwritertracelistener.aspx
[3]: /2006/11/09/so-you-still-can-t-have-a-path-more-than-260-characters-/
