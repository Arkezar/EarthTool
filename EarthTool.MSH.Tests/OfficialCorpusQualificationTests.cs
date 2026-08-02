using AwesomeAssertions;
using EarthTool.MSH.Services;
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
      var privateName = "private-fixture-name.msh";
      await File.WriteAllBytesAsync(
        Path.Combine(directory, privateName),
        OneTriangleMshFixture.Create());
      var eventPath = Path.Combine(directory, "aggregate.json");

      await OfficialCorpusQualification.RunAsync(directory, eventPath);

      var json = await File.ReadAllTextAsync(eventPath);
      json.Should().NotContain(privateName);
      using var document = JsonDocument.Parse(json);
      document.RootElement.GetProperty("corpus").GetProperty("assets").GetInt32().Should().Be(1);
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
  public async Task MalformedMshFailsThePublishedCliOracleWithoutReturningPrivateOutput()
  {
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
      var result = await OfficialCorpusCliOracle.RunAsync([0, 1, 2, 3], "glb", directory);

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
