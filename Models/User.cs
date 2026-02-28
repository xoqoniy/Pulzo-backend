using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace backend.Models
{
    public enum Role { Patient, Doctor}
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // "Patient" or "Doctor"

    }
}
