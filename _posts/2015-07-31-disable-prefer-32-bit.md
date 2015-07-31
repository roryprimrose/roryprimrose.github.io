---
title: Disable Prefer 32 bit
categories: .Net
tags: 
date: 2015-07-31 13:11:00 +11:00
---

I'm working through a support issue with Microsoft to do with Azure Service Bus. One potential issue raised was that my application is running as 32bit rather than 64bit. When I checked it out, the Prefer 32 bit setting was enabled on the project. This means that the exe's will be running as x86 even though it is running on an x64 machine.

<!--more-->

![Disable Prefer 32 bit][0]

I have discovered that this seems to be the project default for VS2013 and VS2015 for exe based projects. This is enabled because the proj file does not contain the Prefer32Bit element in the build configuration. Disabling this checkbox will put this setting into the project config. The project will now compile an application that will run as x64 on an x64 machine.

{% highlight xml %}

  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ">
    <PlatformTarget>AnyCPU</PlatformTarget>
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
    <Prefer32Bit>false</Prefer32Bit>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' ">
    <PlatformTarget>AnyCPU</PlatformTarget>
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
    <Prefer32Bit>false</Prefer32Bit>
  </PropertyGroup>

{% endhighlight %}

Make sure however that you do this to each build configuration (or make the change when the All Configurations build configuration is selected).

So far this seems to be resolving my particular issue. It is something that should be applied to all exe based VS projects as we should never be targeting 32bit operating systems anymore.

[0]: /files/prefer32bit.jpg
