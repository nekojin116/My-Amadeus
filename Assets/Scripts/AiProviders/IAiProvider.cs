using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Generic role/text pair. "role" is always "user" or "model" regardless of provider;
/// each IAiProvider implementation translates that into whatever the target API expects
/// (e.g. OpenAI/Claude call the AI's turn "assistant" instead of "model").
/// </summary>
[Serializable]
public class ChatMessage
{
    public string role; // "user" or "model"
    public string text;

    public ChatMessage(string role, string text)
    {
        this.role = role;
        this.text = text;
    }
}

/// <summary>
/// Anything that can take a conversation + persona and return generated text.
/// ChatAi only ever talks to this interface, never to a specific API.
/// </summary>
public interface IAiProvider
{
    IEnumerator SendMessage(
        List<ChatMessage> conversation,
        string systemPrompt,
        Action<string> onSuccess,
        Action<string> onError
    );
}
