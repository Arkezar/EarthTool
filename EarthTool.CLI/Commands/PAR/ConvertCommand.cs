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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EarthTool.CLI.Commands.PAR;

public class ConvertCommand : CommonCommand<CommonSettings>
{
  private readonly IReader<ParFile> _reader;
  private readonly IWriter<ParFile> _writer;
  private readonly IEarthInfoFactory _earthInfoFactory;

  public ConvertCommand(IReader<ParFile> reader, IWriter<ParFile> writer, IEarthInfoFactory earthInfoFactory)
  {
    _reader = reader;
    _writer = writer;
    _earthInfoFactory = earthInfoFactory;
  }

  protected override Task InternalExecuteAsync(string filePath, CommonSettings settings)
  {
    var outputDirectory = settings.OutputFolderPath.Value ?? Path.GetDirectoryName(filePath);
    var extension = Path.GetExtension(filePath);

    if (extension == ".par")
    {
      ConvertToJson(filePath, outputDirectory);
    }
    else if (extension == ".json")
    {
      ConvertToPar(filePath, outputDirectory);
    }
    else
    {
      AnsiConsole.MarkupLine("[red]Unsupported file format[/]");
    }

    return Task.CompletedTask;
  }

  private void ConvertToJson(string filePath, string outputDirectory)
  {
    var file = _reader.Read(filePath);

    var outputFileName = Path.ChangeExtension(Path.GetFileName(filePath), "json");
    var outputFilePath = Path.Combine(outputDirectory, outputFileName);

    if (!Directory.Exists(outputDirectory))
    {
      Directory.CreateDirectory(outputDirectory);
    }

    var opts = new JsonSerializerOptions { WriteIndented = true };
    opts.Converters.Add(new EntityConverter());
    opts.Converters.Add(new JsonStringEnumConverter());
    var serializedRoot = JsonSerializer.Serialize(file, opts);
    File.WriteAllText(outputFilePath, serializedRoot);
  }

  private void ConvertToPar(string filePath, string outputDirectory)
  {
    var outputFileName = Path.ChangeExtension(Path.GetFileName(filePath), "par");
    var outputFilePath = Path.Combine(outputDirectory, outputFileName);
    var opts = new JsonSerializerOptions();
    opts.Converters.Add(new EntityConverter());
    opts.Converters.Add(new JsonStringEnumConverter());
    var parameters = JsonSerializer.Deserialize<ParFile>(File.ReadAllText(filePath), opts);
    parameters.FileHeader =
        _earthInfoFactory.Get(FileFlags.Resource | FileFlags.Guid, Guid.NewGuid(), ResourceType.Parameters);

    _writer.Write(parameters, outputFilePath);
  }

  private void PrintModelDetails(ParFile model)
  {
    var root = new Tree($"[green]Parameters[/]");
    var groups = root.AddNode("Groups");
    PopulateGroupsHierarchy(groups, model.Groups, model.Research);

    var research = root.AddNode("Research");
    PopulateReaserchHierarchy(research, model.Research);

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

  private void PopulateReaserchHierarchy(TreeNode research, IEnumerable<Research> modelResearch)
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
