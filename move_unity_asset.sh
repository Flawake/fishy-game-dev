#!/bin/bash

# Unity Asset Mover Script
# Automatically moves files AND their corresponding .meta files together
# Usage: ./move_unity_asset.sh source_file destination_folder

if [ $# -ne 2 ]; then
    echo "Usage: $0 <source_file> <destination_folder>"
    echo "Example: $0 Assets/PlayerController.cs Assets/Scripts/Core/"
    exit 1
fi

source_file="$1"
destination_folder="$2"

# Check if source file exists
if [ ! -f "$source_file" ]; then
    echo "Error: Source file '$source_file' does not exist!"
    exit 1
fi

# Check if destination folder exists, create if not
if [ ! -d "$destination_folder" ]; then
    echo "Creating destination folder: $destination_folder"
    mkdir -p "$destination_folder"
fi

# Get the filename and meta file path
filename=$(basename "$source_file")
meta_file="${source_file}.meta"

# Check if meta file exists
if [ ! -f "$meta_file" ]; then
    echo "Warning: No .meta file found for '$source_file'"
    echo "Moving only the main file..."
    mv "$source_file" "$destination_folder/"
else
    echo "Moving '$source_file' and '$meta_file' to '$destination_folder/'"
    mv "$source_file" "$meta_file" "$destination_folder/"
fi

echo "✅ Successfully moved to $destination_folder/" 