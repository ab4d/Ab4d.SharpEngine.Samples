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


xcopy bin\Release\net10.0-browser\browser-wasm\AppBundle\_framework\*.* ..\Ab4d.SharpEngine.Samples.AspNetCoreApp\wwwroot\_framework\ /Y /S

cd ..\Ab4d.SharpEngine.Samples.AspNetCoreApp

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


echo Starting AspNetCore web server

dotnet build .

start "" http:/localhost:5195/index.html
dotnet run .

goto end

:build_error
echo Error compiling project

:end
pause

