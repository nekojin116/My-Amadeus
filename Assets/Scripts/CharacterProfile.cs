using UnityEngine;

/// <summary>
/// Data-only container for a roleplay character. Create one asset per character
/// (Right-click in Project window → Create → Chat AI → Character Profile).
/// Keeping this separate from ChatAi means adding a new character later is
/// "create an asset and drag in a text file" — no script changes required.
/// </summary>
[CreateAssetMenu(fileName = "NewCharacterProfile", menuName = "Chat AI/Character Profile")]
public class CharacterProfile : ScriptableObject
{
    [Header("Identity")]
    public string characterName = "New Character";

    [Header("System prompt")]
    [Tooltip("Preferred source: an external .txt file. Easier to edit than an Inspector text box and keeps long personas out of scene/prefab files.")]
    public TextAsset systemPromptFile;

    [Tooltip("Used only if no systemPromptFile is assigned. Handy for quick prototyping.")]
    [TextArea(3, 10)]
    public string systemPromptFallback;

    [Header("Optional")]
    [Tooltip("If set, shown as the character's opening line when this profile becomes active, before the user sends anything.")]
    [TextArea(2, 4)]
    public string greetingMessage;

    [Tooltip("Provider this character is tuned for / tested on. ChatAi will switch to this when the character is activated, unless the player has manually overridden the provider.")]
    public AiProviderType preferredProvider = AiProviderType.Gemini;

    [Tooltip("Optional model override for the preferred provider. Leave blank to use ChatAi's configured default model for that provider.")]
    public string preferredModel;

    /// <summary>Resolves the actual persona text to send to the API.</summary>
    public string GetSystemPrompt()
    {
        if (systemPromptFile != null && !string.IsNullOrWhiteSpace(systemPromptFile.text))
        {
            return systemPromptFile.text;
        }
        return systemPromptFallback;
    }
}
