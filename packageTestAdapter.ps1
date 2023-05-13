$path = "../SailfishLocalPackages"
If(!(test-path -PathType container $path))
{
    New-Item -ItemType Directory -Path $path
}

$fileList = Get-ChildItem $path
$nextNum = 0;
if ($fileList.Count -eq 0){
    Write-Host "No prior versions found in $path"
} else {
    [int[]]$allVersions = @();
    foreach ($path in $fileList) {
        $fileName = Split-Path -Path $path -Leaf
        $num = [int]($fileName.Split(".")[4])
        $allVersions += $num;
    }
    $sortedVersions = $allVersions | Sort-Object -descending;
    Write-Host "Latest version found:" $sortedVersions[0]
    $nextNum = $sortedVersions[0] + 1;
}

$newVersion = "0.0.$nextNum";
$newFileName = "Sailfish.TestAdapter.$newVersion.nupkg"
Write-Host "New file name: $newFileName"

dotnet build -c Release /p:Version="$newVersion" ./source/Sailfish.TestAdapter

$outputPath = "./source/Sailfish.TestAdapter/bin/Release/$newFileName"
if (Test-Path $outputPath) {
    Move-Item -Force $outputPath "../SailfishLocalPackages/$newFileName"
    Write-Host
    Write-Host "Successfully created $newFileName"
    Write-Host
} else {
    Write-Error "Failed to create the updated package"
}


