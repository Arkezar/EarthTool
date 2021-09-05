using EarthTool.Common.Extensions;
using EarthTool.Common.Interfaces;
using EarthTool.Common.Models;
using EarthTool.PAR.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthTool.PAR
{
  public class PARConverter : IPARConverter
  {
    private readonly ILogger<PARConverter> _logger;

    public PARConverter(ILogger<PARConverter> logger)
    {
      _logger = logger;
    }

    public Task Convert(string filePath, string outputPath = null)
    {
      outputPath ??= Path.GetDirectoryName(filePath);
      var data = File.ReadAllBytes(filePath);
      var filename = Path.GetFileNameWithoutExtension(filePath);
      using (var stream = new MemoryStream(data))
      {
        var file = new ParFile(stream);
        
      }
      return Task.CompletedTask;
    }

    public IConverter WithOptions(IReadOnlyCollection<Option> options)
    {
      throw new NotImplementedException();
    }
  }
}
