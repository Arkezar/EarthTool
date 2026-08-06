#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace EarthTool.GLTF.Internal
{
  internal sealed class CanonicalStaticGltfCreationOptions
  {
    internal Guid CreationGuid { get; }

    internal GltfNewModelImportOptions ImportOptions { get; }

    internal CanonicalStaticGltfCreationOptions(
      Guid creationGuid,
      IReadOnlyDictionary<GltfMaterialHandle, string?>? textureResourceBindings = null
    )
      : this(creationGuid, new GltfNewModelImportOptions(textureResourceBindings))
    { }

    internal CanonicalStaticGltfCreationOptions(
      Guid creationGuid,
      GltfNewModelImportOptions importOptions
    )
    {
      CreationGuid = creationGuid;
      ImportOptions = importOptions ?? throw new ArgumentNullException(nameof(importOptions));
    }
  }

  internal sealed class CanonicalStaticGltfSemanticOptions
  {
    internal GltfNewModelImportOptions ImportOptions { get; }

    internal IReadOnlyDictionary<int, CannonAuthoringValues> CannonValues { get; }

    internal CanonicalStaticGltfSemanticOptions(
      GltfNewModelImportOptions importOptions,
      IReadOnlyDictionary<int, CannonAuthoringValues> cannonValues
    )
    {
      ImportOptions = importOptions ?? throw new ArgumentNullException(nameof(importOptions));
      CannonValues = new ReadOnlyDictionary<int, CannonAuthoringValues>(
        (cannonValues ?? throw new ArgumentNullException(nameof(cannonValues)))
          .ToDictionary(item => item.Key, item => item.Value)
      );
    }
  }
}
