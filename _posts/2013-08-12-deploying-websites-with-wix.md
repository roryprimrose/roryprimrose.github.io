---
title: Deploying websites with WiX
tags : WiX
date: 2013-08-12 15:15:23 +10:00
---

I [posted back in 2010][0] about how to get WiX to correctly deploy a website using harvesting. It was a rather clunky solution that required changes to your development machine (and the same for everyone in your team) as well as any build infrastructure you are running. I have now discovered a much cleaner solution using harvest transforms (tested using WiX 3.7).

As a recap, harvesting pulls files from the project reference compilation into the MSI and puts them into a single output directory. This means that website content and the binaries will be in the root folder of the deployment. This is no good for websites that require binaries to be placed into a bin folder. 

Here is how to fix it. Firstly, you need to add your web project as a project reference in your WiX project.![][1]

You then need to enable harvesting on the properties of that project reference. Set the Directory Id to the Id in your file system that WiX will use for the website. The Project Output Groups is fine with the default of BinariesContentSatellites.![][2]

I do not recall this being an issue in previous versions, but this won’t actually harvest the project. It seems like harvesting is now disabled by default. This needs to be enabled by editing the WiX proj file and adding the following into the first PropertyGroup element.

    <EnableProjectHarvesting&gt;true</EnableProjectHarvesting&gt;{% endhighlight %}

Next, you need to make sure that the Product.Generated component group reference is emitted by your MSI. For example:

    <Feature Id=&quot;ProductFeature&quot;
             Title=&quot;$(var.ShortProductName)&quot;
             Level=&quot;1&quot;
             ConfigurableDirectory=&quot;INSTALLDIR&quot;
             AllowAdvertise=&quot;no&quot;
             InstallDefault=&quot;local&quot;
             Absent=&quot;disallow&quot;
             Display=&quot;expand&quot;&gt;
      <ComponentGroupRef Id=&quot;Product.Generated&quot; /&gt;
    </Feature&gt;{% endhighlight %}

We now need the MSI to create a directory for the bin folder. I like the use a FileSystem.wxs file for this purpose. For example:

    <?xml version=&quot;1.0&quot;
          encoding=&quot;utf-8&quot;?&gt;
    <?include Definitions.wxi ?&gt;
    
    <Wix xmlns=&quot;http://schemas.microsoft.com/wix/2006/wi&quot;
         xmlns:util=&quot;http://schemas.microsoft.com/wix/UtilExtension&quot;&gt;
      <Fragment&gt;
        <Directory Id=&quot;TARGETDIR&quot;
                   Name=&quot;SourceDir&quot;&gt;
          <Directory Id=&quot;ProgramFilesFolder&quot;&gt;
            <Directory Id=&quot;ProgramFilesCompany&quot;
                       Name=&quot;$(var.CompanyName)&quot;&gt;
              <Directory Id=&quot;INSTALLDIR&quot;
                         Name=&quot;$(var.ShortProductName)&quot;&gt;
                <Directory Id=&quot;WebsiteDir&quot;
                           Name=&quot;Website&quot;&gt;
                  <Component Id=&quot;RegisterInstallPath&quot;
                             Guid=&quot;127AC02A-79B9-590E-BDC4-4730E6366533&quot;&gt;
                    <RemoveFolder Id=&quot;RemoveProgramFilesCompany&quot;
                                  Directory=&quot;ProgramFilesCompany&quot;
                                  On=&quot;uninstall&quot; /&gt;
                    <RegistryValue Id=&quot;StoreInstallLocation&quot;
                                   Root='HKLM'
                                   Key='Software\$(var.CompanyName)\$(var.ShortProductName)'
                                   Name='InstallDir'
                                   Action='write'
                                   Type='string'
                                   Value='[INSTALLDIR]' /&gt;
                  </Component&gt;
                  <Component Id=&quot;ConfigurationUpdates&quot; Guid=&quot;2C6E52CC-3492-4483-B5DE-FECC4F529FCA&quot;&gt;
                    <CreateFolder /&gt;
                    <util:XmlConfig Sequence=&quot;1&quot;
                                    Id=&quot;SwitchOffDebug&quot;
                                    File=&quot;[WebsiteDir]\web.config&quot;
                                    Action=&quot;create&quot;
                                    On=&quot;install&quot;
                                    Node=&quot;value&quot;
                                    ElementPath=&quot;/configuration/system.web/compilation&quot;
                                    Name=&quot;debug&quot;
                                    Value=&quot;false&quot; /&gt;
                  </Component&gt;
                  <Directory Id=&quot;WebsiteBinDir&quot;
                             Name=&quot;bin&quot;&gt;
                  </Directory&gt;
                </Directory&gt;
                <Directory Id=&quot;DatabaseDir&quot;
                           Name=&quot;Database&quot;&gt;
                </Directory&gt;
              </Directory&gt;
            </Directory&gt;
          </Directory&gt;
        </Directory&gt;
    
        <ComponentGroup Id=&quot;FileSystem&quot;&gt;
          <ComponentRef Id=&quot;RegisterInstallPath&quot;/&gt;
          <ComponentRef Id=&quot;ConfigurationUpdates&quot;/&gt;
        </ComponentGroup&gt;
        
      </Fragment&gt;
    </Wix&gt;{% endhighlight %}

Next we add an xslt file into the WiX project and mark its build action as None.![image][3]

This file will be used to transform the harvest of the web project. We need to edit the WiX proj file again to hook this xslt file up to the project reference as the transform for the harvest process.

    <ProjectReference Include=&quot;..\MyApp.Web\MyApp.Web.csproj&quot;&gt;
      <Name&gt;MyApp.Web</Name&gt;
      <Project&gt;{408c88b0-0990-483c-91c5-e678bb8ff3da}</Project&gt;
      <Private&gt;True</Private&gt;
      <RefProjectOutputGroups&gt;Binaries;Content;Satellites</RefProjectOutputGroups&gt;
      <RefTargetDir&gt;WebsiteDir</RefTargetDir&gt;
      <Transforms&gt;WebsiteHarvest.xslt</Transforms&gt;
    </ProjectReference&gt;{% endhighlight %}

The WebsiteHarvest.xslt will now look for binaries that have been harvested and redirect their target directory to the WebsiteBinDir reference defined in the example above.

    <xsl:stylesheet version=&quot;1.0&quot;
                xmlns:xsl=&quot;http://www.w3.org/1999/XSL/Transform&quot;
                xmlns:msxsl=&quot;urn:schemas-microsoft-com:xslt&quot;
                exclude-result-prefixes=&quot;msxsl&quot;
                xmlns:wix=&quot;http://schemas.microsoft.com/wix/2006/wi&quot;
                xmlns:my=&quot;my:my&quot;&gt;
    
      <xsl:output method=&quot;xml&quot; indent=&quot;yes&quot; /&gt;
    
      <xsl:strip-space elements=&quot;*&quot;/&gt;
    
      <xsl:template match=&quot;@*|node()&quot;&gt;
        <xsl:copy&gt;
          <xsl:apply-templates select=&quot;@*|node()&quot;/&gt;
        </xsl:copy&gt;
      </xsl:template&gt;
    
      <xsl:template match=&quot;wix:DirectoryRef[substring(wix:Component/wix:File/@Source, string-length(wix:Component/wix:File/@Source) - 3) = '.dll']/@Id&quot;&gt;
        <xsl:attribute name=&quot;Id&quot;&gt;
          <xsl:text&gt;WebsiteBinDir</xsl:text&gt;
        </xsl:attribute&gt;
      </xsl:template&gt;
    </xsl:stylesheet&gt;{% endhighlight %}

Compile the WiX project and deploy the MSI. You should now have a website deployed with the binaries in the bin directory. 

The only disadvantage with this way of deploying websites is that a website publish has not been executed. This means that operations like xml transformations have not been actioned. We handle those scenarios in WiX anyway (see the util:XmlConfig above) so this isn’t a concern. We simply remove web.Debug.config and web.Release.config from the solution so they don’t end up getting bundled in the MSI in the first place.

[0]: /post/2010/06/22/WiX-Heat-extension-to-deploy-web-projects-to-the-bin-directory.aspx
[1]: //blogfiles/image_13.png
[2]: //blogfiles/ScreenShot023.png
[3]: //blogfiles/image_157.png
