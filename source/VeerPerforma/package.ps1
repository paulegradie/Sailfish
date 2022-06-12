$packagepath = "../../../LocalPackages/VeerPerforma.nupkg"

if (Test-Path -Path $packagepath -PathType Leaf)
{
    Remove-Item -Force $packagepath
}
dotnet build
dotnet pack -c Release -o ../../../LocalPackages

# 'C:\Users\paule\code\Veer-Performa\source\VeerPerforma\bin\Debug\VeerPerforma.version.nupkg