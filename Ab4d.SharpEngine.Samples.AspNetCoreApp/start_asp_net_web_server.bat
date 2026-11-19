@echo off

echo Starting AspNetCore web server

dotnet build .

start "" http:/localhost:5195/index.html
dotnet run .

goto end

:build_error
echo Error compiling project

:end
pause

