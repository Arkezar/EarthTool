using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;
using EarthTool.MSH.Interfaces;
using EarthTool.MSH.Models;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EarthTool.CLI.Commands.MSH;

public sealed class ConvertCommand : CommonCommand<ConvertCommand.Settings>
{
  private readonly IReader<IMesh> _meshReader;
  private readonly Dictionary<FileType, IWriter<IMesh>> _meshWriters;

  public sealed class Settings : CommonSettings
  {
    [CommandOption("-f|--output-format")]
    [Description("Selected output format.")]
    [DefaultValue(FileType.DAE)]
    public FileType OutputFormat { get; set; }
  }

  public ConvertCommand(IEnumerable<IReader<IMesh>> meshReaders, IEnumerable<IWriter<IMesh>> meshWriters)
  {
    _meshReader = meshReaders.Single(w => w.InputFileExtension == FileType.MSH);
    _meshWriters = meshWriters.ToDictionary(w => w.OutputFileExtension, w => w);
  }

  protected override Task InternalAnalyzeAsync(string inputFilePath, Settings settings)
  {
    try
    {
      var model = _meshReader.Read(inputFilePath);

      if (model.Descriptor.MeshType == MeshType.Dynamic)
      {
        AnsiConsole.WriteLine("{0}\t{1}", inputFilePath, string.Join('|', model.RootDynamic.SubMeshes.Select(m => m.RootDynamic.Position2).Append(model.RootDynamic.Position2)));
        // AnsiConsole.WriteLine("{0}\t{1}", inputFilePath, model.RootEffect.UnknownFloats1.Last());
      }

      //Part Types
      // AnsiConsole.WriteLine("{0}\t{1}", inputFilePath, string.Join('|', model.Geometries.Select(g => g.PartType)));
    }
    catch
    {
    }

    return Task.CompletedTask;
  }

  protected override Task InternalExecuteAsync(string filePath, ConvertCommand.Settings settings)
  {
    var writer = _meshWriters[settings.OutputFormat];

    var model = _meshReader.Read(filePath);

    var outputFilePath =
      GetOutputFilePath(filePath, settings.OutputFolderPath.Value, writer.OutputFileExtension);

    var outputFile = writer.Write(model, outputFilePath);

    PrintModelDetails(filePath, outputFile, model);

    return Task.CompletedTask;
  }

  private void PrintModelDetails(string inputFilePath, string outputFilePath, IMesh model)
  {
    var modelName = Path.GetFileNameWithoutExtension(inputFilePath);
    var animationFrames = model.Descriptor.Frames.ActionFrames + model.Descriptor.Frames.BuildingFrames +
                          model.Descriptor.Frames.LoopedFrames + model.Descriptor.Frames.MovementFrames;

    var root = new Tree($"[green]Converted {modelName}[/]");
    var details = root.AddNode("Details");
    details.AddNode($"Input file: {inputFilePath}");
    details.AddNode($"Output file: {outputFilePath}");
    details.AddNode($"Number of parts: {model.Geometries.Count()}");
    details.AddNode($"Animation frames: {animationFrames}");

    var textures = root.AddNode("Textures");
    foreach (var texture in model.Geometries.Select(g => g.Texture.FileName).Distinct())
    {
      textures.AddNode(texture);
    }

    var hierarchy = root.AddNode("Hierarchy");
    var id = 0;
    PopulateHierarchy(hierarchy, model.PartsTree, modelName, ref id);

    AnsiConsole.Write(root);
  }

  private void PopulateHierarchy(TreeNode treeNode, PartNode rootNode, string name, ref int id)
  {
    var currentNode =
      treeNode.AddNode(
        $"Part-{id++} ({string.Join(',', rootNode.Parts.Select((p, i) => $"{i}:{p.Texture.FileName}"))})");
    foreach (var child in rootNode.Children)
    {
      PopulateHierarchy(currentNode, child, name, ref id);
    }
  }
}