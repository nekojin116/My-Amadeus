# Amadeus AI Companion 

![Unity](https://img.shields.io/badge/Unity-6-black?logo=unity&logoColor=white)
![Stars](https://img.shields.io/github/stars/nekojin116/My-Amadeus)

A Unity implementation of **Amadeus**, an AI construct built from the memories and personality of Makise Kurisu.

This is a hobby project of mine, nothing is released yet.

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
  
