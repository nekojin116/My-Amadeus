using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ClaudeProvider : IAiProvider
{
    private readonly string apiKey;
    private readonly string model;
    private readonly int maxTokens;

    public ClaudeProvider(string apiKey, string model = "claude-sonnet-4-6", int maxTokens = 1024)
    {
        this.apiKey = apiKey;
        this.model = model;
        this.maxTokens = maxTokens;
    }

    public IEnumerator SendMessage(List<ChatMessage> conversation, string systemPrompt, Action<string> onSuccess, Action<string> onError)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            onError?.Invoke("Claude API key is missing.");
            yield break;
        }

        const string endpoint = "https://api.anthropic.com/v1/messages";

        List<Message> messages = new List<Message>();
        foreach (ChatMessage msg in conversation)
        {
            // Claude calls the AI's turn "assistant" instead of "model".
            string role = msg.role == "model" ? "assistant" : msg.role;
            messages.Add(new Message { role = role, content = msg.text });
        }

        ClaudeRequest requestBody = new ClaudeRequest
        {
            model = model,
            max_tokens = maxTokens,
            system = systemPrompt,
            messages = messages.ToArray()
        };

        byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestBody));
        using UnityWebRequest request = new UnityWebRequest(endpoint, UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("x-api-key", apiKey);
        request.SetRequestHeader("anthropic-version", "2023-06-01");
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
            ClaudeResponse response = JsonUtility.FromJson<ClaudeResponse>(request.downloadHandler.text);
            if (response?.content != null && response.content.Length > 0)
            {
                text = response.content[0].text;
            }
        }
        catch (Exception e)
        {
            onError?.Invoke($"Failed to parse Claude response: {e.Message}");
            yield break;
        }

        if (string.IsNullOrEmpty(text))
        {
            onError?.Invoke("Claude returned no text response.");
            yield break;
        }

        onSuccess?.Invoke(text);
    }

    [Serializable] private class ClaudeRequest { public string model; public int max_tokens; public string system; public Message[] messages; }
    [Serializable] private class Message { public string role; public string content; }
    [Serializable] private class ClaudeResponse { public ContentBlock[] content; }
    [Serializable] private class ContentBlock { public string type; public string text; }
}
