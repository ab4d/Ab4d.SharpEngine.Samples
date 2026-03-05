@echo off

dotnet check_current_folder.cs
if errorlevel 1 goto end

IF EXIST wwwroot\_framework\ (
  del wwwroot\_framework\*.* /q
  del wwwroot\_framework\supportFiles\*.* /q
) ELSE (
  md wwwroot
  md wwwroot\_framework
)

cd ..\Ab4d.SharpEngine.Samples.WebAssemblyDemo

dotnet build -c Debug
if errorlevel 1 (
	rem When changing debug and release mode, it is common than build failes. Many times deleting the obj filder solves the issues.
	del obj\*.* /Q
	dotnet build -c Debug
    if errorlevel 1 goto build_error
)

xcopy ..\Ab4d.SharpEngine.Samples.AspNetCoreApp\wwwroot\*.* ..\Ab4d.SharpEngine.Samples.HtmlWebPage\wwwroot\ /Y
xcopy bin\Debug\net10.0-browser\browser-wasm\AppBundle\_framework\*.* ..\Ab4d.SharpEngine.Samples.HtmlWebPage\wwwroot\_framework\ /Y /S

cd ..\Ab4d.SharpEngine.Samples.HtmlWebPage

goto end

:build_error
echo Error compiling project

:end
pause

