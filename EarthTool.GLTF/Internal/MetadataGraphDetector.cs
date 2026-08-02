#nullable enable

using EarthTool.MSH.Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace EarthTool.GLTF.Internal
{
  internal static class MetadataGraphDetector
  {
    internal static void Detect(
      ParsedGlb parsed,
      MetadataEnvelope manifest,
      StaticMeshAsset asset,
      InterchangeBaseline expected,
      GltfOperationProfile profile,
      MetadataConflictCollector conflicts)
    {
      var nodes = ParseScopes(parsed.Nodes.Select((node, index) =>
        ($"nodes[{index}]", index, node.Metadata)), profile);
      var meshes = ParseScopes(parsed.Meshes.Select((mesh, index) =>
        ($"meshes[{index}]", index, mesh.Metadata)), profile);
      var materials = ParseScopes(parsed.Materials.Select((material, index) =>
        ($"materials[{index}]", index, material.Metadata)), profile);
      var lights = ParseScopes(parsed.Lights.Select((light, index) =>
        ($"extensions.KHR_lights_punctual.lights[{index}]", index, light.Metadata)), profile);
      if (conflicts.IsTruncated)
      {
        return;
      }

      DetectMissingExpectedScopes(parsed, manifest, nodes, meshes, materials, lights, conflicts);
      if (conflicts.IsTruncated)
      {
        return;
      }
      DetectNativeCorrespondence(parsed, manifest, nodes, meshes, conflicts);
      if (conflicts.IsTruncated)
      {
        return;
      }
      DetectDanglingReferences(manifest, nodes, meshes, materials, lights, conflicts);
      if (conflicts.IsTruncated)
      {
        return;
      }
      DetectGuards(meshes.Concat(nodes).Concat(lights), conflicts);
      if (conflicts.IsTruncated)
      {
        return;
      }
      DetectStaleGuards(parsed, nodes, meshes, lights, asset, expected, conflicts);
      if (conflicts.IsTruncated)
      {
        return;
      }
      DetectProvenance(manifest, asset, conflicts);
    }

    private static IReadOnlyList<MetadataScope> ParseScopes(
      IEnumerable<(string Path, int Index, string? Value)> carriers,
      GltfOperationProfile profile)
    {
      return carriers.Where(carrier => carrier.Value is not null)
        .Select(carrier => new MetadataScope(
          carrier.Path,
          carrier.Index,
          GlbDocument.ParseMetadata(carrier.Value!, profile)))
        .ToArray();
    }

    private static void DetectMissingExpectedScopes(
      ParsedGlb parsed,
      MetadataEnvelope manifest,
      IReadOnlyList<MetadataScope> nodes,
      IReadOnlyList<MetadataScope> meshes,
      IReadOnlyList<MetadataScope> materials,
      IReadOnlyList<MetadataScope> lights,
      MetadataConflictCollector conflicts)
    {
      var nodeByIndex = nodes.ToDictionary(scope => scope.Index);
      var meshByIndex = meshes.ToDictionary(scope => scope.Index);
      var materialByIndex = materials.ToDictionary(scope => scope.Index);
      var lightByIndex = lights.ToDictionary(scope => scope.Index);

      for (var meshIndex = 0; meshIndex < parsed.Meshes.Count; meshIndex++)
      {
        if (meshByIndex.ContainsKey(meshIndex))
        {
          continue;
        }
        var expectedId = parsed.Nodes.Select((node, index) => (node, index))
          .Where(item => item.node.MeshIndex == meshIndex && nodeByIndex.ContainsKey(item.index))
          .Select(item => nodeByIndex[item.index].Envelope.LocalId)
          .FirstOrDefault(id => InventoryContains(manifest, "mesh", id));
        if (expectedId > 0)
        {
          conflicts.Add(Conflict(
            GltfDiagnosticCodes.MissingExpectedScope,
            2010,
            $"meshes[{meshIndex}]",
            "An expected mesh metadata scope is absent.",
            "mesh",
            expectedId,
            manifest));
        }
      }

      for (var materialIndex = 0; materialIndex < parsed.Materials.Count; materialIndex++)
      {
        if (materialByIndex.ContainsKey(materialIndex))
        {
          continue;
        }
        var expectedId = meshes.SelectMany(scope =>
          parsed.Meshes[scope.Index].Primitives.Select((primitive, primitiveIndex) =>
            (primitive, primitiveIndex, scope.Envelope)))
          .Where(item => item.primitive.MaterialIndex == materialIndex
            && item.primitiveIndex < item.Envelope.Partitions.Count)
          .Select(item => item.Envelope.Partitions[item.primitiveIndex].LocalId)
          .FirstOrDefault(id => InventoryContains(manifest, "material", id));
        if (expectedId > 0)
        {
          conflicts.Add(Conflict(
            GltfDiagnosticCodes.MissingExpectedScope,
            2010,
            $"materials[{materialIndex}]",
            "An expected material metadata scope is absent.",
            "material",
            expectedId,
            manifest));
        }
      }

      for (var lightIndex = 0; lightIndex < parsed.Lights.Count; lightIndex++)
      {
        if (lightByIndex.ContainsKey(lightIndex))
        {
          continue;
        }
        var expectedId = parsed.Nodes.Select((node, index) => (node, index))
          .Where(item => item.node.LightIndex == lightIndex && nodeByIndex.ContainsKey(item.index))
          .Select(item => nodeByIndex[item.index].Envelope.StaticLightDefinitionLocalId.GetValueOrDefault())
          .FirstOrDefault(id => InventoryContains(manifest, "light", id));
        if (expectedId > 0)
        {
          conflicts.Add(Conflict(
            GltfDiagnosticCodes.MissingExpectedScope,
            2010,
            $"extensions.KHR_lights_punctual.lights[{lightIndex}]",
            "An expected light metadata scope is absent.",
            "light",
            expectedId,
            manifest));
        }
      }
    }

    private static void DetectNativeCorrespondence(
      ParsedGlb parsed,
      MetadataEnvelope manifest,
      IReadOnlyList<MetadataScope> nodes,
      IReadOnlyList<MetadataScope> meshes,
      MetadataConflictCollector conflicts)
    {
      var meshByIndex = meshes.ToDictionary(scope => scope.Index);
      var taggedObjectIds = nodes.Select(scope => scope.Envelope.LocalId).ToHashSet();
      for (var nodeIndex = 0; nodeIndex < parsed.Nodes.Count; nodeIndex++)
      {
        if (parsed.Nodes[nodeIndex].Metadata is not null
          || parsed.Nodes[nodeIndex].MeshIndex is not int meshIndex
          || !meshByIndex.TryGetValue(meshIndex, out var mesh)
          || !InventoryContains(manifest, "object", mesh.Envelope.LocalId)
          || taggedObjectIds.Contains(mesh.Envelope.LocalId))
        {
          continue;
        }
        conflicts.Add(Conflict(
          GltfDiagnosticCodes.AmbiguousPartitionCorrespondence,
          2012,
          $"nodes[{nodeIndex}]",
          "An untagged native object can correspond to a missing expected object scope.",
          "object",
          mesh.Envelope.LocalId,
          manifest));
      }
      foreach (var node in nodes.OrderBy(scope => scope.Path, StringComparer.Ordinal))
      {
        var nodeIndex = node.Index;
        if (parsed.Nodes[nodeIndex].MeshIndex is not int meshIndex
          || !meshByIndex.TryGetValue(meshIndex, out var mesh)
          || node.Envelope.AttachmentRecord is not null
          || node.Envelope.CannonRenderPosition is not null
          || node.Envelope.StaticLightDefinitionLocalId is not null
          || node.Envelope.LocalId == mesh.Envelope.LocalId)
        {
          continue;
        }
        conflicts.Add(Conflict(
          GltfDiagnosticCodes.AmbiguousPartitionCorrespondence,
          2012,
          node.Path,
          "The native object and mesh scopes claim different source-object identities.",
          "object",
          node.Envelope.LocalId,
          node.Envelope,
          new Dictionary<string, string>
          {
            ["meshLocalId"] = mesh.Envelope.LocalId.ToString(
              System.Globalization.CultureInfo.InvariantCulture)
          }));
      }
    }

    private static void DetectDanglingReferences(
      MetadataEnvelope manifest,
      IReadOnlyList<MetadataScope> nodes,
      IReadOnlyList<MetadataScope> meshes,
      IReadOnlyList<MetadataScope> materials,
      IReadOnlyList<MetadataScope> lights,
      MetadataConflictCollector conflicts)
    {
      var materialIds = materials.Select(scope => scope.Envelope.LocalId).ToHashSet();
      var lightIds = lights.Select(scope => scope.Envelope.LocalId).ToHashSet();
      foreach (var mesh in meshes.OrderBy(scope => scope.Path, StringComparer.Ordinal))
      {
        if (conflicts.IsTruncated)
        {
          return;
        }
        for (var partitionIndex = 0; partitionIndex < mesh.Envelope.Partitions.Count; partitionIndex++)
        {
          var localId = mesh.Envelope.Partitions[partitionIndex].LocalId;
          var path = $"{mesh.Path}.payload.partitions[{partitionIndex}].localId";
          if (!InventoryContains(manifest, "material", localId))
          {
            conflicts.Add(Conflict(
              GltfDiagnosticCodes.DanglingMetadataReference,
              2013,
              path,
              "A partition references an unknown material scope.",
              "mesh",
              mesh.Envelope.LocalId,
              mesh.Envelope,
              new Dictionary<string, string> { ["referencedLocalId"] = localId.ToString() }));
            continue;
          }
          if (!materialIds.Contains(localId))
          {
            conflicts.Add(Conflict(
              GltfDiagnosticCodes.DanglingMetadataReference,
              2013,
              path,
              "A partition references a material scope with no envelope.",
              "mesh",
              mesh.Envelope.LocalId,
              mesh.Envelope,
              new Dictionary<string, string>
              {
                ["referencedScopeKind"] = "material",
                ["referencedLocalId"] = localId.ToString(),
                ["nativePath"] = "materials"
              }));
          }
        }
      }

      foreach (var node in nodes.Where(scope => scope.Envelope.StaticLightDefinitionLocalId.HasValue)
        .OrderBy(scope => scope.Path, StringComparer.Ordinal))
      {
        var localId = node.Envelope.StaticLightDefinitionLocalId!.Value;
        if (InventoryContains(manifest, "light", localId) && lightIds.Contains(localId))
        {
          continue;
        }
        conflicts.Add(Conflict(
          GltfDiagnosticCodes.DanglingMetadataReference,
          2013,
          node.Path + ".payload.staticLightInstance.definitionLocalId",
          "A static-light instance references an unknown light definition.",
          "object",
          node.Envelope.LocalId,
          node.Envelope,
          new Dictionary<string, string>
          {
            ["referencedScopeKind"] = "light",
            ["referencedLocalId"] = localId.ToString(),
            ["nativePath"] = "extensions.KHR_lights_punctual.lights"
          }));
      }
    }

    private static void DetectGuards(
      IEnumerable<MetadataScope> scopes,
      MetadataConflictCollector conflicts)
    {
      foreach (var scope in scopes.OrderBy(item => item.Path, StringComparer.Ordinal))
      {
        foreach (var required in RequiredGuards(scope.Envelope))
        {
          var path = $"{scope.Path}.guards.{required.Name}";
          if (!scope.Envelope.GuardProjections.TryGetValue(required.Name, out var guard))
          {
            conflicts.Add(Conflict(
              GltfDiagnosticCodes.MissingRequiredGuard,
              2014,
              path,
              "A required native projection guard is absent.",
              scope.Envelope.ScopeKind,
              scope.Envelope.LocalId,
              scope.Envelope,
              new Dictionary<string, string>
              {
                ["guardName"] = required.Name,
                ["expectedProjection"] = required.Projection,
                ["expectedVersion"] = required.Version.ToString()
              }));
          }
          else if (guard.Projection != required.Projection || guard.Version != required.Version)
          {
            conflicts.Add(Conflict(
              GltfDiagnosticCodes.UnsupportedGuard,
              2015,
              path,
              "The metadata guard projection is unsupported.",
              scope.Envelope.ScopeKind,
              scope.Envelope.LocalId,
              scope.Envelope,
              new Dictionary<string, string>
              {
                ["expectedProjection"] = required.Projection,
                ["expectedVersion"] = required.Version.ToString(),
                ["guardName"] = required.Name,
                ["actualProjection"] = guard.Projection,
                ["actualVersion"] = guard.Version.ToString()
              }));
          }
        }
      }
    }

    private static IEnumerable<(string Name, string Projection, int Version)> RequiredGuards(
      MetadataEnvelope envelope)
    {
      if (envelope.ScopeKind == "mesh")
      {
        yield return ("nativeProjection", "static-geometry", 1);
      }
      else if (envelope.AttachmentRecord is not null)
      {
        yield return ("nativeProjection", "attachment.pose", 1);
      }
      else if (envelope.CannonRenderPosition is not null)
      {
        yield return ("nativeProjection", "cannonRenderPosition.position", 1);
      }
      else if (envelope.ScopeKind == "light")
      {
        foreach (var name in new[]
        {
          "staticLight.pose",
          "staticLight.type",
          "staticLight.color",
          "staticLight.intensity",
          "staticLight.direction",
          "staticLight.cones"
        })
        {
          yield return (name, name, 1);
        }
      }
    }

    private static void DetectStaleGuards(
      ParsedGlb parsed,
      IReadOnlyList<MetadataScope> nodes,
      IReadOnlyList<MetadataScope> meshes,
      IReadOnlyList<MetadataScope> lights,
      StaticMeshAsset asset,
      InterchangeBaseline expected,
      MetadataConflictCollector conflicts)
    {
      foreach (var mesh in meshes.OrderBy(scope => scope.Path, StringComparer.Ordinal))
      {
        if (conflicts.IsTruncated)
        {
          return;
        }
        if (mesh.Envelope.Fingerprint is null
          || mesh.Envelope.FingerprintName != "static-geometry"
          || mesh.Envelope.FingerprintVersion != 1)
        {
          continue;
        }
        var parsedMesh = parsed.Meshes[mesh.Index];
        if (parsedMesh.Primitives.Count != mesh.Envelope.Partitions.Count)
        {
          continue;
        }
        var current = parsedMesh.Primitives.Select((primitive, index) => new GeometryPartition(
          mesh.Envelope.Partitions[index].LocalId,
          primitive.Vertices,
          primitive.Triangles)).ToArray();
        var partitionGuardsMatch = current.Select((partition, index) => string.Equals(
          mesh.Envelope.Partitions[index].Fingerprint,
          StaticGeometryFingerprint.CreatePartition(
            expected,
            partition.LocalId,
            partition.Vertices,
            partition.Triangles),
          StringComparison.Ordinal)).All(matches => matches);
        var actual = StaticGeometryFingerprint.CreateMesh(
          expected,
          mesh.Envelope.LocalId,
          current).Sha256;
        if (partitionGuardsMatch && !string.Equals(mesh.Envelope.Fingerprint, actual, StringComparison.Ordinal))
        {
          conflicts.Add(StaleGuard(mesh, "nativeProjection", actual));
        }
      }

      foreach (var node in nodes.OrderBy(scope => scope.Path, StringComparer.Ordinal))
      {
        if (conflicts.IsTruncated)
        {
          return;
        }
        string? actual = null;
        if (node.Envelope.AttachmentPhysicalNumber.HasValue
          && node.Envelope.AttachmentRecord?.Count == 8)
        {
          var attachmentNumber = GlbDocument.GetAttachmentPhysicalNumber(
            GlbDocument.GetFirstArtistObjectLocalId(asset),
            node.Envelope.LocalId);
          actual = GlbDocument.CreateAttachmentPoseFingerprint(
            expected,
            node.Envelope.LocalId,
            attachmentNumber,
            node.Envelope.AttachmentRecord);
        }
        else if (node.Envelope.CannonRenderPositionNumber is int cannonNumber
          && node.Envelope.CannonRenderPosition?.Count == 12)
        {
          actual = GlbDocument.CreateCannonRenderPositionFingerprint(
            expected,
            node.Envelope.LocalId,
            cannonNumber,
            node.Envelope.CannonRenderPosition);
        }
        if (actual is not null
          && node.Envelope.Fingerprint is not null
          && !string.Equals(node.Envelope.Fingerprint, actual, StringComparison.Ordinal))
        {
          conflicts.Add(StaleGuard(node, "nativeProjection", actual));
        }
      }

      var lightInstanceGroups = nodes.Where(scope => scope.Envelope.StaticLightDefinitionLocalId.HasValue)
        .GroupBy(scope => scope.Envelope.StaticLightDefinitionLocalId!.Value)
        .ToArray();
      foreach (var duplicate in lightInstanceGroups.Where(group =>
        group.Select(scope => scope.Envelope.LocalId).Distinct().Count() > 1))
      {
        if (conflicts.IsTruncated)
        {
          return;
        }
        var conflicting = duplicate.OrderBy(scope => scope.Path, StringComparer.Ordinal).Skip(1).First();
        conflicts.Add(Conflict(
          GltfDiagnosticCodes.AmbiguousPartitionCorrespondence,
          2012,
          conflicting.Path + ".payload.staticLightInstance.definitionLocalId",
          "More than one native object claims the same static-light definition.",
          "object",
          conflicting.Envelope.LocalId,
          conflicting.Envelope,
          new Dictionary<string, string>
          {
            ["definitionLocalId"] = duplicate.Key.ToString(
              System.Globalization.CultureInfo.InvariantCulture)
          }));
      }
      var lightInstances = lightInstanceGroups.ToDictionary(group => group.Key, group => group.First());
      foreach (var light in lights.OrderBy(scope => scope.Path, StringComparer.Ordinal))
      {
        if (conflicts.IsTruncated)
        {
          return;
        }
        if (!lightInstances.TryGetValue(light.Envelope.LocalId, out var instance)
          || light.Envelope.StaticLightType is null
          || light.Envelope.StaticLightPhysicalNumber is not int physicalNumber
          || light.Envelope.StaticLightRecord is null
          || instance.Envelope.StaticLightAttachmentRecord is null)
        {
          continue;
        }
        var expectedGuards = GlbDocument.CreateStaticLightGuards(
          expected,
          light.Envelope.StaticLightType,
          physicalNumber,
          light.Envelope.LocalId,
          light.Envelope.StaticLightRecord.ToArray(),
          instance.Envelope.StaticLightAttachmentRecord.ToArray());
        foreach (var guard in light.Envelope.Guards.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
          if (expectedGuards.TryGetValue(guard.Key, out var actual)
            && !string.Equals(guard.Value, actual, StringComparison.Ordinal))
          {
            conflicts.Add(StaleGuard(light, guard.Key, actual));
          }
        }
      }
    }

    private static MetadataConflictException StaleGuard(
      MetadataScope scope,
      string guardName,
      string actual)
    {
      return Conflict(
        GltfDiagnosticCodes.StaleNativeProjection,
        2016,
        $"{scope.Path}.guards.{guardName}",
        "The native projection guard is stale while its owned native projection is unchanged.",
        scope.Envelope.ScopeKind,
        scope.Envelope.LocalId,
        scope.Envelope,
        new Dictionary<string, string>
        {
          ["guardName"] = guardName,
          ["projection"] = scope.Envelope.GuardProjections.TryGetValue(guardName, out var projection)
            ? projection.Projection
            : string.Empty,
          ["projectionVersion"] = scope.Envelope.GuardProjections.TryGetValue(guardName, out projection)
            ? projection.Version.ToString()
            : string.Empty,
          ["expected"] = scope.Envelope.Guards.TryGetValue(guardName, out var expected)
            ? expected
            : string.Empty,
          ["actual"] = actual
        });
    }

    private static void DetectProvenance(
      MetadataEnvelope manifest,
      StaticMeshAsset asset,
      MetadataConflictCollector conflicts)
    {
      if (manifest.SourceProvenance is null)
      {
        return;
      }
      var serialized = asset.GetSerializedRepresentation().ToArray();
      string digest;
      using (var sha256 = SHA256.Create())
      {
        digest = BitConverter.ToString(sha256.ComputeHash(serialized))
          .Replace("-", string.Empty)
          .ToLowerInvariant();
      }
      if (manifest.SourceProvenance.ByteLength == serialized.Length
        && string.Equals(manifest.SourceProvenance.Sha256, digest, StringComparison.Ordinal))
      {
        return;
      }
      conflicts.Add(Conflict(
        GltfDiagnosticCodes.ProvenanceMismatch,
        2017,
        "scenes[0].payload.origin.source",
        "The informational source provenance contradicts the preserved MSH state.",
        "manifest",
        0,
        manifest,
        new Dictionary<string, string>
        {
          ["expectedByteLength"] = manifest.SourceProvenance.ByteLength.ToString(),
          ["actualByteLength"] = serialized.Length.ToString(),
          ["expectedSha256"] = manifest.SourceProvenance.Sha256,
          ["actualSha256"] = digest
        }));
    }

    private static bool InventoryContains(MetadataEnvelope manifest, string kind, int localId)
    {
      return manifest.ScopeInventory.TryGetValue(kind, out var ids) && ids.Contains(localId);
    }

    private static MetadataConflictException Conflict(
      string code,
      int eventId,
      string path,
      string message,
      string scopeKind,
      int localId,
      MetadataEnvelope identity,
      IReadOnlyDictionary<string, string>? additionalData = null)
    {
      var metadataSeparator = path.IndexOf(".payload", StringComparison.Ordinal);
      var guardSeparator = path.IndexOf(".guards", StringComparison.Ordinal);
      var separator = metadataSeparator < 0
        ? guardSeparator
        : guardSeparator < 0 ? metadataSeparator : Math.Min(metadataSeparator, guardSeparator);
      var carrierPath = separator < 0 ? path : path.Substring(0, separator);
      var data = new Dictionary<string, string>(StringComparer.Ordinal)
      {
        ["lineage"] = identity.AssetLineageId.ToString("D"),
        ["document"] = identity.DocumentId.ToString("D"),
        ["scopeKind"] = scopeKind,
        ["localId"] = localId.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ["carrierType"] = scopeKind switch
        {
          "manifest" => "scene",
          "object" => "node",
          _ => scopeKind
        },
        ["metadataPath"] = path,
        ["nativePath"] = carrierPath,
        ["affectedPayloadPaths"] = path
      };
      if (additionalData is not null)
      {
        foreach (var item in additionalData)
        {
          data[item.Key] = item.Value;
        }
      }
      return new MetadataConflictException(
        code,
        eventId,
        path,
        message,
        data,
        GltfMetadataConflictCatalog.ActionsByCode[code].ToArray());
    }

    private sealed class MetadataScope
    {
      internal string Path { get; }

      internal int Index { get; }

      internal MetadataEnvelope Envelope { get; }

      internal MetadataScope(string path, int index, MetadataEnvelope envelope)
      {
        Path = path;
        Index = index;
        Envelope = envelope;
      }
    }

  }

  internal sealed class MetadataConflictCollector
  {
    private readonly int _maximum;
    private readonly List<MetadataConflictException> _conflicts = new List<MetadataConflictException>();
    private bool _truncated;
    private IReadOnlyList<MetadataConflictException>? _result;

    internal bool IsTruncated => _truncated;

    internal MetadataConflictCollector(int maximum)
    {
      _maximum = maximum;
    }

    internal void Add(MetadataConflictException conflict)
    {
      if (_conflicts.Count < _maximum)
      {
        _conflicts.Add(conflict);
      }
      else
      {
        _truncated = true;
      }
    }

    internal IReadOnlyList<MetadataConflictException> Build()
    {
      if (_result is not null)
      {
        return _result;
      }
      if (!_truncated)
      {
        _result = _conflicts.AsReadOnly();
        return _result;
      }
      _conflicts.RemoveAt(_conflicts.Count - 1);
      _conflicts.Add(new MetadataConflictException(
        GltfDiagnosticCodes.TooManyMetadataConflicts,
        2019,
        "metadata",
        "The bounded metadata conflict inventory was truncated.",
        new Dictionary<string, string>
        {
          ["maximum"] = _maximum.ToString(System.Globalization.CultureInfo.InvariantCulture)
        },
        GltfMetadataConflictCatalog.ActionsByCode[GltfDiagnosticCodes.TooManyMetadataConflicts].ToArray()));
      _result = _conflicts.AsReadOnly();
      return _result;
    }
  }
}
