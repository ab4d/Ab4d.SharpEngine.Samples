@echo off

IF NOT EXIST wwwroot\_framework (
  echo wwwroot\_framework folder does not exist
  echo Call compile_debug_version.bat or compile_publish_version.bat to generate the required files.
  pause
  exit 1
)

start "" http:/localhost:8000/index.html
python server.py

