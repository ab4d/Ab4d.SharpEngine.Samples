@echo off

dotnet check_current_folder.cs
if errorlevel 1 goto end

IF EXIST wwwroot\_framework\ (
  del wwwroot\_framework\*.* /q
) ELSE (
  md wwwroot
  md wwwroot\_framework
)

cd ..\Ab4d.SharpEngine.Samples.WebAssemblyDemo
del obj\*.* /Q

dotnet publish -c Release
if errorlevel 1 (
	rem When changing debug and release mode, it is common than build failes. Many times deleting the obj filder solves the issues.
	del obj\*.* /Q
	dotnet publish -c Release
    if errorlevel 1 goto build_error
)

echo .
echo Ab4d.SharpEngine.Samples.WebAssemblyDemo compiled. Start web server by starting start-node-express-web-server.bat

goto end

:build_error
echo Error compiling Ab4d.SharpEngine.Samples.WebAssemblyDemo project

:end
pause
