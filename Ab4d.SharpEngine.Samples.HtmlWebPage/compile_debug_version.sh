#!/bin/bash

dotnet check_current_folder.cs
if [ $? -ne 0 ]; then
  exit 1
fi

if [ -d "wwwroot/_framework" ]; then
  rm -f wwwroot/_framework/*
  rm -f wwwroot/_framework/supportFiles/*
else
  mkdir -p wwwroot/_framework
fi

cd ../Ab4d.SharpEngine.Samples.WebAssemblyDemo

dotnet build -c Debug
if [ $? -ne 0 ]; then
  # When changing debug and release mode, it is common than build fails. Many times deleting the obj folder solves the issues.
  rm -rf obj/*
  dotnet build -c Debug
  if [ $? -ne 0 ]; then
    echo "Error compiling project"
    read -p "Press enter to continue"
    cd ../Ab4d.SharpEngine.Samples.HtmlWebPage
    exit 1
  fi
fi

cp -r ../Ab4d.SharpEngine.Samples.AspNetCoreApp/wwwroot/* ../Ab4d.SharpEngine.Samples.HtmlWebPage/wwwroot/
cp -r bin/Debug/net10.0-browser/browser-wasm/AppBundle/_framework/* ../Ab4d.SharpEngine.Samples.HtmlWebPage/wwwroot/_framework/

cd ../Ab4d.SharpEngine.Samples.HtmlWebPage

read -p "Press enter to continue"
