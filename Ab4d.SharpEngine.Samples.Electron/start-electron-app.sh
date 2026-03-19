#!/bin/bash

echo "Checking prerequisites"

node -v
if [ $? -ne 0 ]; then
  echo "Node.js is not installed. Please check the https://nodejs.org/en/download/ web page on how to install it."
  read -p "Press enter to continue"
  exit 1
fi

npm -v
if [ $? -ne 0 ]; then
  echo "Node.js is not installed. Please check the https://nodejs.org/en/download/ web page on how to install it."
  read -p "Press enter to continue"
  exit 1
fi


echo "Checking WebAssembly files in wwwroot folder"

if [ -f "wwwroot/index.html" ]; then
  # The following line will enable showing console log messages from the web page to this console window
  export ELECTRON_ENABLE_LOGGING=true
  npm run start
  read -p "Press enter to continue"
  exit 0
fi

if [ -d "../Ab4d.SharpEngine.Samples.BlazorWebAssembly/bin/Release/net10.0/browser-wasm/publish/wwwroot" ]; then
  cp -r ../Ab4d.SharpEngine.Samples.BlazorWebAssembly/bin/Release/net10.0/browser-wasm/publish/wwwroot/* wwwroot/
  # The following line will enable showing console log messages from the web page to this console window
  export ELECTRON_ENABLE_LOGGING=true
  npm run start
  read -p "Press enter to continue"
  exit 0
fi

if [ -d "../Ab4d.SharpEngine.Samples.HtmlWebPage/wwwroot" ]; then
  cp -r ../Ab4d.SharpEngine.Samples.HtmlWebPage/wwwroot/* wwwroot/
  # The following line will enable showing console log messages from the web page to this console window
  export ELECTRON_ENABLE_LOGGING=true
  npm run start
  read -p "Press enter to continue"
  exit 0
fi

echo "Cannot get the published WebAssembly wwwroot folder. To do that publish the Ab4d.SharpEngine.Samples.BlazorWebAssembly project or run \"compile_publish_version.sh\" in the Ab4d.SharpEngine.Samples.HtmlWebPage folder."
read -p "Press enter to continue"
