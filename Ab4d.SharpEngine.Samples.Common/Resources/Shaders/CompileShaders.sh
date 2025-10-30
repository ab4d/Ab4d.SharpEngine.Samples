#!/bin/bash

# before running this script set execute permissions on the file: "chmod u+x file_name.sh"

# VULKAN SDK needs to be installed to be able to run this file

setup_directories()
{
    rm spv/*.spv -f
    rm spv/debug_build.txt -f
    rm txt/*.txt -f
    rm txt/*.json -f
    
    if [[ ! -d "spv" ]]; then
        mkdir spv
    fi
    
    if [[ ! -d "txt" ]]; then
        mkdir txt
    fi    
}


# arguments:
# $1: shader_name
# $2: "vert" or "frag"
# $3: output_file_suffix ('_' is added before the specified suffix except when when started by '-' then nothing is added)
# $4: additional compiler arguments (usually define constants)
# $5: first argument to caller is passed as $5, usually this is "debug" - in this case .debug is added to file name and debug params are used in glslang
compile_shader()
{
    IN_FILE_NAME="$1.$2"
    
    if [[ -z $3 ]]; then
        OUT_FILE_NAME="$1.$2"
    elif [[ ${3::1} == "-" ]]; then
        OUT_FILE_NAME="$1$3.$2"
    else
        OUT_FILE_NAME="$1_$3.$2"
    fi
    
    if [[ $5 == "debug" ]]; then
        # add spv/debug_build.txt to mark that this folder has shaders with debug build (can be used by 
		echo "DEBUG BUILD" > spv/debug_build.txt
		DEBUG_PARAMS="-gVS"
        # The following is used for glslangvalidator
        # DEBUG_PARAMS="-g" 
	else
		# Do not use -g0 parameter to remove all debug information because this would prevent using spirv-cross for reflection (using _1, _2, ... instead of actual names)
		DEBUG_PARAMS=""
	fi
    
    #echo Start compiling $IN_FILE_NAME  $OUT_FILE_NAME
    
    # Using glslang instead of glslc becasue this supports -gVS that is requried by RenderDoc
    # To use #include with glslang, add "#extension GL_ARB_shading_language_include : require" after #version
    
    glslang $IN_FILE_NAME.glsl $4 -V100 $DEBUG_PARAMS --quiet --target-env vulkan1.0 -o spv/$OUT_FILE_NAME.spv || exit_script
    
    # It is also possible to generate text based reflection code:
    #glslang $IN_FILE_NAME.glsl $4 -V100 -q -o txt/$OUT_FILE_NAME.reflection.txt
    
    # We use glslc instead of glslangvalidator because glslc supports include
    # glslangvalidator -V  $DEBUG_PARAMS -S $2 -o spv/$OUT_FILE_NAME.spv -V > txt\%IN_FILE_NAME%.txt $IN_FILE_NAME.glsl
    #glslc $IN_FILE_NAME.glsl $4 -o spv/$OUT_FILE_NAME.spv || exit_script
    
    # To generate human readable form of SPIR-V add "-H > txt\%IN_FILE_NAME%.txt" to glslangvalidator
    # "spirv-cross --dump-resources" writes to stderr so we need to use "2>" instead of ">" to redirest stderr to output
    spirv-cross --vulkan-semantics --dump-resources spv/$OUT_FILE_NAME.spv 2> txt/$OUT_FILE_NAME.resources.txt --output txt/$OUT_FILE_NAME.txt
    spirv-cross --reflect --vulkan-semantics spv/$OUT_FILE_NAME.spv --output txt/$OUT_FILE_NAME.json


	# Do not optimize the shaders as their size is already very small:
	#spirv-opt -Os spv/$OUT_FILE_NAME.spv -o spv/$OUT_FILE_NAME.opt.spv

    echo Compiled $IN_FILE_NAME =\> $OUT_FILE_NAME.spv $5
}


exit_script()
{
    echo "!!!! FAILED TO COMPILE $IN_FILE_NAME into $OUT_FILE_NAME.spv !!!!"
    read -p "Press enter to continue..."
    exit -1
}


setup_directories

compile_shader "FogShader" "vert" "" ""
compile_shader "FogShader" "frag" "" ""
compile_shader "FogShader" "frag" "Texture" "-DUSE_DIFFUSE_TEXTURE"
compile_shader "HsvColorPostProcessShader" "frag" "" ""

read -p "Press enter to continue"
