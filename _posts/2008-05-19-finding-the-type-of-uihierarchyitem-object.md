---
title: Finding the type of UIHierarchyItem.Object
categories: .Net, My Software
tags: Extensibility
date: 2008-05-19 13:50:52 +10:00
---

I have written a few addins over the last couple of years and I have always found the object model really painful to deal with. One of the more common problems I have encountered is trying to identify the type of UIHierarchyItem.Object. This is especially common when you deal with the contents of the Solution Explorer window.

I only know of [one solution][0] for identifying the real type behind the object. You can make a call out to Microsoft.VisualBasic.Information.TypeName(Object) to attempt to resolve the type. This wasn't always successful and was also a little limited as the object often implements multiple types. I realised that there was a very easy and more comprehensive way of figuring out what types were being implemented.

By using this DebugHelper class in your addin solution, you will be able to figure out the object types. The Conditional attribute ensures that these methods (and calls to them) will not appear in your release build. The method takes a UIHierarchyItem and searches all the publicly available types of all the assemblies in the app domain for types that are implemented by the UIHierarchyItem.Object property.

<!--more-->

```csharp
using System;
using System.Diagnostics;
using System.Reflection;
using EnvDTE;
     
namespace Neovolve.Extensibility.VisualStudio
{
    /// <summary>
    /// The <see cref="DebugHelper"/>
    /// class is used to help with debugging tasks for Visual Studio addin development.
    /// </summary>
    /// <remarks>
    /// This class was created by Rory Primrose for the 
    /// <a href="http://www.codeplex.com/NeovolveX" target="_blank">NeovolveX</a>
    /// project.
    /// </remarks>
    public static class DebugHelper
    {
        /// <summary>
        /// Identifies the internal object types.
        /// </summary>
        /// <param name="item">The item.</param>
        [Conditional("DEBUG")]
        public static void IdentifyInternalObjectTypes(UIHierarchyItem item)
        {
            if (item == null)
            {
                Debug.WriteLine("No item provided.");
     
                return;
            }
     
            if (item.Object == null)
            {
                Debug.WriteLine("No item object is available.");
     
                return;
            }
     
            // Loop through all the assemblies in the current app domain
            Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
     
            // Loop through each assembly
            for (Int32 index = 0; index < loadedAssemblies.Length; index++)
            {
                // Assume that the assembly to check against is EnvDTE.dll
                IdentifyInternalObjectTypes(item, loadedAssemblies[index]);
            }
        }
     
        /// <summary>
        /// Identifies the internal object types.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="assemblyToCheck">The assembly to check.</param>
        [Conditional("DEBUG")]
        public static void IdentifyInternalObjectTypes(UIHierarchyItem item, Assembly assemblyToCheck)
        {
            // Get the types that are publically available
            Type[] exportedTypes = assemblyToCheck.GetExportedTypes();
     
            // Loop through each type
            for (Int32 index = 0; index < exportedTypes.Length; index++)
            {
                // Check if the object instance is of this type
                if (exportedTypes[index].IsInstanceOfType(item.Object))
                {
                    Debug.WriteLine(exportedTypes[index].FullName);
                }
            }
        }
    }
}
    
```

Using this debug helper method saved hours if not days in fixing [this problem][1]. I hope this helps other people as well.

[0]: http://forums.microsoft.com/MSDN/ShowPost.aspx?PostID=2123578&amp;SiteID=1
[1]: /2008/05/19/when-is-envdte-project-not-an-envdte-project/
