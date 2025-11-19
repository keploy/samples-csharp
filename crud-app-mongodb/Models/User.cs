using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UserDataAPI.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }  // MongoDB ObjectId

    [BsonElement("Name")]
    public string? Name { get; set; }

    [BsonElement("Age")]
    public int Age { get; set; }
}
