using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class ChatAi : MonoBehaviour
{
    [Header("Active character")]
    [Tooltip("Drag a CharacterProfile asset here. Swap this (or call SetCharacter) to change who the player is talking to.")]
    [SerializeField] private CharacterProfile activeCharacter;

    [Header("Provider selection")]
    [Tooltip("Manual override. Automatically set to the active character's preferredProvider when a character is assigned, unless you change it yourself afterward.")]
    [SerializeField] private AiProviderType providerType = AiProviderType.Gemini;

    [Header("Gemini")]
    [SerializeField] private string geminiApiKey;
    [SerializeField] private string geminiModel = "gemini-2.5-flash";

    [Header("OpenAI")]
    [SerializeField] private string openAiApiKey;
    [SerializeField] private string openAiModel = "gpt-4o-mini";

    [Header("Claude")]
    [SerializeField] private string claudeApiKey;
    [SerializeField] private string claudeModel = "claude-sonnet-4-6";

    [Header("UI")]
    [SerializeField] private TMP_InputField promptInput;
    [SerializeField] private Button sendButton;
    [SerializeField] private TMP_Text responseText;

    public event Action<string> ResponseReceived;

    // Full multi-turn history, provider-agnostic and character-agnostic.
    // Cleared whenever the active character changes (see SetCharacter).
    private readonly List<ChatMessage> conversation = new List<ChatMessage>();

    private void Awake()
    {
        if (activeCharacter != null)
        {
            ApplyCharacterDefaults(activeCharacter);
        }
    }

    private void OnEnable()
    {
        if (sendButton != null) sendButton.onClick.AddListener(SendPromptFromInput);
    }

    private void OnDisable()
    {
        if (sendButton != null) sendButton.onClick.RemoveListener(SendPromptFromInput);
    }

    /// <summary>
    /// Swap the active character at runtime (e.g. from a character-select menu).
    /// Resets conversation history since a new character shouldn't inherit the
    /// previous character's memory, and shows their greeting line if they have one.
    /// </summary>
    public void SetCharacter(CharacterProfile profile)
    {
        activeCharacter = profile;
        conversation.Clear();
        ApplyCharacterDefaults(profile);

        if (profile != null && !string.IsNullOrWhiteSpace(profile.greetingMessage))
        {
            // Shown to the player and kept in history so the AI is aware it already said this.
            conversation.Add(new ChatMessage("model", profile.greetingMessage));
            SetResponseText(profile.greetingMessage);
        }
        else
        {
            SetResponseText(string.Empty);
        }
    }

    private void ApplyCharacterDefaults(CharacterProfile profile)
    {
        if (profile == null) return;
        providerType = profile.preferredProvider;
    }

    public void SendPromptFromInput()
    {
        if (promptInput == null)
        {
            Debug.LogError("Assign a TMP_InputField to ChatAi.promptInput.", this);
            return;
        }
        SendMessage(promptInput.text);
        promptInput.text = string.Empty;
    }

    public void SendMessage(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            SetResponseText("Please enter a message.");
            return;
        }

        if (activeCharacter == null)
        {
            SetResponseText("No character assigned. Set activeCharacter in the Inspector or call SetCharacter().");
            return;
        }

        IAiProvider provider = BuildProvider();
        if (provider == null)
        {
            SetResponseText($"No API key set for {providerType}.");
            return;
        }

        string systemPrompt = activeCharacter.GetSystemPrompt();
        if (string.IsNullOrWhiteSpace(systemPrompt))
        {
            Debug.LogWarning($"Character '{activeCharacter.characterName}' has no system prompt set (missing systemPromptFile and systemPromptFallback).", this);
        }

        SetResponseText("Thinking...");
        SetSendButtonInteractable(false);
        conversation.Add(new ChatMessage("user", prompt));

        StartCoroutine(provider.SendMessage(
            conversation,
            systemPrompt,
            onSuccess: HandleSuccess,
            onError: HandleError
        ));
    }

    private void HandleSuccess(string text)
    {
        conversation.Add(new ChatMessage("model", text));
        SetResponseText(text);
        SetSendButtonInteractable(true);
        ResponseReceived?.Invoke(text);
    }

    private void HandleError(string error)
    {
        Debug.LogError($"AI request failed: {error}", this);
        RemoveLastUserMessage();
        SetResponseText("Sorry, the request failed. Check the Console for details.");
        SetSendButtonInteractable(true);
    }

    /// <summary>
    /// Builds a fresh provider instance based on the current dropdown + keys,
    /// applying the active character's preferredModel override if one is set.
    /// </summary>
    private IAiProvider BuildProvider()
    {
        string modelOverride = activeCharacter != null && !string.IsNullOrWhiteSpace(activeCharacter.preferredModel)
            ? activeCharacter.preferredModel
            : null;

        switch (providerType)
        {
            case AiProviderType.Gemini:
                if (string.IsNullOrWhiteSpace(geminiApiKey)) return null;
                return new GeminiProvider(geminiApiKey, modelOverride ?? geminiModel);
            case AiProviderType.OpenAi:
                if (string.IsNullOrWhiteSpace(openAiApiKey)) return null;
                return new OpenAiProvider(openAiApiKey, modelOverride ?? openAiModel);
            case AiProviderType.Claude:
                if (string.IsNullOrWhiteSpace(claudeApiKey)) return null;
                return new ClaudeProvider(claudeApiKey, modelOverride ?? claudeModel);
            default:
                return null;
        }
    }

    /// <summary>Call from a UI dropdown's OnValueChanged to manually override the character's preferred provider.</summary>
    public void SetProviderType(AiProviderType type) => providerType = type;

    /// <summary>Wipes history but keeps the active character and their persona — use this for "restart roleplay".</summary>
    public void ClearConversation()
    {
        conversation.Clear();
        SetResponseText(string.Empty);
    }

    private void RemoveLastUserMessage()
    {
        if (conversation.Count > 0 && conversation[conversation.Count - 1].role == "user")
        {
            conversation.RemoveAt(conversation.Count - 1);
        }
    }

    private void SetResponseText(string text)
    {
        if (responseText != null) responseText.text = text;
    }

    private void SetSendButtonInteractable(bool interactable)
    {
        if (sendButton != null) sendButton.interactable = interactable;
    }
}