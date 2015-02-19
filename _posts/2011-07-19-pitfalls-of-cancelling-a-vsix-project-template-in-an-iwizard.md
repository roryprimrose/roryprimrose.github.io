---
title: Pitfalls of cancelling a VSIX project template in an IWizard
categories : .Net
tags : Extensibility
date: 2011-07-19 12:36:44 +10:00
---

I’ve been creating a VSIX project template for Visual Studio over the last week or two. The project contains an IWizard class implementation to allow the user to define some information for the template. 

I have noticed one weird quirk when it comes to cancelling the wizard. The project template is written to disk before the IWizard.RunStarted method is invoked. This raises some questions about how to cancel the wizard process and make the solution (and disk) look like nothing ever happened.

**How to cancel the wizard**

Basically any exception thrown from IWizard.RunStarted will cause the wizard process to be cancelled. To be a good citizen in VSIX however, you should throw either the [WizardCancelledException][0] or [WizardBackoutException][1]. The only difference between these exceptions seems to be that the WizardBackoutException takes the user back to the Add New Project dialog whereas the WizardCancelledException shows the IDE like it was before the Add New Project dialog was displayed.![image][2]

Once the exception has been thrown however, the project template still exists on disk. It is up to the VSIX developer to manually clean up the new project folder under the solution to make the disk look like it did before attempting to add the project. 

My project uses the following logic to achieve this.

{% highlight csharp linenos %}
public void RunStarted(
    Object automationObject, Dictionary<String, String> replacementsDictionary, WizardRunKind runKind, Object[] customParams)
{
    DTE dte = automationObject as DTE;
    
    String destinationDirectory = replacementsDictionary["$destinationdirectory$"];
    
    try
    {
        using (PackageDefinition definition = new PackageDefinition(dte, destinationDirectory))
        {
            DialogResult dialogResult = definition.ShowDialog();
    
            if (dialogResult != DialogResult.OK)
            {
                throw new WizardBackoutException();
            }
    
            replacementsDictionary.Add("$packagePath$", definition.PackagePath);
            replacementsDictionary.Add("$packageExtension$", Path.GetExtension(definition.PackagePath));
    
            _dependentProjectName = definition.SelectedProject;
        }
    }
    catch (Exception ex)
    {
        // Clean up the template that was written to disk
        if (Directory.Exists(destinationDirectory))
        {
            Directory.Delete(destinationDirectory, true);
        }
    
        Debug.WriteLine(ex);
    
        throw;
    }
}
{% endhighlight %}

The replacementsDictionary parameter contains the path of the new project. The wizard process will then catch any exceptions and delete the directory to clean up the failed project template. This also covers the scenario where the user cancels or backs out of the wizard in the template.

[0]: http://msdn.microsoft.com/en-us/library/microsoft.visualstudio.templatewizard.wizardcancelledexception.aspx
[1]: http://msdn.microsoft.com/en-us/library/microsoft.visualstudio.templatewizard.wizardbackoutexception.aspx
[2]: /files/image_122.png
