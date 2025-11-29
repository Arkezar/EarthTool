using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;
using EarthTool.Common.Models;
using EarthTool.PAR.Enums;
using EarthTool.PAR.Models;
using EarthTool.PAR.Models.Serialization;
using Spectre.Console;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EarthTool.CLI.Commands.PAR;

public class ConvertCommand : CommonCommand<ParSettings>
{
  private const string ParExtension = ".par";
  private const string JsonExtension = ".json";

  private readonly IReader<ParFile> _reader;
  private readonly IWriter<ParFile> _writer;
  private readonly IEarthInfoFactory _earthInfoFactory;

  public ConvertCommand(IReader<ParFile> reader, IWriter<ParFile> writer, IEarthInfoFactory earthInfoFactory)
  {
    _reader = reader;
    _writer = writer;
    _earthInfoFactory = earthInfoFactory;
  }

  protected override Task InternalAnalyzeAsync(string inputFilePath, ParSettings settings)
  {
    var extension = Path.GetExtension(inputFilePath);

    if (extension == ParExtension)
    {
      var file = _reader.Read(inputFilePath);
      if (file == null)
      {
        AnsiConsole.MarkupLine("[red]Failed to read PAR file.[/]");
        return Task.CompletedTask;
      }

      var researchLookup = file.Research.ToDictionary(r => r.Id, r => r.Name);
      AnsiConsole.MarkupLine("[green]Parameters file loaded successfully.[/]");

      var multiDependencyResearch = file.Research
        .Where(r => r.RequiredResearch.Count() > 1)
        .ToList();

      foreach (var research in multiDependencyResearch)
      {
        var dependencyNames = research.RequiredResearch.Select(id => researchLookup[id]);
        AnsiConsole.MarkupLine(
          $"[yellow]Research {research.Id} has multiple required researches ({string.Join(", ", dependencyNames)}).[/]");
      }
    }

    return Task.CompletedTask;
  }

  protected override Task InternalExecuteAsync(string filePath, ParSettings settings)
  {
    var outputDirectory = settings.OutputFolderPath.Value ?? Path.GetDirectoryName(filePath);
    var extension = Path.GetExtension(filePath);

    if (extension == ParExtension)
    {
      ConvertToJson(filePath, outputDirectory);
    }
    else if (extension == JsonExtension)
    {
      ConvertToPar(filePath, outputDirectory, settings);
    }
    else
    {
      AnsiConsole.MarkupLine($"[red]Unsupported file format: {extension}. Expected {ParExtension} or {JsonExtension}.[/]");
    }

    return Task.CompletedTask;
  }

  private void ConvertToJson(string filePath, string outputDirectory)
  {
    var file = _reader.Read(filePath);
    if (file == null)
    {
      AnsiConsole.MarkupLine("[red]Failed to read PAR file.[/]");
      return;
    }

    var outputFileName = Path.ChangeExtension(Path.GetFileName(filePath), JsonExtension);
    var outputFilePath = Path.Combine(outputDirectory, outputFileName);

    if (!Directory.Exists(outputDirectory))
    {
      Directory.CreateDirectory(outputDirectory);
    }

    var options = CreateJsonSerializerOptions();
    var serializedContent = JsonSerializer.Serialize(file, options);
    File.WriteAllText(outputFilePath, serializedContent);

    AnsiConsole.MarkupLine($"[green]Successfully converted to JSON: {outputFileName}[/]");
  }

  private void ConvertToPar(string filePath, string outputDirectory, ParSettings settings)
  {
    var outputFileName = Path.ChangeExtension(Path.GetFileName(filePath), ParExtension);
    var outputFilePath = Path.Combine(outputDirectory, outputFileName);

    var options = CreateJsonSerializerOptions();
    var jsonContent = File.ReadAllText(filePath);
    var parameters = JsonSerializer.Deserialize<ParFile>(jsonContent, options);

    if (parameters == null)
    {
      AnsiConsole.MarkupLine("[red]Failed to deserialize JSON file.[/]");
      return;
    }

    var fileGuid = settings.Guid ?? Guid.NewGuid();
    parameters.FileHeader =
        _earthInfoFactory.Get(FileFlags.Resource | FileFlags.Guid, fileGuid, ResourceType.Parameters);

    _writer.Write(parameters, outputFilePath);

    AnsiConsole.MarkupLine($"[green]Successfully converted to PAR: {outputFileName}[/]");
  }

  private static JsonSerializerOptions CreateJsonSerializerOptions()
  {
    var options = new JsonSerializerOptions { WriteIndented = true };
    options.Converters.Add(new EntityConverter());
    options.Converters.Add(new JsonStringEnumConverter());
    return options;
  }

  private void PrintModelDetails(ParFile model)
  {
    var root = new Tree($"[green]Parameters[/]");
    var groups = root.AddNode("Groups");
    PopulateGroupsHierarchy(groups, model.Groups, model.Research);

    var research = root.AddNode("Research");
    PopulateResearchHierarchy(research, model.Research);

    AnsiConsole.Write(root);
  }

  private void PopulateGroupsHierarchy(TreeNode groups, IEnumerable<EntityGroup> modelGroups,
      IEnumerable<Research> modelResearch)
  {
    var factionGroups = modelGroups.GroupBy(g => g.Faction);
    foreach (var factionGroup in factionGroups)
    {
      var factionNode = groups.AddNode($"Faction: {factionGroup.Key}");
      foreach (var group in factionGroup)
      {
        var groupTypeNode = factionNode.AddNode($"Group: {group.GroupType}");
        var table = new Table();

        foreach (var entity in group.Entities)
        {
          var data = GetData(entity);
          if (!table.Columns.Any()) table.AddColumns(data.Keys.ToArray());
          table.AddRow(data.Values.ToArray());

          groupTypeNode.AddNode(table);
        }
      }
    }
  }

  private void PopulateResearchHierarchy(TreeNode research, IEnumerable<Research> modelResearch)
  {
    var startNodes = modelResearch.Where(r => !r.RequiredResearch.Any());
    var groupedStartNodes = startNodes.GroupBy(n => n.Faction);

    foreach (var group in groupedStartNodes)
    {
      var groupNode = research.AddNode($"Faction: {group.Key}");
      foreach (var typeGroups in group.GroupBy(g => g.Type))
      {
        var typeNode = groupNode.AddNode($"Type: {typeGroups.Key}");
        foreach (var researchNode in group)
        {
          FillResearchNodeHierarchy(typeNode, researchNode, modelResearch);
        }
      }
    }
  }

  private void FillResearchNodeHierarchy(TreeNode research, Research currentNode, IEnumerable<Research> modelResearch)
  {
    var data = GetData(currentNode);
    var table = new Table();
    table.AddColumns(data.Keys.ToArray());
    table.AddRow(data.Values.ToArray());

    var researchNode = research.AddNode(table);

    foreach (var child in modelResearch.Where(n => n.RequiredResearch.Any(r => r == currentNode.Id)))
    {
      FillResearchNodeHierarchy(researchNode, child, modelResearch);
    }
  }

  private IDictionary<string, string> GetData(object model)
  {
    var data = new Dictionary<string, string>();
    foreach (var property in model.GetType().GetProperties())
    {
      var value = property.GetValue(model);
      if (value is ICollection collection)
      {
        data.Add(property.Name, string.Join(',', collection.Cast<object>().Select(o => o.ToString())));
      }
      else if (value is not null)
      {
        data.Add(property.Name, value.ToString());
      }
    }

    return data;
  }
}
