using ArticulationAPI.TranscriptionService.Grpc;
using Grpc.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ArticulationAPI.PracticeClient;

public sealed class PracticeSessionRunner : BackgroundService
{
    private readonly TranscriptionGrpc.TranscriptionGrpcClient _client;
    private readonly ILogger<PracticeSessionRunner> _logger;
    private readonly string _sessionId = Guid.NewGuid().ToString();

    public PracticeSessionRunner(
        TranscriptionGrpc.TranscriptionGrpcClient client,
        ILogger<PracticeSessionRunner> logger)
    {
        _client = client;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine($"Сессия: {_sessionId}");
        Console.WriteLine("Вводите слова по одному (язык: ru/en/de/fr), Ctrl+C для выхода.\n");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (RpcException ex)
            {
                _logger.LogWarning(
                    "Соединение прервано ({Status}), переподключение через 2 с...",
                    ex.StatusCode);
                await Task.Delay(2000, stoppingToken);
            }
        }
    }

    private async Task RunAsync(CancellationToken stoppingToken)
    {
        using var call = _client.PracticeSession(cancellationToken: stoppingToken);

        var receiving = ReceiveEventsAsync(call.ResponseStream, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            Console.Write("> ");
            var line = Console.ReadLine();

            if (line is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split(' ', 2);
            var text = parts[0];
            var language = parts.Length > 1 ? parts[1] : "ru";

            await call.RequestStream.WriteAsync(new PracticeMessage
            {
                SessionId = _sessionId,
                CreatedAt = DateTime.UtcNow.ToString("O"),
                Text = text,
                Language = language
            }, stoppingToken);
        }

        await call.RequestStream.CompleteAsync();
        await receiving;
    }

    private async Task ReceiveEventsAsync(
        IAsyncStreamReader<PracticeEvent> responseStream,
        CancellationToken stoppingToken)
    {
        await foreach (var evt in responseStream.ReadAllAsync(stoppingToken))
        {
            switch (evt.Type)
            {
                case PracticeEventType.State:
                    Console.WriteLine($"[состояние] разобрано слов в сессии: {evt.WordsPracticed}");
                    break;

                case PracticeEventType.Phonemes:
                    var phonemes = string.Join(" ", evt.Phonemes.Select(p => p.Ipa));
                    Console.WriteLine(
                        $"[{evt.Language}] {evt.OriginalText} → {phonemes} (всего разобрано: {evt.WordsPracticed})");
                    break;

                case PracticeEventType.Error:
                    Console.WriteLine($"[ошибка] {evt.ErrorMessage}");
                    break;
            }
        }
    }
}