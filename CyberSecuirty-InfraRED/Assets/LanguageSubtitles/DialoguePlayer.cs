using System;
using System.Collections;
using UnityEngine;

namespace Game.Dialogue
{
    [RequireComponent(typeof(AudioSource))]
    public class DialoguePlayer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LanguageSettings languageSettings;
        [SerializeField] private DialoguePresenter presenter;
        [SerializeField] private AudioSource voiceSource;

        [Header("Behavior")]
        [SerializeField] private bool autoAdvanceAfterVoice = true;
        [SerializeField] private float extraDelayAfterVoice = 0.15f;

        public bool IsPlaying => _isPlaying;
        public event Action OnDialogueFinished;

        private DialogueSequence _sequence;
        private int _index = -1;
        private bool _isPlaying;
        private Coroutine _autoRoutine;

        private void Reset()
        {
            voiceSource = GetComponent<AudioSource>();
            voiceSource.playOnAwake = false;
            voiceSource.loop = false;
        }

        private void Awake()
        {
            if (voiceSource == null) voiceSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            if (languageSettings != null)
                languageSettings.OnLanguageChanged += RefreshCurrentSubtitle;
        }

        private void OnDisable()
        {
            if (languageSettings != null)
                languageSettings.OnLanguageChanged -= RefreshCurrentSubtitle;
        }

        public void Play(DialogueSequence sequence)
        {
            if (sequence == null || sequence.lines == null || sequence.lines.Count == 0)
                return;

            StopInternal(noFinishedEvent: true);

            _sequence = sequence;
            _index = -1;
            _isPlaying = true;

            presenter.Show();
            Next(); // start first line
        }

        public void Next()
        {
            if (!_isPlaying || _sequence == null) return;

            if (_autoRoutine != null)
            {
                StopCoroutine(_autoRoutine);
                _autoRoutine = null;
            }

            _index++;
            if (_index >= _sequence.lines.Count)
            {
                Stop();
                return;
            }

            PlayCurrentLine();
        }

        public void Stop()
        {
            if (!_isPlaying) return;
            StopInternal(noFinishedEvent: false);
        }

        private void StopInternal(bool noFinishedEvent)
        {
            if (_autoRoutine != null)
            {
                StopCoroutine(_autoRoutine);
                _autoRoutine = null;
            }

            if (voiceSource != null)
                voiceSource.Stop();

            _isPlaying = false;
            _sequence = null;
            _index = -1;

            if (presenter != null)
            {
                presenter.Clear();
                presenter.Hide();
            }

            if (!noFinishedEvent)
                OnDialogueFinished?.Invoke();
        }

        private void PlayCurrentLine()
        {
            var line = _sequence.lines[_index];
            var lang = languageSettings != null ? languageSettings.CurrentLanguage : GameLanguage.English;

            presenter.SetLine(line.GetText(lang), line.speakerName);

            var clip = line.GetVoice(lang);
            if (clip != null)
            {
                voiceSource.clip = clip;
                voiceSource.Play();

                if (autoAdvanceAfterVoice)
                    _autoRoutine = StartCoroutine(AutoAdvanceWhenVoiceEnds());
            }
        }

        private IEnumerator AutoAdvanceWhenVoiceEnds()
        {
            while (voiceSource != null && voiceSource.isPlaying)
                yield return null;

            if (extraDelayAfterVoice > 0f)
                yield return new WaitForSeconds(extraDelayAfterVoice);

            if (_isPlaying)
                Next();
        }

        private void RefreshCurrentSubtitle(GameLanguage _)
        {
            if (!_isPlaying || _sequence == null) return;
            if (_index < 0 || _index >= _sequence.lines.Count) return;

            var line = _sequence.lines[_index];
            var lang = languageSettings.CurrentLanguage;

            presenter.SetLine(line.GetText(lang), line.speakerName);
        }
    }
}