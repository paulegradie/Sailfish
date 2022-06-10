$version = "0.0.5";
$packagepath = "../../../LocalPackages/VeerPerforma.TestAdapter.${version}.nupkg"

if (Test-Path -Path $packagepath -PathType Leaf)
{
    Remove-Item -Force $packagepath
}
dotnet pack -c Release -o ../../../LocalPackages --version-suffix $version

# 'C:\Users\paule\code\Veer-Performa\source\VeerPerforma.TestAdapter\bin\Debug\VeerPerforma.TestAdapter.0.0.1.nupkg