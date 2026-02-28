using backend.Models;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Services
{
    public class MongoDbService
    {
        private readonly IMongoCollection<User> _users;
        private readonly IMongoCollection<DiaryEntry> _diaryEntries;
        private readonly IMongoCollection<ClinicalNote> _clinicalNotes;

        public MongoDbService(IConfiguration config)
        {

            var client = new MongoClient(config.GetConnectionString("MongoDb"));
            var database = client.GetDatabase("HealthcareHackathonDb");

            _users = database.GetCollection<User>("Users");
            _diaryEntries = database.GetCollection<DiaryEntry>("DiaryEntries");
            _clinicalNotes = database.GetCollection<ClinicalNote>("ClinicalNotes");
        }

        // --- Diary Entry Methods ---
        public async Task CreateDiaryEntryAsync(DiaryEntry entry) =>
            await _diaryEntries.InsertOneAsync(entry);

        public async Task<List<DiaryEntry>> GetPatientDiaryEntriesAsync(string patientId) =>
            await _diaryEntries.Find(e => e.PatientId == patientId)
                               .SortByDescending(e => e.CreatedAt)
                               .ToListAsync();

        // --- Clinical Note Methods ---
        public async Task CreateClinicalNoteAsync(ClinicalNote note) =>
            await _clinicalNotes.InsertOneAsync(note);

        public async Task<List<ClinicalNote>> GetPatientClinicalNotesAsync(string patientId) =>
            await _clinicalNotes.Find(n => n.PatientId == patientId)
                                .SortByDescending(n => n.CreatedAt)
                                .ToListAsync();

        // Example: Get unread notifications for the patient
        public async Task<List<ClinicalNote>> GetUnreadNotesForPatientAsync(string patientId) =>
            await _clinicalNotes.Find(n => n.PatientId == patientId && !n.IsReadByPatient)
                                .ToListAsync();
    }
}