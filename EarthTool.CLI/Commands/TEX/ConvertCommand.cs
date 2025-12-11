using EarthTool.Common.Interfaces;
using EarthTool.TEX;
using EarthTool.TEX.Interfaces;
using SkiaSharp;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EarthTool.CLI.Commands.TEX;

public sealed class ConvertCommand : CommonCommand<ConvertCommand.Settings>
{
  private readonly IReader<ITexFile> _reader;

  public sealed class Settings : CommonSettings
  {
    [CommandOption("--highres")]
    [Description("Extract only high res mipmaps.")]
    [DefaultValue(true)]
    public bool HighResolutionOnly { get; set; }
  }

  public ConvertCommand(IReader<ITexFile> reader)
  {
    _reader = reader;
  }

  protected override Task InternalAnalyzeAsync(string inputFilePath, Settings settings)
  {
    var options = new JsonSerializerOptions();
    options.Converters.Add(new JsonStringEnumConverter());
    var texFile = _reader.Read(inputFilePath);
    AnalyzeFileHeader(inputFilePath, texFile.Header, options);
    AnalyzeImages(inputFilePath, texFile.Images, options);
    return Task.CompletedTask;
  }

  private void AnalyzeFileHeader(string inputFilePath, TexHeader header, JsonSerializerOptions options)
  {
    if (header == null) return;
    var json = JsonSerializer.Serialize(header, options);
    AnsiConsole.WriteLine("{0}\t\tGroup: {1}", inputFilePath, json);
  }

  private void AnalyzeImages(string inputFilePath, IEnumerable<IEnumerable<TexImage>> images,
    JsonSerializerOptions options)
  {
    foreach (var group in images)
    {
      foreach (var image in group)
      {
        var json = JsonSerializer.Serialize(image.Header, options);
        AnsiConsole.WriteLine("{0}\t\tImage: {1}", inputFilePath, json);
      }
    }
  }

  protected override Task InternalExecuteAsync(string filePath, Settings settings)
  {
    var texFile = _reader.Read(filePath);
    SaveTex(filePath, texFile, settings);
    return Task.CompletedTask;
  }

  private void SaveTex(string filePath, ITexFile texFile, Settings settings)
  {
    var outputPath = GetOutputDirectory(filePath, settings.OutputFolderPath.Value);
    var fileName = Path.GetFileNameWithoutExtension(filePath);

    var saved = texFile.Images.SelectMany((group, i) =>
      group.SelectMany((img, j) =>
      {
        return img.Mipmaps.Take(settings.HighResolutionOnly ? 1 : img.Mipmaps.Count())
          .Select(mm => SaveBitmap(outputPath, $"{fileName}_{i}_{j}", mm, settings));
      }));

    AnsiConsole.MarkupLine($"[bold green]Saved:\n[/]{string.Join("\n", saved)}");
  }
  
  private string SaveBitmap(string workDir, string filename, SKBitmap image, Settings settings)
  {
    if (!Directory.Exists(workDir))
    {
      Directory.CreateDirectory(workDir);
    }

    var outputFileName = settings.HighResolutionOnly
      ? $"{filename}.png"
      : $"{filename}_{image.Width}x{image.Height}.png";

    var filePath = Path.Combine(workDir, outputFileName);
    using (var stream = new SKFileWStream(filePath))
    {
      image.Encode(stream, SKEncodedImageFormat.Png, 100);
    }

    return filePath;
  }
}
