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

if [ ! -f "package-lock.json" ]; then
  echo "Installing Electron"
  npm i electron
fi


echo "Checking WebAssembly files in wwwroot folder"

if [ ! -f "wwwroot/index.html" ]; then

  if [ ! -d "../Ab4d.SharpEngine.Samples.BlazorWebAssembly/bin/Release/net10.0/publish/wwwroot" ]; then
    echo "Generating publish build for Ab4d.SharpEngine.Samples.BlazorWebAssembly"
	dotnet publish ../Ab4d.SharpEngine.Samples.BlazorWebAssembly/Ab4d.SharpEngine.Samples.BlazorWebAssembly.csproj -c Release
  fi
  
  if [ ! -d wwwroot ]; then
    mkdir wwwroot
  fi
    
  echo "Copying published files to local wwwroot"
  cp -r ../Ab4d.SharpEngine.Samples.BlazorWebAssembly/bin/Release/net10.0/publish/wwwroot/* wwwroot/
  
  # Delete compressed files because they are not used by Electron - serving files from the local hard disk is very fast.
  # When creating an installer for Electron app, the files will be compressed so the distribuited installer size will be small.
  find wwwroot -type f -name "*.gz" -delete
  find wwwroot -type f -name "*.br" -delete
  
  # Fix the base href path for the Electron app (replace "/" with "./")
  sed -i '' 's|<base href="/" />|<base href="./" />|g' "./wwwroot/index.html" 
  
fi

echo "Starting Electron app"
 
# The following line will enable showing console log messages from the web page to this console window (showing SharpEngine warnings and errors)
# export ELECTRON_ENABLE_LOGGING=true  
  
npm run start
read -p "Press enter to continue"
