using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;

namespace EarthTool.WD.GUI.Services;

/// <summary>
/// Service for managing Text flags on archive items.
/// </summary>
public interface ITextFlagService
{
    /// <summary>
    /// Sets the Text flag on an archive item.
    /// </summary>
    /// <param name="item">The archive item to modify.</param>
    void SetTextFlag(IArchiveItem item);

    /// <summary>
    /// Clears the Text flag from an archive item.
    /// </summary>
    /// <param name="item">The archive item to modify.</param>
    void ClearTextFlag(IArchiveItem item);

    /// <summary>
    /// Checks if an archive item has the Text flag set.
    /// </summary>
    /// <param name="item">The archive item to check.</param>
    /// <returns>True if the Text flag is set, false otherwise.</returns>
    bool HasTextFlag(IArchiveItem item);

    /// <summary>
    /// Checks if a file path has a text file extension.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <returns>True if the file has a text extension, false otherwise.</returns>
    bool IsTextFileExtension(string filePath);

    /// <summary>
    /// Gets the list of text file extensions.
    /// </summary>
    string[] GetTextFileExtensions();
}