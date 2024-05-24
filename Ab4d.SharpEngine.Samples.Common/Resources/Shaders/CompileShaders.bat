@echo off

rem VULKAN SDK needs to be installed to be able to run this batch file


rem Debug parametrs for glslc:
rem -g          generate debug information
rem -O0         disables optimization


rem Debug parametrs for glslangvalidator:
rem -g          generate debug information
rem -Od         disables optimization; may cause illegal SPIR-V for HLSL
rem -m          memory leak mode

set DEBUG_PARAMS=
if [%1%] == [debug] set DEBUG_PARAMS=-g -O0

del spv\*.spv /Q
del txt\*.txt /Q
del txt\*.json /Q



set SHADER_NAME=FogShader

echo #### Start compiling %SHADER_NAME% shaders ####


set IN_FILE_NAME=%SHADER_NAME%.vert
set OUT_FILE_NAME=%IN_FILE_NAME%

rem We use glslc instead of glslangvalidator because glslc supports include
rem glslangvalidator %IN_FILE_NAME%.glsl -o spv\%IN_FILE_NAME%.spv -V > txt\%IN_FILE_NAME%.txt 
glslc %IN_FILE_NAME%.glsl -o spv\%OUT_FILE_NAME%.spv
if errorlevel 1 goto onError
echo Compiled %IN_FILE_NAME% into %OUT_FILE_NAME%

rem To generate human readable form of SPIR-V add "-H > txt\%IN_FILE_NAME%.txt" to glslangvalidator
rem "spirv-cross --dump-resources" writes to stderr so we need to use "2>" instead of ">" to redirest stderr to output
spirv-cross --vulkan-semantics --dump-resources spv\%OUT_FILE_NAME%.spv 2> txt\%OUT_FILE_NAME%.resources.txt --output txt\%OUT_FILE_NAME%.txt
spirv-cross --reflect --vulkan-semantics --output txt\%OUT_FILE_NAME%.json spv\%OUT_FILE_NAME%.spv


set IN_FILE_NAME=%SHADER_NAME%.frag
set OUT_FILE_NAME=%IN_FILE_NAME%

rem glslangvalidator %IN_FILE_NAME%.glsl -o spv\%IN_FILE_NAME%.spv -V > txt\%IN_FILE_NAME%.txt
glslc %IN_FILE_NAME%.glsl -o spv\%OUT_FILE_NAME%.spv
if errorlevel 1 goto onError
echo Compiled %IN_FILE_NAME% into %OUT_FILE_NAME%

spirv-cross --vulkan-semantics --dump-resources spv\%OUT_FILE_NAME%.spv 2> txt\%OUT_FILE_NAME%.resources.txt --output txt\%OUT_FILE_NAME%.txt
spirv-cross --reflect --vulkan-semantics --output txt\%OUT_FILE_NAME%.json spv\%OUT_FILE_NAME%.spv

echo.




if [%1%] == [debug] (
echo ######## Compiled shaders with DEBUG options ########
echo.
) else (
echo ######## Compiled shaders with RELEASE options ########
echo.
)

goto end

:onError
echo.
echo FAILED TO COMPILE %IN_FILE_NAME% into %OUT_FILE_NAME%

rem Open the last processed file output (NO not any more because glslc output error to stdout and not to file)
rem txt\%IN_FILE_NAME%.txt

rem exit /b 1

:end

if not [%2%] == [no_pause] pause