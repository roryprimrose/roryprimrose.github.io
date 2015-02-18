---
title: Transport connection closed exception with nDumbster
categories : .Net
tags : Unit Testing
date: 2009-08-14 10:49:46 +10:00
---

I have been using nDumbster on a project to unit test sending emails. It has been an interesting experience working with this little tool. It is a great product but has been out of development for a while and [has some issues][0].

The more I started using nDumbster in test runs, the more I was finding that it wasn’t working so well. I was consistently getting the following exception:

> _Test method [TestName] threw exception:&#160; System.Net.Mail.SmtpException: Failure sending mail. --->&#160; System.IO.IOException: Unable to read data from the transport connection: net_io_connectionclosed.._
> _System.Net.Mail.SmtpReplyReaderFactory.ProcessRead(Byte[] buffer, Int32 offset, Int32 read, Boolean readLine)_
> _System.Net.Mail.SmtpReplyReaderFactory.ReadLines(SmtpReplyReader caller, Boolean oneLine)_
> _System.Net.Mail.SmtpReplyReaderFactory.ReadLine(SmtpReplyReader caller)_
> _System.Net.Mail.CheckCommand.Send(SmtpConnection conn, String& response)_
> _System.Net.Mail.MailCommand.Send(SmtpConnection conn, Byte[] command, String from)_
> _System.Net.Mail.SmtpTransport.SendMail(MailAddress sender, MailAddressCollection recipients, String deliveryNotify, SmtpFailedRecipientException& exception)_
> _System.Net.Mail.SmtpClient.Send(MailMessage message)_
> _System.Net.Mail.SmtpClient.Send(MailMessage message)_

I worked on this one for a while. Without wanting to get my fingers into the nDumbster code, I found a solution that works from within the unit testing code itself. The solution is to create a new instance of the nDumbster SMTP server using a unique port number for each test. This ensures that there is a fresh connection for each test in the test run.

The unit test code looks like the following.

{% highlight csharp linenos %}
/// <summary>
/// Stores the random generator.
/// </summary>
private static readonly Random RandomPort = new Random(Environment.TickCount);
    
/// <summary>
/// Stores the SMTP server.
/// </summary>
private static SimpleSmtpServer _server;
    
#region Setup/Teardown
    
/// <summary>
/// Cleans up after running a unit test.
/// </summary>
[TestCleanup]
public void TestCleanup()
{
    _server.Stop();
    _server = null;
}
    
/// <summary>
/// Initializes the test.
/// </summary>
[TestInitialize]
public void TestInitialize()
{
    // HACK: The test must recreate the nDumbster server for each test using a different port number
    // This gets around issues where some tests consistently failed when the whole test class was run because the server
    // was not handling the connection properly. This appears to be the only solution that works.
    Int32 nextPortNumber = RandomPort.Next(1024, 6666);
    
    _server = SimpleSmtpServer.Start(nextPortNumber);
}
    
#endregion
{% endhighlight %}

[0]: http://blogs.blackmarble.co.uk/blogs/rfennell/archive/2008/09/27/mocking-out-an-email-server.aspx
