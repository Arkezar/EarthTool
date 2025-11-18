#!/bin/bash
set -e

# Usage: ./repack.sh <path_to_EarthTool.CLI> <path_to_WD_archives_folder> [--compress] [--preserve-timestamps]
# Example: ./repack.sh ./EarthTool.CLI/bin/Release/net8.0/EarthTool.CLI ./WDFiles_Orginal
# Example with compression: ./repack.sh "dotnet run --project EarthTool.CLI/EarthTool.CLI.csproj -c Release --" ./WDFiles_Orginal --compress
# Example preserving timestamps: ./repack.sh "dotnet run..." ./WDFiles_Orginal --compress --preserve-timestamps

if [ $# -lt 2 ]; then
    echo "Usage: $0 <path_to_EarthTool.CLI> <path_to_WD_archives_folder> [OPTIONS]"
    echo "Example: $0 ./EarthTool.CLI/bin/Release/net8.0/EarthTool.CLI ./WDFiles_Orginal"
    echo ""
    echo "Options:"
    echo "  --compress              Enable compression for repacked files (default: no compression)"
    echo "  --preserve-timestamps   Preserve original file modification times"
    echo ""
    echo "IMPORTANT: This script cannot automatically detect per-file compression settings."
    echo "           Use --compress if original archives had compressed files."
    echo "           Omit --compress if original archives had uncompressed files."
    echo "           Mixed compression archives may not repack byte-for-byte identical."
    echo ""
    echo "TIMESTAMPS: Use --preserve-timestamps to copy original timestamps (useful for tests)."
    exit 1
fi

CLI_COMMAND="$1"
WD_FOLDER="$2"
COMPRESS_FLAG="--no-compress"
PRESERVE_TIMESTAMPS=false
TEMP_DIR="$(pwd)/temp_repack"
REPACK_DIR="$(pwd)/REPACK"

# Parse optional flags (starting from 3rd argument)
shift 2
while [ $# -gt 0 ]; do
    case "$1" in
        --compress)
            COMPRESS_FLAG=""
            shift
            ;;
        --preserve-timestamps)
            PRESERVE_TIMESTAMPS=true
            shift
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Display configuration
if [ "$COMPRESS_FLAG" = "" ]; then
    echo "Note: Compression ENABLED for repacked files"
else
    echo "Note: Compression DISABLED (preserving original compression state)"
fi

if [ "$PRESERVE_TIMESTAMPS" = true ]; then
    echo "Note: PRESERVING original timestamps"
fi

# Function to run CLI command
run_cli() {
    if [[ "$CLI_COMMAND" == *"dotnet run"* ]]; then
        # Handle dotnet run command
        eval "$CLI_COMMAND $@"
    else
        # Handle direct executable path
        "$CLI_COMMAND" "$@"
    fi
}

# Validate inputs
# Check if CLI_COMMAND is a file, a .dll, or a command (like "dotnet run...")
if [[ "$CLI_COMMAND" != *"dotnet"* ]] && [ ! -f "$CLI_COMMAND" ] && [ ! -f "${CLI_COMMAND}.dll" ]; then
    echo "ERROR: EarthTool.CLI not found at: $CLI_COMMAND"
    exit 1
fi

if [ ! -d "$WD_FOLDER" ]; then
    echo "ERROR: WD archives folder not found: $WD_FOLDER"
    exit 1
fi

# Prepare output directory
mkdir -p "$REPACK_DIR"

# Clean temp directory if exists
rm -rf "$TEMP_DIR"

echo ""
echo "==================================="
echo "EarthTool WD Archive Repacker"
echo "==================================="
echo "CLI Command: $CLI_COMMAND"
echo "WD Folder: $WD_FOLDER"
echo "Temp Dir: $TEMP_DIR"
echo "Output Dir: $REPACK_DIR"
echo "==================================="
echo ""

# Process each .wd file
total_files=0
success_count=0
failed_count=0

for archive_path in "$WD_FOLDER"/*.wd; do
    # Check if file exists (handles case when no .wd files found)
    if [ ! -f "$archive_path" ]; then
        continue
    fi
    
    archive_name=$(basename "$archive_path")
    base_name="${archive_name%.wd}"
    total_files=$((total_files + 1))
    
    echo "[$total_files] Processing: $archive_name"
    
    # Clean temp directory for this archive
    rm -rf "$TEMP_DIR"
    mkdir -p "$TEMP_DIR"
    
    # Extract archive to temp directory
    echo "  -> Extracting..."
    if ! run_cli wd extract "$archive_path" -o "$TEMP_DIR" > /dev/null 2>&1; then
        echo "  -> ERROR: Failed to extract $archive_name"
        failed_count=$((failed_count + 1))
        continue
    fi
    
    # Check if extraction created any files or folders
    # Note: Some archives extract to subfolder, others extract files directly
    extracted_content=$(find "$TEMP_DIR" -mindepth 1 | head -1)
    
    if [ -z "$extracted_content" ]; then
        # Archive might be empty or extraction failed
        echo "  -> WARNING: No files extracted (empty archive?), copying original..."
        cp "$archive_path" "$REPACK_DIR/$archive_name"
        success_count=$((success_count + 1))
        continue
    fi
    
    # Count extracted files
    file_count=$(find "$TEMP_DIR" -type f | wc -l)
    echo "  -> Extracted $file_count file(s)"
    
    # Determine what to pack
    # Strategy: Preserve original structure
    # - If archive had files in subfolder(s), extracted structure will have subdirs
    # - If archive had files at root, extracted structure will have files in TEMP_DIR root
    subdirs=$(find "$TEMP_DIR" -mindepth 1 -maxdepth 1 -type d | wc -l)
    files_in_root=$(find "$TEMP_DIR" -mindepth 1 -maxdepth 1 -type f | wc -l)
    
    if [ $subdirs -eq 1 ] && [ $files_in_root -eq 0 ]; then
        # Single subdirectory with no files in root - pack the subdirectory
        # This preserves folder structure like "Parameters/file.txt"
        pack_source=$(find "$TEMP_DIR" -mindepth 1 -maxdepth 1 -type d | head -1)
        echo "  -> Structure: Single folder ($(basename "$pack_source"))"
    else
        # Files in root or multiple subdirectories - pack everything as-is
        # This preserves flat structure or multi-folder structure
        # Use a wrapper to pass multiple files/folders to CLI
        pack_source="$TEMP_DIR"
        echo "  -> Structure: Flat or multi-folder ($files_in_root files, $subdirs dirs)"
    fi
    
    # Create new archive from extracted files
    echo "  -> Creating new archive..."
    if ! run_cli wd create "$REPACK_DIR/$archive_name" -i "$pack_source" -r $COMPRESS_FLAG > /dev/null 2>&1; then
        echo "  -> ERROR: Failed to create $archive_name"
        failed_count=$((failed_count + 1))
        continue
    fi
    
    # Preserve original timestamp if requested
    if [ "$PRESERVE_TIMESTAMPS" = true ]; then
        orig_timestamp=$(stat -c %Y "$archive_path")
        touch -d "@$orig_timestamp" "$REPACK_DIR/$archive_name"
        echo "  -> Preserved original timestamp: $(date -d "@$orig_timestamp" '+%Y-%m-%d %H:%M:%S')"
    fi
    
    # Compare file sizes
    orig_size=$(stat -c %s "$archive_path")
    new_size=$(stat -c %s "$REPACK_DIR/$archive_name")
    size_diff=$((orig_size - new_size))
    
    # Allow small differences (up to 10 bytes for timestamp differences)
    if [ ${size_diff#-} -le 10 ]; then
        status="✓"
        success_count=$((success_count + 1))
    else
        status="✗ SIZE DIFF: $size_diff bytes"
        failed_count=$((failed_count + 1))
    fi
    
    echo "  -> Original: $orig_size bytes, New: $new_size bytes [$status]"
    echo ""
done

# Clean up temp directory
rm -rf "$TEMP_DIR"

echo "==================================="
echo "Repacking Summary"
echo "==================================="
echo "Total archives: $total_files"
echo "Successful: $success_count"
echo "Failed: $failed_count"
echo "==================================="

if [ $failed_count -gt 0 ]; then
    exit 1
fi

exit 0
