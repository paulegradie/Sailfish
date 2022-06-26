# A helper for local dev -- you can run this to build and package 
# Sailfish for local reference in your project, or in the demo project

$packageDir = "../../../LocalPackages/"

if (Test-Path $packageDir) {   
    Write-Host "Output dir exists"
}
else
{
    New-Item $packageDir -ItemType Directory
    Write-Host "Output dir created"
}

dotnet build
dotnet pack -c Release -o ../../../LocalPackages

