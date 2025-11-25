#!/bin/bash

# Create test archive script for EarthTool.WD.GUI
# This creates a simple test WD archive that can be opened in the GUI

echo "Creating test files..."
mkdir -p test_data
echo "This is test file 1" > test_data/file1.txt
echo "This is test file 2 with more content to test compression" > test_data/file2.txt
echo "File 3" > test_data/file3.txt

echo "Building CLI tool..."
cd ..
dotnet build EarthTool.CLI

if [ $? -ne 0 ]; then
    echo "ERROR: Build failed"
    exit 1
fi

echo "Creating test archive..."
cd EarthTool.CLI
dotnet run -- wd pack ../EarthTool.WD.GUI/test_data ../EarthTool.WD.GUI/test_archive.WD

if [ $? -eq 0 ]; then
    echo "SUCCESS: Test archive created at EarthTool.WD.GUI/test_archive.WD"
    echo ""
    echo "You can now test the GUI by opening this archive."
    echo ""
    echo "To run the GUI:"
    echo "  cd ../EarthTool.WD.GUI"
    echo "  dotnet run"
else
    echo "ERROR: Failed to create archive"
    exit 1
fi
