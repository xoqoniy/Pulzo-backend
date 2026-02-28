using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace backend.Models
{
    public class DiaryEntry
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string PatientId { get; set; } = string.Empty;
        public string? AudioFileUri { get; set; }
        public string RawText { get; set; } = string.Empty;

        public int? MoodScore { get; set; }
        public int? StressLevel { get; set; }
        public int? EnergyLevel { get; set; }

        public string Category { get; set; } = "General";
        public string AiPatientFeedback { get; set; } = string.Empty;
        public string? TrendWarning { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}