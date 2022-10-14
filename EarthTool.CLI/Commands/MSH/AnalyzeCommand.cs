using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;
using EarthTool.MSH.Interfaces;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EarthTool.CLI.Commands.MSH;

public class AnalyzeCommand : CommonCommand<CommonSettings>
{
  private IReader<IMesh> _reader;

  public AnalyzeCommand(IEnumerable<IReader<IMesh>> readers)
  {
    _reader = readers.Single(r => r.InputFileExtension == FileType.MSH);
  }

  protected override Task InternalExecuteAsync(string inputFilePath, CommonSettings settings)
  {
    try
    {
      var model = _reader.Read(inputFilePath);

      //Part Types
      // AnsiConsole.WriteLine("{0}\t{1}", inputFilePath, string.Join('|', model.Geometries.Select(g => g.PartType)));

      //Animation Types
      // AnsiConsole.WriteLine("{0}\t{1}", inputFilePath, string.Join('|', model.Geometries.Select(g => g.AnimationType)));

      // UnknownAnimationData
      // if (model.Geometries.Any(g => g.Animations.UnknownAnimationData.Any()))
      //   AnsiConsole.WriteLine("{0}\t{1}", inputFilePath,
      //     model.Geometries.Max(g => g.Animations.UnknownAnimationData.Count()));
    }
    catch
    {
    }


    return Task.CompletedTask;
  }
}