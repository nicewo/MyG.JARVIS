using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MyG.JARVIS.App.Services;

internal sealed class OpenAiChatService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(90)
    };

    private readonly string _apiKey;
    private readonly string _model;

    public OpenAiChatService()
    {
        _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty;
        _model = Environment.GetEnvironmentVariable("MYG_JARVIS_OPENAI_MODEL") ?? "gpt-6-astra";
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<string> SendAsync(
        IReadOnlyList<(string Role, string Text)> conversation,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "OPENAI_API_KEY ortam değişkeni bulunamadı. API anahtarını Windows kullanıcı ortam değişkenlerine ekleyip uygulamayı yeniden başlat.");
        }

        var input = new List<object>
        {
            new
            {
                role = "developer",
                content = "Sen MyG JARVIS'sin; Türkçe konuşan, teknik konularda yetkin, açık ve pratik bir kişisel asistansın. Bilmediğin şeyleri uydurma. Bu aşamada bilgisayarda işlem yapma yetkin yok; yalnızca sohbet yanıtı ver."
            }
        };

        foreach (var item in conversation)
        {
            if (item.Role is "user" or "assistant")
            {
                input.Add(new
                {
                    role = item.Role,
                    content = item.Text
                });
            }
        }

        var payload = JsonSerializer.Serialize(new
        {
            model = _model,
            input
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        using var response = await HttpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string detail = TryGetErrorMessage(responseBody);
            throw new InvalidOperationException(
                $"OpenAI API isteği başarısız ({(int)response.StatusCode} - {response.ReasonPhrase}). {detail}");
        }

        using var document = JsonDocument.Parse(responseBody);
        if (document.RootElement.TryGetProperty("output", out var output))
        {
            var text = new StringBuilder();
            foreach (var item in output.EnumerateArray())
            {
                if (!item.TryGetProperty("content", out var content))
                    continue;

                foreach (var part in content.EnumerateArray())
                {
                    if (part.TryGetProperty("type", out var type) &&
                        type.GetString() == "output_text" &&
                        part.TryGetProperty("text", out var value))
                    {
                        text.Append(value.GetString());
                    }
                }
            }

            if (text.Length > 0)
                return text.ToString().Trim();
        }

        throw new InvalidOperationException("OpenAI yanıtı alındı ancak metin içeriği bulunamadı.");
    }

    private static string TryGetErrorMessage(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var message))
            {
                return message.GetString() ?? body;
            }
        }
        catch (JsonException)
        {
            // If the response isn't JSON, return a short raw excerpt below.
        }

        return body.Length > 500 ? body[..500] : body;
    }
}
