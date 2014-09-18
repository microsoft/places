param($installPath, $toolsPath, $package, $project) 

Write-Host "Saving all files before making changes to the project"
$dte.ExecuteCommand("File.SaveAll", "");

#
# Load and parse appxmanifest.xml
#
$projectPath = Split-Path -Path $project.FullName -Parent 
$manifestPath = $projectPath + "\Package.appxmanifest"
$doc = [xml](Get-Content -Path $manifestPath) 
if( $doc )
{
	#
	# Add required namespace
	#
	$ns = New-Object Xml.XmlNamespaceManager $doc.NameTable
	$ns.AddNamespace( "ns", "http://schemas.microsoft.com/appx/2010/manifest" )
	$ns.AddNamespace( "m2", "http://schemas.microsoft.com/appx/2013/manifest" )

	#
	# Make sure Capabilities node exists
	#	
	$capsnode = $doc.SelectSingleNode("/ns:Package/ns:Capabilities", $ns)
	if( $capsnode -eq $null )
	{
		Write-Host "Adding Capabilities node"
		$capsnode = $doc.CreateElement("Capabilities", $doc.DocumentElement.NamespaceURI)
		$doc.Package.AppendChild($capsnode)
	}

	#
	# Make sure location capability exists
	#	
	$locelement = $capsnode.SelectSingleNode("//ns:DeviceCapability[@Name = 'location']", $ns)
	if( $locelement -eq $null )
	{
		Write-Host "Adding location capability"
		$locelement = $doc.CreateElement("DeviceCapability", $doc.DocumentElement.NamespaceURI)
		$nameattribute = $doc.CreateAttribute("Name")
		$nameattribute.Value = "location"
		$locelement.Attributes.Append($nameattribute)
		$capsnode.AppendChild($locelement)
	}
	
	#
	# Make sure humaninterfacedevice capability exists
	#	
	$hidelement = $capsnode.SelectSingleNode("//m2:DeviceCapability[@Name = 'humaninterfacedevice']", $ns)
	if( $hidelement -eq $null )
	{
		Write-Host "Adding HID node"
		$hidelement = $doc.CreateElement("m2:DeviceCapability", $ns.LookupNamespace("m2"))
		$hidelement.Attributes.Append($doc.CreateAttribute("Name"))
		$hidelement.Name = "humaninterfacedevice"
		$capsnode.AppendChild($hidelement)
	}
	
	#
	# Make sure Sense HID device node exists
	#	
	$hiddeviceelement = $hidelement.SelectSingleNode("//m2:Device[@Id = 'vidpid:0421 0716']", $ns)
	if( $hiddeviceelement -eq $null )
	{
		Write-Host "Adding HID device node"
		$hiddeviceelement = $doc.CreateElement("m2:Device", $ns.LookupNamespace("m2"))
		$hiddeviceelement.Attributes.Append($doc.CreateAttribute("Id"))
		$hiddeviceelement.Id = "vidpid:0421 0716"
		$hidelement.AppendChild($hiddeviceelement)
	}
	
	#
	# Make sure required usage pages exist
	#	
	$page1element =  $hiddeviceelement.SelectSingleNode("//m2:Function[@Type = 'usage:ffaa 0001']", $ns)
	if( $page1element -eq $null )
	{
		Write-Host "Adding usage page 1 element"
		$page1element = $doc.CreateElement("m2:Function", $ns.LookupNamespace("m2"))
		$page1element.Attributes.Append($doc.CreateAttribute("Type"))
		$page1element.Type = "usage:ffaa 0001"
		$hiddeviceelement.AppendChild($page1element)
	}
	
	$page2element =  $hiddeviceelement.SelectSingleNode("//m2:Function[@Type = 'usage:ffee 0001']", $ns)
	if( $page2element -eq $null )
	{
		Write-Host "Adding usage page 2 element"
		$page2element = $doc.CreateElement("m2:Function", $ns.LookupNamespace("m2"))
		$page2element.Attributes.Append($doc.CreateAttribute("Type"))
		$page2element.Type = "usage:ffee 0001"
		$hiddeviceelement.AppendChild($page2element)
	}
	
	$page3element =  $hiddeviceelement.SelectSingleNode("//m2:Function[@Type = 'usage:ffee 0002']", $ns)
	if( $page3element -eq $null )
	{
		Write-Host "Adding usage page 3 element"
		$page3element = $doc.CreateElement("m2:Function", $ns.LookupNamespace("m2"))
		$page3element.Attributes.Append($doc.CreateAttribute("Type"))
		$page3element.Type = "usage:ffee 0002"
		$hiddeviceelement.AppendChild($page3element)
	}
	
	$page4element =  $hiddeviceelement.SelectSingleNode("//m2:Function[@Type = 'usage:ffee 0003']", $ns)
	if( $page4element -eq $null )
	{
		Write-Host "Adding usage page 4 element"
		$page4element = $doc.CreateElement("m2:Function", $ns.LookupNamespace("m2"))
		$page4element.Attributes.Append($doc.CreateAttribute("Type"))
		$page4element.Type = "usage:ffee 0003"
		$hiddeviceelement.AppendChild($page4element)
	}
	
	$page5element =  $hiddeviceelement.SelectSingleNode("//m2:Function[@Type = 'usage:ffee 0004']", $ns)
	if( $page5element -eq $null )
	{
		Write-Host "Adding usage page 5 element"
		$page5element = $doc.CreateElement("m2:Function", $ns.LookupNamespace("m2"))
		$page5element.Attributes.Append($doc.CreateAttribute("Type"))
		$page5element.Type = "usage:ffee 0004"
		$hiddeviceelement.AppendChild($page5element)
	}
	
	#
	# Save modified manifest
	#	
	$doc.Save($manifestPath)
}
else
{
	Write-Host "Could not find appxmanifest.xml"
}

#
# Load and parse wmappmanifest.xml
#
$manifest2Path = $projectPath + "\Properties\WMAppManifest.xml"
$doc2 = [xml](Get-Content -Path $manifest2Path) 
if( $doc2 )
{
	#
	# Add required namespace
	#
	$ns2 = New-Object Xml.XmlNamespaceManager $doc2.NameTable
	$ns2.AddNamespace( "ns", "http://schemas.microsoft.com/windowsphone/2014/deployment" )
	$ns2.AddNamespace( "ns2", "" )

	#
	# Make sure Capabilities node exists
	#	
	$capsnode2 = $doc2.SelectSingleNode("/ns:Deployment/App/Capabilities", $ns2)
	if( $capsnode2 -eq $null )
	{
		Write-Host "Adding Capabilities node"
		$capsnode2 = $doc2.CreateElement("ns2:Capabilities", $ns2.LookupNamespace("ns2"))
		$doc2.Deployment.App.AppendChild($capsnode2)
	}

	#
	# Make sure ID_CAP_LOCATION exists
	#	
	$locelement2 = $capsnode2.SelectSingleNode("//Capability[@Name = 'ID_CAP_LOCATION']")
	if( $locelement2 -eq $null )
	{
		Write-Host "Adding ID_CAP_LOCATION capability"
		$locelement2 = $doc2.CreateElement("ns2:Capability", $ns2.LookupNamespace("ns2"))
		$nameattribute3 = $doc2.CreateAttribute("Name")
		$nameattribute3.Value = "ID_CAP_LOCATION"
		$locelement2.Attributes.Append($nameattribute3)
		$capsnode2.AppendChild($locelement2)
	}
		
	#
	# Save modified manifest
	#	
	$doc2.Save($manifest2Path)
}
else
{
	Write-Host "Could not find wmappmanifest.xml"
}

# Need to load MSBuild assembly if it's not loaded yet.
Add-Type -AssemblyName 'Microsoft.Build, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'

#save the project file
$project.Save();

# Update file date in order to trigger Visual Studio to reload project.
Set-ItemProperty -Path $project.FullName -Name LastWriteTime -Value (get-date)

if ($dte -eq $null -or $dte.Solution -eq $null)
{
	return
}

$defaultPlatform = "ARM"

$activeContext = $dte.Solution.SolutionBuild.ActiveConfiguration.SolutionContexts | Select -First 1 
if ($activeContext -eq $null -or $activeContext.PlatformName -eq $defaultPlatform)
{
	return
}

# Try find the configuration matching the default platform and the currently selected configuration, e.g. Debug or Release.
$defaultConfiguration = $dte.Solution.SolutionBuild.SolutionConfigurations | 
						%{ $_.SolutionContexts } | 
						? { $_.PlatformName -eq $defaultPlatform -and $_.ConfigurationName -eq $activeContext.ConfigurationName } | 
						Select -First 1
if ($defaultConfiguration -ne $null)
{
	$defaultConfiguration.Collection.Parent.Activate();
}
