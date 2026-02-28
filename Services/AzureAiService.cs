using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.ClientModel;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace backend.Services
{
    public class AzureAiService
    {
        private readonly string? _speechKey;
        private readonly string? _speechRegion;
        private readonly AzureOpenAIClient _openAiClient;
        private readonly string? _openAiDeploymentName;
        private readonly bool _useMockAi;

        public AzureAiService(IConfiguration config)
        {
            _useMockAi = config.GetValue<bool>("Azure:UseMockAi");

            if (!_useMockAi)
            {
                _speechKey = config["Azure:SpeechKey"] ?? throw new ArgumentNullException("SpeechKey is missing");
                _speechRegion = config["Azure:SpeechRegion"] ?? throw new ArgumentNullException("SpeechRegion is missing");

                var openAiEndpoint = config["Azure:OpenAIEndpoint"] ?? throw new ArgumentNullException("OpenAIEndpoint is missing");
                var openAiKey = config["Azure:OpenAIKey"] ?? throw new ArgumentNullException("OpenAIKey is missing");
                _openAiDeploymentName = config["Azure:OpenAIDeploymentName"] ?? "gpt-4o-mini";

                _openAiClient = new AzureOpenAIClient(new Uri(openAiEndpoint), new ApiKeyCredential(openAiKey));
            }
        }

        public async Task<string> TranscribeAudioAsync(string audioFilePath)
        {
            if (_useMockAi) return "This is a mocked audio transcription for testing.";

            var speechConfig = SpeechConfig.FromSubscription(_speechKey, _speechRegion);
            speechConfig.SpeechRecognitionLanguage = "en-US";

            using var audioConfig = AudioConfig.FromWavFileInput(audioFilePath);
            using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

            var result = await recognizer.RecognizeOnceAsync();

            return result.Reason == ResultReason.RecognizedSpeech ? result.Text : $"Speech recognition failed: {result.Reason}";
        }

        public async Task<string> GenerateSoapAndPatientSummaryAsync(string doctorDictation)
        {
            if (_useMockAi) return @"{ ""SoapSubjective"": ""Patient reports headache."", ""SoapObjective"": ""Vitals normal."", ""SoapAssessment"": ""Tension headache."", ""SoapPlan"": ""Rest and hydration."", ""PatientFriendlyExplanation"": ""Make sure to get some rest and drink plenty of water."" }";

            var prompt = $@"
You are a medical AI assistant. Analyze the following doctor's dictation: '{doctorDictation}'
Output a JSON object strictly in this format:
{{
  ""SoapSubjective"": ""..."",
  ""SoapObjective"": ""..."",
  ""SoapAssessment"": ""..."",
  ""SoapPlan"": ""..."",
  ""PatientFriendlyExplanation"": ""Write a warm, empathetic 2-sentence summary for the patient at a 5th-grade reading level.""
}}";
            return await GetOpenAiResponseAsync(prompt);
        }

        public async Task<string> AnalyzePatientDiaryAsync(string currentLog, string pastHistory)
        {
            if (_useMockAi) return @"{ ""MoodScore"": 4, ""StressLevel"": 7, ""EnergyLevel"": 2, ""Category"": ""Diet"", ""TrendWarning"": ""I noticed you've been drinking a lot of energy drinks lately. This might be causing your crash."", ""AiPatientFeedback"": ""Try drinking some water instead today."" }";

            var prompt = $@"
            You are a preventative healthcare AI engine. 

            Here is the patient's recent diary history for context to detect trends:
            {pastHistory}

            Analyze their LATEST diary entry: '{currentLog}'

            Return a JSON object strictly in this format:
            {{
              ""MoodScore"": 5, //Extract 1-10 scale if mentioned. If mood score is not relavant, use null
              ""StressLevel"": 5, //Extract 1-10 scale if mentioned. If stress level is not relevant, use null
              ""EnergyLevel"": 8, // Extract 1-10 scale if mentioned. If energy is not mentioned, use null.
              ""Category"": ""Symptom"", // Choose strictly one: Symptom, Diet, General, Mood
              ""TrendWarning"": ""null"", // If history shows multiple days of unhealthy habits (high sugar, excessive caffeine, prolonged symptoms), write a strict but friendly warning here. Otherwise, use null.
              ""AiPatientFeedback"": ""A brief, empathetic response validating their feelings.""
            }}";
            return await GetOpenAiResponseAsync(prompt);
        }

        private async Task<string> GetOpenAiResponseAsync(string prompt)
        {
            ChatClient chatClient = _openAiClient.GetChatClient(_openAiDeploymentName);
            var options = new ChatCompletionOptions() { Temperature = 0.3f };
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a helpful healthcare AI. Always return raw JSON, no markdown blocks."),
                new UserChatMessage(prompt)
            };

            ChatCompletion completion = await chatClient.CompleteChatAsync(messages, options);
            return completion.Content[0].Text;
        }
    }
}