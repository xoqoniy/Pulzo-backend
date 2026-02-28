using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PatientController : ControllerBase
    {
        private readonly MongoDbService _mongoService;
        private readonly AzureAiService _aiService;

        public PatientController(MongoDbService mongoService, AzureAiService aiService)
        {
            _mongoService = mongoService;
            _aiService = aiService;
        }

        [HttpGet("{patientId}/diary")]
        public async Task<IActionResult> GetPatientDiary(string patientId)
        {
            var entries = await _mongoService.GetPatientDiaryEntriesAsync(patientId);
            return Ok(entries);
        }

        [HttpGet("{patientId}/notifications")]
        public async Task<IActionResult> GetNotifications(string patientId)
        {
            var notes = await _mongoService.GetUnreadNotesForPatientAsync(patientId);
            return Ok(notes);
        }

        public class PatientDiaryRequest
        {
            public IFormFile? AudioFile { get; set; }
            public string? RawText { get; set; }
        }

        [HttpPost("{patientId}/diary")]
        public async Task<IActionResult> AddDiaryEntry(string patientId, [FromForm] PatientDiaryRequest request)
        {
            if (request.AudioFile == null && string.IsNullOrWhiteSpace(request.RawText))
                return BadRequest("Please provide either an audio file or text.");

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

            var pastEntries = await _mongoService.GetPatientDiaryEntriesAsync(patientId);
            var historyText = "No previous history.";
            
            if (pastEntries.Any())
            {
                var recentLogs = pastEntries.Take(3).Select(e => $"- {e.CreatedAt.ToShortDateString()}: {e.RawText}");
                historyText = string.Join("\n", recentLogs);
            }

            string aiJsonResponse = await _aiService.AnalyzePatientDiaryAsync(transcribedText, historyText);

            var newEntry = new DiaryEntry
            {
                PatientId = patientId,
                RawText = transcribedText,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                using JsonDocument doc = JsonDocument.Parse(aiJsonResponse);
                JsonElement root = doc.RootElement;

                newEntry.MoodScore = root.TryGetProperty("MoodScore", out var mood) ? mood.GetInt32() : 5;
                newEntry.StressLevel = root.TryGetProperty("StressLevel", out var stress) ? stress.GetInt32() : 5;
                newEntry.Category = root.TryGetProperty("Category", out var cat) ? cat.GetString() ?? "General" : "General";
                newEntry.AiPatientFeedback = root.TryGetProperty("AiPatientFeedback", out var fb) ? fb.GetString() ?? "" : "";
                
                if (root.TryGetProperty("EnergyLevel", out var energy) && energy.ValueKind != JsonValueKind.Null)
                    newEntry.EnergyLevel = energy.GetInt32();

                if (root.TryGetProperty("TrendWarning", out var warning) && warning.ValueKind != JsonValueKind.Null && warning.GetString() != "null")
                    newEntry.TrendWarning = warning.GetString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"JSON Parsing Error: {ex.Message}");
            }

            await _mongoService.CreateDiaryEntryAsync(newEntry);
            return Ok(newEntry);
        }
    }
}