# Pocket TTS Unity Usage Tutorial

This project is already configured to use the `ai.lookbe.pockettts` package, and the included sample script shows the expected Unity usage pattern.

The package is a Unity integration for the Pocket-TTS model family and relies on ONNX Runtime. In this repository, the sample script is located at `Assets/Samples/Pocket TTS Unity/0.0.1/Basic Pocket TTS/BasicPocketTTS.cs`.

---

## 1. What this package does

`PocketTTS` gives you a text-to-speech component that:

- loads a local TTS model
- converts text to speech audio
- reports status changes during loading and generation
- can be triggered from a Unity button or script

The sample usage is a simple chat-like UI where the user types text, presses SEND, and the model speaks it.

---

## 2. Package installation and dependencies

The package is already added in this project via `Packages/manifest.json` with the required entries:

```json
{
  "dependencies": {
    "com.github.asus4.onnxruntime": "0.4.2",
    "com.github.asus4.onnxruntime.unity": "0.4.2",
    "ai.lookbe.pockettts": "https://github.com/lookbe/pocket-tts-unity.git"
  },
  "scopedRegistries": [
    {
      "name": "npm",
      "url": "https://registry.npmjs.com",
      "scopes": [
        "com.github.asus4"
      ]
    }
  ]
}
```

If you need to reinstall it in another project, add the same package and the scoped registry for `com.github.asus4`.

> Important: the package README says the current supported platforms are Windows and Android.

---

## 3. Download the required model files

The package requires the Pocket-TTS ONNX model files.

Choose one language from the model repo and download these files:

- `mimi_encoder.onnx`
- `mimi_decoder_int8.onnx`
- `text_conditioner.onnx`
- `flow_lm_main_int8.onnx`
- `flow_lm_flow_int8.onnx`
- `tokenizer.model`
- `bos_before_voice.npy`

Put them in the folder that your local package or runtime expects. In practice, the package sample expects the model files to be available to the runtime before `InitModel()` is called.

If you are unsure, use the package sample scene as your reference and keep every required model file in the same runtime-accessible model directory.

---

## 4. Import the sample scene

The package includes a sample named:

- `Basic Pocket TTS`

In Unity, import the sample from the package manager or use the sample folder already added to this project.

The sample demonstrates the exact pattern you should follow:

- a `PocketTTS.PocketTTS` field
- a `TMP_InputField` for text
- a `Button` for sending text
- a `TMP_Text` area for chat/history

---

## 5. Create the basic Unity setup

Use the same structure as the sample:

1. Create an empty GameObject and name it `BasicTTS`
2. Add a `PocketTTS` component to it
3. Create a UI Canvas
4. Add:
   - `TMP_InputField` named `ChatInput`
   - `Button` named `Button`
   - `TMP_Text` named `ChatHistory`
5. Attach the script below to the GameObject

The sample script is already included and works as a practical starter:

```csharp
using PocketTTS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BasicPocketTTS : MonoBehaviour
{
    public PocketTTS.PocketTTS tts;

    public TMP_Text chatHistory;
    public TMP_InputField chatInputField;
    public Button sendButton;

    void Start()
    {
        tts.InitModel();
        sendButton.onClick.AddListener(OnSendButtonClicked);
    }

    private void OnEnable()
    {
        if (tts != null)
        {
            tts.OnStatusChanged += OnBotStatusChanged;
            OnBotStatusChanged(tts.status);
        }
    }

    private void OnDisable()
    {
        if (tts != null)
        {
            tts.OnStatusChanged -= OnBotStatusChanged;
        }
    }

    void OnBotStatusChanged(ModelStatus status)
    {
        switch (status)
        {
            case ModelStatus.Loading:
                sendButton.interactable = false;
                break;
            case ModelStatus.Ready:
                sendButton.GetComponentInChildren<TMP_Text>().text = "SEND";
                sendButton.interactable = true;
                ClearInput();
                break;
            case ModelStatus.Generate:
                sendButton.GetComponentInChildren<TMP_Text>().text = "STOP";
                break;
            case ModelStatus.Error:
                sendButton.interactable = true;
                break;
        }
    }

    protected virtual void ClearInput()
    {
        chatInputField.text = "";
    }

    public void OnSendButtonClicked()
    {
        if (tts)
        {
            if (tts.status == ModelStatus.Ready)
            {
                string message = chatInputField.text;
                if (!string.IsNullOrEmpty(message))
                {
                    chatHistory.text += "tts: " + message + "\n";
                    tts.Prompt(message);
                    ClearInput();
                }
            }
            else
            {
                sendButton.interactable = false;
            }
        }
    }
}
```

---

## 6. How the sample works

This is the important runtime flow:

1. `InitModel()` loads the model and initializes ONNX runtime.
2. The component reports its state through `ModelStatus`.
3. When state becomes `Ready`, the user can send text.
4. `Prompt(text)` requests speech generation.
5. The component updates UI text to `Generate` while speaking and returns to `Ready` when finished.

Common states:

- `Loading`
- `Ready`
- `Generate`
- `Error`

Use `OnStatusChanged` to react to state changes and enable or disable the UI safely.

---

## 7. Basic custom script example

If you want a minimal script instead of the sample scene, use this pattern:

```csharp
using PocketTTS;
using UnityEngine;

public class SimpleTTSExample : MonoBehaviour
{
    public PocketTTS.PocketTTS tts;

    void Start()
    {
        if (tts != null)
        {
            tts.InitModel();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && tts != null)
        {
            tts.Prompt("Hello from Pocket TTS");
        }
    }
}
```

This pattern is enough for quick speech testing in a scene.

---

## 8. Recommended project workflow

Follow this order when setting up a new scene:

1. Add the package dependencies in `Packages/manifest.json`
2. Download model files for the language you want to use
3. Import the sample and open the Basic TTS scene
4. Assign the `PocketTTS` component to the script fields
5. Make sure `InitModel()` is called during startup
6. Trigger `Prompt()` from a button or keyboard input
7. Watch the console for `[PocketTTS Stats]` and `[TTS RT]` logs

---

## 9. Mobile tuning (important for Android)

The package README highlights that mobile devices need tuning for performance and latency.

### Main tuning values

- `DiffusionStep`
  - Lower values reduce CPU load and help stop stutter
  - Higher values improve quality but increase computation cost

- `AudioChunkSize`
  - Larger chunks improve throughput but add latency
  - Smaller chunks improve responsiveness but can cause stutter

### Example profiles

- Mid-range device: `DiffusionStep = 3`, `AudioChunkSize = 16`
- High-end device: `DiffusionStep = 10`, `AudioChunkSize = 8`

The README also recommends watching console logs like:

```text
[TTS Stats] AR: 50ms | Flow: 20ms | Mimi: 300ms
[TTS RT] Ratio: 0.70x (LAGGING)
```

Use those values to balance:

- quality
- latency
- real-time playback stability

---

## 10. Typical troubleshooting checklist

If the model does not speak, check these in order:

- the package dependency is installed correctly
- `com.github.asus4.onnxruntime` is present
- model files are downloaded and accessible at runtime
- `tts.InitModel()` is called before `Prompt()`
- `tts.status` is `Ready` before sending text
- the built target is Windows or Android
- no model file path mismatch exists in the runtime folder

---

## 11. Best practice for this project

For an avatar or NPC project, the simplest and most reliable setup is:

- keep the sample UI as a test harness
- create a dedicated speech manager
- call `Prompt()` from your dialogue system
- subscribe to `OnStatusChanged` to show loading states
- avoid spamming `Prompt()` while the status is `Generate`

This keeps the TTS pipeline stable and makes it easy to integrate with general dialogue logic.

---

## 12. Quick summary

The pattern is simple:

```csharp
tts.InitModel();
tts.Prompt("Hello world");
```

And the safest UX flow is:

```text
Loading -> Ready -> Generate -> Ready
```

Once the model loads successfully, this package works best as a speech-output layer for interactive scenes, NPC dialogue, and voice-driven assistants.

---

## 13. Next step

If you want, the next useful addition is to turn this into a reusable `VoiceManager` that speaks dialogue lines from your game logic and exposes `Speak(string text)`, `Stop()`, and a `bool IsSpeaking` state.
