using AwesomeAssertions;
using EarthTool.GLTF;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using EarthTool.MSH.Services;
using System.Numerics;
using System.Text.Json;

namespace EarthTool.MSH.Tests;

public class OfficialCorpusQualificationTests
{
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
      var meshesDirectory = Path.Combine(directory, "meshes");
      Directory.CreateDirectory(meshesDirectory);
      var privateNames = new[]
      {
        "private-fixture-name.MSH",
        "second-private-fixture-name.msh",
        "third-private-fixture-name.msh",
        "fourth-private-fixture-name.msh"
      };
      foreach (var privateName in privateNames)
      {
        await File.WriteAllBytesAsync(
          Path.Combine(meshesDirectory, privateName),
          OneTriangleMshFixture.Create());
      }
      var dynamicBuild = DynamicMeshBuilder.Create()
        .SetRoot(DynamicEffectRecipes.Group([DynamicEffectRecipes.Group()]))
        .Build();
      dynamicBuild.TryGetValue(out var dynamicAsset).Should().BeTrue();
      var dynamicPath = Path.Combine(meshesDirectory, "dynamic-private-fixture-name.msh");
      var dynamicWrite = await new MshWriter().WriteFileAsync(dynamicAsset!, dynamicPath);
      dynamicWrite.Succeeded.Should().BeTrue();
      await File.WriteAllBytesAsync(Path.Combine(directory, "not-framed.msh"), [0, 1, 2, 3]);
      var eventPath = Path.Combine(directory, "aggregate.json");
      var profilePath = Path.Combine(directory, "profile.json");

      await OfficialCorpusQualification.RunAsync(
        directory,
        eventPath,
        workerCount: 2,
        profilePath);
      var serialEventPath = Path.Combine(directory, "serial-aggregate.json");
      await OfficialCorpusQualification.RunAsync(directory, serialEventPath, workerCount: 1);

      var json = await File.ReadAllTextAsync(eventPath);
      json.Should().Be(await File.ReadAllTextAsync(serialEventPath));
      foreach (var privateName in privateNames)
      {
        json.Should().NotContain(privateName);
      }
      using var document = JsonDocument.Parse(json);
      document.RootElement.GetProperty("version").GetInt32().Should().Be(2);
      document.RootElement.GetProperty("corpus").GetProperty("assets").GetInt32().Should().Be(5);
      document.RootElement.GetProperty("corpus").GetProperty("discoveredMshFiles").GetInt32().Should().Be(6);
      document.RootElement.GetProperty("corpus").GetProperty("excludedNonFramedOrUnsupported")
        .GetInt32().Should().Be(1);
      document.RootElement.GetProperty("corpus").GetProperty("staticAssets").GetInt32().Should().Be(4);
      document.RootElement.GetProperty("corpus").GetProperty("dynamicAssets").GetInt32().Should().Be(1);
      var dynamicCoverage = document.RootElement.GetProperty("dynamicCoverage");
      dynamicCoverage.GetProperty("assets").GetInt32().Should().Be(1);
      dynamicCoverage.GetProperty("objects").GetInt32().Should().Be(2);
      dynamicCoverage.GetProperty("maximumDepth").GetInt32().Should().Be(2);
      dynamicCoverage.GetProperty("nestedAssets").GetInt32().Should().Be(1);
      dynamicCoverage.GetProperty("effectTypes").EnumerateArray().Should().ContainSingle(item =>
        item.GetProperty("effectType").GetString() == "Group"
        && item.GetProperty("count").GetInt32() == 2);
      var exportAllMeshes = document.RootElement.GetProperty("exportAllMeshes");
      exportAllMeshes.GetProperty("assets").GetInt32().Should().Be(5);
      exportAllMeshes.GetProperty("staticAssets").GetInt32().Should().Be(4);
      exportAllMeshes.GetProperty("dynamicAssets").GetInt32().Should().Be(1);
      exportAllMeshes.GetProperty("succeeded").GetInt32().Should().Be(5);
      exportAllMeshes.GetProperty("failed").GetInt32().Should().Be(0);
      exportAllMeshes.GetProperty("cancelled").GetInt32().Should().Be(0);
      exportAllMeshes.GetProperty("unsupportedDomainDiagnostics").GetInt32().Should().Be(0);
      exportAllMeshes.GetProperty("outputFiles").GetInt32().Should().Be(5);
      document.RootElement.GetProperty("operations").GetArrayLength().Should().Be(23);
      document.RootElement.GetProperty("operations").EnumerateArray().Should().OnlyContain(operation =>
        operation.GetProperty("attempted").GetInt32() == 5
        && operation.GetProperty("passed").GetInt32() == 5
        && operation.GetProperty("failed").GetInt32() == 0);
      document.RootElement.GetProperty("failures").GetProperty("total").GetInt32().Should().Be(0);

      var profileJson = await File.ReadAllTextAsync(profilePath);
      foreach (var privateName in privateNames)
      {
        profileJson.Should().NotContain(privateName);
      }
      using var profile = JsonDocument.Parse(profileJson);
      profile.RootElement.GetProperty("format").GetString()
        .Should().Be("earthtool.official-msh-corpus-profile-event");
      profile.RootElement.GetProperty("workers").GetInt32().Should().Be(2);
      profile.RootElement.GetProperty("wallClockMilliseconds").GetDouble().Should().BeGreaterThan(0);
      profile.RootElement.GetProperty("stages").EnumerateArray().Should().Contain(stage =>
        stage.GetProperty("stage").GetString() == "glb.cli-export"
        && stage.GetProperty("count").GetInt32() == 5);
      var validatorStarts = profile.RootElement.GetProperty("stages").EnumerateArray().Single(stage =>
        stage.GetProperty("stage").GetString() == "khronos.process-start");
      validatorStarts.GetProperty("count").GetInt32().Should().BeInRange(1, 2);
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
        GltfPackageKind.Glb,
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
