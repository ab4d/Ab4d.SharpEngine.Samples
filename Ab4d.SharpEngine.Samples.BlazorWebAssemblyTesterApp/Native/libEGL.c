// This native reference file is required to create the EGL context in the WebGLDevice object.
// This file needs to be referenced by using NativeFileReference and with ScanForPInvokes attribute set to true in the csproj file, for example:
// <NativeFileReference Include="Native/libEGL.c" ScanForPInvokes="true" />
//
// If this file is not included, then DllNotFoundException with libEGL as parameter is throw.
//
// The usual source for EGL files is (update the version number):
// C:\Program Files\dotnet\packs\Microsoft.NET.Runtime.Emscripten.3.1.56.Sdk.win-x64\9.0.8\tools\emscripten\system\include\EGL
#include <EGL/egl.h>