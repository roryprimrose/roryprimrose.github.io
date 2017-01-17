---
title: Creating Web Custom Controls With ASP.Net 1.1 - Part VI - Handling Dependencies
date: 2005-04-11 03:30:00 +10:00
---

The main purpose of creating controls is so they can be reused in other projects. The beauty of Web Custom Controls as opposed to Web User Controls is that they are compiled into an assembly which can easily be copied between ASP.Net projects. 

When I am creating controls, I always have dependencies to support the them. At the very least, I have JavaScript that needs to be rendered for client-side behaviours of the control. I have written several controls that also rely on style-sheets and images to be used with the control. Deployment of these dependencies becomes more than a little nightmare.

Take an image for example. What happens when a control needs to render an image to the browser? It needs to render some HTML like &lt;IMG src= 'test.gif' /&gt;. The browser then makes a request to the web server for test.gif. Thats all very well and good, but to deploy this control, you not only need to copy the controls assembly to the deployment bin folder, but also need to deploy test.gif to the correct folder to be requested by the browser. Each dependency of the control needs to be copied across each of the ASP.Net projects that use the control. This very quickly becomes a maintenance problem.

<!--more-->

What we want to do is package all of these dependencies into the controls assembly. To achieve this, move the required files from the ASP.Net project and put them in the controls project. For each of these files, go to the property grid and set their Build Action to Embedded Resource. This will add each file as a resource to the controls assembly when it is compiled. We have now solved the deployment problem for the dependencies of the controls. When the controls assembly is copied for use in another project, all the dependencies are deployed with it as they are all bundled in the one dll file. 

The next question that should be ringing around your head is how do these decencies get requested and rendered to the browser? Here is where our friend IHttpHandler comes into play. When a class implements the IHttpHandler interface, it is forced to support the ProcessRequest method. The code in this method can determine the resource being requested by the browser and take appropriate action. In the case of our dependencies, we will determine which resource is requested, extract it from the assembly and then render it to the browser.

Using IHttpHandler is perfect for covering our dependency deployment problems, but there is a just a little configuration that needs to be done in the web site to get it working. The web.config file allows us to specify a page/url reference that will cause our IHttpHandler to be called into action. This configuration causes the server to call our IHttpHandler implementing class when a specific url is requested. Just in case you are a little confused about this, the page requested doesn't actually exist, but the browser can still request it. When this happens, instead of returning a 404 error, ASP.Net hijacks the request and passes it on to the handler.

This is what is required in the web.config file:

```xml
<configuration >
    <system.web >
        <httpHandlers >
            <add  verb ="*"  path ="MyDev.Web.Controls.aspx"  type ="MyDev.Web.Controls.ResHandler,MyDev.Web.Controls" />
        </httpHandlers >
    </system.web >
</configuration >
```

We have now been able to hook up a web request to call the IHttpHandler interface in a specific class and assembly. Now when the request comes through, we need to identify which resource in the assembly is being requested.When I was originally researching this, I came across an article by [Eric Woodruff][0] that had a great idea for not only identifying the resource requested, but also supporting a caching flag. It will be through querystrings that the handler will be told what resource is being requested.

In the previous article, I discussed the best ways of rendering scripts for a control. Most control developers render the script directly to the page either when the page starts rendering, or when the page finishes rendering or when the control renders. As discussed, these scripts can be pulled out of the assembly for easier development, but rendering the script directly in the page doesn't allow for any caching of the data. With this method, a control could render a script tag that looks like <code>&lt;SCRIPT type='text/javascript' src='MyDev.Web.Controls.aspx?Res=MyControlScript.js' /&gt;</code>. This causes the browser to make a separate request for the script which can now be cached by the browser. The result is that the pages will load much faster.

I created a class that implements IHttpHandler and added as many helpful features as I could think of. It will handle text and binary resources and make its best determination as to the content-type of the resource. It also supports some useful methods for registering resources with a page. As this class deals with reflection, assemblies and resources, I have cached objects and data that are often referenced and minimised the number of times the same method is called when processing the resources. Let me know if you find more improvements that can be made. 

Here are the goods:

```vbnet
# Region " Imports "

 Imports System
 Imports System.Web

# End  Region

Public  Class ResHandler

# Region " Declarations "

Implements IHttpHandler

Public  Structure TResType

    Public PerceivedType As  String

    Public ContentType As  String

End  Structure

Private m_objAssembly As Reflection.Assembly

Private m_sAssemblyName As  String

# End  Region

# Region " Sub Procedures "

Private  Sub LoadTypeFromRegistry( _
    ByVal sFileExt As  String , _
    ByRef objType As TResType)

    Dim objKey As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey("." & sFileExt, False )

    ' Check if the Content Type exists
    If  Not objKey.GetValue("Content Type") Is  Nothing  Then

        objType.ContentType = CType (objKey.GetValue("Content Type"), String )

    End  If  ' End checking if the Content Type exists

    ' Check if the PerceivedType exists
    If  Not objKey.GetValue("PerceivedType") Is  Nothing  Then

        ' Store the PerceivedType value
        objType.PerceivedType = CType (objKey.GetValue("PerceivedType"), String )

    End  If  ' Check if the PerceivedType exists

    ' Check if the perceived type doesn't exist, but can be determined from the content type
    If objType.PerceivedType = vbNullString _
        AndAlso objType.ContentType <> vbNullString _
        AndAlso objType.ContentType.IndexOf("/") > -1 Then

        objType.PerceivedType = objType.ContentType.Substring(0, objType.ContentType.IndexOf("/"))

    End  If  ' End checking if the perceived type doesn't exist, but can be determined from the content type

End  Sub

Private  Sub LoadTypeFromName( _
    ByVal sFileExt As  String , _
    ByRef objType As TResType)

    Select  Case sFileExt

        Case "mid", "midi", "mp3", "wav", "wma"

            ' Toggle sfileext as appropriate for storage
            If sFileExt = "mp3" Then sFileExt = "mpeg"

            If sFileExt = "wma" Then sFileExt = "x-ms-wma"

            objType.PerceivedType = "audio"

        Case "csv", "xls", "doc", "dot", "hta", "ppt", "zip", "tgz"

            ' Toggle sfileext as appropriate for storage

            If sFileExt = "csv" OrElse sFileExt = "xls" Then sFileExt = "vnd.ms-excel"

            If sFileExt = "doc" OrElse sFileExt = "dot" Then sFileExt = "msword"

            If sFileExt = "ppt" Then sFileExt = "vnd.ms-powerpoint"

            If sFileExt = "zip" Then sFileExt = "x-zip-compressed"

            If sFileExt = "tgz" Then sFileExt = "x-compressed"

            objType.PerceivedType = "application"

        Case "bmp", "gif", "jpg", "jpeg", "png", "tiff", "tif", "ico"

            ' Toggle sfileext as appropriate for storage
            If sFileExt = "ico" Then sFileExt = "x-icon"

            If sFileExt = "tif" Then sFileExt = "tiff"

            objType.PerceivedType = "image"

        Case "mht", "mhtml"

            ' Toggle sfileext as appropriate for storage
            sFileExt = "rfc822"

            objType.PerceivedType = "message"

        Case "txt", "tab", "vb", "vbs", "js", "xml", "xsl", "htc", "htm", "html"

            ' Toggle sfileext as appropriate for storage
            If sFileExt = "vb" OrElse sFileExt = "vbs" Then sFileExt = "vbscript"

            If sFileExt = "js" Then sFileExt = "javascript"

            If sFileExt = "txt" OrElse sFileExt = "tab" Then sFileExt = "plain"

            If sFileExt = "xsl" Then sFileExt = "xml"

            If sFileExt = "htc" Then sFileExt = "x-component"

            If sFileExt = "htm" Then sFileExt = "html"

            objType.PerceivedType = "text"

        Case "avi", "mov", "mpe", "mpeg"

            ' Toggle sfileext as appropriate for storage
            If sFileExt = "mov" Then sFileExt = "quicktime"

            If sFileExt = "mpe" Then sFileExt = "mpeg"

            objType.PerceivedType = "video"

        Case  Else

            ' Default to binary data
            objType.PerceivedType = "application"

            sFileExt = "octet-stream"

    End  Select

    ' Create the content type from the perceived type and the file extension
    objType.ContentType = objType.PerceivedType & "/" & sFileExt

End  Sub

Public  Sub RegisterWithPage( _
    ByVal sResource As  String , _
    ByRef objPage As UI.Page, _
    Optional  ByVal bRegisterAsStartupScript As  Boolean = False )

    ' Check if the resource is valid
    If IsValid(sResource) = True  Then

        ' Check if we should register the resource
        If (((bRegisterAsStartupScript = False ) _
            AndAlso (objPage.IsClientScriptBlockRegistered(sResource) = False )) _
            OrElse ((bRegisterAsStartupScript = True ) _
            AndAlso (objPage.IsStartupScriptRegistered(sResource) = False ))) Then

            ' Check if we can get a page reference
            Dim sReference As  String = PageReference(sResource)

            ' Check if there is a reference to use
            If sReference <> vbNullString Then

                ' Check if the reference is a startup script
                If bRegisterAsStartupScript = True  Then

                    ' Register the resource
                    objPage.RegisterStartupScript(sResource, sReference)

                Else  ' This is not a startup script

                    ' Register the resource
                    objPage.RegisterClientScriptBlock(sResource, sReference)

                End  If  ' End checking if the reference is a startup script

            End  If  ' End checking if there is a reference to use

        End  If  ' End checking if we should register the resource

    End  If  ' End checking if the resource is valid

End  Sub

Public  Sub ProcessRequest( _
    ByVal context As HttpContext) _
    Implements IHttpHandler.ProcessRequest

    ' Clear any existing response data

    context.Response.Clear()

    Try

        Dim sResource As  String = context.Request.QueryString("Res")
        Dim bSuccess As  Boolean

        ' Check if the resource request is valid
        If IsValid(sResource) = True  Then

            Dim bNoCache As  Boolean = False
            Dim tType As TResType = ResourceType(sResource)

            ' Check if NoCache has been specified
            If context.Request.QueryString("NoCache") <> vbNullString Then

            Try

                ' Get the cache value
                bNoCache = System.Boolean.Parse(context.Request.QueryString("NoCache"))

            Catch

                ' Do nothing

            End  Try

            End  If  ' End checking if NoCache has been specified

            With context.Response

                ' Set up the response
                .ContentType = tType.ContentType
                .Cache.VaryByParams("Res") = True
                .Cache.VaryByParams("NoCache") = True
                .Cache.SetValidUntilExpires( False )

                ' Check if caching is enabled
                If bNoCache = False  Then

                    .Cache.SetExpires(Now.AddDays(7))
                    .Cache.SetCacheability(HttpCacheability.Public)

                Else  ' Caching is disabled

                    .Cache.SetExpires(Now.AddDays(-7))
                    .Cache.SetCacheability(HttpCacheability.NoCache)

                End  If  ' End checking if caching is enabled

                ' Check if the type of resource is text
                If tType.PerceivedType = "text" Then

                    Dim sResourceValue As  String

                    ' Check if the resource was successfully loaded
                    If LoadTextResource(sResource, sResourceValue) = True  Then

                        ' Write the value to the browser
                        .Write(sResourceValue)

                        ' Set the success flag
                        bSuccess = True

                    End  If  ' End checking if the resource was successfully loaded

                Else  ' This resource is binary

                    Dim aBuffer As  Byte ()

                    ' Check if the resource was successfully loaded
                    If LoadBinaryResource(sResource, aBuffer) = True  Then

                        ' Write the value to the browser
                        .OutputStream.Write(aBuffer, 0, aBuffer.Length)

                        ' Set the success flag
                        bSuccess = True

                    End  If  ' End checking if the resource was loaded successfully

                End  If  ' End checking if the type of resource is text

            End  With  ' End With context.Response

        End  If  ' End checking if the resource request is valid

        ' Check if the resource failed to load
        If bSuccess = False  Then

            With context.Response

                .Cache.SetExpires(Now.AddDays(-7))
                .Cache.SetCacheability(HttpCacheability.NoCache)
                .Status = "The resource requested doesn't exist."
                .StatusCode = 404
                .StatusDescription = .Status

            End  With  ' End With context.Response

        End  If  ' End checking if the resource failed to load

    Catch ex As Exception

        With context.Response

            .Cache.SetExpires(Now.AddDays(-7))
            .Cache.SetCacheability(HttpCacheability.NoCache)
            .Status = ex.Message
            .StatusCode = 500
            .StatusDescription = .Status

        End  With  ' End With context.Response

    End  Try

End  Sub

# End  Region

# Region " Functions "

Public  Function IsValid( _
    ByRef sResource As  String ) As  Boolean

    ' Exit if no value is specified
    If sResource = vbNullString Then  Return  False

    Dim aResources() As  String = ThisAssembly.GetManifestResourceNames
    Dim sCheckName As  String = sResource.ToLower
    Dim nCount As Int32

    ' Add the assembly namespace to the resource name if it isn't already
    If sCheckName.StartsWith(AssemblyName.ToLower) = False  Then sCheckName = AssemblyName.ToLower & "." & sCheckName

    ' Loop through the resource names
    For nCount = 0 To aResources.Length - 1

        ' Check if the resource name matches
        If sCheckName = aResources(nCount).ToLower Then

            ' Update the resource parameter
            sResource = aResources(nCount)

            ' The resource was found
            Return  True

        End  If  ' End checking if the resource name matches

    Next  ' Loop through the resource names

    ' The resource wasn't found
    Return  False

End  Function

Public  Function URL( _
    ByVal sResource As  String , _
    Optional  ByVal bNoCache As  Boolean = False ) As  String

    Dim sURL As  String = AssemblyName & ".aspx?Res=" & HttpUtility.UrlEncode(sResource)

    ' Check if NoCache is specified
    If bNoCache = True  Then

        sURL &= "&NoCache=" & bNoCache

    End  If  ' End checking if NoCache is specified

    ' Return the URL built
    Return sURL

End  Function

Public  Function PageReference( _
    ByVal sResource As  String ) As  String

    Dim tType As TResType = ResourceType(sResource)

    If tType.PerceivedType = "image" Then

        Return "<IMG src='" & URL(sResource) & "'></IMG>"

    ElseIf tType.ContentType = "text/css" Then

        Return "<LINK rel='Stylesheet' href='" & URL(sResource) & "'></LINK>"

    ElseIf tType.ContentType = "text/javascript" Then

        Return "<SCRIPT language='javascript' type='text/javascript' src='" & URL(sResource) & "'></SCRIPT>"

    ElseIf tType.ContentType = "text/vbscript" Then

        Return "<SCRIPT language='vbscript' type='text/vbscript' src='" & URL(sResource) & "'></SCRIPT>"

    ElseIf tType.PerceivedType = "text" Then

        Dim sValue As  String

        ' Check if the resource was loaded successfully
        If LoadTextResource(sResource, sValue) = True  Then

            ' Return the resource value
            Return sValue

        Else  ' The resource wasn't loaded successfully

            Return vbNullString

        End  If  ' End checking if the resource was loaded successfully

    Else  ' This type of resource is not handled

        Return vbNullString

    End  If

End  Function

Public  Function ResourceType( _
    ByVal sResource As  String ) As TResType

    ' Check if there is a .
    If sResource <> vbNullString _
        AndAlso sResource.LastIndexOf(".") > -1 Then

        ' Get the file extension
        Dim sFileExt As  String = sResource.Substring(sResource.LastIndexOf(".") + 1).ToLower
        Dim objType As TResType

        ' Determine the file type from the registry
        Call LoadTypeFromRegistry(sFileExt, objType)

        ' Check if there is any type information missing
        If objType.PerceivedType = vbNullString _
            OrElse objType.ContentType = vbNullString Then

            Dim objTempType As TResType

            ' Get the type from the name
            Call LoadTypeFromName(sFileExt, objTempType)

            ' Store the values as appropriate
            If objType.ContentType = vbNullString AndAlso objTempType.ContentType <> vbNullString Then objType.ContentType = objTempType.ContentType

            If objType.PerceivedType = vbNullString AndAlso objTempType.PerceivedType <> vbNullString Then objType.PerceivedType = objTempType.PerceivedType

        End  If  ' End checking if there is any type information missing

        ' Return the type information determined
        Return objType

    Else  ' An invalid value has been specified

        Return  Nothing

    End  If  ' End checking if there is a .

End  Function

Public  Function LoadTextResource( _
    ByVal sResource As  String , _
    ByRef sValue As  String ) As  Boolean

    Dim bReturn As  Boolean

    ' Check if the resource name is valid
    If IsValid(sResource) = True  Then

        Dim tType As TResType = ResourceType(sResource)

        ' Check if the resource is a text resource

        If tType.PerceivedType = "text" Then

            Dim objStream As IO.Stream = ThisAssembly.GetManifestResourceStream(sResource)

            Dim objReader As  New IO.StreamReader(objStream)

            Try

                ' Read the resource stream
                sValue = objReader.ReadToEnd

                ' We have loaded the resource successfully, set the return flag
                bReturn = True

            Catch

                ' Do nothing

            Finally

                ' Check if the reader exists
                If  Not objReader Is  Nothing  Then

                    ' Close the reader
                    objReader.Close()

                End  If  ' End checking if the reader exists

                ' Check if the stream exists

                If  Not objStream Is  Nothing  Then

                    ' Close the stream
                    objStream.Close()

                End  If  ' End checking if the stream exists

            End  Try

        End  If  ' End checking if the resource is a text resource

    End  If  ' End checking if the resource name is valid

    ' Return the success flag

    Return bReturn

End  Function

Public  Function LoadBinaryResource( _
    ByVal sResource As  String , _
    ByRef aBuffer As  Byte ()) As  Boolean

    Dim bReturn As  Boolean

    ' Check if the resource name is valid
    If IsValid(sResource) = True  Then

        Dim tType As TResType = ResourceType(sResource)

        ' Check if the resource is a text resource
        If tType.PerceivedType <> "text" Then

            Dim objStream As IO.Stream = ThisAssembly.GetManifestResourceStream(sResource)

            Dim objReader As  New IO.BinaryReader(objStream)

            Try

                ' Resize the buffer to fit the resource
                ReDim aBuffer( CType (objStream.Length, Int32))

                ' Load the resource into the buffer
                objReader.Read(aBuffer, 0, CType (objStream.Length, Int32))

                ' We have loaded the resource successfully, set the return flag
                bReturn = True

            Catch

                ' Do nothing

            Finally

                ' Check if the reader exists
                If  Not objReader Is  Nothing  Then

                    ' Close the reader
                    objReader.Close()

                End  If  ' End checking if the reader exists

                ' Check if the stream exists
                If  Not objStream Is  Nothing  Then

                    ' Close the stream
                    objStream.Close()

                End  If  ' End checking if the stream exists

            End  Try

        End  If  ' End checking if the resource is a text resource

    End  If  ' End checking if the resource name is valid

    ' Return the success flag

    Return bReturn

End  Function

# End  Region

# Region " Properties "

Private  ReadOnly  Property ThisAssembly() As System.Reflection.Assembly
    Get

        ' Check if there is an assembly reference
        If m_objAssembly Is  Nothing  Then

            ' Get a reference to the assembly
            m_objAssembly = System.Reflection.Assembly.GetExecutingAssembly

        End  If  ' End checking if there is an assembly reference

        ' Return the object reference
        Return m_objAssembly

    End  Get

End  Property

Private  ReadOnly  Property AssemblyName() As  String
    Get

        ' Check if the value is stored
        If m_sAssemblyName = vbNullString Then

            m_sAssemblyName = Me .GetType.Namespace

        End  If  ' End checking if the name is stored

        ' Return the stored value
        Return m_sAssemblyName

    End  Get

End  Property

Public  ReadOnly  Property IsReusable() As  Boolean  Implements IHttpHandler.IsReusable

    Get

        Return  True

    End  Get

End  Property

# End  Region

End  Class
```

[0]: http://www.codeproject.com/aspnet/ressrvpage.asp
