using EarthTool.MSH.Interfaces;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.MSH.Models
{
  public class MeshDescriptor : IMeshDescriptor
  {
    public int MeshType { get; set; }

    public IModelTemplate Template { get; set; }

    public IMeshFrames Frames { get; set; }

    public int UnknownValue1 { get; set; }

    public IEnumerable<IVector> MountPoints { get; set; }

    public IEnumerable<ISpotLight> SpotLights { get; set; }

    public IEnumerable<IOmniLight> OmniLights { get; set; }

    public ITemplateDetails TemplateDetails { get; set; }

    public IModelSlots Slots { get; set; }

    public IMeshBoundries Boundries { get; set; }

    public PartType MeshSubType { get; set; }

    public byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          bw.Write(MeshType);
          bw.Write(Template.ToByteArray(encoding));
          bw.Write(Frames.ToByteArray(encoding));
          bw.Write(UnknownValue1);
          bw.Write(MountPoints.SelectMany(x => x.ToByteArray(encoding)).ToArray());
          bw.Write(SpotLights.SelectMany(x => x.ToByteArray(encoding)).ToArray());
          bw.Write(OmniLights.SelectMany(x => x.ToByteArray(encoding)).ToArray());
          bw.Write(TemplateDetails.ToByteArray(encoding));
          bw.Write(Slots.ToByteArray(encoding));
          bw.Write(Boundries.ToByteArray(encoding));
          bw.Write((int)MeshSubType);
        }
        return output.ToArray();
      }
    }
  }
}