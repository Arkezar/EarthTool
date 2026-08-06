#nullable enable

using EarthTool.GLTF;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace EarthTool.CLI.Commands.MSH;

internal abstract class GltfCommandSettings : CommandSettings
{
  [CommandOption("-o|--output <DIRECTORY>")]
  [Description("Destination directory. Defaults to the input directory.")]
  public string? OutputDirectory { get; init; }

  [CommandOption("--report <FILE>")]
  [Description("Optional versioned machine report path.")]
  public string? ReportPath { get; init; }
}

internal sealed class ExportGltfSettings : GltfCommandSettings
{
  [CommandArgument(0, "<INPUT>")]
  [Description("One or more concrete input files or file patterns.")]
  public string[] Inputs { get; init; } = [];

  [CommandOption("--format <FORMAT>")]
  [Description("Package form: Glb (default) or Gltf.")]
  [DefaultValue(GltfPackageKind.Glb)]
  public GltfPackageKind Format { get; init; } = GltfPackageKind.Glb;

  [CommandOption("--tex-root <DIRECTORY>")]
  [Description("Ordered absolute TEX preview search root. Repeat to add roots.")]
  public string[] TextureSearchRoots { get; init; } = [];

  [CommandOption("--msh-root <DIRECTORY>")]
  [Description("Ordered absolute MSH preview search root. Repeat to add roots.")]
  public string[] MeshResourceSearchRoots { get; init; } = [];
}

internal sealed class ImportGltfSettings : GltfCommandSettings
{
  [CommandArgument(0, "<INPUT>")]
  [Description("One or more concrete input files or file patterns.")]
  public string[] Inputs { get; init; } = [];

  [CommandOption("--plan <FILE>")]
  [Description("Optional version-2 typed import plan.")]
  public string? PlanPath { get; init; }
}
