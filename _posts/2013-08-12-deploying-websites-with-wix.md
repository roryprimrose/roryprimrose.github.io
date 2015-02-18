---
title: Deploying websites with WiX
tags : WiX
date: 2013-08-12 15:15:23 +10:00
---

I [posted back in 2010][0] about how to get WiX to correctly deploy a website using harvesting. It was a rather clunky solution that required changes to your development machine (and the same for everyone in your team) as well as any build infrastructure you are running. I have now discovered a much cleaner solution using harvest transforms (tested using WiX 3.7).

As a recap, harvesting pulls files from the project reference compilation into the MSI and puts them into a single output directory. This means that website content and the binaries will be in the root folder of the deployment. This is no good for websites that require binaries to be placed into a bin folder. 

Here is how to fix it. Firstly, you need to add your web project as a project reference in your WiX project.![][1]

You then need to enable harvesting on the properties of that project reference. Set the Directory Id to the Id in your file system that WiX will use for the website. The Project Output Groups is fine with the default of BinariesContentSatellites.![][2]

I do not recall this being an issue in previous versions, but this won’t actually harvest the project. It seems like harvesting is now disabled by default. This needs to be enabled by editing the WiX proj file and adding the following into the first PropertyGroup element.{% highlight xml linenos %}
<EnableProjectHarvesting>true</EnableProjectHarvesting>
{% endhighlight %}

Next, you need to make sure that the Product.Generated component group reference is emitted by your MSI. For example:{% highlight xml linenos %}
<Feature Id="ProductFeature"
            Title="$(var.ShortProductName)"
            Level="1"
            ConfigurableDirectory="INSTALLDIR"
            AllowAdvertise="no"
            InstallDefault="local"
            Absent="disallow"
            Display="expand">
    <ComponentGroupRef Id="Product.Generated" />
</Feature>
{% endhighlight %}

We now need the MSI to create a directory for the bin folder. I like the use a FileSystem.wxs file for this purpose. For example:{% highlight xml linenos %}
<?xml version="1.0"
        encoding="utf-8"?>
<?include Definitions.wxi ?>
    
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi"
        xmlns:util="http://schemas.microsoft.com/wix/UtilExtension">
    <Fragment>
    <Directory Id="TARGETDIR"
                Name="SourceDir">
        <Directory Id="ProgramFilesFolder">
        <Directory Id="ProgramFilesCompany"
                    Name="$(var.CompanyName)">
            <Directory Id="INSTALLDIR"
                        Name="$(var.ShortProductName)">
            <Directory Id="WebsiteDir"
                        Name="Website">
                <Component Id="RegisterInstallPath"
                            Guid="127AC02A-79B9-590E-BDC4-4730E6366533">
                <RemoveFolder Id="RemoveProgramFilesCompany"
                                Directory="ProgramFilesCompany"
                                On="uninstall" />
                <RegistryValue Id="StoreInstallLocation"
                                Root='HKLM'
                                Key='Software\$(var.CompanyName)\$(var.ShortProductName)'
                                Name='InstallDir'
                                Action='write'
                                Type='string'
                                Value='[INSTALLDIR]' />
                </Component>
                <Component Id="ConfigurationUpdates" Guid="2C6E52CC-3492-4483-B5DE-FECC4F529FCA">
                <CreateFolder />
                <util:XmlConfig Sequence="1"
                                Id="SwitchOffDebug"
                                File="[WebsiteDir]\web.config"
                                Action="create"
                                On="install"
                                Node="value"
                                ElementPath="/configuration/system.web/compilation"
                                Name="debug"
                                Value="false" />
                </Component>
                <Directory Id="WebsiteBinDir"
                            Name="bin">
                </Directory>
            </Directory>
            <Directory Id="DatabaseDir"
                        Name="Database">
            </Directory>
            </Directory>
        </Directory>
        </Directory>
    </Directory>
    
    <ComponentGroup Id="FileSystem">
        <ComponentRef Id="RegisterInstallPath"/>
        <ComponentRef Id="ConfigurationUpdates"/>
    </ComponentGroup>
        
    </Fragment>
</Wix>
{% endhighlight %}

Next we add an xslt file into the WiX project and mark its build action as None.![image][3]

This file will be used to transform the harvest of the web project. We need to edit the WiX proj file again to hook this xslt file up to the project reference as the transform for the harvest process.{% highlight xml linenos %}
<ProjectReference Include="..\MyApp.Web\MyApp.Web.csproj">
    <Name>MyApp.Web</Name>
    <Project>{408c88b0-0990-483c-91c5-e678bb8ff3da}</Project>
    <Private>True</Private>
    <RefProjectOutputGroups>Binaries;Content;Satellites</RefProjectOutputGroups>
    <RefTargetDir>WebsiteDir</RefTargetDir>
    <Transforms>WebsiteHarvest.xslt</Transforms>
</ProjectReference>
{% endhighlight %}

The WebsiteHarvest.xslt will now look for binaries that have been harvested and redirect their target directory to the WebsiteBinDir reference defined in the example above.{% highlight xml linenos %}
<xsl:stylesheet version="1.0"
            xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
            xmlns:msxsl="urn:schemas-microsoft-com:xslt"
            exclude-result-prefixes="msxsl"
            xmlns:wix="http://schemas.microsoft.com/wix/2006/wi"
            xmlns:my="my:my">
    
    <xsl:output method="xml" indent="yes" />
    
    <xsl:strip-space elements="*"/>
    
    <xsl:template match="@*|node()">
    <xsl:copy>
        <xsl:apply-templates select="@*|node()"/>
    </xsl:copy>
    </xsl:template>
    
    <xsl:template match="wix:DirectoryRef[substring(wix:Component/wix:File/@Source, string-length(wix:Component/wix:File/@Source) - 3) = '.dll']/@Id">
    <xsl:attribute name="Id">
        <xsl:text>WebsiteBinDir</xsl:text>
    </xsl:attribute>
    </xsl:template>
</xsl:stylesheet>
{% endhighlight %}

Compile the WiX project and deploy the MSI. You should now have a website deployed with the binaries in the bin directory. 

The only disadvantage with this way of deploying websites is that a website publish has not been executed. This means that operations like xml transformations have not been actioned. We handle those scenarios in WiX anyway (see the util:XmlConfig above) so this isn’t a concern. We simply remove web.Debug.config and web.Release.config from the solution so they don’t end up getting bundled in the MSI in the first place.

[0]: /post/2010/06/22/WiX-Heat-extension-to-deploy-web-projects-to-the-bin-directory.aspx
[1]: //blogfiles/image_13.png
[2]: //blogfiles/ScreenShot023.png
[3]: //blogfiles/image_157.png
