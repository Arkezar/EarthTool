using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;
using EarthTool.MSH.Interfaces;
using EarthTool.MSH.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EarthTool.CLI.Commands.DAE;

public sealed class ConvertCommand : CommonCommand<CommonSettings>
{
  private readonly IReader<IMesh> _meshReader;
  private readonly IWriter<IMesh> _meshWriter;

  public ConvertCommand(IEnumerable<IReader<IMesh>> meshReaders, IEnumerable<IWriter<IMesh>> meshWriters)
  {
    _meshReader = meshReaders.Single(w => w.InputFileExtension == FileType.DAE);
    _meshWriter = meshWriters.Single(w => w.OutputFileExtension == FileType.MSH);
  }

  protected override Task InternalExecuteAsync(string filePath, CommonSettings settings)
  {
    var model = _meshReader.Read(filePath);

    var outputFilePath =
      GetOutputFilePath(filePath, settings.OutputFolderPath.Value, _meshWriter.OutputFileExtension);

    var outputFile = _meshWriter.Write(model, outputFilePath);

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