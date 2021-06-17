# dynamics crm ssis components

Included Projects in solution:
- ConsoleTestApp - project for testing CrmComponents
- CrmComponents - main project
- SetupProject2 - Wix/Windows Installer Package (.msi) - not working

Setting up and building dll files:
1) Add following dlls as references:
	- Microsoft.SqlServer.Dts.Design
	- Microsoft.SqlServer.DTSPipelineWrap - set "Embed Interop Type" to False
	- Microsoft.SqlServer.DTSRuntimeWrap - set "Embed Interop Type" to False
	- Microsoft.SqlServer.ManagedDTS
	- Microsoft.SqlServer.PipelineHost

	Versions of these DLLs should match SQL Server versions:
   - version 14: SQL Server 2017
   - version 13: SQL Server 2016
   - version 12: SQL Server 2014			
   - version 11: SQL Server 2012
   - version 10: SQL Server 2008

2) Set version in AssemblyInfo.cs
	- [assembly: AssemblyVersion("1.2017.14.0")] for SQL Server 2017
	- [assembly: AssemblyVersion("1.2016.13.0")] for SQL Server 2016
	- [assembly: AssemblyVersion("1.2014.12.0")] for SQL Server 2014
	...

3) Set Attributes on following classes, where "Version=1.xxxx.yy.0" is the same as in AssemblyInfo
	- CrmConnection.cs :     
		    [DtsConnection(ConnectionType = "CrmConnection", DisplayName = "Crm Connection", ConnectionContact = "TestComponent", Description = "Svasta",
         UITypeName = "CrmComponents.CrmConnectionInterface, CrmComponents, Version=1.2014.12.0, Culture=neutral, PublicKeyToken=81423fe9ba0539ea",
        LocalizationType = typeof(CrmConnection)), EditorBrowsable(EditorBrowsableState.Never)]
		
	- DestinationComponent.cs:
	      [DtsPipelineComponent(DisplayName = "CRM Destination", ComponentType = ComponentType.DestinationAdapter, 
        IconResource = "CrmComponents.Resources.Icon1.ico", CurrentVersion = 4,
        UITypeName = "CrmComponents.DestinationComponentInterface, CrmComponents, Version=1.2014.12.0, Culture=neutral, PublicKeyToken=81423fe9ba0539ea")]
	
	- SourceComponent.cs:
		    [DtsPipelineComponent(DisplayName = "CRM Source", ComponentType = ComponentType.SourceAdapter, IconResource = "CrmComponents.Resources.Icon1.ico",
        UITypeName = "CrmComponents.SourceComponentInterface, CrmComponents, Version=1.2014.12.0, Culture=neutral, PublicKeyToken=81423fe9ba0539ea")]
