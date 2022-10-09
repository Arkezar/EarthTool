using EarthTool.Common.Interfaces;
using EarthTool.MSH.Interfaces;
using EarthTool.MSH.Models;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EarthTool.CLI.Commands.DAE;

public sealed class ConvertCommand : AsyncCommand<CommonSettings>
{
  private readonly IReader<IMesh> _meshReader;
  private readonly IWriter<IMesh> _meshWriter;

  public ConvertCommand(IEnumerable<IReader<IMesh>> meshReaders, IEnumerable<IWriter<IMesh>> meshWriters)
  {
    _meshReader = meshReaders.Single(w => w.InputFileExtension == "dae");
    _meshWriter = meshWriters.Single(w => w.OutputFileExtension == "msh");
  }

  public override Task<int> ExecuteAsync(CommandContext context, CommonSettings settings)
  {
    var path = Path.GetDirectoryName(settings.InputFilePath);
    if (string.IsNullOrEmpty(path))
    {
      path = Environment.CurrentDirectory;
    }

    var filePattern = Path.GetFileName(settings.InputFilePath);
    var files = Directory.GetFiles(path, filePattern, SearchOption.TopDirectoryOnly);

    foreach (var filePath in files)
    {
      var fileName = Path.ChangeExtension(Path.GetFileName(filePath), "msh");
      var model = _meshReader.Read(filePath);
      var outputFile = _meshWriter.Write(model, Path.Combine(settings.OutputFolderPath.Value, fileName));
      PrintModelDetails(filePath, outputFile, model);
    }

    return Task.FromResult(0);
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
    var currentNode = treeNode.AddNode($"{name}-Part-{id++} ({rootNode.Part.Texture.FileName})");
    foreach (var child in rootNode.Children)
    {
      PopulateHierarchy(currentNode, child, name, ref id);
    }
  }
}