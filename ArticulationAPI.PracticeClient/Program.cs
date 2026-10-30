//cd "/Users/ustinabeseleva/Desktop/ArticulationAPI.PracticeClient"
//dotnet run

using ArticulationAPI.PracticeClient;
using ArticulationAPI.TranscriptionService.Grpc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddGrpcClient<TranscriptionGrpc.TranscriptionGrpcClient>(o =>
{
    o.Address = new Uri(builder.Configuration["TranscriptionService:GrpcUrl"] ?? "http://localhost:5099");
});

builder.Services.AddHostedService<PracticeSessionRunner>();

builder.Build().Run();