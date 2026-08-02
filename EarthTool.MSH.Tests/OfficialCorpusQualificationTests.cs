using AwesomeAssertions;
using EarthTool.GLTF;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Services;
using System.Numerics;
using System.Text.Json;

namespace EarthTool.MSH.Tests;

public class OfficialCorpusQualificationTests
{
  [Fact]
  public async Task UnchangedSpotLightDirectionsIgnoreQuaternionRoundTripRounding()
  {
    var changedHeadings = new List<byte>();
    for (var heading = 0; heading <= byte.MaxValue; heading++)
    {
      var source = StaticLightMshFixture.Create(
        new Dictionary<int, StaticLightMshFixture.SpotRecord>
        {
          [1] = new(
            Vector3.Zero,
            Vector3.One,
            1,
            (byte)heading,
            [0, 0, 0],
            0.1f,
            0.2f,
            0,
            1)
        },
        activeSpots: [1]);
      var read = await new MshReader().ReadAsync(new MemoryStream(source));
      var glb = new MemoryStream();
      var interchange = new GltfInterchange();
      var export = await interchange.ExportGlbAsync(
        (StaticMeshAsset)read.Value!,
        glb,
        new GltfExportOptions(
          new Guid("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee"),
          new Guid("11111111-2222-4333-8444-555555555555")));
      var import = await interchange.ImportEditGlbAsync(
        new MemoryStream(glb.ToArray()),
        export.Value!.Baseline);
      if (import.Value!.Preservation.Changes.Any(change =>
        change.FieldPath.EndsWith(".Direction", StringComparison.Ordinal)
        && change.Disposition != EarthTool.MSH.Authoring.PreservationDisposition.Retained))
      {
        changedHeadings.Add((byte)heading);
      }
    }

    changedHeadings.Should().BeEmpty();
  }

  [Fact]
  public async Task UnchangedAnimationIgnoresQuaternionRenormalizationRounding()
  {
    var rotation = Quaternion.Normalize(new Quaternion(
      -0.3707679f,
      0.22968513f,
      0.28517193f,
      0.46527398f));
    var source = StaticAnimationMshFixture.Create(
      0,
      new StaticAnimationMshFixture.AnimationLengths(1, 0, 0, 0),
      matrices: [Matrix4x4.CreateFromQuaternion(rotation)]);
    var read = await new MshReader().ReadAsync(new MemoryStream(source));
    var glb = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      (StaticMeshAsset)read.Value!,
      glb,
      new GltfExportOptions(
        new Guid("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee"),
        new Guid("11111111-2222-4333-8444-555555555555")));

    var import = await interchange.ImportEditGlbAsync(
      new MemoryStream(glb.ToArray()),
      export.Value!.Baseline);

    import.Succeeded.Should().BeTrue();
    import.Value!.Preservation.Changes.Should().OnlyContain(change =>
      change.Disposition == EarthTool.MSH.Authoring.PreservationDisposition.Retained);
    import.Value.Asset.GetSerializedRepresentation().Should().Equal(source);
  }

  [Fact]
  public void CorpusFingerprintIgnoresPathAndInputOrder()
  {
    var first = OfficialCorpusQualification.ComputeCorpusFingerprint([
      [1, 2, 3],
      [4, 5]
    ]);
    var second = OfficialCorpusQualification.ComputeCorpusFingerprint([
      [4, 5],
      [1, 2, 3]
    ]);

    first.Should().Be(second);
  }

  [Fact]
  public async Task SemanticDigestIgnoresNonserializedLineageIdentity()
  {
    var bytes = OneTriangleMshFixture.Create();
    var first = await new MshReader().ReadAsync(new MemoryStream(bytes));
    var second = await new MshReader().ReadAsync(new MemoryStream(bytes));

    OfficialCorpusQualification.ComputeSemanticDigest(first.Value!)
      .Should().Be(OfficialCorpusQualification.ComputeSemanticDigest(second.Value!));
  }

  [Fact]
  public async Task SyntheticCorpusRunsEveryOracleAndEmitsOnlyAggregateEvidence()
  {
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
      var privateName = "private-fixture-name.MSH";
      await File.WriteAllBytesAsync(
        Path.Combine(directory, privateName),
        OneTriangleMshFixture.Create());
      await File.WriteAllBytesAsync(Path.Combine(directory, "not-framed.msh"), [0, 1, 2, 3]);
      var eventPath = Path.Combine(directory, "aggregate.json");

      await OfficialCorpusQualification.RunAsync(directory, eventPath);

      var json = await File.ReadAllTextAsync(eventPath);
      json.Should().NotContain(privateName);
      using var document = JsonDocument.Parse(json);
      document.RootElement.GetProperty("corpus").GetProperty("assets").GetInt32().Should().Be(1);
      document.RootElement.GetProperty("corpus").GetProperty("discoveredMshFiles").GetInt32().Should().Be(2);
      document.RootElement.GetProperty("corpus").GetProperty("excludedNonFramedOrUnsupported")
        .GetInt32().Should().Be(1);
      document.RootElement.GetProperty("corpus").GetProperty("staticAssets").GetInt32().Should().Be(1);
      document.RootElement.GetProperty("operations").GetArrayLength().Should().Be(23);
      document.RootElement.GetProperty("failures").GetProperty("total").GetInt32().Should().Be(0);
    }
    finally
    {
      Directory.Delete(directory, recursive: true);
    }
  }

  [Fact]
  public async Task EmptyCorpusFailsAfterWritingAPathFreeAggregateEvent()
  {
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
      var eventPath = Path.Combine(directory, "aggregate.json");
      Func<Task> qualify = () => OfficialCorpusQualification.RunAsync(directory, eventPath);

      await qualify.Should().ThrowAsync<Xunit.Sdk.XunitException>();

      var json = await File.ReadAllTextAsync(eventPath);
      json.Should().Contain("empty-corpus").And.NotContain(directory);
    }
    finally
    {
      Directory.Delete(directory, recursive: true);
    }
  }

  [Fact]
  public async Task MissingCorpusFailsAfterWritingAPathFreeAggregateEvent()
  {
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var eventPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "aggregate.json");
    try
    {
      Func<Task> qualify = () => OfficialCorpusQualification.RunAsync(directory, eventPath);

      await qualify.Should().ThrowAsync<Xunit.Sdk.XunitException>();

      var json = await File.ReadAllTextAsync(eventPath);
      json.Should().Contain("corpus-discovery-failure").And.NotContain(directory);
    }
    finally
    {
      var eventDirectory = Path.GetDirectoryName(eventPath)!;
      if (Directory.Exists(eventDirectory))
      {
        Directory.Delete(eventDirectory, recursive: true);
      }
    }
  }

  [Fact]
  public async Task MalformedMshFailsThePublishedCliOracleWithoutReturningPrivateOutput()
  {
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
      var result = await OfficialCorpusCliOracle.RunAsync(
        [0, 1, 2, 3],
        "glb",
        directory,
        directory);

      result.ExportSucceeded.Should().BeFalse();
      result.ImportSucceeded.Should().BeFalse();
      result.ExportDiagnostics.Should().OnlyContain(diagnostic =>
        diagnostic.Code.StartsWith("ET", StringComparison.Ordinal));
    }
    finally
    {
      Directory.Delete(directory, recursive: true);
    }
  }

  [Fact]
  [Trait("Category", "OfficialCorpusQualification")]
  public Task OfficialCorpusPassesEveryRequiredOracle()
  {
    return OfficialCorpusQualification.RunAsync();
  }
}
