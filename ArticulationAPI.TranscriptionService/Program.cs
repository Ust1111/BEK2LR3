//cd "/Users/ustinabeseleva/Desktop/ArticulationAPI.TranscriptionService"
//dotnet run

using ArticulationAPI.TranscriptionService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

builder.Services.AddScoped<RussianTranscriptionService>();
builder.Services.AddScoped<LatinTranscriptionService>();
builder.Services.AddSingleton<PracticeSessionStore>();

var app = builder.Build();

app.MapGrpcService<TranscriptionGrpcService>();
app.MapGet("/", () => "Transcription gRPC service is running. Use a gRPC client to communicate.");

app.Run();