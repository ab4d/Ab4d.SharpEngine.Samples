@echo off

IF EXIST wwwroot\_framework\ (
  del wwwroot\_framework\*.* /q
  del wwwroot\_framework\supportFiles\*.* /q
) ELSE (
  md wwwroot
  md wwwroot\_framework
)

cd ..\Ab4d.SharpEngine.Samples.WebAssemblyDemo
dotnet publish -c Release
if errorlevel 1 goto build_error


xcopy bin\Release\net9.0-browser\browser-wasm\AppBundle\_framework\*.* ..\Ab4d.SharpEngine.Samples.HtmlWebPage\wwwroot\_framework\ /Y /S

cd ..\Ab4d.SharpEngine.Samples.HtmlWebPage

IF EXIST "..\ThirdParty\brotli\brotli.exe" (
  for %%f in (wwwroot\_framework\*.wasm) do (
    echo "Compressing: %%f"
    ..\ThirdParty\brotli\brotli -f %%f
  )

  for %%f in (wwwroot\_framework\*.js) do (
    echo "Compressing: %%f"
    ..\ThirdParty\brotli\brotli -f %%f
   )
)

goto end

:build_error
echo Error compiling project

:end
pause

