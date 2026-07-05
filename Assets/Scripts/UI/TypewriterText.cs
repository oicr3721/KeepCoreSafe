using System;
using System.Collections;
using KeepCoreSafe.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace KeepCoreSafe.UI
{
    public sealed class TypewriterText : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text textLabel;
        [Tooltip("Optional full-screen/background button used for skip and advance input.")]
        [SerializeField] private Button inputButton;

        [Header("Typing")]
        [Tooltip("Visible characters revealed per second. Uses unscaled time.")]
        [SerializeField, Min(1f)] private float charactersPerSecond = 32f;
        [SerializeField] private AudioCue typingSound = new();
        [SerializeField] private bool playSoundForWhitespace;

        [Header("Inspector Events")]
        [SerializeField] private UnityEvent onTypingStarted;
        [SerializeField] private UnityEvent onTypingFinished;
        [SerializeField] private UnityEvent onSkip;

        private Coroutine typingRoutine;
        private string currentText = string.Empty;

        public bool IsTyping { get; private set; }

        public event Action TypingStarted;
        public event Action TypingFinished;
        public event Action Skipped;
        public event Action AdvanceRequested;

        private void Awake()
        {
            if (textLabel == null)
                textLabel = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            inputButton?.onClick.AddListener(HandleInput);
        }

        private void OnDisable()
        {
            inputButton?.onClick.RemoveListener(HandleInput);
            StopTyping(false);
        }

        public void Play(string text)
        {
            StopTyping(false);
            currentText = text ?? string.Empty;
            if (textLabel == null)
                return;

            textLabel.text = currentText;
            textLabel.maxVisibleCharacters = 0;
            textLabel.ForceMeshUpdate();
            IsTyping = true;
            TypingStarted?.Invoke();
            onTypingStarted?.Invoke();
            typingRoutine = StartCoroutine(TypeRoutine());
        }

        public void CompleteImmediately()
        {
            if (!IsTyping || textLabel == null)
                return;

            StopTyping(false);
            textLabel.maxVisibleCharacters = int.MaxValue;
            IsTyping = false;
            Skipped?.Invoke();
            onSkip?.Invoke();
            NotifyFinished();
        }

        public void Clear()
        {
            StopTyping(false);
            currentText = string.Empty;
            if (textLabel != null)
            {
                textLabel.text = string.Empty;
                textLabel.maxVisibleCharacters = int.MaxValue;
            }
        }

        private IEnumerator TypeRoutine()
        {
            int characterCount = textLabel.textInfo.characterCount;
            float interval = 1f / Mathf.Max(1f, charactersPerSecond);
            for (int visible = 1; visible <= characterCount; visible++)
            {
                textLabel.maxVisibleCharacters = visible;
                char character = textLabel.textInfo.characterInfo[visible - 1].character;
                if (playSoundForWhitespace || !char.IsWhiteSpace(character))
                {
                    AudioManager.Play(typingSound);
                }

                yield return new WaitForSecondsRealtime(interval);
            }

            typingRoutine = null;
            IsTyping = false;
            NotifyFinished();
        }

        private void HandleInput()
        {
            if (IsTyping)
                CompleteImmediately();
            else
                AdvanceRequested?.Invoke();
        }

        private void NotifyFinished()
        {
            TypingFinished?.Invoke();
            onTypingFinished?.Invoke();
        }

        private void StopTyping(bool finish)
        {
            if (typingRoutine != null)
            {
                StopCoroutine(typingRoutine);
                typingRoutine = null;
            }

            bool wasTyping = IsTyping;
            IsTyping = false;
            if (finish && wasTyping)
                NotifyFinished();
        }
    }
}
