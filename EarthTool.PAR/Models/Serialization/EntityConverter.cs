using EarthTool.PAR.Models.Abstracts;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models.Serialization
{
  public class EntityConverter : JsonConverter<Entity>
  {
    public override Entity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      Utf8JsonReader tempReader = reader;
      TypeReader typeReader = JsonSerializer.Deserialize<TypeReader>(ref tempReader, options);
      Type concreteType = Type.GetType(typeReader.TypeName);
      return (Entity)JsonSerializer.Deserialize(ref reader, concreteType, options);
    }

    public override void Write(Utf8JsonWriter writer, Entity value, JsonSerializerOptions options)
    {
      JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }

    private class TypeReader
    {
      [JsonPropertyName("$type")]
      public string TypeName { get; set; }
    }
  }
}
