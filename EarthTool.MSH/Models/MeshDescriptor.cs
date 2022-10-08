using EarthTool.MSH.Interfaces;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.MSH.Models
{
  public class MeshDescriptor : IMeshDescriptor
  {
    public MeshType MeshType { get; set; }

    public IModelTemplate Template { get; set; }

    public IMeshFrames Frames { get; set; }

    public int Empty { get; set; }

    public IEnumerable<IVector> MountPoints { get; set; }

    public IEnumerable<ISpotLight> SpotLights { get; set; }

    public IEnumerable<IOmniLight> OmnidirectionalLights { get; set; }

    public ITemplateDetails TemplateDetails { get; set; }

    public IModelSlots Slots { get; set; }

    public IMeshBoundries Boundaries { get; set; }

    public int MeshSubType { get; set; }

    public MeshSubType? RegularMeshSubType
    {
      get => MeshType == MeshType.Regular ? (MeshSubType?)MeshSubType : null;
      set
      {
        if (MeshType == MeshType.Regular)
        {
          MeshSubType = value.HasValue ? (int)value.Value : 0;
        }
      }
    }

    public DynamicMeshSubType? DynamicMeshSubType
    {
      get => MeshType == MeshType.Dynamic ? (DynamicMeshSubType?)MeshSubType : null;
      set
      {
        if (MeshType == MeshType.Dynamic)
        {
          MeshSubType = value.HasValue ? (int)value.Value : 0;
        }
      }
    }

    public byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          bw.Write((int)MeshType);
          bw.Write(Template.ToByteArray(encoding));
          bw.Write(Frames.ToByteArray(encoding));
          bw.Write(Empty);
          bw.Write(MountPoints.SelectMany(x => x.ToByteArray(encoding)).ToArray());
          bw.Write(SpotLights.SelectMany(x => x.ToByteArray(encoding)).ToArray());
          bw.Write(OmnidirectionalLights.SelectMany(x => x.ToByteArray(encoding)).ToArray());
          bw.Write(TemplateDetails.ToByteArray(encoding));
          bw.Write(Slots.ToByteArray(encoding));
          bw.Write(Boundaries.ToByteArray(encoding));
          bw.Write(MeshSubType);
        }
        return output.ToArray();
      }
    }
  }
}