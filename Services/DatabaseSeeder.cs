using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using backend.Models;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Services
{
    public static class DatabaseSeeder
    {
        public static async Task SeedDataAsync(IServiceProvider serviceProvider)
        {
            var mongoService = serviceProvider.GetRequiredService<MongoDbService>();

            var existingEntries = await mongoService.GetPatientDiaryEntriesAsync("patient_123");
            if (existingEntries.Count > 0) return;

            Console.WriteLine("Seeding 15 patients into MongoDB...");

            var entries = new List<DiaryEntry>
            {
                // --- PATIENT 1: JOHN (The Migraine Demo) ---
                new DiaryEntry { PatientId = "patient_123", RawText = "Feeling pretty good today, just a normal day at work.", MoodScore = 7, StressLevel = 4, EnergyLevel = 8, Category = "General", AiPatientFeedback = "Glad to hear you are having a good day!", CreatedAt = DateTime.UtcNow.AddDays(-5) },
                new DiaryEntry { PatientId = "patient_123", RawText = "Work was really overwhelming today. Slight headache.", MoodScore = 5, StressLevel = 7, EnergyLevel = 4, Category = "Symptom", AiPatientFeedback = "Make sure to drink water and step away from screens.", CreatedAt = DateTime.UtcNow.AddDays(-4) },
                new DiaryEntry { PatientId = "patient_123", RawText = "Terrible migraine. Barely can look at the light.", MoodScore = 2, StressLevel = 9, EnergyLevel = 1, Category = "Symptom", TrendWarning = "You've logged worsening headaches for multiple days. Please contact your clinician.", AiPatientFeedback = "Please rest in a dark room.", CreatedAt = DateTime.UtcNow.AddDays(-3) },
                new DiaryEntry { PatientId = "patient_123", RawText = "Saw the doctor. Taking medication. Pain is less.", MoodScore = 5, StressLevel = 5, EnergyLevel = 3, Category = "General", AiPatientFeedback = "Recovery takes time. Keep following the plan.", CreatedAt = DateTime.UtcNow.AddDays(-1) },
                new DiaryEntry { PatientId = "patient_123", RawText = "Much better today! Went for a short walk.", MoodScore = 8, StressLevel = 3, EnergyLevel = 7, Category = "Mood", AiPatientFeedback = "Wonderful news! Light exercise is great.", CreatedAt = DateTime.UtcNow },

                // --- PATIENT 2: SARAH (The Preventative Diet Demo) ---
                new DiaryEntry { PatientId = "patient_456", RawText = "Drank 3 Monster energy drinks to get through the shift. Feeling jittery.", MoodScore = 6, StressLevel = 8, EnergyLevel = 9, Category = "Diet", AiPatientFeedback = "High caffeine can increase stress. Try switching to water.", CreatedAt = DateTime.UtcNow.AddDays(-2) },
                new DiaryEntry { PatientId = "patient_456", RawText = "Crashing hard. Ate a bunch of candy to stay awake. Stomach hurts.", MoodScore = 3, StressLevel = 7, EnergyLevel = 2, Category = "Diet", TrendWarning = "High sugar and caffeine intake detected over 48 hours leading to crashes. Consider a balanced meal to stabilize blood sugar.", AiPatientFeedback = "Sugar spikes lead to hard crashes. Try resting.", CreatedAt = DateTime.UtcNow.AddDays(-1) },
                new DiaryEntry { PatientId = "patient_456", RawText = "Still exhausted and my resting heart rate feels high.", MoodScore = 4, StressLevel = 6, EnergyLevel = 3, Category = "Symptom", AiPatientFeedback = "Please monitor your heart rate and hydrate.", CreatedAt = DateTime.UtcNow }
            };

            // --- PATIENTS 3 TO 15: (Background Data for Doctor Dashboard) ---
            var conditions = new[] { "Allergies acting up.", "Feeling great today.", "A bit of lower back pain.", "Slept perfectly.", "Stressed about exams." };
            var random = new Random();

            for (int i = 3; i <= 15; i++)
            {
                entries.Add(new DiaryEntry
                {
                    PatientId = $"patient_{i:000}",
                    RawText = conditions[random.Next(conditions.Length)],
                    MoodScore = random.Next(4, 9),
                    StressLevel = random.Next(2, 8),
                    EnergyLevel = random.Next(3, 9),
                    Category = "General",
                    AiPatientFeedback = "Thank you for checking in today.",
                    CreatedAt = DateTime.UtcNow.AddHours(-random.Next(1, 48))
                });
            }

            foreach (var entry in entries)
            {
                await mongoService.CreateDiaryEntryAsync(entry);
            }

            // --- DOCTOR'S NOTE FOR JOHN ---
            await mongoService.CreateClinicalNoteAsync(new ClinicalNote
            {
                PatientId = "patient_123",
                DoctorId = "doctor_999",
                RawDictation = "Patient presented with a severe migraine with photophobia. Prescribed sumatriptan 50mg.",
                SoapSubjective = "Patient reports severe migraine starting this morning.",
                SoapObjective = "Patient appears uncomfortable in standard lighting. Vitals stable.",
                SoapAssessment = "Acute migraine without aura.",
                SoapPlan = "1. Prescribe Sumatriptan 50mg PRN. 2. Rest in dark/quiet environment.",
                PatientFriendlyExplanation = "You are experiencing a standard migraine. I've prescribed a medication called Sumatriptan to help with the pain. Please rest in a dark room.",
                IsReadByPatient = false,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            });

            Console.WriteLine("15 Patients seeded successfully!");
        }
    }
}