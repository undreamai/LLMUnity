#!/bin/bash

if [ "$(basename "$1")" == "README.md" ]; then
    PREFIX="# Overview\n\n"
    # Read the existing content of README.md
    EXISTING_CONTENT=`cat "$1" | sed -e 's:<p:<h1:g' | sed -e 's:</p:</h1:g'`

    # Prepend the new content to the existing content
    echo -e "$PREFIX$EXISTING_CONTENT$SUFFIX"
else
    # If the input file is not README.md, simply output its content
    cat "$1"
fi
