using EarthTool.PAR.Models;
using System.Threading.Tasks;

namespace EarthTool.PAR.GUI.Services;

/// <summary>
/// Service for loading and saving PAR files.
/// </summary>
public interface IParFileService
{
  /// <summary>
  /// Loads a PAR file from the specified path.
  /// </summary>
  /// <param name="filePath">Path to the PAR file.</param>
  /// <returns>The loaded ParFile.</returns>
  Task<ParFile> LoadAsync(string filePath);

  /// <summary>
  /// Saves a PAR file to the specified path.
  /// </summary>
  /// <param name="parFile">The ParFile to save.</param>
  /// <param name="filePath">Path where to save the file.</param>
  Task SaveAsync(ParFile parFile, string filePath);

  /// <summary>
  /// Creates a new empty PAR file.
  /// </summary>
  /// <returns>A new ParFile instance.</returns>
  Task<ParFile> CreateNewAsync();

  /// <summary>
  /// Creates a deep clone of a PAR file.
  /// </summary>
  /// <param name="original">The original ParFile.</param>
  /// <returns>A cloned ParFile.</returns>
  ParFile Clone(ParFile original);
}
