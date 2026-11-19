@echo off

IF EXIST wwwroot\_framework\ (
  del wwwroot\_framework\*.* /q
  del wwwroot\_framework\supportFiles\*.* /q
) ELSE (
  md wwwroot
  md wwwroot\_framework
)

echo Start compiling and publishing Ab4d.SharpEngine.Samples.WebAssemblyDemo project

cd ..\Ab4d.SharpEngine.Samples.WebAssemblyDemo
dotnet publish -c Release
if errorlevel 1 goto build_error


xcopy bin\Release\net11.0-browser\publish\wwwroot\_framework\*.* ..\Ab4d.SharpEngine.Samples.AspNetCoreApp\wwwroot\_framework\ /Y /S

cd ..\Ab4d.SharpEngine.Samples.AspNetCoreApp

IF NOT EXIST "wwwroot\_framework\dotnet.js.br" (
  IF EXIST "..\ThirdParty\brotli\brotli.exe" (
    echo Start compressing files with brotli
  
    for %%f in (wwwroot\_framework\*.wasm) do (
      echo "Compressing: %%f"
      ..\ThirdParty\brotli\brotli -f %%f
    )

    for %%f in (wwwroot\_framework\*.js) do (
      echo "Compressing: %%f"
      ..\ThirdParty\brotli\brotli -f %%f
     )
  )
)


echo.
echo Everything compiled and copied to local wwwroot folder. Start web server by starting start_asp_net_web_server.bat or start the Ab4d.SharpEngine.Samples.AspNetCoreApp project from Visual Studio or other IDE.

goto end

:build_error
echo Error compiling project

:end
pause

