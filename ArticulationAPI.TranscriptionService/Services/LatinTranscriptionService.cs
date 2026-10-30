using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ArticulationAPI.TranscriptionService.Services;

public class LatinTranscriptionService : ITranscriptionService
{
    private readonly IWebHostEnvironment _env;

    public LatinTranscriptionService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<TranscriptionResponse> TranscribeAsync(string text, string language)
    {
        if (language == "en")
        {
            return await TranscribeEnglishAsync(text);
        }

        return await TranscribeOtherAsync(text, language);
    }

    private async Task<TranscriptionResponse> TranscribeEnglishAsync(string text)
    {
        var tokens = await ProcessTextWithPython(text);

        var phonemes = new List<Phoneme>();

        foreach (var token in tokens)
        {
            if (token.Type == "word")
            {
                foreach (var ipa in token.Phonemes)
                {
                    phonemes.Add(new Phoneme
                    {
                        Symbol = ipa,
                        Ipa = ipa,
                        Position = phonemes.Count,
                        Stress = 0
                    });
                }
            }
            else if (token.Type == "space")
            {
                phonemes.Add(new Phoneme
                {
                    Symbol = "[PAUSE]",
                    Ipa = "",
                    Position = phonemes.Count,
                    Stress = 0
                });
            }
            else if (token.Type == "punctuation")
            {
                phonemes.Add(new Phoneme
                {
                    Symbol = $"[{token.Text}]",
                    Ipa = "",
                    Position = phonemes.Count,
                    Stress = 0
                });
            }
        }

        return new TranscriptionResponse
        {
            OriginalText = text,
            Language = "en",
            Phonemes = phonemes
        };
    }

    private async Task<List<Token>> ProcessTextWithPython(string text)
    {
        try
        {
            string projectPath = _env.ContentRootPath;

            string pythonPath = Path.Combine(projectPath, "venv", "bin", "python3");
            if (!File.Exists(pythonPath))
            {
                pythonPath = Path.Combine(projectPath, ".venv", "bin", "python3");
            }
            if (!File.Exists(pythonPath))
            {
                pythonPath = "python3";
            }

            string scriptPath = Path.Combine(projectPath, "transcribe_service.py");

            var startInfo = new ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = $"\"{scriptPath}\" \"{text}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            using var process = Process.Start(startInfo);

            if (process == null)
            {
                return new List<Token>();
            }

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (string.IsNullOrWhiteSpace(output))
            {
                return new List<Token>();
            }

            var tokens = JsonSerializer.Deserialize<List<Token>>(output);
            return tokens ?? new List<Token>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
            return new List<Token>();
        }
    }

    private Task<TranscriptionResponse> TranscribeOtherAsync(string text, string language)
    {
        var phonemes = new List<Phoneme>();
        foreach (char c in text.ToLower())
        {
            if (char.IsLetter(c))
            {
                phonemes.Add(new Phoneme
                {
                    Symbol = c.ToString(),
                    Ipa = c.ToString(),
                    Position = phonemes.Count,
                    Stress = 0
                });
            }
        }

        return Task.FromResult(new TranscriptionResponse
        {
            OriginalText = text,
            Language = language,
            Phonemes = phonemes
        });
    }
}