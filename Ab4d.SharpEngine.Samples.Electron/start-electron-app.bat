@echo off

echo Checking prerequisites

node -v
if errorlevel 1 goto node_error

call npm -v
if errorlevel 1 goto node_error

if not exist package-lock.json (
   echo Installing Electron
   call npm i electron
   if errorlevel 1 goto node_error
)


echo Checking WebAssembly files in wwwroot folder

if exist "wwwroot\index.html" goto start_electron

if not exist "..\Ab4d.SharpEngine.Samples.BlazorWebAssembly\bin\Release\net10.0\publish\wwwroot" (
    echo Generating publish build for Ab4d.SharpEngine.Samples.BlazorWebAssembly 
	dotnet publish ..\Ab4d.SharpEngine.Samples.BlazorWebAssembly\Ab4d.SharpEngine.Samples.BlazorWebAssembly.csproj -c Release /p:PublishProfile=..\Ab4d.SharpEngine.Samples.BlazorWebAssembly\Properties\PublishProfiles\FolderProfile.pubxml
)


echo Copying published files to local wwwroot

xcopy /E /Y /C "..\Ab4d.SharpEngine.Samples.BlazorWebAssembly\bin\Release\net10.0\publish\wwwroot\*" "wwwroot\"

rem Delete compressed files because they are not used by Electron - serving files from the local hard disk is very fast.
rem When creating an installer for Electron app, the files will be compressed so the distribuited installer size will be small.
del wwwroot\*.gz /q /s
del wwwroot\*.br /q /s

rem Fix the base href path for the Electron app (replace "/" with "./")
powershell -Command "(Get-Content wwwroot\index.html) -replace '<base href=\"/\" />', '<base href=\"./\" />' | Set-Content wwwroot\index.html"


rem To start a simple demo app, you can also copy the files from the Ab4d.SharpEngine.Samples.HtmlWebPage\wwwroot
rem xcopy /E /Y "..\Ab4d.SharpEngine.Samples.HtmlWebPage\wwwroot\*" "wwwroot\"


:start_electron
echo Starting Electron app

rem The following line will enable showing console log messages from the web page to this console window (showing SharpEngine warnings and errors)
rem set ELECTRON_ENABLE_LOGGING=true

npm run start

goto end


:node_error
echo Node.js is not installed. Please check the https://nodejs.org/en/download/ web page on how to install it.
pause

:end
pause