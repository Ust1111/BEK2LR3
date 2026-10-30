using System.Text.Json.Serialization;

namespace ArticulationAPI.TranscriptionService.Services;

public class TranscriptionResponse
{
    public string OriginalText { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public List<Phoneme> Phonemes { get; set; } = new();
}

public class Phoneme
{
    public string Symbol { get; set; } = string.Empty;
    public string Ipa { get; set; } = string.Empty;
    public int Position { get; set; }
    public float Stress { get; set; }
}

// Модель для токенов, которые возвращает Python-скрипт
public class Token
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("phonemes")]
    public List<string> Phonemes { get; set; } = new();
}