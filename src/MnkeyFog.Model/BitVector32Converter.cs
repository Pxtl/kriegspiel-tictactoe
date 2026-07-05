using System.Collections.Specialized;

namespace MnkeyFog.Model;

public class BitVector32Converter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(BitVector32);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        // If the JSON token is an integer, pass it into the BitVector32 constructor
        if (reader.TokenType == JsonToken.Integer)
        {
            int data = serializer.Deserialize<int>(reader);
            return new BitVector32(data);
        }
        
        // Handle null values
        if (reader.TokenType == JsonToken.Null)
        {
            return new BitVector32(0);
        }

        throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing BitVector32.");
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
#pragma warning disable CS8605 // Unboxing a possibly null value.
		BitVector32 bitVector = (BitVector32)value;
#pragma warning restore CS8605 // Unboxing a possibly null value.
		serializer.Serialize(writer, bitVector.Data);
    }
}
