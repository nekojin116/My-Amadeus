# Amadeus AI Companion 

![Unity](https://img.shields.io/badge/Unity-6-black?logo=unity&logoColor=white)
![Stars](https://img.shields.io/github/stars/nekojin116/My-Amadeus)

A Unity implementation of **Amadeus**, an AI construct built from the memories and personality of Makise Kurisu.

This is a hobby project of mine, nothing is released yet.

## Features

- **Amadeus persona** : defined in `Amadeus_SystemPrompt.txt`, covering her identity rules (she is not Kurisu, and corrects anyone who calls her that), tone and behavioral guidelines.
- **Multi-turn conversation** : Amadeus remembers earlier corrections, established facts, and the flow of the conversation.
- **Swappable AI backend** : Gemini, OpenAI, and Claude are all implemented behind a shared `IAiProvider` interface, selectable from the Inspector. This exists so the project isn't stuck if a provider's pricing, rate limits, or quality changes : not to support arbitrary other characters.

## Requirements

- Unity 6 (or a recent Unity 2022 LTS+ version)
- [TextMeshPro](https://docs.unity3d.com/Packages/com.unity.textmeshpro@latest)
- An API key for at least one provider:
  - [Google AI Studio](https://aistudio.google.com/) (Gemini)
  - [OpenAI Platform](https://platform.openai.com/)
  - [Anthropic Console](https://console.anthropic.com/)

## Setup

1. **Clone the repo** and open it in Unity.
2. On the `ChatAi` component in the scene, enter an API key for at least one provider (Gemini, OpenAI, or Claude) in the Inspector.
   > ⚠️ API keys currently live directly on the `ChatAi` component. Secure, gitignored key storage isn't implemented yet — see the roadmap below. Do not commit a scene/prefab with real keys filled in.
3. The `Amadeus` character profile is already set up, referencing `Amadeus_SystemPrompt.txt`. Assign it to `ChatAi`'s `Active Character` field if it isn't already.
4. Wire up the UI references on `ChatAi` (`promptInput`, `sendButton`, `responseText`) if not already set.
5. Press Play and talk to Amadeus. (So far it's only a text box that barely looks like a texting app lol)

## Roadmap

| Feature | Status |
|---|---|
| Multi-provider backend (Gemini / OpenAI / Claude) | ✅ |
| Multi-turn conversation memory | ✅ |
| Amadeus character profile & persona | ✅ |
| Secure, gitignored API key storage | ❌ |
| Visual 3D Kurisu model | ❌ |
| Model Animations | ❌ |
| Model LipSync| ❌ |
| Prompt caching | ❌ |
| Streaming responses | ❌ |
| Mock/offline provider for UI testing | ❌ |
  
