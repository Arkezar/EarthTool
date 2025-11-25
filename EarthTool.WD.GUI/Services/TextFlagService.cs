using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EarthTool.WD.GUI.Services;

/// <summary>
/// Service for managing Text flags on archive items.
/// </summary>
public class TextFlagService : ITextFlagService
{
    private readonly HashSet<string> _textExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".cfg", ".ini", ".log", ".json", ".xml",
        ".inf",  // Windows INF files (common in game configs)
        ".lua", ".script", ".bat", ".cmd", ".ps1", ".sh",
        ".properties", ".yaml", ".yml", ".toml", ".md"
    };

    /// <inheritdoc/>
    public void SetTextFlag(IArchiveItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        item.Header.SetFlag(FileFlags.Text);
    }

    /// <inheritdoc/>
    public void ClearTextFlag(IArchiveItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        item.Header.RemoveFlag(FileFlags.Text);
    }

    /// <inheritdoc/>
    public bool HasTextFlag(IArchiveItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Header.Flags.HasFlag(FileFlags.Text);
    }

    /// <inheritdoc/>
    public bool IsTextFileExtension(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return false;

        var extension = Path.GetExtension(filePath);
        return _textExtensions.Contains(extension);
    }

    /// <inheritdoc/>
    public string[] GetTextFileExtensions()
    {
        return _textExtensions.ToArray();
    }
}