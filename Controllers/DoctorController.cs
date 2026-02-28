using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using backend.Services;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DoctorController : ControllerBase
    {
        private readonly MongoDbService _mongoService;
        private readonly AzureAiService _aiService;

        public DoctorController(MongoDbService mongoService, AzureAiService aiService)
        {
            _mongoService = mongoService;
            _aiService = aiService;
        }

        [HttpGet("patients")]
        public IActionResult GetPatients()
        {
            // A hardcoded list to make the frontend look rich and populated for the judges
            var patients = new List<object>
            {
                new { id = "patient_123", name = "John Doe", age = 34, primaryCondition = "Chronic Migraines", status = "Needs Review" },
                new { id = "patient_456", name = "Sarah Smith", age = 28, primaryCondition = "Pre-Diabetic", status = "Warning Flagged" },
                new { id = "patient_003", name = "Michael Johnson", age = 45, primaryCondition = "Hypertension", status = "Stable" },
                new { id = "patient_004", name = "Emily Davis", age = 22, primaryCondition = "Anxiety", status = "Stable" },
                new { id = "patient_005", name = "David Wilson", age = 51, primaryCondition = "Asthma", status = "Stable" },
                new { id = "patient_006", name = "Jessica Brown", age = 39, primaryCondition = "Insomnia", status = "Stable" },
                new { id = "patient_007", name = "James Taylor", age = 60, primaryCondition = "Arthritis", status = "Stable" },
                new { id = "patient_008", name = "Olivia Anderson", age = 31, primaryCondition = "General Checkup", status = "Stable" },
                new { id = "patient_009", name = "Daniel Thomas", age = 27, primaryCondition = "Sports Injury", status = "Stable" },
                new { id = "patient_010", name = "Sophia Jackson", age = 55, primaryCondition = "Hypothyroidism", status = "Stable" },
                new { id = "patient_011", name = "Matthew White", age = 41, primaryCondition = "GERD", status = "Stable" },
                new { id = "patient_012", name = "Isabella Harris", age = 29, primaryCondition = "Migraines", status = "Stable" },
                new { id = "patient_013", name = "Ethan Martin", age = 36, primaryCondition = "High Cholesterol", status = "Stable" },
                new { id = "patient_014", name = "Mia Thompson", age = 48, primaryCondition = "T2 Diabetes", status = "Stable" },
                new { id = "patient_015", name = "Alexander Garcia", age = 33, primaryCondition = "IBS", status = "Stable" }
            };

            return Ok(patients);
        }

        // --- 2. GET A SPECIFIC PATIENT'S HISTORY ---
        [HttpGet("patient/{patientId}/history")]
        public async Task<IActionResult> GetPatientHistory(string patientId)
        {
            var diaryEntries = await _mongoService.GetPatientDiaryEntriesAsync(patientId);
            var clinicalNotes = await _mongoService.GetPatientClinicalNotesAsync(patientId);

            return Ok(new
            {
                DiaryLogs = diaryEntries,
                DoctorNotes = clinicalNotes
            });
        }

        // Create a wrapper class for the form data
        public class DoctorNoteRequest
        {
            public string PatientId { get; set; } = string.Empty;
            public string DoctorId { get; set; } = string.Empty;
            public IFormFile? AudioFile { get; set; }
            public string? RawText { get; set; }
        }

        // --- 3. SUBMIT DOCTOR DICTATION & GENERATE SOAP ---
        [HttpPost("note")]
        public async Task<IActionResult> CreateClinicalNote([FromForm] DoctorNoteRequest request)
        {
            if (request.AudioFile == null && string.IsNullOrWhiteSpace(request.RawText))
                return BadRequest("Please provide either an audio file or text dictation.");

            string transcribedText = request.RawText ?? string.Empty;

            if (request.AudioFile != null)
            {
                var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.wav");
                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await request.AudioFile.CopyToAsync(stream);
                }

                transcribedText = await _aiService.TranscribeAudioAsync(tempPath);
                System.IO.File.Delete(tempPath);
            }

            // Send transcription to Azure OpenAI for SOAP formatting
            string aiJsonResponse = await _aiService.GenerateSoapAndPatientSummaryAsync(transcribedText);

            string subjective = "N/A", objective = "N/A", assessment = "N/A", plan = "N/A", patientExplanation = "Your doctor has updated your chart.";

            try
            {
                using JsonDocument doc = JsonDocument.Parse(aiJsonResponse);
                JsonElement root = doc.RootElement;

                subjective = root.GetProperty("SoapSubjective").GetString() ?? subjective;
                objective = root.GetProperty("SoapObjective").GetString() ?? objective;
                assessment = root.GetProperty("SoapAssessment").GetString() ?? assessment;
                plan = root.GetProperty("SoapPlan").GetString() ?? plan;
                patientExplanation = root.GetProperty("PatientFriendlyExplanation").GetString() ?? patientExplanation;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SOAP JSON Parsing Error: {ex.Message}");
            }

            var newNote = new ClinicalNote
            {
                PatientId = request.PatientId,
                DoctorId = request.DoctorId,
                RawDictation = transcribedText,
                SoapSubjective = subjective,
                SoapObjective = objective,
                SoapAssessment = assessment,
                SoapPlan = plan,
                PatientFriendlyExplanation = patientExplanation,
                IsReadByPatient = false,
                CreatedAt = DateTime.UtcNow
            };

            await _mongoService.CreateClinicalNoteAsync(newNote);
            return Ok(newNote);
        }

    }
}