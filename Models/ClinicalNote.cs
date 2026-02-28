using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace backend.Models
{
    public class ClinicalNote
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string PatientId { get; set; } = string.Empty;

        public string DoctorId { get; set; } = string.Empty;

        public string? DoctorAudioUri { get; set; }
        public string RawDictation { get; set; } = string.Empty;

        public string SoapSubjective { get; set; } = string.Empty;
        public string SoapObjective { get; set; } = string.Empty;
        public string SoapAssessment { get; set; } = string.Empty;
        public string SoapPlan { get; set; } = string.Empty;

        public string PatientFriendlyExplanation { get; set; } = string.Empty;
        public bool IsReadByPatient { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
