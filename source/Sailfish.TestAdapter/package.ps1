$packagepath = "../../../LocalPackages/Sailfish.TestAdapter.nupkg"

if (Test-Path -Path $packagepath -PathType Leaf)
{
    Remove-Item -Force $packagepath
}
dotnet build
dotnet pack -c Release -o ../../../LocalPackages

# 'C:\Users\paule\code\Sailfish\source\Sailfish.TestAdapter\bin\Debug\Sailfish.TestAdapter.nupkg