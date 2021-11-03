dotnet publish src/Platformer -c Release -p:PublishSingleFile=true -p:PublishTrimmed=true -r win-x64 -o publish/win-x64
dotnet garnet pack -input publish/win-x64 -output publish/platformer-{version}-win-x64.zip -compression 1 -recurse -ignore *.pdb
