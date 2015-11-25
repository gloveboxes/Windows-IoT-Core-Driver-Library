rem automate nuget process

mkdir lib\uap10.0
copy ..\bin\ARM\release\Glovebox.IoT.Devices.dll lib\uap10.0 /y
del *.nupkg
\data\software\nuget\nuget pack Glovebox.IoT.Devices.dll.nuspec
del lib\uap10.0\*.* /q
rmdir lib /s /q

rem \data\software\nuget\nuget setApiKey API Key
rem \data\software\nuget\nuget push YourPackage.nupkg