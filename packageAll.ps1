$HomePath = Get-Location;

function IncrementVersion ($csProjXMLPath)
{
    $xml = New-Object XML
	$xml.Load($csProjXMLPath)

	$versionNode = $xml.Project.PropertyGroup.Version

    $parts = $versionNode.Split(".");
    $build = [int]$parts[2];

    $newVersion = "0.0." + ($build + 1);
    Write-Host  "Old: $versionNode => New: $newVersion"
    $xml.Project.PropertyGroup.Version = $newVersion;
    $xml.Save($csProjXMLPath)
}

# GO TO ADAPTER
$adapterPath = "C:\Users\paule\code\VeerPerformaRelated\Veer-Performa\source\VeerPerforma.TestAdapter"
Set-Location $adapterPath
$csProjAdapter = $adapterPath + "\VeerPerforma.TestAdapter.csproj";
IncrementVersion $csProjAdapter

dotnet build
dotnet pack -c Release -o ../../../LocalPackages


# Go To VeerPerforma
$corePath = "C:\Users\paule\code\VeerPerformaRelated\Veer-Performa\source\VeerPerforma"
Set-Location $corePath
$csProjCore = $corePath + "\VeerPerforma.csproj";
IncrementVersion $csProjCore
dotnet build
dotnet pack -c Release -o ../../../LocalPackages


Set-Location $HomePath
Write-Host "All set"
exit 0;

