using EarthTool.Common.Interfaces;
using EarthTool.PAR.Models;
using EarthTool.PAR.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthTool.PAR.GUI.Services;

/// <summary>
/// Implementation of PAR file service.
/// </summary>
public class ParFileService : IParFileService
{
  private readonly ILogger<ParFileService> _logger;
  private readonly ParameterReader _reader;
  private readonly ParameterWriter _writer;

  public ParFileService(
    ILogger<ParFileService> logger,
    IEarthInfoFactory earthInfoFactory,
    Encoding encoding)
  {
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _reader = new ParameterReader(earthInfoFactory, encoding);
    _writer = new ParameterWriter(encoding);
  }

  /// <inheritdoc/>
  public async Task<ParFile> LoadAsync(string filePath)
  {
    if (string.IsNullOrWhiteSpace(filePath))
      throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

    _logger.LogInformation("Loading PAR file: {FilePath}", filePath);

    try
    {
      var parFile = await Task.Run(() => _reader.Read(filePath));
      _logger.LogInformation("Successfully loaded PAR file with {GroupCount} groups and {ResearchCount} research entries",
        parFile.Groups?.Count() ?? 0,
        parFile.Research?.Count() ?? 0);

      return parFile;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to load PAR file: {FilePath}", filePath);
      throw;
    }
  }

  /// <inheritdoc/>
  public async Task SaveAsync(ParFile parFile, string filePath)
  {
    if (parFile == null)
      throw new ArgumentNullException(nameof(parFile));

    if (string.IsNullOrWhiteSpace(filePath))
      throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

    _logger.LogInformation("Saving PAR file: {FilePath}", filePath);

    try
    {
      await Task.Run(() => _writer.Write(parFile, filePath));
      _logger.LogInformation("Successfully saved PAR file to: {FilePath}", filePath);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to save PAR file: {FilePath}", filePath);
      throw;
    }
  }

  /// <inheritdoc/>
  public Task<ParFile> CreateNewAsync()
  {
    _logger.LogInformation("Creating new PAR file");

    var parFile = new ParFile
    {
      Groups = new List<EntityGroup>(),
      Research = new List<Research>()
    };

    return Task.FromResult(parFile);
  }

  /// <inheritdoc/>
  public ParFile Clone(ParFile original)
  {
    if (original == null)
      throw new ArgumentNullException(nameof(original));

    _logger.LogDebug("Cloning PAR file");

    // Use serialization to create a deep clone
    // This is a simple approach - for production might need optimization
    var json = System.Text.Json.JsonSerializer.Serialize(original);
    var clone = System.Text.Json.JsonSerializer.Deserialize<ParFile>(json);

    if (clone == null)
      throw new InvalidOperationException("Failed to clone PAR file");

    return clone;
  }
}
