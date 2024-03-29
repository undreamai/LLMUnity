#!/bin/bash

download_path=/tmp/doxygen.tar.gz
if [ ! -f $download_path ]; then
  wget -O $download_path https://www.doxygen.nl/files/doxygen-1.10.0.linux.bin.tar.gz
fi

doxygen=/tmp/doxygen-1.10.0/bin/doxygen
if [ ! -f $doxygen ]; then
  tar -xvf $download_path -C /tmp
fi

if [ ! -f $doxygen ]; then
  exit 1;
fi

$doxygen .github/doxygen/Doxyfile

output_dir=html
mkdir -p $output_dir/images/
cp .github/*png .github/*.gif $output_dir/images/
sed -i 's:LLM for Unity:Package:g' $output_dir/navtreedata.js
