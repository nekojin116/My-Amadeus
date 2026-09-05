using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class GeminiProvider : IAiProvider
{
    private readonly string apiKey;
    private readonly string model;

    public GeminiProvider(string apiKey, string model = "gemini-2.5-flash")
    {
        this.apiKey = apiKey;
        this.model = model;
    }

    public IEnumerator SendMessage(List<ChatMessage> conversation, string systemPrompt, Action<string> onSuccess, Action<string> onError)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            onError?.Invoke("Gemini API key is missing.");
            yield break;
        }

        string endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        List<Content> contents = new List<Content>();
        foreach (ChatMessage msg in conversation)
        {
            // Gemini uses "model" for the AI turn, which matches our generic role already.
            contents.Add(new Content { role = msg.role, parts = new[] { new Part { text = msg.text } } });
        }

        GeminiRequest requestBody = new GeminiRequest
        {
            contents = contents.ToArray(),
            system_instruction = string.IsNullOrWhiteSpace(systemPrompt)
                ? null
                : new Content { parts = new[] { new Part { text = systemPrompt } } }
        };

        byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestBody));
        using UnityWebRequest request = new UnityWebRequest(endpoint, UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 30;

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke($"{request.error} | {request.downloadHandler.text}");
            yield break;
        }

        string text = null;
        try
        {
            GeminiResponse response = JsonUtility.FromJson<GeminiResponse>(request.downloadHandler.text);
            if (response?.candidates != null && response.candidates.Length > 0 &&
                response.candidates[0].content?.parts != null && response.candidates[0].content.parts.Length > 0)
            {
                text = response.candidates[0].content.parts[0].text;
            }
        }
        catch (Exception e)
        {
            onError?.Invoke($"Failed to parse Gemini response: {e.Message}");
            yield break;
        }

        if (string.IsNullOrEmpty(text))
        {
            onError?.Invoke("Gemini returned no text response.");
            yield break;
        }

        onSuccess?.Invoke(text);
    }

    [Serializable] private class GeminiRequest { public Content[] contents; public Content system_instruction; }
    [Serializable] private class Content { public string role; public Part[] parts; }
    [Serializable] private class Part { public string text; }
    [Serializable] private class GeminiResponse { public Candidate[] candidates; }
    [Serializable] private class Candidate { public Content content; }
}
