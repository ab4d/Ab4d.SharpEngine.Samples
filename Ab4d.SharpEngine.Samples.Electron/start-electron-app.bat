@echo off

echo Checking prerequisites

node -v
if errorlevel 1 goto node_error

npm -v
if errorlevel 1 goto node_error


echo Checking WebAssembly files in wwwroot folder

if exist "wwwroot\index.html" goto start_electron

if exist "..\Ab4d.SharpEngine.Samples.BlazorWebAssembly\bin\Release\net10.0\browser-wasm\publish\wwwroot" (
    xcopy /E /Y "..\Ab4d.SharpEngine.Samples.BlazorWebAssembly\bin\Release\net10.0\browser-wasm\publish\wwwroot\*" "wwwroot\"
    goto start_electron
)

if exist "..\Ab4d.SharpEngine.Samples.HtmlWebPage\wwwroot" (
    xcopy /E /Y "..\Ab4d.SharpEngine.Samples.HtmlWebPage\wwwroot\*" "wwwroot\"
    goto start_electron
)

echo Cannot get the published WebAssembly wwwroot folder. To do that publish the Ab4d.SharpEngine.Samples.BlazorWebAssembly project or run "compile_publish_version.bat" in the Ab4d.SharpEngine.Samples.HtmlWebPage folder.
pause
goto end


:start_electron
rem The following line will enable showing console log messages from the web page to this console window
set ELECTRON_ENABLE_LOGGING=true
npm run start

goto end


:node_error
echo Node.js is not installed. Please check the https://nodejs.org/en/download/ web page on how to install it.
pause

:end
pause