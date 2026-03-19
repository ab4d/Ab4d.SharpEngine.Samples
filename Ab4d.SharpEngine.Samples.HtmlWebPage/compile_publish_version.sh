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
rm -rf obj/*

dotnet publish -c Release
if [ $? -ne 0 ]; then
  # When changing debug and release mode, it is common than build fails. Many times deleting the obj folder solves the issues.
  rm -rf obj/*
  dotnet publish -c Release
  if [ $? -ne 0 ]; then
    echo "Error compiling project"
    read -p "Press enter to continue"
    cd ../Ab4d.SharpEngine.Samples.HtmlWebPage
    exit 1
  fi
fi

cp -r ../Ab4d.SharpEngine.Samples.AspNetCoreApp/wwwroot/* ../Ab4d.SharpEngine.Samples.HtmlWebPage/wwwroot/
cp -r bin/Release/net10.0-browser/browser-wasm/AppBundle/_framework/* ../Ab4d.SharpEngine.Samples.HtmlWebPage/wwwroot/_framework/

cd ../Ab4d.SharpEngine.Samples.HtmlWebPage

# NOTE: The brotli compression tool in ThirdParty is available only for Windows.
# To add brotli compression, please compile the source from https://github.com/google/brotli
# and uncomment the code below:
#
#if [ -f "../ThirdParty/brotli/brotli" ]; then
#  for file in wwwroot/_framework/*.wasm; do
#    echo "Compressing: $file"
#    ../ThirdParty/brotli/brotli -f "$file"
#  done
#
#  for file in wwwroot/_framework/*.js; do
#    echo "Compressing: $file"
#    ../ThirdParty/brotli/brotli -f "$file"
#  done
#fi

read -p "Press enter to continue"
