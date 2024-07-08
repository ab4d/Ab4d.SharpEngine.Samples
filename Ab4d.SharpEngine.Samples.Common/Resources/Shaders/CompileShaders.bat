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

for %%f in (*.glsl) do (
	rem We use glslc instead of glslangvalidator because glslc supports include
	rem glslangvalidator "%FILE_NAME%" -o "spv\%FILE_STUB%.spv" -V > "txt\%FILE_STUB%.txt"
	glslc "%%f" -o "spv\%%~nf.spv"
	if errorlevel 1 goto onError
	echo Compiled "%%f" into "%%~nf.spv"

	rem To generate human readable form of SPIR-V add "-H > txt\%IN_FILE_NAME%.txt" to glslangvalidator
	rem "spirv-cross --dump-resources" writes to stderr so we need to use "2>" instead of ">" to redirest stderr to output
	spirv-cross --vulkan-semantics --dump-resources "spv\%%~nf.spv" 2> "txt\%%~nf.resources.txt" --output "txt\%%~nf.txt"
	spirv-cross --reflect --vulkan-semantics --output "txt\%%~nf.json" "spv\%%~nf.spv"
)

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