using EarthTool.Common.Enums;
using EarthTool.Common.Factories;
using EarthTool.Common.Interfaces;
using EarthTool.MSH.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.MSH.Models
{
  public class EarthMesh : IMesh
  {
    public EarthMesh()
    {
      FileHeader = new EarthInfoFactory(Encoding.UTF8).Get(FileFlags.Guid, Guid.NewGuid());
      BaseHeader = new MeshBaseHeader { MeshKind = MeshKind.Static };
    }

    public IEarthInfo FileHeader { get; set; }

    public IMeshBaseHeader BaseHeader { get; set; }

    public IEnumerable<IModelPart> Geometries { get; set; }

    public PartNode PartsTree { get; set; }

    public IDynamicPart RootDynamic { get; set; }

    public uint? TrailingHierarchyUnwindCount
      => BaseHeader?.MeshKind == MeshKind.Static ? GetStaticTrailingHierarchyUnwindCount() : (uint?)null;

    public byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          ValidateArchiveHeader();
          bw.Write(FileHeader.ToByteArray(encoding));
          bw.Write(BaseHeader.ToByteArray(encoding));
          if (BaseHeader.MeshKind == MeshKind.Static)
          {
            var geometries = Geometries?.ToArray() ?? Array.Empty<IModelPart>();
            var trailingUnwind = GetStaticTrailingHierarchyUnwindCount(geometries);
            ValidateRecordMarkers(geometries);
            bw.Write(trailingUnwind);
            bw.Write(geometries.SelectMany(p => p.ToByteArray(encoding)).ToArray());
          }
          else if (BaseHeader.MeshKind == MeshKind.Dynamic)
          {
            if (RootDynamic == null)
            {
              throw new InvalidOperationException("Dynamic mesh requires a RootDynamic record.");
            }

            bw.Write(RootDynamic.ToByteArray(encoding));
          }
        }

        return output.ToArray().ToArray();
      }
    }

    internal static byte[] ToNestedDynamicByteArray(IMesh mesh, Encoding encoding)
    {
      if (mesh?.BaseHeader?.MeshKind != MeshKind.Dynamic || mesh.RootDynamic == null)
      {
        throw new InvalidOperationException("Nested dynamic mesh requires a dynamic BaseHeader and RootDynamic record.");
      }

      using (var output = new MemoryStream())
      using (var writer = new BinaryWriter(output, encoding))
      {
        writer.Write(mesh.BaseHeader.ToByteArray(encoding));
        writer.Write(mesh.RootDynamic.ToByteArray(encoding));
        return output.ToArray();
      }
    }

    private uint GetStaticTrailingHierarchyUnwindCount()
      => GetStaticTrailingHierarchyUnwindCount(Geometries?.ToArray() ?? Array.Empty<IModelPart>());

    private static uint GetStaticTrailingHierarchyUnwindCount(IReadOnlyList<IModelPart> geometries)
    {
      if (geometries.Count == 0)
      {
        throw new InvalidOperationException("Static mesh requires at least one render record.");
      }

      var sourceDepth = 0;
      foreach (var geometry in geometries)
      {
        sourceDepth = AdvanceSourceDepth(sourceDepth, geometry);
      }

      return (uint)sourceDepth + 1;
    }

    internal static int AdvanceSourceDepth(int sourceDepth, IModelPart geometry)
    {
      if (geometry == null)
      {
        throw new InvalidOperationException("Static render-record collection cannot contain null records.");
      }

      if ((geometry.PartType & Enums.PartType.Subpart) != 0)
      {
        sourceDepth++;
      }

      if (geometry.BackTrackDepth > sourceDepth)
      {
        throw new InvalidOperationException(
          $"Static render-record hierarchy underflow: unwind {geometry.BackTrackDepth} exceeds source depth {sourceDepth}.");
      }

      return sourceDepth - geometry.BackTrackDepth;
    }

    private static void ValidateRecordMarkers(IReadOnlyList<IModelPart> geometries)
    {
      for (var i = 0; i < geometries.Count; i++)
      {
        var isFinal = i == geometries.Count - 1;
        if (isFinal && geometries[i].NextRecordMarker != 0)
        {
          throw new InvalidOperationException("Final static NextRecordMarker must be zero.");
        }

        if (!isFinal && geometries[i].NextRecordMarker == 0)
        {
          throw new InvalidOperationException("Non-final static NextRecordMarker must be nonzero.");
        }
      }
    }

    private void ValidateArchiveHeader()
    {
      if (FileHeader == null)
      {
        throw new InvalidOperationException("FileHeader is required.");
      }

      if (BaseHeader == null)
      {
        throw new InvalidOperationException("BaseHeader is required.");
      }

      if (BaseHeader.MeshKind == MeshKind.Static)
      {
        if (FileHeader.Flags != FileFlags.Guid || !FileHeader.Guid.HasValue ||
            FileHeader.ResourceType.HasValue || !string.IsNullOrEmpty(FileHeader.TranslationId))
        {
          throw new InvalidOperationException("Static FileHeader must contain exactly the GUID archive field.");
        }
      }
      else if (FileHeader.Flags != (FileFlags.Resource | FileFlags.Guid) ||
               FileHeader.ResourceType != ResourceType.Effect ||
               !FileHeader.Guid.HasValue ||
               !string.IsNullOrEmpty(FileHeader.TranslationId))
      {
        throw new InvalidOperationException("Dynamic FileHeader must contain Effect resource and GUID archive fields.");
      }
    }
  }
}
