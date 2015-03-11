---
title: SSAS fails to process TFS cube in Tfs_Analysis
tags : TFS
date: 2012-03-02 10:43:47 +10:00
---



One of the TFS instances that I am responsible for started failing to process its Analysis Services cube a few days ago. The nature of the environment is that I can only do a reboot after hours. I also wanted to try to find out what was wrong before resorting to a reboot so that we could try to fix the problem rather than just doing a band-aid.  

The topology of the TFS deployment is the following:  

* TFS App Tier – also hosts SSRS
* TFS Data Tier – SQL Server, SSIS and SSAS
* TFS SharePoint Tier
* TFS Controllers
* TFS Build Environment
* CI Host Platform

<!--more-->

Each of the services use domain accounts that are unique to each service.   

The primary problem was that the 2 hourly job to reprocess the Tfs_Analysis SSAS cube from the Tfs_Warehouse database failed because SSAS could not connect to the SQL Server data engine. The following is most of the exception detail of the failure.  

{% highlight text %}
[Full Analysis Database Sync]: 
AnalysisDatabaseProcessingType=Full, needCubeSchemaUpdate=True. 
Microsoft.TeamFoundation.Server.WarehouseException: TF221122: An error occurred running job Full Analysis Database Sync for team project collection or Team Foundation server TEAM FOUNDATION. 
Microsoft.TeamFoundation.Server.WarehouseException: Failed to Process Analysis Database 'Tfs_Analysis'. 
Microsoft.TeamFoundation.Server.WarehouseException: Internal error: The operation terminated unsuccessfully.
OLE DB error: OLE DB or ODBC error: A network-related or instance-specific error has occurred while establishing a connection to SQL Server. Server is not found or not accessible. Check if instance name is correct and if SQL Server is configured to allow remote connections. For more information see SQL Server Books Online.; 08001; Client unable to establish connection; 08001; Encryption not supported on the client.; 08001.
Errors in the high-level relational engine. A connection could not be made to the data source with the DataSourceID of 'Tfs_AnalysisDataSource', Name of 'Tfs_AnalysisDataSource'.
Errors in the OLAP storage engine: An error occurred while the dimension, with the ID of 'Today', Name of 'Today' was being processed.
Errors in the OLAP storage engine: An error occurred while the 'Day Of Year' attribute of the 'Today' dimension from the 'Tfs_Analysis' database was being processed.
Server: The operation has been cancelled.
OLE DB error: OLE DB or ODBC error: A network-related or instance-specific error has occurred while establishing a connection to SQL Server. Server is not found or not accessible. Check if instance name is correct and if SQL Server is configured to allow remote connections. For more information see SQL Server Books Online.; 08001; Client unable to establish connection; 08001; Encryption not supported on the client.; 08001.
Errors in the high-level relational engine. A connection could not be made to the data source with the DataSourceID of 'Tfs_AnalysisDataSource', Name of 'Tfs_AnalysisDataSource'.
Errors in the OLAP storage engine: An error occurred while the dimension, with the ID of 'Today', Name of 'Today' was being processed.
Errors in the OLAP storage engine: An error occurred while the 'Day Of Month' attribute of the 'Today' dimension from the 'Tfs_Analysis' database was being processed.
OLE DB error: OLE DB or ODBC error: A network-related or instance-specific error has occurred while establishing a connection to SQL Server. Server is not found or not accessible. Check if instance name is correct and if SQL Server is configured to allow remote connections. For more information see SQL Server Books Online.; 08001; Client unable to establish connection; 08001; Encryption not supported on the client.; 08001.
Errors in the high-level relational engine. A connection could not be made to the data source with the DataSourceID of 'Tfs_AnalysisDataSource', Name of 'Tfs_AnalysisDataSource'.
Errors in the OLAP storage engine: An error occurred while the dimension, with the ID of 'Today', Name of 'Today' was being processed.
Errors in the OLAP storage engine: An error occurred while the 'Year' attribute of the 'Today' dimension from the 'Tfs_Analysis' database was being processed.
OLE DB error: OLE DB or ODBC error: A network-related or instance-specific error has occurred while establishing a connection to SQL Server. Server is not found or not accessible. Check if instance name is correct and if SQL Server is configured to allow remote connections. For more information see SQL Server Books Online.; 08001; Client unable to establish connection; 08001; Encryption not supported on the client.; 08001.
Errors in the high-level relational engine. A connection could not be made to the data source with the DataSourceID of 'Tfs_AnalysisDataSource', Name of 'Tfs_AnalysisDataSource'.
Errors in the OLAP storage engine: An error occurred while the dimension, with the ID of 'Today', Name of 'Today' was being processed.
Errors in the OLAP storage engine: An error occurred while the 'Month Of Year' attribute of the 'Today' dimension from the 'Tfs_Analysis' database was being processed.
OLE DB error: OLE DB or ODBC error: A network-related or instance-specific error has occurred while establishing a connection to SQL Server. Server is not found or not accessible. Check if instance name is correct and if SQL Server is configured to allow remote connections. For more information see SQL Server Books Online.; 08001; Client unable to establish connection; 08001; Encryption not supported on the client.; 08001.
Errors in the high-level relational engine. A connection could not be made to the data source with the DataSourceID of 'Tfs_AnalysisDataSource', Name of 'Tfs_AnalysisDataSource'.
Errors in the OLAP storage engine: An error occurred while the dimension, with the ID of 'Today', Name of 'Today' was being processed.
Errors in the OLAP storage engine: An error occurred while the 'Week Of Year' attribute of the 'Today' dimension from the 'Tfs_Analysis' database was being processed.
OLE DB error: OLE DB or ODBC error: A network-related or instance-specific error has occurred while establishing a connection to SQL Server. Server is not found or not accessible. Check if instance name is correct and if SQL Server is configured to allow remote connections. For more information see SQL Server Books Online.; 08001; Client unable to establish connection; 08001; Encryption not supported on the client.; 08001.
Errors in the high-level relational engine. A connection could not be made to the data source with the DataSourceID of 'Tfs_AnalysisDataSource', Name of 'Tfs_AnalysisDataSource'.
Errors in the OLAP storage engine: An error occurred while the dimension, with the ID of 'Today', Name of 'Today' was being processed.
Errors in the OLAP storage engine: An error occurred while the 'Month' attribute of the 'Today' dimension from the 'Tfs_Analysis' database was being processed.
OLE DB error: OLE DB or ODBC error: A network-related or instance-specific error has occurred while establishing a connection to SQL Server. Server is not found or not accessible. Check if instance name is correct and if SQL Server is configured to allow remote connections. For more information see SQL Server Books Online.; 08001; Client unable to establish connection; 08001; Encryption not supported on the client.; 08001.
Errors in the high-level relational engine. A connection could not be made to the data source with the DataSourceID of 'Tfs_AnalysisDataSource', Name of 'Tfs_AnalysisDataSource'.
Errors in the OLAP storage engine: An error occurred while the dimension, with the ID of 'Today', Name of 'Today' was being processed.
Errors in the OLAP storage engine: An error occurred while the 'Week' attribute of the 'Today' dimension from the 'Tfs_Analysis' database was being processed.
OLE DB error: OLE DB or ODBC error: A network-related or instance-specific error has occurred while establishing a connection to SQL Server. Server is not found or not accessible. Check if instance name is correct and if SQL Server is configured to allow remote connections. For more information see SQL Server Books Online.; 08001; Client unable to establish connection; 08001; Encryption not supported on the client.; 08001.
Errors in the high-level relational engine. A connection could not be made to the data source with the DataSourceID of 'Tfs_AnalysisDataSource', Name of 'Tfs_AnalysisDataSource'.
Errors in the OLAP storage engine: An error occurred while the dimension, with the ID of 'Today', Name of 'Today' was being processed.
Errors in the OLAP storage engine: An error occurred while the 'Date' attribute of the 'Today' dimension from the 'Tfs_Analysis' database was being processed.
{% endhighlight %}

These two services are running on the same local VM and are each responding to other remote requests. TFS still works for WIT and source control, SSAS still responds to report requests. Weirdly, SQL Profiler was showing activity on SQL Server when attempting to process the cube. Perhaps only part of the processing could get a connection to SQL Server whereas a later part of the process could not.

Some search results were indicating that it might be a problem with a loopback address. The datasource in Tfs_Analysis was using the CNAME for the data tier to point to the local box so this might be a contributing factor. This did seem unlikely though given that this TFS instance has worked fine for months. We changed the data source to several combinations (localhost, (local) and .) but the cube processing continued to fail. To rule out a DNS problem, we also tried the data source with the local IP address of 127.0.0.1 with no joy.

The configuration of the data source in SSAS uses impersonation to connect to SQL Server rather than the service account of SSAS. Just for kicks, we added the SSAS service account as a local admin on the VM. We then got a different error. Processing the cube then failed because of a login timeout rather than a connectivity problem.

{% highlight text %}
[Full Analysis Database Sync]: 
---> AnalysisDatabaseProcessingType=Full, needCubeSchemaUpdate=False. ---> Microsoft.TeamFoundation.Server.WarehouseException: TF221122: An error occurred running job Full Analysis Database Sync for team project collection or Team Foundation server TEAM FOUNDATION. 
---> Microsoft.TeamFoundation.Server.WarehouseException: Failed to Process Analysis Database 'Tfs_Analysis'. 
---> Microsoft.TeamFoundation.Server.WarehouseException: Internal error: The operation terminated unsuccessfully. 
OLE DB error: OLE DB or ODBC error: Login timeout expired; HYT00; A network-related or instance-specific error has occurred while establishing a connection to SQL Server. Server is not found or not accessible. Check if instance name is correct and if SQL Server is configured to allow remote connections. For more information see SQL Server Books Online.; 08001; SQL Server Network Interfaces: Error getting enabled protocols list from registry [xFFFFFFFF]. ; 08001. 
Errors in the high-level relational engine. A connection could not be made to the data source with the DataSourceID of 'Tfs_AnalysisDataSource', Name of 'Tfs_AnalysisDataSource'. 
Errors in the OLAP storage engine: An error occurred while the dimension, with the ID of 'Dim Build Platform', Name of 'Build Platform' was being processed. 
Errors in the OLAP storage engine: An error occurred while the 'Build Platform' attribute of the 'Build Platform' dimension from the 'Tfs_Analysis' database was being processed. Server: The operation has been cancelled. 
OLE DB error: OLE DB or ODBC error: Login timeout expired; HYT00; A network-related or instance-specific error has occurred while establishing a connection to SQL Server. Server is not found or not accessible. Check if instance name is correct and if SQL Server is configured to allow remote connections. For more information see SQL Server Books Online.; 08001; SQL Server Network Interfaces: Error getting enabled protocols list from registry [xFFFFFFFF]. ; 08001. 
Errors in the high-level relational engine. A connection could not be made to the data source with the DataSourceID of 'Tfs_AnalysisDataSource', Name of 'Tfs_AnalysisDataSource'. 
Errors in the OLAP storage engine: An error occurred while the dimension, with the ID of 'Dim WorkItem Link Type', Name of 'Work Item Link Type' was being processed. 
Errors in the OLAP storage engine: An error occurred while the 'TeamProjectCollectionSK' attribute of the 'Work Item Link Type' dimension from the 'Tfs_Analysis' database was being processed. 
OLE DB error: OLE DB or ODBC error: Login timeout expired; HYT00; A network-related or instance-specific error has occurred while establishing a connection to SQL Server. Server is not found or not accessible. Check if instance name is correct and if SQL Server is configured to allow remote connections. For more information see SQL Server Books Online.; 08001; SQL Server Network Interfaces: Error getting enabled protocols list from registry [xFFFFFFFF]. ; 08001. 
Errors in the high-level relational engine. A connection could not be made to the data source with the DataSourceID of 'Tfs_AnalysisDataSource', Name of 'Tfs_AnalysisDataSource'. 
Errors in the OLAP storage engine: An error occurred while the dimension, with the ID of 'Dim WorkItem Link Type', Name of 'Work Item Link Type' was being processed. 
Errors in the OLAP storage engine: An error occurred while the 'Link ID' attribute of the 'Work Item Link Type' dimension from the 'Tfs_Analysis' database was being processed. 
OLE DB error: OLE DB or ODBC error: Login timeout expired; HYT00; A network-related or instance-specific error has occurred while establishing a connection to SQL Server. Server is not found or not accessible. Check if instance name is correct and if SQL Server is configured to allow remote connections. For more information see SQL Server Books Online.; 08001; SQL Server Network Interfaces: Error getting enabled protocols list from registry [xFFFFFFFF]. ; 08001. 
Errors in the high-level relational engine. A connection could not be made to the data source with the DataSourceID of 'Tfs_AnalysisDataSource', Name of 'Tfs_AnalysisDataSource'. 
Errors in the OLAP storage engine: An error occurred while the dimension, with the ID of 'Dim WorkItem Link Type', Name of 'Work Item Link Type' was being processed. 
Errors in the OLAP storage engine: An error occurred while the 'WorkItemLinkTypeBK' attribute of the 'Work Item Link Type' dimension from the 'Tfs_Analysis' database was being processed. 
OLE DB error: OLE DB or ODBC error: Login timeout expired; HYT00; A network-related or instance-specific error has occurred while establishing a connection to SQL Server. Server is not found or not accessible. Check if instance name is correct and if SQL Server is configured to allow remote connections. For more information see SQL Server Books Online.; 08001; SQL Server Network Interfaces: Error getting enabled protocols list from registry [xFFFFFFFF]. ; 08001. 
Errors in the high-level relational engine. A connection could not be made to the data source with the DataSourceID of 'Tfs_AnalysisDataSource', Name of 'Tfs_AnalysisDataSource'. 
Errors in the OLAP storage engine: An error occurred while the dimension, with the ID of 'Dim WorkItem Link Type', Name of 'Work Item Link Type' was being processed. 
Errors in the OLAP storage engine: An error occurred while the 'Reference Name' attribute of the 'Work Item Link Type' dimension from the 'Tfs_Analysis' database was being processed. 
OLE DB error: OLE DB or ODBC error: Login timeout expired; HYT00; A network-related or instance-specific error has occurred while establishing a connection to SQL Server. Server is not found or not accessible. Check if instance name is correct and if SQL Server is configured to allow remote connections. For more information see SQL Server Books Online.; 08001; SQL Server Network Interfaces: Error getting enabled protocols list from registry [xFFFFFFFF]. ; 08001. 
Errors in the high-level relational engine. A connection could not be made to the data source with the DataSourceID of 'Tfs_AnalysisDataSource', Name of 'Tfs_AnalysisDataSource'. 
Errors in the OLAP storage engine: An error occurred while the dimension, with the ID of 'Dim WorkItem Link Type', Name of 'Work Item Link Type' was being processed. 
Errors in the OLAP storage engine: An error occurred while the 'Link Name' attribute of the 'Work Item Link Type' dimension from the 'Tfs_Analysis' database was being processed. 
OLE DB error: OLE DB or ODBC error: Login timeout expired; HYT00; A network-related or instance-specific error has occurred while establishing a connection to SQL Server. Server is not found or not accessible. Check if instance name is correct and if SQL Server is configured to allow remote connections. For more information see SQL Server Books Online.; 08001; SQL Server Network Interfaces: Error getting enabled protocols list from registry [xFFFFFFFF]. ; 08001. 
Errors in the high-level relational engine. A connection could not be made to the data source with the DataSourceID of 'Tfs_AnalysisDataSource', Name of 'Tfs_AnalysisDataSource'. 
Errors in the OLAP storage engine: An error occurred while the dimension, with the ID of 'Dim WorkItem Link Type', Name of 'Work Item Link Type' was being processed. 
Errors in the OLAP storage engine: An error occurred while the 'Rules' attribute of the 'Work Item Link Type' dimension from the 'Tfs_Analysis' database was being processed. 
OLE DB error: OLE DB or ODBC error: Login timeout expired; HYT00; A network-related or instance-specific error has occurred while establishing a connection to SQL Server. Server is not found or not accessible. Check if instance name is correct and if SQL Server is configured to allow remote connections. For more information see SQL Server Books Online.; 08001; SQL Server Network Interfaces: Error getting enabled protocols list from registry [xFFFFFFFF]. ; 0800
{% endhighlight %}

We then removed impersonation and added the SSAS service account as a warehouse reader in Tfs_Warehouse. Processing the cube could now get an authenticated connection to SQL Server, but it then failed because of schema issues. The cube was trying to reference columns in a Tfs_Warehouse view that did not exist.

There were no answers after several days of trying to resolve this issue without a reboot. The problems being experienced also did not make any sense.

A reboot fixed all these problems last night. SSAS can now connect to SQL Server using its original configuration and can successfully process the cube. It’s frustrating that several days were lost and there are no answers other than a band-aid. On the positive side however, the problem has been resolved.
