using EarthTool.Common.Interfaces;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EarthTool.CLI.Commands.WD;

public sealed class ExtractCommand : CommonCommand<CommonSettings>
{
  private readonly IWDExtractor _extractor;

  public ExtractCommand(IWDExtractor extractor)
  {
    _extractor = extractor;
  }

  protected override async Task InternalExecuteAsync(string filePath, CommonSettings settings)
  {
    await _extractor.Extract(filePath, settings.OutputFolderPath.Value ?? Path.GetDirectoryName(filePath));
  }
}