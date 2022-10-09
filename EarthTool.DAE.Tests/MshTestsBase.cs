using Autofac;
using AutoFixture;
using AutoFixture.Kernel;
using EarthTool.Common;
using EarthTool.Common.Interfaces;
using EarthTool.Common.Models;
using EarthTool.MSH;
using EarthTool.MSH.Interfaces;
using EarthTool.MSH.Models;
using EarthTool.MSH.Models.Collections;
using EarthTool.MSH.Models.Elements;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text;

namespace EarthTool.DAE.Tests;

public class MshTestsBase
{
  public Fixture Fixture { get; }
  public IReader<IMesh> MeshReader { get; }
  public IWriter<IMesh> MeshWriter { get; }

  public MshTestsBase()
  {
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    Fixture = new Fixture();
    Fixture.Customizations.Add(new TypeRelay(typeof(IMesh), typeof(EarthMesh)));
    Fixture.Customizations.Add(new TypeRelay(typeof(IMeshDescriptor), typeof(MeshDescriptor)));
    Fixture.Customizations.Add(new TypeRelay(typeof(IEarthInfo), typeof(EarthInfo)));
    Fixture.Customizations.Add(new TypeRelay(typeof(IModelPart), typeof(ModelPart)));

    Fixture.Customizations.Add(new TypeRelay(typeof(IAnimations), typeof(Animations)));
    Fixture.Customizations.Add(new TypeRelay(typeof(IRotationFrame), typeof(RotationFrame)));
    Fixture.Customizations.Add(new TypeRelay(typeof(IFace), typeof(Face)));
    Fixture.Customizations.Add(new TypeRelay(typeof(ITextureInfo), typeof(TextureInfo)));
    Fixture.Customizations.Add(new TypeRelay(typeof(IVertex), typeof(Vertex)));
    Fixture.Customizations.Add(new TypeRelay(typeof(IUVMap), typeof(UVMap)));

    Fixture.Customizations.Add(new TypeRelay(typeof(IModelTemplate), typeof(ModelTemplate)));
    Fixture.Customizations.Add(new TypeRelay(typeof(IMeshFrames), typeof(MeshFrames)));
    Fixture.Customizations.Add(new TypeRelay(typeof(IVector), typeof(Vector)));
    Fixture.Customizations.Add(new TypeRelay(typeof(ISpotLight), typeof(SpotLight)));
    Fixture.Customizations.Add(new TypeRelay(typeof(IOmniLight), typeof(OmniLight)));
    Fixture.Customizations.Add(new TypeRelay(typeof(IModelSlots), typeof(ModelSlots)));
    Fixture.Customizations.Add(new TypeRelay(typeof(ISlot), typeof(Slot)));
    Fixture.Customizations.Add(new TypeRelay(typeof(ITemplateDetails), typeof(TemplateDetails)));
    Fixture.Customizations.Add(new TypeRelay(typeof(IMeshBoundries), typeof(MeshBoundries)));

    Fixture.Customize<EarthMesh>(c => c.Without(p => p.PartsTree));

    var hierarchyBuilderMock = new Mock<IHierarchyBuilder>();

    var cb = new ContainerBuilder();
    cb.RegisterModule<CommonModule>();
    cb.RegisterModule<MSHModule>();
    cb.RegisterInstance(hierarchyBuilderMock);
    cb.RegisterInstance(new NullLoggerFactory()).AsImplementedInterfaces();
    cb.RegisterGeneric(typeof(NullLogger<>)).As(typeof(ILogger<>)).SingleInstance();
    var container = cb.Build();

    MeshReader = container.Resolve<IReader<IMesh>>();
    MeshWriter = container.Resolve<IWriter<IMesh>>();
  }


  [Fact]
  public void MeshShouldBeCorrectlySerialized()
  {
    var testMesh = Fixture.Create<IMesh>();
    var filename = "test.msh";

    var exportedFile = MeshWriter.Write(testMesh, Path.Combine(Environment.CurrentDirectory, filename));
    var loadedMesh = MeshReader.Read(exportedFile);

    loadedMesh.Should().BeEquivalentTo(testMesh);
  }
}