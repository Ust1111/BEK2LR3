namespace ArticulationAPI.TranscriptionService.Services;

public interface ITranscriptionService
{
    Task<TranscriptionResponse> TranscribeAsync(string text, string language);
}