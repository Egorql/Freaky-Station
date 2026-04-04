using System.IO;
using Content.Shared._FreakyStation.ERP;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using YamlDotNet.RepresentationModel;

namespace Content.Tests.Shared._FreakyStation.ERP;

[TestFixture, TestOf(typeof(ERPPrototype))]
public sealed class ERPPrototypeTests : ContentUnitTest
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        IoCManager.Resolve<ISerializationManager>().Initialize();
    }

    [Test]
    public void DeserializeLegacyFieldsIntoPrimaryModel()
    {
        var prototype = ReadPrototype("""
- type: interaction
  id: LegacyInteraction
  name: Legacy Interaction
  lovePercent: 12
""");

        Assert.That(prototype.ArousalDelta, Is.EqualTo(12));
    }

    [Test]
    public void DeserializePrimaryFieldsIntoPrimaryModel()
    {
        var prototype = ReadPrototype("""
- type: interaction
  id: NewInteraction
  name: New Interaction
  arousalDelta: 18
""");

        Assert.That(prototype.ArousalDelta, Is.EqualTo(18));
    }

    private static ERPPrototype ReadPrototype(string yaml)
    {
        using TextReader stream = new StringReader(yaml);

        var yamlStream = new YamlStream();
        yamlStream.Load(stream);
        var document = yamlStream.Documents[0];
        var rootNode = (YamlSequenceNode) document.RootNode;
        var proto = (YamlMappingNode) rootNode[0];
        var serializationManager = IoCManager.Resolve<ISerializationManager>();

        return serializationManager.Read<ERPPrototype>(new MappingDataNode(proto));
    }
}
