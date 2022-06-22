$packagepath = "../../../LocalPackages/Sailfish.nupkg"

if (Test-Path -Path $packagepath -PathType Leaf)
{
    Remove-Item -Force $packagepath
}
dotnet build
dotnet pack -c Release -o ../../../LocalPackages

# 'C:\Users\paule\code\Sailfish\source\Sailfish\bin\Debug\Sailfish.version.nupkg