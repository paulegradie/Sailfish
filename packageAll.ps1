$HomePath = Get-Location;

function IncrementVersion ($csProjXMLPath)
{
    $xml=New-Object XML
	$xml.Load($csProjXMLPath)
	$versionNode = $xml.Project.PropertyGroup.Version
    $version = $versionNode.InnerText;
    $version = $version + "1"
    $versionNode.InnerText = $version
    $xml.Save($csProjXMLPath)
}

# GO TO ADAPTER
$adapterPath = "C:\Users\paule\code\VeerPerformaRelated\Veer-Performa\source\Tests.VeerPerforma.TestAdapter"
Set-Location $adapterPath
$csProjAdapter = $adapterPath + "VeerPerforma.TestAdapter.csproj";
IncrementVersion $csProjAdapter

dotnet build
dotnet pack -c Release -o ../../../LocalPackages
# 'C:\Users\paule\code\Veer-Performa\source\VeerPerforma.TestAdapter\bin\Debug\VeerPerforma.TestAdapter.nupkg


# Go To VeerPerforma
$corePath = "C:\Users\paule\code\VeerPerformaRelated\Veer-Performa\source\VeerPerforma"
Set-Location $corePath
$csProjCore = $corePath + "VeerPerforma.csproj";
IncrementVersion $csProjCore
dotnet build
dotnet pack -c Release -o ../../../LocalPackages
# 'C:\Users\paule\code\Veer-Performa\source\VeerPerforma.TestAdapter\bin\Debug\VeerPerforma.TestAdapter.nupkg

Set-Location $HomePath
Write-Host "All set"
exit 0;

