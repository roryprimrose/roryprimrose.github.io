---
title: Finding solutions not covered by automated builds
tags : TFS
date: 2012-02-07 17:01:07 +10:00
---

I am slowing building a set of automated tasks in my current role as a TFS administrator to verify the state of TFS. My latest task looks for solutions that are not covered by automated builds.

It’s a fairly straight forward task that enumerates solution files and matches them to build definitions across all projects and collections in a TFS instance.

{% highlight csharp %}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Xml;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.VersionControl.Client;
    
namespace TFSVerifier
{
    [Export(typeof(ITask))]
    public class FindSolutionsWithoutBuildsTask : ITask
    {
        private Uri TfsAddress = new Uri("http://[TfsAddressHere]:8080/tfs", UriKind.Absolute);
    
        public string Name
        {
            get { return GetType().Name; }
        }
    
        public void Execute()
        {
            TfsTeamProjectCollection server = new TfsTeamProjectCollection(TfsAddress);
    
            server.EnsureAuthenticated();
    
            TfsConfigurationServer configurationServer = server.ConfigurationServer;
    
            ReadOnlyCollection<CatalogNode> collectionNodes = configurationServer.CatalogNode.QueryChildren(
                new[] { CatalogResourceTypes.ProjectCollection },
                false, CatalogQueryOptions.None);
    
            collectionNodes.ToList().ForEach(x => ProcessCollection(server, x));
        }
    
        private void ProcessCollection(TfsTeamProjectCollection server, CatalogNode collectionNode)
        {
            Guid collectionId = new Guid(collectionNode.Resource.Properties["InstanceId"]);
            TfsTeamProjectCollection teamProjectCollection = server.ConfigurationServer.GetTeamProjectCollection(collectionId);
    
            ReadOnlyCollection<CatalogNode> projectNodes = collectionNode.QueryChildren(
                new[] { CatalogResourceTypes.TeamProject },
                false, CatalogQueryOptions.None);
    
            projectNodes.ToList().ForEach(x => ProcessProject(server, collectionNode, x));
        }
    
        private void ProcessProject(TfsTeamProjectCollection server, CatalogNode collectionNode, CatalogNode projectNode)
        {
            String projectName = projectNode.Resource.DisplayName;
                
            VersionControlServer versionControl = server.GetService<VersionControlServer>();
            ItemSpec spec = new ItemSpec("$/" + projectName + "/*.sln", RecursionType.Full);
            ItemSet set = versionControl.GetItems(spec, VersionSpec.Latest, DeletedState.NonDeleted, ItemType.File, false);
    
            if (set.Items.Any() == false)
            {
                return;
            }
    
            IEnumerable<String> solutionsInProject = from x in set.Items
                                                        select x.ServerItem;
    
            IBuildServer buildServer = server.GetService<IBuildServer>();
            IBuildDefinition[] definitions = buildServer.QueryBuildDefinitions(projectNode.Resource.DisplayName, QueryOptions.Definitions);
            IEnumerable<String> projectsBeingBuild = ProjectsBuiltInProject(definitions);
            IEnumerable<String> projectsNotBeingBuild = solutionsInProject.Except(projectsBeingBuild);
    
            if (projectsNotBeingBuild.Any())
            {
                Console.WriteLine(collectionNode.Resource.DisplayName + ": " + projectName);
    
                Console.ForegroundColor = ConsoleColor.Yellow;
    
                projectsNotBeingBuild.ToList().ForEach(x => Console.WriteLine(x));
    
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
    
        private IEnumerable<String> ProjectsBuiltInProject(IBuildDefinition[] definitions)
        {
            foreach (IBuildDefinition definition in definitions)
            {
                IEnumerable<String> projectsToBuild = ProjectsToBuild(definition);
    
                foreach (String projectToBuild in projectsToBuild)
                {
                    yield return projectToBuild;
                }
            }
        }
    
        private IEnumerable<String> ProjectsToBuild(IBuildDefinition definition)
        {
            XmlDocument doc = new XmlDocument();
            XmlNamespaceManager manager = new XmlNamespaceManager(doc.NameTable);
    
            manager.AddNamespace("y", "clr-namespace:System.Collections.Generic;assembly=mscorlib");
            manager.AddNamespace("x", "clr-namespace:Microsoft.TeamFoundation.Build.Workflow.Activities;assembly=Microsoft.TeamFoundation.Build.Workflow");
    
            doc.LoadXml(definition.ProcessParameters);
    
            XmlNode node = doc.SelectSingleNode("//y:Dictionary/x:BuildSettings/@ProjectsToBuild", manager);
    
            if (node == null)
            {
                return new List<String>();
            }
    
            String projectsToBuild = node.Value;
            String[] projects = projectsToBuild.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
    
            return projects;
        }
    }
}
{% endhighlight %}

Enjoy


