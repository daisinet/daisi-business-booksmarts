using Daisi.Protos.V1;
using Daisi.SDK.Clients.V1.Host;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace BookSmarts.Web.Services;

/// <summary>
/// Wraps InferenceClientFactory for AI inference calls from Blazor pages.
/// Manages inference session lifecycle: create -> send -> collect -> close.
/// </summary>
public class BookSmartsInferenceService
{
    private readonly InferenceClientFactory _factory;

    public BookSmartsInferenceService(InferenceClientFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Single-shot analysis: sends a prompt and collects the full response.
    /// </summary>
    public async Task<string> AnalyzeAsync(string systemPrompt, string userPrompt, float temperature = 0.5f, CancellationToken ct = default)
    {
        var client = _factory.Create();

        try
        {
            await client.CreateAsync(new CreateInferenceRequest
            {
                SessionId = client.SessionManager.SessionId,
                InitializationPrompt = systemPrompt,
                ThinkLevel = ThinkLevels.Basic
            });

            var request = SendInferenceRequest.CreateDefault();
            request.Text = userPrompt;
            request.Temperature = temperature;

            var sb = new StringBuilder();
            var stream = client.Send(request);

            while (await stream.ResponseStream.MoveNext(ct))
            {
                var chunk = stream.ResponseStream.Current;
                if (chunk.Type == InferenceResponseTypes.Text)
                    sb.Append(chunk.Content);
            }

            return sb.ToString();
        }
        finally
        {
            try { await client.CloseAsync(); } catch { }
        }
    }

    /// <summary>
    /// Streaming analysis: yields tokens as they arrive for real-time UI updates.
    /// </summary>
    public async IAsyncEnumerable<string> StreamAsync(
        string systemPrompt,
        string userPrompt,
        float temperature = 0.7f,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var client = _factory.Create();

        try
        {
            await client.CreateAsync(new CreateInferenceRequest
            {
                SessionId = client.SessionManager.SessionId,
                InitializationPrompt = systemPrompt,
                ThinkLevel = ThinkLevels.Basic
            });

            var request = SendInferenceRequest.CreateDefault();
            request.Text = userPrompt;
            request.Temperature = temperature;

            var stream = client.Send(request);

            while (await stream.ResponseStream.MoveNext(ct))
            {
                var chunk = stream.ResponseStream.Current;
                if (chunk.Type == InferenceResponseTypes.Text)
                    yield return chunk.Content;
            }
        }
        finally
        {
            try { await client.CloseAsync(); } catch { }
        }
    }

    /// <summary>
    /// Streaming for multi-turn chat: sends a message using an existing client session.
    /// Caller manages the client lifecycle for multi-turn conversations.
    /// </summary>
    public async IAsyncEnumerable<string> StreamChatAsync(
        InferenceClient client,
        string userPrompt,
        float temperature = 0.7f,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var request = SendInferenceRequest.CreateDefault();
        request.Text = userPrompt;
        request.Temperature = temperature;

        var stream = client.Send(request);

        while (await stream.ResponseStream.MoveNext(ct))
        {
            var chunk = stream.ResponseStream.Current;
            if (chunk.Type == InferenceResponseTypes.Text)
                yield return chunk.Content;
        }
    }

    /// <summary>
    /// JSON-structured analysis: sends a prompt expecting a JSON response and deserializes it.
    /// </summary>
    public async Task<T?> AnalyzeJsonAsync<T>(string systemPrompt, string userPrompt, float temperature = 0.3f, CancellationToken ct = default) where T : class
    {
        var response = await AnalyzeAsync(systemPrompt, userPrompt, temperature, ct);

        // Extract JSON from the response (model may wrap in markdown code fences)
        var json = ExtractJson(response);

        try
        {
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Creates an inference client with a system prompt for multi-turn chat sessions.
    /// Caller is responsible for closing the client.
    /// </summary>
    public async Task<InferenceClient> CreateChatSessionAsync(string systemPrompt, CancellationToken ct = default)
    {
        var client = _factory.Create();

        await client.CreateAsync(new CreateInferenceRequest
        {
            SessionId = client.SessionManager.SessionId,
            InitializationPrompt = systemPrompt,
            ThinkLevel = ThinkLevels.Basic
        });

        return client;
    }

    private static string ExtractJson(string response)
    {
        // Try to extract JSON from markdown code fences
        var start = response.IndexOf("```json", StringComparison.OrdinalIgnoreCase);
        if (start >= 0)
        {
            start = response.IndexOf('\n', start) + 1;
            var end = response.IndexOf("```", start, StringComparison.Ordinal);
            if (end > start)
                return response[start..end].Trim();
        }

        // Try plain code fence
        start = response.IndexOf("```", StringComparison.Ordinal);
        if (start >= 0)
        {
            start = response.IndexOf('\n', start) + 1;
            var end = response.IndexOf("```", start, StringComparison.Ordinal);
            if (end > start)
                return response[start..end].Trim();
        }

        // Try to find raw JSON object or array
        var jsonStart = response.IndexOfAny(['{', '[']);
        if (jsonStart >= 0)
        {
            var closer = response[jsonStart] == '{' ? '}' : ']';
            var jsonEnd = response.LastIndexOf(closer);
            if (jsonEnd > jsonStart)
                return response[jsonStart..(jsonEnd + 1)];
        }

        return response.Trim();
    }
}
