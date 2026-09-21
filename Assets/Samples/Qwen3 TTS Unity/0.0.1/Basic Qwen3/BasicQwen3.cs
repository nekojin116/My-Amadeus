using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Qwen3TTS.Samples
{
    public class BasicQwen3 : MonoBehaviour
    {
        public Qwen3TTS tts;
        public TMP_Text chatHistory;
        public TMP_InputField chatInputField;
        public Button sendButton;

        private void Start()
        {
            if (tts != null)
            {
                tts.InitModel();
            }

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

            sendButton.onClick.RemoveListener(OnSendButtonClicked);
        }

        void OnBotStatusChanged(ModelStatus status)
        {
            switch (status)
            {
                case ModelStatus.Loading:
                    {
                        sendButton.interactable = false;
                    }
                    break;
                case ModelStatus.Ready:
                    {
                        sendButton.GetComponentInChildren<TMP_Text>().text = "SEND";
                        sendButton.interactable = true;
                        ClearInput();
                    }
                    break;
                case ModelStatus.Generate:
                    {
                        sendButton.GetComponentInChildren<TMP_Text>().text = "STOP";
                    }
                    break;
                case ModelStatus.Error:
                    {
                        sendButton.interactable = true;
                    }
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
                        tts.Synthesize(message);
                        ClearInput();
                    }
                }
                else
                {
                    sendButton.interactable = false;
                    tts.Stop();
                }
            }
        }
    }
}
