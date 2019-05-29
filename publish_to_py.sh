dotnet publish --configuration Release 
scp ./bin/Release/netcoreapp2.2/publish/* pi@199.27.179.226:~/CSLiveStreamServer
scp ./HTML/* pi@199.27.179.226:~/CSLiveStreamServer/HTML