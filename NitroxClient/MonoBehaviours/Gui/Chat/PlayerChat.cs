﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NitroxClient.GameLogic.ChatUI;
using NitroxClient.GameLogic.Settings;
using NitroxModel.Core;
using UnityEngine;
using UnityEngine.UI;

namespace NitroxClient.MonoBehaviours.Gui.Chat
{
    public class PlayerChat : uGUI_InputGroup
    {
        private const int LINE_CHAR_LIMIT = 255;
        private const int MESSAGES_LIMIT = 64;
        private const float TOGGLED_TRANSPARENCY = 0.4f;
        public const float CHAT_VISIBILITY_TIME_LENGTH = 6f;

        private static readonly Queue<ChatLogEntry> entries = new Queue<ChatLogEntry>();
        private Image[] backgroundImages;
        private CanvasGroup canvasGroup;
        private InputField inputField;
        private GameObject logEntryPrefab;

        private PlayerChatManager playerChatManager;
        private bool transparent;

        public static bool IsReady { get; private set; }

        public string InputText
        {
            get => inputField.text;
            set => inputField.text = value;
        }

        public IEnumerator SetupChatComponents()
        {
            playerChatManager = NitroxServiceLocator.LocateService<PlayerChatManager>();

            canvasGroup = GetComponent<CanvasGroup>();

            logEntryPrefab = GameObject.Find("ChatLogEntryPrefab");
            logEntryPrefab.AddComponent<PlayerChatLogItem>();
            logEntryPrefab.SetActive(false);

            GetComponentsInChildren<Button>()[0].onClick.AddListener(ToggleBackgroundTransparency);
            GetComponentsInChildren<Button>()[1].gameObject.AddComponent<PlayerChatPinButton>();

            inputField = GetComponentInChildren<InputField>();
            inputField.gameObject.AddComponent<PlayerChatInputField>().InputField = inputField;
            inputField.GetComponentInChildren<Button>().onClick.AddListener(playerChatManager.SendMessage);

            // We pick any image that's inside the chat component to have all of their opacity lowered
            backgroundImages = transform.GetComponentsInChildren<Image>();

            yield return new WaitForEndOfFrame(); //Needed so Select() works on initialization
            IsReady = true;
            if (NitroxPrefs.SilenceChat.Value)
            {
                Log.InGame(Language.main.Get("Nitrox_SilencedChatNotif"));
            }
        }

        public void WriteLogEntry(string playerName, string message, Color color)
        {
            if (entries.Count == MESSAGES_LIMIT)
            {
                Destroy(entries.Dequeue().EntryObject);
            }

            ChatLogEntry chatLogEntry;
            GameObject chatLogEntryObject;
            if (entries.Count != 0 && entries.Last().PlayerName == playerName)
            {
                chatLogEntry = entries.Last();
                chatLogEntry.MessageText += $"{Environment.NewLine}{message}";
                chatLogEntry.UpdateTime();
                chatLogEntryObject = chatLogEntry.EntryObject;
            }
            else
            {
                chatLogEntry = new ChatLogEntry(playerName, SanitizeMessage(message), color);
                chatLogEntryObject = Instantiate(logEntryPrefab, logEntryPrefab.transform.parent, false);
                chatLogEntry.EntryObject = chatLogEntryObject;
                entries.Enqueue(chatLogEntry);
            }

            chatLogEntryObject.GetComponent<PlayerChatLogItem>().ApplyOnPrefab(chatLogEntry);
        }

        public void Show()
        {
            PlayerChatInputField.ResetTimer();
            StartCoroutine(ToggleChatFade(true));
        }

        public void Hide()
        {
            StartCoroutine(ToggleChatFade(false));
        }

        public void Select()
        {
            base.Select(true);
            inputField.Select();
            inputField.ActivateInputField();
        }

        private static string SanitizeMessage(string message)
        {
            message = message.Trim().TrimEnd('\n').Trim();
            return message.Length < LINE_CHAR_LIMIT ? message : message.Substring(0, LINE_CHAR_LIMIT);
        }

        private void ToggleBackgroundTransparency()
        {
            float alpha = transparent ? 1f : TOGGLED_TRANSPARENCY;
            transparent = !transparent;

            foreach (Image backgroundImage in backgroundImages)
            {
                backgroundImage.CrossFadeAlpha(alpha, 0.5f, false);
            }
        }

        private IEnumerator ToggleChatFade(bool fadeIn)
        {
            if (fadeIn)
            {
                while (canvasGroup.alpha < 1f)
                {
                    canvasGroup.alpha += 0.01f;
                    yield return null;
                }
            }
            else
            {
                while (canvasGroup.alpha > 0f)
                {
                    canvasGroup.alpha -= 0.01f;
                    yield return null;
                }
            }
        }
    }
}