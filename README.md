# dynamics crm ssis components

- Microsoft Dynamics CRM 2011, 2016, D365, Dataverse
- SOAP 2011 endpoint, REST Web Api endpoint
- SQL Server Integration Services

![Image](/Images/crm_ssis_destination02.png)

Included Projects in solution:
- ConsoleTestApp - project for testing CrmComponents
- CrmComponents - main project
- SetupProject2 - Wix/Windows Installer Package (.msi) - not working

## Open issues (TODO list)
- OAuth2 login with certificate isn't implemented
- Discovery Service SOAP endpoints for CRM online (https://disco.crmX.dynamics.com) are depricated by Microsoft, so SOAP connection in VS SSDT GUI will not work for SOAP online connection type
- Automatic buils for more than one SQL Server versions are not implemented: one should manually set dll-s, AssemblyInfo and class attributes for every version of SQL Server
- Windows Installer project is not implemented: one can only manually install CrmComponents.dll to GAC and then copy it to Program Files folders of SQL server

## SQL server versions
|SQL Server version|Version number|
|------------------|--------------|
|SQL Server 2019   | 15           |
|SQL Server 2017   | 14           |
|SQL Server 2016   | 13           |
|SQL Server 2014   | 12           |
|SQL Server 2012   | 11           |
|SQL Server 2008   | 10           |

DLLs needed for SSIS project are in folder DLLs in the project https://github.com/tgrbus/dynamics_crm_ssis_components/tree/main/dlls

## Setting up and building project:
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

4) Build the project

5) Install CrmComponents.dll in GAC

6) Copy CrmComponents.dll in x86 folders for Visual Studio/SQL Server Data Tools:  
    %programfiles(x86)%\Microsoft SQL Server\130\DTS\PipelineComponents  
    and  
    %programfiles(x86)%\Microsoft SQL Server\130\DTS\Connections, where 130 is folder for SQL Server 2016, 140 for SQL Server 2017, etc..

7) Copy CrmComponents.dll in x64 folders for running SSIS jobs in SQL Server Agent:  
    %programfiles%\Microsoft SQL Server\130\DTS\PipelineComponents  
    and  
    %programfiles%\Microsoft SQL Server\130\DTS\Connections, where 130 is folder for SQL Server 2016, 140 for SQL Server 2017, etc..

8) Open SQL Server Data Tools and create new or open existing SSIS project. Set SQL Server version -> right click on project in Solution Explorer -> Properties -> Configuration Properties -> General -> TargetServerVersion

## For development computer - setting up Post-Build Event 
CrmComponents -> Properties -> Build Events.  
Have to run Visual Studio as Administrator.

cd $(ProjectDir)  
@SET COMPONENTSDIR="C:\Program Files (x86)\Microsoft SQL Server\120\DTS\PipelineComponents\"  
@SET COMPONENTSDIR_64 = "C:\Program Files\Microsoft SQL Server\120\DTS\PipelineComponents\"  
@SET CONNECTIONSDIR= "C:\Program Files (x86)\Microsoft SQL Server\120\DTS\Connections\"  
@SET CONNECTIONSDIR_64= "C:\Program Files\Microsoft SQL Server\120\DTS\Connections\"  
@SET GACUTIL="C:\Program Files (x86)\Microsoft SDKs\Windows\v8.0A\bin\NETFX 4.0 Tools\gacutil.exe"  
Echo Instaling dll in GAC  
Echo $(OutDir)  
Echo $(TargetFileName)  
%GACUTIL% -if "$(OutDir)$(TargetFileName)"  
Echo Copying files to Components 32bit  
copy "$(OutDir)$(TargetFileName)" %COMPONENTSDIR%  
Echo Copying files to Components 64bit  
copy "$(OutDir)$(TargetFileName)" %COMPONENTSDIR64_01%  
Echo Copying files to Connections 32bit  
copy "$(OutDir)$(TargetFileName)" %CONNECTIONSDIR%  
Echo Copying files to Connections 64bit  
copy "$(OutDir)$(TargetFileName)" %CONNECTIONSDIR_64%  

## Structure
1) Crm Connection:
    - CrmConnection.cs
    - CrmConnectionEditor.cs
    - CrmConnectionInterface.cs
2) Destination Component:
    - DestinationComponent.cs
    - DestinationComponentEditor.cs
    - DestinationComponentInterface.cs
3) Source Component:
    - SourceComponent.cs
    - SourceComponentEditor.cs
    - SourceComponentInterface.cs

## Screenshots
### Connection Component
![Image](/Images/crm_ssis_connection01.png)

### Destination Component
![Image](/Images/crm_ssis_destination01.png)
![Image](/Images/crm_ssis_destination03.png)

### Source Component
![Image](/Images/crm_ssis_source01.png)
![Image](/Images/crm_ssis_source02.png)  
![Image](/Images/crm_ssis_source03.png)