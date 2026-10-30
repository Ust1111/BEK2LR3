using System.Collections.Concurrent;

namespace ArticulationAPI.TranscriptionService.Services;

public class PracticeSessionStore
{
    private readonly ConcurrentDictionary<string, int> _wordsPracticed = new();

    public int GetCount(string sessionId)
        => _wordsPracticed.GetValueOrDefault(sessionId, 0);

    public int Increment(string sessionId)
        => _wordsPracticed.AddOrUpdate(sessionId, 1, (_, current) => current + 1);
}