@echo off

IF NOT EXIST wwwroot\_framework (
  echo wwwroot\_framework folder does not exist
  echo Call compile_debug_version.bat or compile_publish_version.bat to generate the required files.
  pause
  exit 1
)

start "" http:/localhost:8000/index.html
node server.js

if not %errorlevel%==0 (
	echo Error starting express node.js server. Make sure that node.js is installed. If not, download it from https://nodejs.org/en/download
	pause
)