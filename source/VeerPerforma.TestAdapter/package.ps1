$packagepath = "../../../LocalPackages/VeerPerforma.TestAdapter.nupkg"

if (Test-Path -Path $packagepath -PathType Leaf)
{
    Remove-Item -Force $packagepath
}
dotnet build
dotnet pack -c Release -o ../../../LocalPackages

# 'C:\Users\paule\code\Veer-Performa\source\VeerPerforma.TestAdapter\bin\Debug\VeerPerforma.TestAdapter.nupkg