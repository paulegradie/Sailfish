# manually increment this until you figure out how to autoincrement :D
$version="0.0.333"
$path = "../SailfishLocalPackages"
If(!(test-path -PathType container $path))
{
    New-Item -ItemType Directory -Path $path
}


dotnet build -c Release /p:Version=$version ./source/Sailfish.TestAdapter


Move-Item -Force ./source/Sailfish.TestAdapter/bin/Release/Sailfish.TestAdapter.$version.nupkg ../SailfishLocalPackages/Sailfish.TestAdapter.$version.nupkg

