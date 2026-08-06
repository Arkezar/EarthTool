using AwesomeAssertions;
using EarthTool.Common.Operations;
using EarthTool.GLTF;
using EarthTool.GLTF.Internal;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using EarthTool.MSH.Internal;
using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using System.Text.Json.Nodes;

namespace EarthTool.MSH.Tests;

public sealed class CanonicalStaticGltfCreationTests
{
  private static readonly Guid _creationGuid = new("20220220-2222-4222-8222-202202202202");

  [Fact]
  public async Task CompleteStaticSemanticsAssembleWithoutSourceMsh()
  {
    var source = CreateSourceAsset();
    var glb = await ExportCanonicalGlbAsync(source, (root, meshNodes) =>
    {
      SetStaticOwner(
        meshNodes[0],
        1,
        new StaticSourceAuthoringValues(
          roles: GltfStaticObjectRoles.ViewerFaced
        )
      );
      SetStaticOwner(
        meshNodes[1],
        2,
        new StaticSourceAuthoringValues(
          roles: GltfStaticObjectRoles.Barrel,
          barrelMaximumAngle: 37
        )
      );
    });

    var result = GltfInterchange.ImportCanonicalStaticGlb(
      glb,
      new CanonicalStaticGltfCreationOptions(_creationGuid),
      GltfOperationProfile.Default,
      CancellationToken.None
    );

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    var asset = result.Value!;
    asset.Origin.Should().Be(MeshAssetOrigin.Canonical);
    asset.ArchiveFraming.CreationGuid.Should().Be(_creationGuid);
    asset.RootSourceObject.StaticRenderObjects.Should().HaveCount(2);
    asset.RootSourceObject.Children.Should().ContainSingle();
    asset.RootSourceObject.StaticRenderObjects[0].KnownFlags.Should().HaveFlag(
      StaticRenderObjectFlags.ViewerFaced
    );
    var child = asset.RootSourceObject.Children[0];
    child.StaticRenderObjects.Should().ContainSingle();
    child.StaticRenderObjects[0].KnownFlags.Should().HaveFlag(StaticRenderObjectFlags.Barrel);
    child.StaticRenderObjects[0].BarrelMaximumAngle.Should().Be(37);
    child.StaticRenderObjects[0].Pivot.Should().Be(new Vector3(4, -6, 5));
    child.StaticRenderObjects[0].AnimationClassValue.Should().Be((uint)StaticAnimationClass.B);
    child.StaticRenderObjects[0].AnimationTracks.ScaleFrames.Should().HaveCount(2);
    child.StaticRenderObjects[0].AnimationTracks.TranslationFrames.Should().HaveCount(2);
    child.StaticRenderObjects[0].AnimationTracks.Matrices.Should().HaveCount(2);
  }

  [Fact]
  public async Task DuplicateStaticIdentifiersFailBeforeAssembly()
  {
    var glb = await ExportCanonicalGlbAsync(CreateSourceAsset(), (root, meshNodes) =>
    {
      SetStaticOwner(meshNodes[0], 1, new StaticSourceAuthoringValues());
      SetStaticOwner(meshNodes[1], 1, new StaticSourceAuthoringValues());
    });

    var result = GltfInterchange.ImportCanonicalStaticGlb(
      glb,
      new CanonicalStaticGltfCreationOptions(_creationGuid),
      GltfOperationProfile.Default,
      CancellationToken.None
    );

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle(item =>
      item.Code == GltfAuthoringMetadataDiagnosticCodes.DuplicateOwner
    );
  }

  [Fact]
  public async Task TextureIdentityComesOnlyFromExplicitCreationBindings()
  {
    const string sourceKey = "Textures\\source-name.tex";
    const string explicitKey = "Textures\\explicit-binding.tex";
    var glb = await ExportCanonicalGlbAsync(
      CreateSourceAsset(sourceKey),
      (root, meshNodes) =>
      {
        SetStaticOwner(meshNodes[0], 1, new StaticSourceAuthoringValues());
        SetStaticOwner(meshNodes[1], 2, new StaticSourceAuthoringValues());
        root["materials"]![0]!["name"] = sourceKey;
        root["images"]![0]!["name"] = sourceKey;
      }
    );

    var withoutBinding = GltfInterchange.ImportCanonicalStaticGlb(
      glb,
      new CanonicalStaticGltfCreationOptions(_creationGuid),
      GltfOperationProfile.Default,
      CancellationToken.None
    );
    var withBinding = GltfInterchange.ImportCanonicalStaticGlb(
      glb,
      new CanonicalStaticGltfCreationOptions(
        _creationGuid,
        new Dictionary<GltfMaterialHandle, string?>
        {
          [new GltfMaterialHandle(1)] = explicitKey,
        }
      ),
      GltfOperationProfile.Default,
      CancellationToken.None
    );

    withoutBinding.Status.Should().Be(OperationStatus.Failed);
    withoutBinding.Diagnostics.Should().Contain(item =>
      item.Code == GltfDiagnosticCodes.TextureResourceBindingRequired
    );
    withBinding.Status.Should().Be(
      OperationStatus.Succeeded,
      Diagnostics(withBinding.Diagnostics)
    );
    Encoding.ASCII.GetString(
      withBinding.Value!.StaticRenderObjectSequence[0].TexturePathBytes.ToArray()
    ).Should().Be(explicitKey);
  }

  [Fact]
  public async Task InvalidOptionalTypedValuesDefaultWithWarnings()
  {
    var glb = await ExportCanonicalGlbAsync(CreateSourceAsset(), (root, meshNodes) =>
    {
      meshNodes[0]["name"] = "ET_Static_1";
      meshNodes[0]["extras"] = new JsonObject
      {
        ["earthtool"] = "{\"format\":\"earthtool.msh.authoring\",\"version\":1,"
          + "\"values\":{\"role\":{\"viewerFaced\":true}}}",
      };
      SetStaticOwner(meshNodes[1], 2, new StaticSourceAuthoringValues());
    });

    var result = GltfInterchange.ImportCanonicalStaticGlb(
      glb,
      new CanonicalStaticGltfCreationOptions(_creationGuid),
      GltfOperationProfile.Default,
      CancellationToken.None
    );

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    result.Value!.RootSourceObject.StaticRenderObjects[0].KnownFlags.Should().Be(
      StaticRenderObjectFlags.None
    );
    result.Diagnostics.Should().Contain(item =>
      item.Code == GltfAuthoringMetadataDiagnosticCodes.OptionalValueDefaulted
      && item.Severity == DiagnosticSeverity.Warning
      && item.Path.Contains("role", StringComparison.Ordinal)
    );
  }

  [Fact]
  public async Task PublicCreationEntryPointDoesNotUseHiddenStaticPath()
  {
    var glb = await ExportCanonicalGlbAsync(CreateSourceAsset(), AddCanonicalOwners);
    await using var input = new MemoryStream(glb);

    var result = await new GltfInterchange().CreateMeshAsync(input);

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    var asset = result.Value!.Asset.Should().BeOfType<StaticMeshAsset>().Subject;
    asset.RootSourceObject.StaticRenderObjects[0].KnownFlags.Should().Be(
      StaticRenderObjectFlags.None
    );
  }

  [Fact]
  public async Task RequiredDynamicOwnerFailsInStaticCreation()
  {
    var glb = await ExportCanonicalGlbAsync(CreateSourceAsset(), (root, meshNodes) =>
    {
      AddCanonicalOwners(root, meshNodes);
      var nodes = root["nodes"]!.AsArray();
      var rootIndex = root["scenes"]![0]!["nodes"]![0]!.GetValue<int>();
      nodes[rootIndex]!["name"] = "ET_Dynamic_1_Group";
    });

    var result = GltfInterchange.ImportCanonicalStaticGlb(
      glb,
      new CanonicalStaticGltfCreationOptions(_creationGuid),
      GltfOperationProfile.Default,
      CancellationToken.None
    );

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle(item =>
      item.Code == GltfAuthoringMetadataDiagnosticCodes.RequiredValueMissing
    );
  }

  [Fact]
  public async Task TypedMetadataOutsideNamedNodesIsRejected()
  {
    var glb = await ExportCanonicalGlbAsync(CreateSourceAsset(), AddCanonicalOwners);
    glb = RewriteGlb(glb, root =>
      root["scenes"]![0]!["extras"] = new JsonObject
      {
        ["earthtool"] = "{\"format\":\"earthtool.msh.authoring\",\"version\":1,"
          + "\"values\":{}}",
      }
    );

    var result = GltfInterchange.ImportCanonicalStaticGlb(
      glb,
      new CanonicalStaticGltfCreationOptions(_creationGuid),
      GltfOperationProfile.Default,
      CancellationToken.None
    );

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle(item =>
      item.Code == GltfDiagnosticCodes.OrphanEnvelope
    );
  }

  [Fact]
  public async Task EquivalentGlbAndSeparateGltfProduceIdenticalBytesWithFixedGuid()
  {
    var source = CreateSourceAsset();
    var interchange = new GltfInterchange();
    var glb = await ExportCanonicalGlbAsync(source, AddCanonicalOwners);
    var directory = Path.Combine(Path.GetTempPath(), $"earthtool-static-202-{Guid.NewGuid():N}");
    var path = Path.Combine(directory, "model.gltf");
    Directory.CreateDirectory(directory);
    try
    {
      var exported = await interchange.ExportGltfFileAsync(source, path);
      exported.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(exported.Diagnostics));
      var json = JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject();
      RemoveEarthToolMetadata(json);
      AddCanonicalOwners(json, MeshNodes(json));
      await File.WriteAllTextAsync(path, json.ToJsonString());
      var bufferUri = json["buffers"]![0]!["uri"]!.GetValue<string>();
      var binary = await File.ReadAllBytesAsync(Path.Combine(directory, bufferUri));
      var options = new CanonicalStaticGltfCreationOptions(_creationGuid);

      var glbResult = GltfInterchange.ImportCanonicalStaticGlb(
        glb,
        options,
        GltfOperationProfile.Default,
        CancellationToken.None
      );
      var gltfResult = GltfInterchange.ImportCanonicalStaticSeparate(
        Encoding.UTF8.GetBytes(json.ToJsonString()),
        binary,
        options,
        GltfOperationProfile.Default,
        CancellationToken.None
      );

      glbResult.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(glbResult.Diagnostics));
      gltfResult.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(gltfResult.Diagnostics));
      gltfResult.Value!.GetSerializedRepresentation().Should().Equal(
        glbResult.Value!.GetSerializedRepresentation()
      );
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  private static StaticMeshAsset CreateSourceAsset(string? textureResourceKey = null)
  {
    var root = new CanonicalStaticSourceObject(
      [CreateRenderObject(0, textureResourceKey), CreateRenderObject(10)],
      [
        new CanonicalStaticSourceObject(
          [CreateRenderObject(20)],
          role: new CanonicalStaticObjectRole(
            StaticRenderObjectFlags.Barrel,
            barrelMaximumAngle: 37
          )
        ),
      ],
      new CanonicalStaticObjectRole(StaticRenderObjectFlags.ViewerFaced)
    );
    var frames = new[]
    {
      new ProjectedAnimationFrame(Vector3.One, Quaternion.Identity, Vector3.Zero),
      new ProjectedAnimationFrame(new Vector3(2), Quaternion.Identity, new Vector3(1, 2, 3)),
    };
    var animation = new StaticAnimationReplacement(
      StaticAnimationProjection.CreateCanonicalTracks(frames),
      (uint)StaticAnimationClass.B
    );
    var build = CanonicalStaticMeshAssembler.Assemble(
      new CanonicalStaticMeshAssemblyInput(
        Guid.NewGuid(),
        new CanonicalStaticBaseHeaderInput(
          new AnimationClassBytes(0, 2, 0, 0),
          root.RenderObjects.Concat(root.Children[0].RenderObjects).SelectMany(item =>
            item.RenderVertices
          )
        ),
        root,
        new Dictionary<int, Vector3> { [1] = new Vector3(4, -6, 5) },
        new Dictionary<int, StaticAnimationReplacement> { [1] = animation },
        new Dictionary<int, string?>
        {
          [0] = textureResourceKey,
          [1] = null,
          [2] = null,
        }
      )
    );
    build.TryGetValue(out var asset).Should().BeTrue();
    return asset!;
  }

  private static CanonicalStaticRenderObject CreateRenderObject(
    float x,
    string? textureResourceKey = null
  )
  {
    return new CanonicalStaticRenderObject(
      [
        new CanonicalStaticVertex(new Vector3(x, 0, 0), Vector3.UnitZ, Vector2.Zero),
        new CanonicalStaticVertex(new Vector3(x + 1, 0, 0), Vector3.UnitZ, Vector2.UnitX),
        new CanonicalStaticVertex(new Vector3(x, 1, 0), Vector3.UnitZ, Vector2.UnitY),
      ],
      [new CanonicalTriangle(0, 1, 2)],
      textureResourceKey
    );
  }

  private static async Task<byte[]> ExportCanonicalGlbAsync(
    StaticMeshAsset source,
    Action<JsonObject, IReadOnlyList<JsonObject>> rewrite
  )
  {
    await using var destination = new MemoryStream();
    var exported = await new GltfInterchange().ExportGlbAsync(source, destination);
    exported.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(exported.Diagnostics));
    return RewriteGlb(destination.ToArray(), root =>
    {
      RemoveEarthToolMetadata(root);
      rewrite(root, MeshNodes(root));
    });
  }

  private static void AddCanonicalOwners(
    JsonObject root,
    IReadOnlyList<JsonObject> meshNodes
  )
  {
    SetStaticOwner(
      meshNodes[0],
      1,
      new StaticSourceAuthoringValues(roles: GltfStaticObjectRoles.ViewerFaced)
    );
    SetStaticOwner(
      meshNodes[1],
      2,
      new StaticSourceAuthoringValues(
        roles: GltfStaticObjectRoles.Barrel,
        barrelMaximumAngle: 37
      )
    );
  }

  private static void SetStaticOwner(
    JsonObject node,
    int number,
    StaticSourceAuthoringValues values
  )
  {
    var name = $"ET_Static_{number}";
    node["name"] = name;
    node["extras"] = new JsonObject
    {
      ["earthtool"] = CanonicalAuthoringMetadata.Write(
        CanonicalAuthoringOwner.Parse(name),
        values,
        GltfOperationProfile.Default
      ),
    };
  }

  private static IReadOnlyList<JsonObject> MeshNodes(JsonObject root)
  {
    return root["nodes"]!
      .AsArray()
      .Select(node => node!.AsObject())
      .Where(node => node.ContainsKey("mesh"))
      .ToArray();
  }

  private static void RemoveEarthToolMetadata(JsonNode node)
  {
    if (node is JsonObject @object)
    {
      @object.Remove("earthtool");
      foreach (var child in @object.ToArray())
      {
        if (child.Value is not null)
        {
          RemoveEarthToolMetadata(child.Value);
        }
      }
    }
    else if (node is JsonArray array)
    {
      foreach (var child in array)
      {
        if (child is not null)
        {
          RemoveEarthToolMetadata(child);
        }
      }
    }
  }

  private static byte[] RewriteGlb(byte[] glb, Action<JsonObject> rewrite)
  {
    var jsonLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(12));
    var root = JsonNode.Parse(glb.AsSpan(20, jsonLength))!.AsObject();
    rewrite(root);
    var json = Encoding.UTF8.GetBytes(root.ToJsonString());
    var paddedJsonLength = (json.Length + 3) & ~3;
    var oldBinaryHeader = 20 + jsonLength;
    var binaryLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(oldBinaryHeader));
    var result = new byte[12 + 8 + paddedJsonLength + 8 + binaryLength];
    BinaryPrimitives.WriteUInt32LittleEndian(result, 0x46546C67);
    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(4), 2);
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(8), result.Length);
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(12), paddedJsonLength);
    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(16), 0x4E4F534A);
    json.CopyTo(result.AsSpan(20));
    result.AsSpan(20 + json.Length, paddedJsonLength - json.Length).Fill(0x20);
    var newBinaryHeader = 20 + paddedJsonLength;
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(newBinaryHeader), binaryLength);
    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(newBinaryHeader + 4), 0x004E4942);
    glb.AsSpan(oldBinaryHeader + 8, binaryLength).CopyTo(result.AsSpan(newBinaryHeader + 8));
    return result;
  }

  private static string Diagnostics(IEnumerable<OperationDiagnostic> diagnostics)
  {
    return string.Join(Environment.NewLine, diagnostics.Select(item => item.ToString()));
  }
}
