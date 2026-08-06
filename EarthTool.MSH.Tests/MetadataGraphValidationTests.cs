using AwesomeAssertions;
using EarthTool.GLTF;
using System.Text.Json;

namespace EarthTool.MSH.Tests;

public class MetadataGraphValidationTests
{
  private static readonly Guid _lineageId = new("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee");
  private static readonly Guid _documentId = new("11111111-2222-4333-8444-555555555555");

  [Fact]
  public void MetadataIdentitiesRequireVersionFourUuids()
  {
    var versionOne = Guid.Parse("11111111-2222-1333-8444-555555555555");

    var createOptions = () => new GltfExportOptions(versionOne, _documentId);
    var createBaseline = () => new InterchangeBaseline(_lineageId, versionOne);

    createOptions.Should().Throw<ArgumentException>();
    createBaseline.Should().Throw<ArgumentException>();
  }

  [Fact]
  public void PreservedUnknownMetadataRequiresAnAdditivePointerAndValidRawJson()
  {
    var knownMember = () =>
      new GltfExportOptions(
        _lineageId,
        _documentId,
        preservedUnknownMetadata: new Dictionary<string, string> { ["manifest:0:/format"] = "1" }
      );
    var invalidJson = () =>
      new GltfExportOptions(
        _lineageId,
        _documentId,
        preservedUnknownMetadata: new Dictionary<string, string> { ["manifest:0:/future"] = "{" }
      );
    var noncanonicalScope = () =>
      new GltfExportOptions(
        _lineageId,
        _documentId,
        preservedUnknownMetadata: new Dictionary<string, string> { ["object:01:/future"] = "1" }
      );

    knownMember.Should().Throw<ArgumentException>();
    invalidJson.Should().Throw<ArgumentException>();
    noncanonicalScope.Should().Throw<ArgumentException>();
  }

  [Fact]
  public void MetadataConflictCatalogMatchesSerializedApproval()
  {
    var actual =
      JsonSerializer
        .Serialize(
          GltfMetadataConflictCatalog.ActionsByCode,
          new JsonSerializerOptions { WriteIndented = true }
        )
        .ReplaceLineEndings("\n") + "\n";
    var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
    var approved = File.ReadAllText(
        Path.Combine(
          root,
          "EarthTool.MSH.Tests",
          "Approvals",
          "gltf-metadata-conflicts.approved.json"
        )
      )
      .ReplaceLineEndings("\n");

    actual.Should().Be(approved);
  }

  [Fact]
  public void MetadataConflictCatalogCoversTheCompleteReservedRange()
  {
    GltfMetadataConflictCatalog
      .ActionsByCode.Keys.Should()
      .Equal(Enumerable.Range(2000, 21).Select(eventId => $"ETG{eventId}"));
    GltfMetadataConflictCatalog
      .ActionsByCode.Values.Should()
      .AllSatisfy(actions =>
      {
        actions.Should().NotBeEmpty();
        actions.Should().OnlyHaveUniqueItems();
      });
  }
}
