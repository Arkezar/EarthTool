using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EarthTool.WD.Resources
{
  public class Directory
  {
    readonly ResourceFactory _resourceFactory;

    public IEnumerable<Resource> Resources
    {
      get;
    }

    public Directory(byte[] directoryData)
    {
      _resourceFactory = new ResourceFactory();

      Resources = ReadFileDescriptors(directoryData.Skip(10).ToArray());
    }

    private IEnumerable<Resource> ReadFileDescriptors(byte[] fileDescriptorsData)
    {
      var result = new List<Resource>();

      using (var stream = new MemoryStream(fileDescriptorsData))
      {
        try
        {
          while (stream.Position < stream.Length)
          {
            result.Add(_resourceFactory.Create(stream));
          }
        }
        catch
        {
          File.WriteAllBytes("error.txt", stream.ToArray());
          throw;
        }
      }

      return result;
    }
  }
}
