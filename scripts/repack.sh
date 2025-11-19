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
COMPRESS_FLAG=""
PRESERVE_TIMESTAMPS=false
TEMP_DIR="$(pwd)/temp_repack"
REPACK_DIR="$(pwd)/REPACK"
SCRIPT_DIR="$(pwd)"

# Convert CLI_COMMAND to use absolute paths if it contains relative paths
if [[ "$CLI_COMMAND" == *"dotnet run"* ]] && [[ "$CLI_COMMAND" == *"--project "* ]]; then
    # Extract project path and convert to absolute
    # Pattern: --project <path>
    if [[ "$CLI_COMMAND" =~ --project[[:space:]]+([^[:space:]]+) ]]; then
        rel_project="${BASH_REMATCH[1]}"
        abs_project="$(cd "$(dirname "$rel_project")" && pwd)/$(basename "$rel_project")"
        CLI_COMMAND="${CLI_COMMAND//$rel_project/$abs_project}"
    fi
fi

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
        # Handle direct executable path or dotnet with DLL
        eval "$CLI_COMMAND $@"
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

    # Get original archive internal timestamp (always preserve for byte-by-byte recreation)
    orig_timestamp=$(run_cli wd info "$archive_path" --timestamp-only 2>/dev/null | grep -v "^Using launch settings" | tail -1)
    if [ -z "$orig_timestamp" ]; then
        echo "  -> WARNING: Could not read archive timestamp"
        timestamp_arg=""
    else
        timestamp_arg="--timestamp $orig_timestamp"
        echo "  -> Using original timestamp: $orig_timestamp"
    fi

    # Get original archive GUID
    guid_arg=""
    orig_guid=$(run_cli wd info "$archive_path" --guid-only 2>/dev/null | grep -v "^Using launch settings" | tail -1)
    if [ -n "$orig_guid" ]; then
        guid_arg="--guid $orig_guid"
        echo "  -> Using original GUID: $orig_guid"
    fi

    # Get list of files in archive order
    # Filter out dotnet diagnostic messages and convert backslashes to forward slashes
    file_list=$(run_cli wd list "$archive_path" --names-only 2>/dev/null | grep -v "^Using launch settings" | tr '\\' '/')
    echo "  -> File list obtained: $(echo "$file_list" | wc -l) files"

    # Create new archive
    echo "  -> Creating new archive..."

    # Create temporary file list with files in order
    temp_file_list="$TEMP_DIR/.file_order.txt"
    echo "$file_list" > "$temp_file_list"

    # Read files one by one and build the archive
    first_file=true
    failed_creation=false
    while IFS= read -r file; do
        # Skip empty lines
        [ -z "$file" ] && continue
        
        # Check if file exists
        if [ ! -f "$TEMP_DIR/$file" ]; then
            echo "  -> WARNING: File not found: $file"
            continue
        fi
        
        if [ "$first_file" = true ]; then
            # Create archive with first file, using TEMP_DIR as base directory
            if ! run_cli wd create "$REPACK_DIR/$archive_name" -i "$TEMP_DIR/$file" --base-dir "$TEMP_DIR" $COMPRESS_FLAG $timestamp_arg $guid_arg > /dev/null 2>&1; then
                echo "  -> ERROR: Failed to create $archive_name"
                failed_count=$((failed_count + 1))
                failed_creation=true
                break
            fi
            first_file=false
        else
            # Add subsequent files with TEMP_DIR as base directory and preserve timestamp
            if ! run_cli wd add "$REPACK_DIR/$archive_name" "$TEMP_DIR/$file" --base-dir "$TEMP_DIR" --preserve-timestamp $COMPRESS_FLAG > /dev/null 2>&1; then
                echo "  -> ERROR: Failed to add file $file to $archive_name"
                failed_count=$((failed_count + 1))
                failed_creation=true
                break
            fi
        fi
    done < "$temp_file_list"
    
    # Check if creation failed
    if [ "$failed_creation" = true ] || [ ! -f "$REPACK_DIR/$archive_name" ]; then
        continue
    fi
    
    # Also set file system timestamp if preserving timestamps
    # Note: We don't touch the filesystem timestamp because FileTime values
    # are not compatible with Unix timestamps used by touch command
    
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
