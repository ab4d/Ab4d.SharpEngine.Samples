#!/bin/bash

if [ ! -d "wwwroot/_framework" ]; then
  echo "wwwroot/_framework folder does not exist"
  echo "Call compile_debug_version.sh or compile_publish_version.sh to generate the required files."
  read -p "Press enter to continue"
  exit 1
fi

xdg-open "http://localhost:8000/index.html" 2>/dev/null || open "http://localhost:8000/index.html" 2>/dev/null || echo "Please open http://localhost:8000/index.html in your browser"
node server.js

if [ $? -ne 0 ]; then
  echo "Error starting express node.js server. Make sure that node.js is installed. If not, download it from https://nodejs.org/en/download"
  read -p "Press enter to continue"
fi
