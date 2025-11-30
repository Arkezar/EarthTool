using EarthTool.Common.Interfaces;
using EarthTool.PAR.Models;
using EarthTool.PAR.Models.Abstracts;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace EarthTool.CLI.Commands.PAR;

public sealed class ItemCommand : Command<ItemSettings>
{
  private readonly IReader<ParFile> _reader;

  public ItemCommand(IReader<ParFile> reader)
  {
    _reader = reader;
  }

  public override int Execute(CommandContext context, ItemSettings settings, CancellationToken cancellationToken)
  {
    var parFile = _reader.Read(settings.ParFilePath);
    if (parFile == null)
    {
      AnsiConsole.MarkupLine("[red]Failed to read PAR file.[/]");
      return 1;
    }

    var searchName = settings.ItemName;
    var comparison = settings.ExactMatch
      ? StringComparison.Ordinal
      : StringComparison.OrdinalIgnoreCase;

    // Search in Research
    var matchingResearch = settings.ExactMatch
      ? parFile.Research.Where(r => r.Name.Equals(searchName, comparison)).ToList()
      : parFile.Research.Where(r => r.Name.Contains(searchName, comparison)).ToList();

    // Search in Entities
    var matchingEntities = new List<(EntityGroup Group, Entity Entity)>();
    foreach (var group in parFile.Groups)
    {
      var matches = settings.ExactMatch
        ? group.Entities.Where(e => e.Name.Equals(searchName, comparison))
        : group.Entities.Where(e => e.Name.Contains(searchName, comparison));

      foreach (var entity in matches)
      {
        matchingEntities.Add((group, entity));
      }
    }

    var totalMatches = matchingResearch.Count + matchingEntities.Count;

    if (totalMatches == 0)
    {
      AnsiConsole.MarkupLine($"[yellow]No items found matching '{searchName}'[/]");
      return 0;
    }

    if (totalMatches > 1 && !settings.ExactMatch)
    {
      AnsiConsole.MarkupLine($"[yellow]Found {totalMatches} items matching '{searchName}':[/]");
      AnsiConsole.WriteLine();

      if (matchingResearch.Any())
      {
        AnsiConsole.MarkupLine("[bold]Research:[/]");
        foreach (var research in matchingResearch)
        {
          AnsiConsole.MarkupLine($"  - {research.Name} (ID: {research.Id}, Faction: {research.Faction})");
        }
        AnsiConsole.WriteLine();
      }

      if (matchingEntities.Any())
      {
        AnsiConsole.MarkupLine("[bold]Entities:[/]");
        foreach (var (group, entity) in matchingEntities)
        {
          AnsiConsole.MarkupLine($"  - {entity.Name} (Group: {group.GroupType}, Faction: {group.Faction})");
        }
        AnsiConsole.WriteLine();
      }

      AnsiConsole.MarkupLine("[dim]Use --exact flag for exact matching or provide a more specific name.[/]");
      return 0;
    }

    // Display detailed information for single match or all matches in exact mode
    if (matchingResearch.Any())
    {
      foreach (var research in matchingResearch)
      {
        DisplayResearchDetails(research, parFile.Research);
      }
    }

    if (matchingEntities.Any())
    {
      foreach (var (group, entity) in matchingEntities)
      {
        DisplayEntityDetails(entity, group);
      }
    }

    return 0;
  }

  private void DisplayResearchDetails(Research research, IEnumerable<Research> allResearch)
  {
    var tree = new Tree($"[bold green]Research: {research.Name}[/]");

    var basicInfo = tree.AddNode("[bold]Basic Information[/]");
    basicInfo.AddNode($"ID: {research.Id}");
    basicInfo.AddNode($"Faction: {research.Faction}");
    basicInfo.AddNode($"Type: {research.Type}");

    var costs = tree.AddNode("[bold]Costs[/]");
    costs.AddNode($"Campaign Cost: {research.CampaignCost}");
    costs.AddNode($"Skirmish Cost: {research.SkirmishCost}");
    costs.AddNode($"Campaign Time: {research.CampaignTime}");
    costs.AddNode($"Skirmish Time: {research.SkirmishTime}");

    if (!string.IsNullOrEmpty(research.Video))
    {
      tree.AddNode($"Video: {research.Video}");
    }

    if (!string.IsNullOrEmpty(research.Mesh))
    {
      var meshInfo = tree.AddNode("[bold]Mesh Information[/]");
      meshInfo.AddNode($"Mesh: {research.Mesh}");
      meshInfo.AddNode($"Mesh Params Index: {research.MeshParamsIndex}");
    }

    if (research.RequiredResearch.Any())
    {
      var requirements = tree.AddNode("[bold]Required Research[/]");
      var researchLookup = allResearch.ToDictionary(r => r.Id, r => r);
      foreach (var reqId in research.RequiredResearch)
      {
        if (researchLookup.TryGetValue(reqId, out var reqResearch))
        {
          requirements.AddNode($"[{reqId}] {reqResearch.Name}");
        }
        else
        {
          requirements.AddNode($"[{reqId}] (Unknown)");
        }
      }
    }

    AnsiConsole.Write(tree);
    AnsiConsole.WriteLine();
  }

  private void DisplayEntityDetails(Entity entity, EntityGroup group)
  {
    var tree = new Tree($"[bold green]Entity: {entity.Name}[/]");

    var basicInfo = tree.AddNode("[bold]Basic Information[/]");
    basicInfo.AddNode($"Class: {entity.ClassId}");
    basicInfo.AddNode($"Type: {entity.GetType().Name}");
    basicInfo.AddNode($"Faction: {group.Faction}");
    basicInfo.AddNode($"Group Type: {group.GroupType}");

    if (entity.RequiredResearch.Any())
    {
      var requirements = tree.AddNode($"[bold]Required Research IDs[/]: {string.Join(", ", entity.RequiredResearch)}");
    }

    // Display all properties
    var table = new Table();
    table.AddColumn("Property");
    table.AddColumn("Value");
    table.Border(TableBorder.Rounded);

    foreach (var prop in entity.GetType().GetProperties())
    {
      // Skip special properties
      if (prop.Name == "Name" || prop.Name == "ClassId" || prop.Name == "RequiredResearch" ||
          prop.Name == "FieldTypes" || prop.Name == "TypeName")
      {
        continue;
      }

      var value = prop.GetValue(entity);
      if (value == null)
      {
        continue;
      }

      string displayValue;
      if (value is ICollection collection && value is not string)
      {
        displayValue = string.Join(", ", collection.Cast<object>().Select(o => o?.ToString() ?? "null"));
        if (string.IsNullOrEmpty(displayValue))
        {
          continue;
        }
      }
      else
      {
        displayValue = value.ToString();
        if (string.IsNullOrEmpty(displayValue) || displayValue == "0")
        {
          continue;
        }
      }

      table.AddRow(prop.Name, displayValue);
    }

    AnsiConsole.Write(tree);
    if (table.Rows.Count > 0)
    {
      var properties = tree.AddNode("[bold]Properties[/]");
      AnsiConsole.Write(table);
    }

    AnsiConsole.WriteLine();
  }
}
