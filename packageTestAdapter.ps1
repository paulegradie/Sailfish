$version="0.0.258"

dotnet publish -c Release /p:Version=$version ./source/Sailfish.TestAdapter 

Move-Item -Force ./source/Sailfish.TestAdapter/bin/Release/Sailfish.TestAdapter.$version.nupkg ../SailfishLocalPackages

