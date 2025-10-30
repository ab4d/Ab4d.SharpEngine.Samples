#!/bin/bash

# before running this script set execute permissions on the file: "chmod u+x file_name.sh"

# VULKAN SDK needs to be installed to be able to run this file

# Debug parameters for glslc:
# -g          generate debug information
# -O0         disables optimization

if [[ $1 == "debug" ]]; then
  DEBUG_PARAMS="-g -O0"
else
  DEBUG_PARAMS=""
fi

setup_directories()
{
    rm spv/*.spv -f
    rm txt/*.txt -f
    rm txt/*.json -f
    
    if [[ ! -d "bin" ]]; then
        mkdir bin
    fi
}


# arguments:
# shader_name
# "vert" or "frag"
# output_file_suffix ('_' is added before the specified suffix except when when started by '-' then nothing is added)
# additional compiler arguments (usually define constants)
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
    
    #echo Start compiling $IN_FILE_NAME  $OUT_FILE_NAME
    
    # We use glslc instead of glslangvalidator because glslc supports include
    # glslangvalidator %IN_FILE_NAME%.glsl -o bin\%IN_FILE_NAME%.spv -V > txt\%IN_FILE_NAME%.txt 
    glslc $IN_FILE_NAME.glsl $4 -o spv/$OUT_FILE_NAME.spv || exit_script
       
    
    # To generate human readable form of SPIR-V add "-H > txt\%IN_FILE_NAME%.txt" to glslangvalidator
    # "spirv-cross --dump-resources" writes to stderr so we need to use "2>" instead of ">" to redirest stderr to output
    spirv-cross --vulkan-semantics --dump-resources spv/$OUT_FILE_NAME.spv 2> txt/$OUT_FILE_NAME.resources.txt --output txt/$OUT_FILE_NAME.txt
    spirv-cross --reflect --vulkan-semantics spv/$OUT_FILE_NAME.spv --output txt/$OUT_FILE_NAME.json

    echo Compiled $IN_FILE_NAME =\> $OUT_FILE_NAME
}


exit_script()
{
    echo "!!!! FAILED TO COMPILE $IN_FILE_NAME into $OUT_FILE_NAME !!!!"
    read -p "Press enter to continue"
    exit -1
}

setup_directories

compile_shader "FogShader" "vert" "" ""
compile_shader "FogShader" "frag" "" ""
compile_shader "FogShader" "frag" "Texture" "-DUSE_DIFFUSE_TEXTURE"
compile_shader "HsvColorPostProcessShader" "frag" "" ""

read -p "Press enter to continue"
