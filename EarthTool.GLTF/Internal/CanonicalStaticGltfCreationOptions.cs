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

    internal IReadOnlyDictionary<GltfMaterialHandle, string?> TextureResourceBindings { get; }

    internal CanonicalStaticGltfCreationOptions(
      Guid creationGuid,
      IReadOnlyDictionary<GltfMaterialHandle, string?>? textureResourceBindings = null
    )
    {
      var bindings = textureResourceBindings?.ToDictionary(item => item.Key, item => item.Value)
        ?? new Dictionary<GltfMaterialHandle, string?>();
      if (bindings.Keys.Any(handle => handle.Value <= 0))
      {
        throw new ArgumentOutOfRangeException(nameof(textureResourceBindings));
      }

      CreationGuid = creationGuid;
      TextureResourceBindings = new ReadOnlyDictionary<GltfMaterialHandle, string?>(bindings);
    }
  }
}
