using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class OpenAiProvider : IAiProvider
{
    private readonly string apiKey;
    private readonly string model;

    public OpenAiProvider(string apiKey, string model = "gpt-4o-mini")
    {
        this.apiKey = apiKey;
        this.model = model;
    }

    public IEnumerator SendMessage(List<ChatMessage> conversation, string systemPrompt, Action<string> onSuccess, Action<string> onError)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            onError?.Invoke("OpenAI API key is missing.");
            yield break;
        }

        const string endpoint = "https://api.openai.com/v1/chat/completions";

        List<Message> messages = new List<Message>();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            messages.Add(new Message { role = "system", content = systemPrompt });
        }
        foreach (ChatMessage msg in conversation)
        {
            // OpenAI calls the AI's turn "assistant" instead of "model".
            string role = msg.role == "model" ? "assistant" : msg.role;
            messages.Add(new Message { role = role, content = msg.text });
        }

        OpenAiRequest requestBody = new OpenAiRequest { model = model, messages = messages.ToArray() };
        byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestBody));

        using UnityWebRequest request = new UnityWebRequest(endpoint, UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
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
            OpenAiResponse response = JsonUtility.FromJson<OpenAiResponse>(request.downloadHandler.text);
            if (response?.choices != null && response.choices.Length > 0)
            {
                text = response.choices[0].message.content;
            }
        }
        catch (Exception e)
        {
            onError?.Invoke($"Failed to parse OpenAI response: {e.Message}");
            yield break;
        }

        if (string.IsNullOrEmpty(text))
        {
            onError?.Invoke("OpenAI returned no text response.");
            yield break;
        }

        onSuccess?.Invoke(text);
    }

    [Serializable] private class OpenAiRequest { public string model; public Message[] messages; }
    [Serializable] private class Message { public string role; public string content; }
    [Serializable] private class OpenAiResponse { public Choice[] choices; }
    [Serializable] private class Choice { public Message message; }
}
