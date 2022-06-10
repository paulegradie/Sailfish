$version = "0.0.4";
$packagepath = "../../../LocalPackages/VeerPerforma.${version}.nupkg"

if (Test-Path -Path $packagepath -PathType Leaf)
{
    Remove-Item -Force $packagepath
}
dotnet pack -c Release -o ../../../LocalPackages

# 'C:\Users\paule\code\Veer-Performa\source\VeerPerforma\bin\Debug\VeerPerforma.version.nupkg