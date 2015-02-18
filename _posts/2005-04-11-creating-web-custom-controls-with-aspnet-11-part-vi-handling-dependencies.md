---
title: Creating Web Custom Controls With ASP.Net 1.1 - Part VI - Handling Dependencies
date: 2005-04-11 03:30:00 +10:00
---

The main purpose of creating controls is so they can be reused in other projects. The beauty of Web Custom Controls as opposed to Web User Controls is that they are compiled into an assembly which can easily be copied between ASP.Net projects. 

When I am creating controls, I always have dependencies to support the them. At the very least, I have JavaScript that needs to be rendered for client-side behaviours of the control. I have written several controls that also rely on style-sheets and images to be used with the control. Deployment of these dependencies becomes more than a little nightmare.

Take an image for example. What happens when a control needs to render an image to the browser? It needs to render some HTML like <IMG src= 'test.gif' /&gt;. The browser then makes a request to the web server for test.gif. Thats all very well and good, but to deploy this control, you not only need to copy the controls assembly to the deployment bin folder, but also need to deploy test.gif to the correct folder to be requested by the browser. Each dependency of the control needs to be copied across each of the ASP.Net projects that use the control. This very quickly becomes a maintenance problem.

What we want to do is package all of these dependencies into the controls assembly. To achieve this, move the required files from the ASP.Net project and put them in the controls project. For each of these files, go to the property grid and set their Build Action to Embedded Resource. This will add each file as a resource to the controls assembly when it is compiled. We have now solved the deployment problem for the dependencies of the controls. When the controls assembly is copied for use in another project, all the dependencies are deployed with it as they are all bundled in the one dll file. 

The next question that should be ringing around your head is how do these decencies get requested and rendered to the browser? Here is where our friend IHttpHandler comes into play. When a class implements the IHttpHandler interface, it is forced to support the ProcessRequest method. The code in this method can determine the resource being requested by the browser and take appropriate action. In the case of our dependencies, we will determine which resource is requested, extract it from the assembly and then render it to the browser.

Using IHttpHandler is perfect for covering our dependency deployment problems, but there is a just a little configuration that needs to be done in the web site to get it working. The web.config file allows us to specify a page/url reference that will cause our IHttpHandler to be called into action. This configuration causes the server to call our IHttpHandler implementing class when a specific url is requested. Just in case you are a little confused about this, the page requested doesn't actually exist, but the browser can still request it. When this happens, instead of returning a 404 error, ASP.Net hijacks the request and passes it on to the handler.

This is what is required in the web.config file:

 < configuration &gt;

 < system.web &gt;

 < httpHandlers &gt;

 < add  verb =&quot;*&quot;  path =&quot;MyDev.Web.Controls.aspx&quot;  type =&quot;MyDev.Web.Controls.ResHandler,MyDev.Web.Controls&quot; /&gt;

 </ httpHandlers &gt;

 </ system.web &gt;

 </ configuration &gt;

We have now been able to hook up a web request to call the IHttpHandler interface in a specific class and assembly. Now when the request comes through, we need to identify which resource in the assembly is being requested.When I was originally researching this, I came across an article by [Eric Woodruff][0] that had a great idea for not only identifying the resource requested, but also supporting a caching flag. It will be through querystrings that the handler will be told what resource is being requested.

In the previous article, I discussed the best ways of rendering scripts for a control. Most control developers render the script directly to the page either when the page starts rendering, or when the page finishes rendering or when the control renders. As discussed, these scripts can be pulled out of the assembly for easier development, but rendering the script directly in the page doesn't allow for any caching of the data. With this method, a control could render a script tag that looks like <SCRIPT type='text/javascript' src='MyDev.Web.Controls.aspx?Res=MyControlScript.js' /&gt;. This causes the browser to make a separate request for the script which can now be cached by the browser. The result is that the pages will load much faster.

I created a class that implements IHttpHandler and added as many helpful features as I could think of. It will handle text and binary resources and make its best determination as to the content-type of the resource. It also supports some useful methods for registering resources with a page. As this class deals with reflection, assemblies and resources, I have cached objects and data that are often referenced and minimised the number of times the same method is called when processing the resources. Let me know if you find more improvements that can be made. 

Here are the goods:

# Region &quot; Imports &quot;

 Imports System

 Imports System.Web

# End  Region

 ''' -----------------------------------------------------------------------------

 ''' Project : MyDev.Web.Controls

 ''' Class : Web.Controls.ResHandler

 '''

 ''' -----------------------------------------------------------------------------

 ''' <summary&gt;

 ''' ResHandler object.

 ''' </summary&gt;

 ''' <remarks&gt;

 ''' Handles HTTP requests for resources in this assembly.

 ''' </remarks&gt;

 ''' <history&gt;

 ''' 25/Jan/2005 Created

 ''' </history&gt;

 ''' -----------------------------------------------------------------------------

 Public  Class ResHandler

# Region &quot; Declarations &quot;

 Implements IHttpHandler

 ''' -----------------------------------------------------------------------------

 ''' Project : MyDev.Web.Controls

 ''' Struct : Web.Controls.ResHandler.TResType

 '''

 ''' -----------------------------------------------------------------------------

 ''' <summary&gt;

 ''' Stores details about a resource type.

 ''' </summary&gt;

 ''' <remarks&gt;

 ''' None.

 ''' </remarks&gt;

 ''' <history&gt;

 ''' 25/Jan/2005 Created

 ''' </history&gt;

 ''' -----------------------------------------------------------------------------

 Public  Structure TResType

 ''' -----------------------------------------------------------------------------

 ''' <summary&gt;

 ''' The perceived type is the broad type of the resource, such as text or image.

 ''' </summary&gt;

 ''' <remarks&gt;

 ''' None.

 ''' </remarks&gt;

 ''' <history&gt;

 ''' 25/Jan/2005 Created

 ''' </history&gt;

 ''' -----------------------------------------------------------------------------

 Public PerceivedType As  String

 ''' -----------------------------------------------------------------------------

 ''' <summary&gt;

 ''' The content type that is specified to the client.

 ''' </summary&gt;

 ''' <remarks&gt;

 ''' None.

 ''' </remarks&gt;

 ''' <history&gt;

 ''' 25/Jan/2005 Created

 ''' </history&gt;

 ''' -----------------------------------------------------------------------------

 Public ContentType As  String

 End  Structure

 ''' -----------------------------------------------------------------------------

 ''' <summary&gt;

 ''' Assembly object used to cache the object reference.

 ''' </summary&gt;

 ''' <remarks&gt;

 ''' None.

 ''' </remarks&gt;

 ''' <history&gt;

 ''' 25/Jan/2005 Created

 ''' </history&gt;

 ''' -----------------------------------------------------------------------------

 Private m_objAssembly As Reflection.Assembly

 ''' -----------------------------------------------------------------------------

 ''' <summary&gt;

 ''' The name of the current assemblies namespace.

 ''' </summary&gt;

 ''' <remarks&gt;

 ''' None.

 ''' </remarks&gt;

 ''' <history&gt;

 ''' 27/Jan/2005 Created

 ''' </history&gt;

 ''' -----------------------------------------------------------------------------

 Private m_sAssemblyName As  String

# End  Region

# Region &quot; Constructors and Destructor &quot;

# End  Region

# Region &quot; Events &quot;

# End  Region

# Region &quot; Sub Procedures &quot;

 ''' -----------------------------------------------------------------------------

 ''' <summary&gt;

 ''' Loads the file type information from the registry.

 ''' </summary&gt;

 ''' <param name=&quot;sFileExt&quot;&gt;The file extension to check.</param&gt;

 ''' <param name=&quot;objType&quot;&gt;The type information for the file type.</param&gt;

 ''' <remarks&gt;

 ''' None.

 ''' </remarks&gt;

 ''' <history&gt;

 ''' 25/Jan/2005 Created

 ''' </history&gt;

 ''' -----------------------------------------------------------------------------

 Private  Sub LoadTypeFromRegistry( _

 ByVal sFileExt As  String , _

 ByRef objType As TResType)

 Dim objKey As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(&quot;.&quot; & sFileExt, False )

 ' Check if the Content Type exists

 If  Not objKey.GetValue(&quot;Content Type&quot;) Is  Nothing  Then

 objType.ContentType = CType (objKey.GetValue(&quot;Content Type&quot;), String )

 End  If  ' End checking if the Content Type exists

 ' Check if the PerceivedType exists

 If  Not objKey.GetValue(&quot;PerceivedType&quot;) Is  Nothing  Then

 ' Store the PerceivedType value

 objType.PerceivedType = CType (objKey.GetValue(&quot;PerceivedType&quot;), String )

 End  If  ' Check if the PerceivedType exists

 ' Check if the perceived type doesn't exist, but can be determined from the content type

 If objType.PerceivedType = vbNullString _

 AndAlso objType.ContentType <&gt; vbNullString _

 AndAlso objType.ContentType.IndexOf(&quot;/&quot;) &gt; -1 Then

 objType.PerceivedType = objType.ContentType.Substring(0, objType.ContentType.IndexOf(&quot;/&quot;))

 End  If  ' End checking if the perceived type doesn't exist, but can be determined from the content type

 End  Sub

 ''' -----------------------------------------------------------------------------

 ''' <summary&gt;

 ''' Loads the file type information by the file type.

 ''' </summary&gt;

 ''' <param name=&quot;sFileExt&quot;&gt;The file extension to check.</param&gt;

 ''' <param name=&quot;objType&quot;&gt;The type information for the file type.</param&gt;

 ''' <remarks&gt;

 ''' None.

 ''' </remarks&gt;

 ''' <history&gt;

 ''' 25/Jan/2005 Created

 ''' </history&gt;

 ''' -----------------------------------------------------------------------------

 Private  Sub LoadTypeFromName( _

 ByVal sFileExt As  String , _

 ByRef objType As TResType)

 Select  Case sFileExt

 Case &quot;mid&quot;, &quot;midi&quot;, &quot;mp3&quot;, &quot;wav&quot;, &quot;wma&quot;

 ' Toggle sfileext as appropriate for storage

 If sFileExt = &quot;mp3&quot; Then sFileExt = &quot;mpeg&quot;

 If sFileExt = &quot;wma&quot; Then sFileExt = &quot;x-ms-wma&quot;

 objType.PerceivedType = &quot;audio&quot;

 Case &quot;csv&quot;, &quot;xls&quot;, &quot;doc&quot;, &quot;dot&quot;, &quot;hta&quot;, &quot;ppt&quot;, &quot;zip&quot;, &quot;tgz&quot;

 ' Toggle sfileext as appropriate for storage

 If sFileExt = &quot;csv&quot; OrElse sFileExt = &quot;xls&quot; Then sFileExt = &quot;vnd.ms-excel&quot;

 If sFileExt = &quot;doc&quot; OrElse sFileExt = &quot;dot&quot; Then sFileExt = &quot;msword&quot;

 If sFileExt = &quot;ppt&quot; Then sFileExt = &quot;vnd.ms-powerpoint&quot;

 If sFileExt = &quot;zip&quot; Then sFileExt = &quot;x-zip-compressed&quot;

 If sFileExt = &quot;tgz&quot; Then sFileExt = &quot;x-compressed&quot;

 objType.PerceivedType = &quot;application&quot;

 Case &quot;bmp&quot;, &quot;gif&quot;, &quot;jpg&quot;, &quot;jpeg&quot;, &quot;png&quot;, &quot;tiff&quot;, &quot;tif&quot;, &quot;ico&quot;

 ' Toggle sfileext as appropriate for storage

 If sFileExt = &quot;ico&quot; Then sFileExt = &quot;x-icon&quot;

 If sFileExt = &quot;tif&quot; Then sFileExt = &quot;tiff&quot;

 objType.PerceivedType = &quot;image&quot;

 Case &quot;mht&quot;, &quot;mhtml&quot;

 ' Toggle sfileext as appropriate for storage

 sFileExt = &quot;rfc822&quot;

 objType.PerceivedType = &quot;message&quot;

 Case &quot;txt&quot;, &quot;tab&quot;, &quot;vb&quot;, &quot;vbs&quot;, &quot;js&quot;, &quot;xml&quot;, &quot;xsl&quot;, &quot;htc&quot;, &quot;htm&quot;, &quot;html&quot;

 ' Toggle sfileext as appropriate for storage

 If sFileExt = &quot;vb&quot; OrElse sFileExt = &quot;vbs&quot; Then sFileExt = &quot;vbscript&quot;

 If sFileExt = &quot;js&quot; Then sFileExt = &quot;javascript&quot;

 If sFileExt = &quot;txt&quot; OrElse sFileExt = &quot;tab&quot; Then sFileExt = &quot;plain&quot;

 If sFileExt = &quot;xsl&quot; Then sFileExt = &quot;xml&quot;

 If sFileExt = &quot;htc&quot; Then sFileExt = &quot;x-component&quot;

 If sFileExt = &quot;htm&quot; Then sFileExt = &quot;html&quot;

 objType.PerceivedType = &quot;text&quot;

 Case &quot;avi&quot;, &quot;mov&quot;, &quot;mpe&quot;, &quot;mpeg&quot;

 ' Toggle sfileext as appropriate for storage

 If sFileExt = &quot;mov&quot; Then sFileExt = &quot;quicktime&quot;

 If sFileExt = &quot;mpe&quot; Then sFileExt = &quot;mpeg&quot;

 objType.PerceivedType = &quot;video&quot;

 Case  Else

 ' Default to binary data

 objType.PerceivedType = &quot;application&quot;

 sFileExt = &quot;octet-stream&quot;

 End  Select

 ' Create the content type from the perceived type and the file extension

 objType.ContentType = objType.PerceivedType & &quot;/&quot; & sFileExt

 End  Sub

 ''' -----------------------------------------------------------------------------

 ''' <summary&gt;

 ''' Registers the resource with the page.

 ''' </summary&gt;

 ''' <param name=&quot;sResource&quot;&gt;The name of the resource to register.</param&gt;

 ''' <param name=&quot;objPage&quot;&gt;The page to register the resource with.</param&gt;

 ''' <param name=&quot;bRegisterAsStartupScript&quot;&gt;True if the resource is to be registered as a startup script, otherwise False.</param&gt;

 ''' <remarks&gt;

 ''' None.

 ''' </remarks&gt;

 ''' <history&gt;

 ''' 27/Jan/2005 Created

 ''' </history&gt;

 ''' -----------------------------------------------------------------------------

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

 If sReference <&gt; vbNullString Then

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

 ''' -----------------------------------------------------------------------------

 ''' <summary&gt;

 ''' Enables processing of HTTP Web requests by a custom HttpHandler that implements the IHttpHandler interface.

 ''' </summary&gt;

 ''' <param name=&quot;context&quot;&gt;An HttpContext object that provides references to the intrinsic server objects (for example, Request, Response, Session, and Server) used to service HTTP requests.</param&gt;

 ''' <remarks&gt;

 ''' None.

 ''' </remarks&gt;

 ''' <history&gt;

 ''' 25/Jan/2005 Created

 ''' </history&gt;

 ''' -----------------------------------------------------------------------------

 Public  Sub ProcessRequest( _

 ByVal context As HttpContext) _

 Implements IHttpHandler.ProcessRequest

 ' Clear any existing response data

 context.Response.Clear()

 Try

 Dim sResource As  String = context.Request.QueryString(&quot;Res&quot;)

 Dim bSuccess As  Boolean

 ' Check if the resource request is valid

 If IsValid(sResource) = True  Then

 Dim bNoCache As  Boolean = False

 Dim tType As TResType = ResourceType(sResource)

 ' Check if NoCache has been specified

 If context.Request.QueryString(&quot;NoCache&quot;) <&gt; vbNullString Then

 Try

 ' Get the cache value

 bNoCache = System.Boolean.Parse(context.Request.QueryString(&quot;NoCache&quot;))

 Catch

 ' Do nothing

 End  Try

 End  If  ' End checking if NoCache has been specified

 With context.Response

 ' Set up the response

 .ContentType = tType.ContentType

 .Cache.VaryByParams(&quot;Res&quot;) = True

 .Cache.VaryByParams(&quot;NoCache&quot;) = True

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

 If tType.PerceivedType = &quot;text&quot; Then

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

 .Status = &quot;The resource requested doesn't exist.&quot;

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

# Region &quot; Functions &quot;

 ''' -----------------------------------------------------------------------------

 ''' <summary&gt;

 ''' Determines whether the resource name specified exists in the assembly.

 ''' </summary&gt;

 ''' <param name=&quot;sResource&quot;&gt;The name of the resource to find.</param&gt;

 ''' <returns&gt;True if the resource exists in the assembly, otherwise False.</returns&gt;

 ''' <remarks&gt;

 ''' If the resource name is found, the sResource value is updated to be the full name of the resource, including the assemlby name.

 ''' </remarks&gt;

 ''' <history&gt;

 ''' 25/Jan/2005 Created

 ''' </history&gt;

 ''' -----------------------------------------------------------------------------

 Public  Function IsValid( _

 ByRef sResource As  String ) As  Boolean

 ' Exit if no value is specified

 If sResource = vbNullString Then  Return  False

 Dim aResources() As  String = ThisAssembly.GetManifestResourceNames

 Dim sCheckName As  String = sResource.ToLower

 Dim nCount As Int32

 ' Add the assembly namespace to the resource name if it isn't already

 If sCheckName.StartsWith(AssemblyName.ToLower) = False  Then sCheckName = AssemblyName.ToLower & &quot;.&quot; & sCheckName

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

 ''' -----------------------------------------------------------------------------

 ''' <summary&gt;

 ''' Returns a URL that is used to reference a resource in the assembly.

 ''' </summary&gt;

 ''' <param name=&quot;sResource&quot;&gt;The name of the resource.</param&gt;

 ''' <returns&gt;A URL that is used to reference a resource in the assembly.</returns&gt;

 ''' <remarks&gt;

 ''' NoCache is False by default in this handler.

 ''' </remarks&gt;

 ''' <history&gt;

 ''' 25/Jan/2005 Created

 ''' </history&gt;

 ''' -----------------------------------------------------------------------------

 Public  Function URL( _

 ByVal sResource As  String , _

 Optional  ByVal bNoCache As  Boolean = False ) As  String

 Dim sURL As  String = AssemblyName & &quot;.aspx?Res=&quot; & HttpUtility.UrlEncode(sResource)

 ' Check if NoCache is specified

 If bNoCache = True  Then

 sURL &= &quot;&NoCache=&quot; & bNoCache

 End  If  ' End checking if NoCache is specified

 ' Return the URL built

 Return sURL

 End  Function

 ''' -----------------------------------------------------------------------------

 ''' <summary&gt;

 ''' Returns a string that can be put into a page to reference a specified resource.

 ''' </summary&gt;

 ''' <param name=&quot;sResource&quot;&gt;The name of the resource to reference.</param&gt;

 ''' <returns&gt;A string that can be put into a page to reference a specified resource.</returns&gt;

 ''' <remarks&gt;

 ''' This function will only return a value for specific resources types.

 ''' The return value of this function will either be a SCRIPT or IMG tag, or the resource contents itself.

 ''' </remarks&gt;

 ''' <history&gt;

 ''' 27/Jan/2005 Created

 ''' </history&gt;

 ''' -----------------------------------------------------------------------------

 Public  Function PageReference( _

 ByVal sResource As  String ) As  String

 Dim tType As TResType = ResourceType(sResource)

 If tType.PerceivedType = &quot;image&quot; Then

 Return &quot;<IMG src='&quot; & URL(sResource) & &quot;'&gt;</IMG&gt;&quot;

 ElseIf tType.ContentType = &quot;text/css&quot; Then

 Return &quot;<LINK rel='Stylesheet' href='&quot; & URL(sResource) & &quot;'&gt;</LINK&gt;&quot;

 ElseIf tType.ContentType = &quot;text/javascript&quot; Then

 Return &quot;<SCRIPT language='javascript' type='text/javascript' src='&quot; & URL(sResource) & &quot;'&gt;</SCRIPT&gt;&quot;

 ElseIf tType.ContentType = &quot;text/vbscript&quot; Then

 Return &quot;<SCRIPT language='vbscript' type='text/vbscript' src='&quot; & URL(sResource) & &quot;'&gt;</SCRIPT&gt;&quot;

 ElseIf tType.PerceivedType = &quot;text&quot; Then

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

 ''' -----------------------------------------------------------------------------

 ''' <summary&gt;

 ''' Returns the type of resource specified.

 ''' </summary&gt;

 ''' <param name=&quot;sResource&quot;&gt;The name of the resource to check.</param&gt;

 ''' <returns&gt;The type details of the resource.</returns&gt;

 ''' <remarks&gt;

 ''' None.

 ''' </remarks&gt;

 ''' <history&gt;

 ''' 25/Jan/2005 Created

 ''' </history&gt;

 ''' -----------------------------------------------------------------------------

 Public  Function ResourceType( _

 ByVal sResource As  String ) As TResType

 ' Check if there is a .

 If sResource <&gt; vbNullString _

 AndAlso sResource.LastIndexOf(&quot;.&quot;) &gt; -1 Then

 ' Get the file extension

 Dim sFileExt As  String = sResource.Substring(sResource.LastIndexOf(&quot;.&quot;) + 1).ToLower

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

 If objType.ContentType = vbNullString AndAlso objTempType.ContentType <&gt; vbNullString Then objType.ContentType = objTempType.ContentType

 If objType.PerceivedType = vbNullString AndAlso objTempType.PerceivedType <&gt; vbNullString Then objType.PerceivedType = objTempType.PerceivedType

 End  If  ' End checking if there is any type information missing

 ' Return the type information determined

 Return objType

 Else  ' An invalid value has been specified

 Return  Nothing

 End  If  ' End checking if there is a .

 End  Function

 ''' -----------------------------------------------------------------------------

 ''' <summary&gt;

 ''' Loads a text resource from the assembly.

 ''' </summary&gt;

 ''' <param name=&quot;sResouce&quot;&gt;The name of the resource to load.</param&gt;

 ''' <param name=&quot;sValue&quot;&gt;The string the resource will be stored in.</param&gt;

 ''' <returns&gt;True if the resource was successfully loaded, otherwise False.</returns&gt;

 ''' <remarks&gt;

 ''' None.

 ''' </remarks&gt;

 ''' <history&gt;

 ''' 27/Jan/2005 Created

 ''' </history&gt;

 ''' -----------------------------------------------------------------------------

 Public  Function LoadTextResource( _

 ByVal sResource As  String , _

 ByRef sValue As  String ) As  Boolean

 Dim bReturn As  Boolean

 ' Check if the resource name is valid

 If IsValid(sResource) = True  Then

 Dim tType As TResType = ResourceType(sResource)

 ' Check if the resource is a text resource

 If tType.PerceivedType = &quot;text&quot; Then

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

 ''' -----------------------------------------------------------------------------

 ''' <summary&gt;

 ''' Loads a binary resource from the assembly.

 ''' </summary&gt;

 ''' <param name=&quot;sResource&quot;&gt;The name of the resource to load.</param&gt;

 ''' <param name=&quot;aBuffer&quot;&gt;The buffer to store the resource contents in.</param&gt;

 ''' <returns&gt;True if the resource was successfully loaded, otherwise False.</returns&gt;

 ''' <remarks&gt;

 ''' None.

 ''' </remarks&gt;

 ''' <history&gt;

 ''' 27/Jan/2005 Created

 ''' </history&gt;

 ''' -----------------------------------------------------------------------------

 Public  Function LoadBinaryResource( _

 ByVal sResource As  String , _

 ByRef aBuffer As  Byte ()) As  Boolean

 Dim bReturn As  Boolean

 ' Check if the resource name is valid

 If IsValid(sResource) = True  Then

 Dim tType As TResType = ResourceType(sResource)

 ' Check if the resource is a text resource

 If tType.PerceivedType <&gt; &quot;text&quot; Then

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

# Region &quot; Properties &quot;

 ''' -----------------------------------------------------------------------------

 ''' <summary&gt;

 ''' Returns a reference to the current cached assembly.

 ''' </summary&gt;

 ''' <value&gt;A reference to the current cached assembly.</value&gt;

 ''' <remarks&gt;

 ''' None.

 ''' </remarks&gt;

 ''' <history&gt;

 ''' 25/Jan/2005 Created

 ''' </history&gt;

 ''' -----------------------------------------------------------------------------

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

 ''' -----------------------------------------------------------------------------

 ''' <summary&gt;

 ''' Gets the namespace value of the current assembly.

 ''' </summary&gt;

 ''' <value&gt;The namespace value of the current assembly.</value&gt;

 ''' <remarks&gt;

 ''' None.

 ''' </remarks&gt;

 ''' <history&gt;

 ''' 27/Jan/2005 Created

 ''' </history&gt;

 ''' -----------------------------------------------------------------------------

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

 ''' -----------------------------------------------------------------------------

 ''' <summary&gt;

 ''' Gets a value indicating whether another request can use the IHttpHandler instance.

 ''' </summary&gt;

 ''' <value&gt;True if the IHttpHandler instance is reusable; otherwise, False.</value&gt;

 ''' <remarks&gt;

 ''' </remarks&gt;

 ''' <history&gt;

 ''' 25/Jan/2005 Created

 ''' </history&gt;

 ''' -----------------------------------------------------------------------------

 Public  ReadOnly  Property IsReusable() As  Boolean  Implements IHttpHandler.IsReusable

 Get

 Return  True

 End  Get

 End  Property

# End  Region

# Region &quot; Classes &quot;

# End  Region

 End  Class

[0]: http://www.codeproject.com/aspnet/ressrvpage.asp
