using ArticulationAPI.TranscriptionService.Grpc;
using Grpc.Core;

namespace ArticulationAPI.TranscriptionService.Services;

public class TranscriptionGrpcService : Grpc.TranscriptionGrpc.TranscriptionGrpcBase
{
    private static readonly string[] SupportedLanguages = { "ru", "en", "de", "fr" };
    private const int MaxTextLength = 500;

    private readonly RussianTranscriptionService _russian;
    private readonly LatinTranscriptionService _latin;
    private readonly PracticeSessionStore _sessions;

    public TranscriptionGrpcService(RussianTranscriptionService russian, LatinTranscriptionService latin, PracticeSessionStore sessions)
    {
        _russian = russian;
        _latin = latin;
        _sessions = sessions;
    }

    public override async Task<TranscribeReply> Transcribe(TranscribeRequest request, ServerCallContext context)
    {

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Text is required"));
        }

        if (request.Text.Length > MaxTextLength)
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                $"Text is too long (max {MaxTextLength} characters)"));
        }

        if (!SupportedLanguages.Contains(request.Language))
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                $"Language '{request.Language}' is not supported. Supported: {string.Join(", ", SupportedLanguages)}"));
        }

        ITranscriptionService service = request.Language == "ru" ? _russian : _latin;

        TranscriptionResponse result;
        try
        {
            result = await service.TranscribeAsync(request.Text, request.Language);
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.Internal, $"Transcription failed: {ex.Message}"));
        }

        var reply = new TranscribeReply
        {
            OriginalText = result.OriginalText,
            Language = result.Language
        };

        reply.Phonemes.AddRange(result.Phonemes.Select(p => new PhonemeMessage
        {
            Symbol = p.Symbol,
            Ipa = p.Ipa,
            Position = p.Position,
            Stress = p.Stress
        }));

        return reply;
    }

    public override Task<GetSupportedLanguagesReply> GetSupportedLanguages(
        GetSupportedLanguagesRequest request, ServerCallContext context)
    {
        var reply = new GetSupportedLanguagesReply();
        reply.Languages.AddRange(SupportedLanguages);
        return Task.FromResult(reply);
    }

    public override async Task PracticeSession(
        IAsyncStreamReader<PracticeMessage> requestStream,
        IServerStreamWriter<PracticeEvent> responseStream,
        ServerCallContext context)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
        string? sessionId = null;
        var stateSent = false;

        async Task SendPeriodicStateAsync()
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
            try
            {
                while (await timer.WaitForNextTickAsync(cts.Token))
                {
                    if (sessionId is null) continue;

                    await responseStream.WriteAsync(new PracticeEvent
                    {
                        Type = PracticeEventType.State,
                        CreatedAt = DateTime.UtcNow.ToString("O"),
                        WordsPracticed = _sessions.GetCount(sessionId)
                    }, cts.Token);
                }
            }
            catch (OperationCanceledException) { }
        }

        var periodicTask = SendPeriodicStateAsync();

        try
        {
            await foreach (var message in requestStream.ReadAllAsync(cts.Token))
            {
                sessionId = message.SessionId;

                if (!stateSent)
                {
                    stateSent = true;
                    await responseStream.WriteAsync(new PracticeEvent
                    {
                        Type = PracticeEventType.State,
                        CreatedAt = DateTime.UtcNow.ToString("O"),
                        WordsPracticed = _sessions.GetCount(sessionId)
                    }, cts.Token);
                }
                if (string.IsNullOrWhiteSpace(message.Text))
                {
                    await responseStream.WriteAsync(new PracticeEvent
                    {
                        Type = PracticeEventType.Error,
                        CreatedAt = DateTime.UtcNow.ToString("O"),
                        ErrorMessage = "Text is required"
                    }, cts.Token);
                    continue;
                }

                string[] supported = { "ru", "en", "de", "fr" };
                if (!supported.Contains(message.Language))
                {
                    await responseStream.WriteAsync(new PracticeEvent
                    {
                        Type = PracticeEventType.Error,
                        CreatedAt = DateTime.UtcNow.ToString("O"),
                        ErrorMessage = $"Language '{message.Language}' is not supported"
                    }, cts.Token);
                    continue;
                }

                ITranscriptionService service = message.Language == "ru" ? _russian : _latin;
                TranscriptionResponse result;
                try
                {
                    result = await service.TranscribeAsync(message.Text, message.Language);
                }
                catch (Exception ex)
                {
                    await responseStream.WriteAsync(new PracticeEvent
                    {
                        Type = PracticeEventType.Error,
                        CreatedAt = DateTime.UtcNow.ToString("O"),
                        ErrorMessage = $"Transcription failed: {ex.Message}"
                    }, cts.Token);
                    continue;
                }

                var wordsPracticed = _sessions.Increment(sessionId);

                var evt = new PracticeEvent
                {
                    Type = PracticeEventType.Phonemes,
                    CreatedAt = DateTime.UtcNow.ToString("O"),
                    OriginalText = result.OriginalText,
                    Language = result.Language,
                    WordsPracticed = wordsPracticed
                };
                evt.Phonemes.AddRange(result.Phonemes.Select(p => new PhonemeMessage
                {
                    Symbol = p.Symbol, Ipa = p.Ipa, Position = p.Position, Stress = p.Stress
                }));

                await responseStream.WriteAsync(evt, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {}

        await cts.CancelAsync();
        await periodicTask;
    }
}