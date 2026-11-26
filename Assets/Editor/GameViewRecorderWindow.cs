using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEditor;
using UnityEditor.Media;
using UnityEngine;

namespace Aiara.EditorTools
{
    public class GameViewRecorderWindow : EditorWindow
    {
        [MenuItem("Tools/Game View Recorder/Open Recorder Window", false, 10)]
        public static void ShowWindow()
        {
            var win = GetWindow<GameViewRecorderWindow>("Game Recorder");
            win.minSize = new Vector2(360f, 340f);
            win.Show();
        }

        [MenuItem("Tools/Game View Recorder/Toggle Recording %#r", false, 11)]
        public static void ToggleRecordingShortcut()
        {
            if (GameViewRecorderSession.IsRecording)
                GameViewRecorderSession.StopRecording();
            else
                GameViewRecorderSession.RequestStartRecording();
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdateRepaint;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdateRepaint;
        }

        private void OnEditorUpdateRepaint()
        {
            if (GameViewRecorderSession.IsRecording || GameViewRecorderSession.IsPendingStart)
            {
                Repaint();
            }
        }

        private void OnGUI()
        {
            GUILayout.Space(8f);
            DrawStatusHeader();
            GUILayout.Space(8f);
            DrawSettingsSection();
            GUILayout.Space(10f);
            DrawControlsSection();
            GUILayout.Space(8f);
            DrawLastRecordingSection();
        }

        private void DrawStatusHeader()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                bool recording = GameViewRecorderSession.IsRecording;
                bool pending = GameViewRecorderSession.IsPendingStart;

                var oldColor = GUI.color;
                GUI.color = recording ? new Color(1f, 0.35f, 0.35f) : (pending ? Color.yellow : new Color(0.55f, 0.95f, 0.6f));
                string stateLabel = recording
                    ? $"● RECORDING   {FormatDuration(GameViewRecorderSession.ElapsedSeconds)}"
                    : (pending ? "◐ STARTING PLAY MODE..." : "○ IDLE (Ready)");
                var boldLarge = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
                EditorGUILayout.LabelField(stateLabel, boldLarge);
                GUI.color = oldColor;

                if (recording)
                {
                    EditorGUILayout.LabelField(
                        $"Video: {GameViewRecorderSession.VideoWidth}x{GameViewRecorderSession.VideoHeight} @ {GameViewRecorderSession.TargetFps} fps | Frames: {GameViewRecorderSession.EncodedFrames}");
                    if (GameViewRecorderSession.CaptureAudio)
                    {
                        EditorGUILayout.LabelField(
                            $"Audio: {GameViewRecorderSession.AudioSampleRate} Hz, {GameViewRecorderSession.AudioChannels} ch | Samples: {GameViewRecorderSession.EncodedAudioSamples:N0}");
                        Rect levelRect = GUILayoutUtility.GetRect(18f, 14f, GUILayout.ExpandWidth(true));
                        EditorGUI.ProgressBar(levelRect, Mathf.Clamp01(GameViewRecorderSession.AudioPeakLevel * 4f),
                            GameViewRecorderSession.AudioListenerFound ? "Audio Input Level" : "Waiting for AudioListener...");
                    }
                    EditorGUILayout.LabelField("Saving to:", GameViewRecorderSession.CurrentRelativePath, EditorStyles.miniLabel);
                }
            }
        }

        private void DrawSettingsSection()
        {
            EditorGUILayout.LabelField("Recording Settings", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(GameViewRecorderSession.IsRecording || GameViewRecorderSession.IsPendingStart))
            {
                string folder = EditorGUILayout.TextField(
                    new GUIContent("Relative Output Folder", "Thư mục lưu tính từ thư mục gốc dự án (ví dụ: Recordings)"),
                    GameViewRecorderSession.RelativeOutputFolder);
                if (folder != GameViewRecorderSession.RelativeOutputFolder)
                {
                    GameViewRecorderSession.RelativeOutputFolder = folder;
                }

                string previewRel = GameViewRecorderSession.GetPreviewRelativePath();
                EditorGUILayout.LabelField("Output Relative Path:", previewRel, EditorStyles.miniLabel);

                int[] fpsOptions = { 24, 30, 60 };
                string[] fpsLabels = { "24 FPS", "30 FPS", "60 FPS" };
                int currentFps = GameViewRecorderSession.TargetFps;
                int fpsIdx = Array.IndexOf(fpsOptions, currentFps);
                if (fpsIdx < 0) fpsIdx = 1;
                int newFpsIdx = EditorGUILayout.Popup("Frame Rate", fpsIdx, fpsLabels);
                if (newFpsIdx != fpsIdx) GameViewRecorderSession.TargetFps = fpsOptions[newFpsIdx];

                var newBitrate = (VideoBitrateMode)EditorGUILayout.EnumPopup("Quality (Bitrate)", GameViewRecorderSession.BitrateMode);
                if (newBitrate != GameViewRecorderSession.BitrateMode) GameViewRecorderSession.BitrateMode = newBitrate;

                bool newAudio = EditorGUILayout.Toggle("Capture Audio", GameViewRecorderSession.CaptureAudio);
                if (newAudio != GameViewRecorderSession.CaptureAudio) GameViewRecorderSession.CaptureAudio = newAudio;
            }

            if (GameViewRecorderSession.CaptureAudio && EditorUtility.audioMasterMute)
            {
                EditorGUILayout.HelpBox(
                    "Game View đang bật Mute Audio nên video sẽ không có tiếng. Hãy tắt Mute Audio trước khi quay.",
                    MessageType.Warning);
                if (GUILayout.Button("Unmute Game View Audio"))
                {
                    EditorUtility.audioMasterMute = false;
                }
            }
        }

        private void DrawControlsSection()
        {
            bool recording = GameViewRecorderSession.IsRecording;
            bool pending = GameViewRecorderSession.IsPendingStart;

            var oldBg = GUI.backgroundColor;
            if (recording)
            {
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("Stop Recording (Save MP4)", GUILayout.Height(38f)))
                {
                    GameViewRecorderSession.StopRecording();
                }
            }
            else if (pending)
            {
                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("Cancel Pending Start", GUILayout.Height(38f)))
                {
                    GameViewRecorderSession.CancelPendingStart();
                }
            }
            else
            {
                GUI.backgroundColor = new Color(0.35f, 0.9f, 0.5f);
                string btnText = EditorApplication.isPlaying
                    ? "Start Recording Game View"
                    : "Enter Play Mode & Start Recording";
                if (GUILayout.Button(btnText, GUILayout.Height(38f)))
                {
                    GameViewRecorderSession.RequestStartRecording();
                }
            }
            GUI.backgroundColor = oldBg;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Output Folder"))
                {
                    string absDir = GameViewRecorderSession.GetResolvedOutputDirectory();
                    Directory.CreateDirectory(absDir);
                    EditorUtility.RevealInFinder(absDir);
                }

                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(GameViewRecorderSession.LastRecordedRelativePath)))
                {
                    if (GUILayout.Button("Reveal Last Video"))
                    {
                        string absFile = GameViewRecorderSession.ToAbsolutePath(GameViewRecorderSession.LastRecordedRelativePath);
                        if (File.Exists(absFile))
                            EditorUtility.RevealInFinder(absFile);
                        else
                            EditorUtility.DisplayDialog("File Not Found", $"Không tìm thấy file:\n{absFile}", "OK");
                    }
                }
            }
        }

        private void DrawLastRecordingSection()
        {
            if (string.IsNullOrEmpty(GameViewRecorderSession.LastRecordedRelativePath)) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Last Saved Video", EditorStyles.boldLabel);
                EditorGUILayout.SelectableLabel(GameViewRecorderSession.LastRecordedRelativePath, EditorStyles.miniLabel, GUILayout.Height(18f));
                EditorGUILayout.LabelField(GameViewRecorderSession.LastRecordedSummary, EditorStyles.miniLabel);
            }
        }

        private static string FormatDuration(double seconds)
        {
            var ts = TimeSpan.FromSeconds(Math.Max(0, seconds));
            return $"{(int)ts.TotalMinutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 100}";
        }
    }

    [InitializeOnLoad]
    public static class GameViewRecorderSession
    {
        private const string PrefRelativeFolder = "GameViewRecorder_RelativeFolder";
        private const string PrefTargetFps = "GameViewRecorder_TargetFps";
        private const string PrefBitrateMode = "GameViewRecorder_BitrateMode";
        private const string PrefCaptureAudio = "GameViewRecorder_CaptureAudio";
        private const string SessionPendingStartKey = "GameViewRecorder_PendingStart";
        private const string SessionLastRelativePathKey = "GameViewRecorder_LastRelPath";
        private const string SessionLastSummaryKey = "GameViewRecorder_LastSummary";

        private static MediaEncoder _encoder;
        private static GameViewRecorderRunner _runner;
        private static GameViewAudioTap _audioTap;
        private static RenderTexture _evenRt;
        private static Texture2D _evenTex;
        private static Texture2D _ownedLatestTex;
        private static Texture2D _latestTex;

        private static readonly object _audioLock = new object();
        private static readonly Queue<float[]> _pendingAudioChunks = new Queue<float[]>();
        private static readonly Stack<float[]> _audioChunkPool = new Stack<float[]>();
        private static readonly List<float> _audioScratch = new List<float>(8192);

        private static double _recordStartDspTime;
        private static double _recordStartWallTime;
        private static double _nextCaptureClock;
        private static volatile bool _captureAudioCached = true;
        private static volatile bool _hasStartedEncoder;
        private static volatile bool _stopRequested;
        private static int _warmupFrames;

        static GameViewRecorderSession()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        public static string RelativeOutputFolder
        {
            get => EditorPrefs.GetString(PrefRelativeFolder, "Recordings");
            set => EditorPrefs.SetString(PrefRelativeFolder, SanitizeRelativeFolder(value));
        }

        public static int TargetFps
        {
            get => Mathf.Clamp(EditorPrefs.GetInt(PrefTargetFps, 30), 10, 120);
            set => EditorPrefs.SetInt(PrefTargetFps, Mathf.Clamp(value, 10, 120));
        }

        public static VideoBitrateMode BitrateMode
        {
            get => (VideoBitrateMode)EditorPrefs.GetInt(PrefBitrateMode, (int)VideoBitrateMode.High);
            set => EditorPrefs.SetInt(PrefBitrateMode, (int)value);
        }

        public static bool CaptureAudio
        {
            get => EditorPrefs.GetBool(PrefCaptureAudio, true);
            set => EditorPrefs.SetBool(PrefCaptureAudio, value);
        }

        public static bool IsRecording { get; private set; }
        public static bool IsPendingStart => SessionState.GetBool(SessionPendingStartKey, false);
        public static int VideoWidth { get; private set; }
        public static int VideoHeight { get; private set; }
        public static int AudioSampleRate { get; private set; }
        public static ushort AudioChannels { get; private set; }
        public static long EncodedFrames { get; private set; }
        public static long EncodedAudioSamples { get; private set; }
        public static float AudioPeakLevel { get; private set; }
        public static bool AudioListenerFound => _audioTap != null;
        public static string CurrentRelativePath { get; private set; } = string.Empty;
        public static string CurrentAbsolutePath { get; private set; } = string.Empty;

        public static string LastRecordedRelativePath
        {
            get => SessionState.GetString(SessionLastRelativePathKey, string.Empty);
            private set => SessionState.SetString(SessionLastRelativePathKey, value ?? string.Empty);
        }

        public static string LastRecordedSummary
        {
            get => SessionState.GetString(SessionLastSummaryKey, string.Empty);
            private set => SessionState.SetString(SessionLastSummaryKey, value ?? string.Empty);
        }

        public static double ElapsedSeconds
        {
            get
            {
                if (!IsRecording || !_hasStartedEncoder) return 0;
                return Math.Max(0, GetCurrentClockSeconds());
            }
        }

        public static string GetProjectRoot()
        {
            return Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();
        }

        public static string GetResolvedOutputDirectory()
        {
            string rel = SanitizeRelativeFolder(RelativeOutputFolder);
            return Path.GetFullPath(Path.Combine(GetProjectRoot(), rel));
        }

        public static string GetPreviewRelativePath()
        {
            string rel = SanitizeRelativeFolder(RelativeOutputFolder).Replace('\\', '/');
            return $"{rel}/GameRecording_{DateTime.Now:yyyyMMdd_HHmmss}.mp4";
        }

        public static string ToAbsolutePath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return string.Empty;
            return Path.GetFullPath(Path.Combine(GetProjectRoot(), relativePath));
        }

        private static string SanitizeRelativeFolder(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Recordings";
            string cleaned = raw.Trim().Replace('\\', '/').Trim('/');
            if (Path.IsPathRooted(cleaned) || cleaned.Contains(".."))
            {
                return "Recordings";
            }
            return string.IsNullOrEmpty(cleaned) ? "Recordings" : cleaned;
        }

        public static void RequestStartRecording()
        {
            if (IsRecording) return;

            if (EditorApplication.isPlaying)
            {
                StartRecording();
            }
            else
            {
                SessionState.SetBool(SessionPendingStartKey, true);
                EditorApplication.isPlaying = true;
            }
        }

        public static void CancelPendingStart()
        {
            SessionState.SetBool(SessionPendingStartKey, false);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(SessionPendingStartKey, false))
            {
                SessionState.SetBool(SessionPendingStartKey, false);
                StartRecording();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode && IsRecording)
            {
                StopRecording();
            }
        }

        private static void OnEditorUpdate()
        {
            if (IsRecording && !EditorApplication.isPlaying)
            {
                StopRecording();
            }
        }

        public static void StartRecording()
        {
            if (IsRecording) return;
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[GameViewRecorder] Chỉ có thể quay trong Play Mode.");
                return;
            }

            if (CaptureAudio && EditorUtility.audioMasterMute)
            {
                Debug.LogWarning("[GameViewRecorder] Game View đang bật Mute Audio -> audio thu được có thể bị im lặng.");
            }

            string relFolder = SanitizeRelativeFolder(RelativeOutputFolder).Replace('\\', '/');
            string fileName = $"GameRecording_{DateTime.Now:yyyyMMdd_HHmmss}.mp4";
            CurrentRelativePath = $"{relFolder}/{fileName}";
            CurrentAbsolutePath = ToAbsolutePath(CurrentRelativePath);

            string dir = Path.GetDirectoryName(CurrentAbsolutePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            ClearAudioBuffers();
            _captureAudioCached = CaptureAudio;
            _hasStartedEncoder = false;
            _stopRequested = false;
            _warmupFrames = 0;
            _nextCaptureClock = 0;
            EncodedFrames = 0;
            EncodedAudioSamples = 0;
            AudioPeakLevel = 0f;
            IsRecording = true;

            var runnerGo = new GameObject("[GameViewRecorderRunner]") { hideFlags = HideFlags.HideAndDontSave };
            UnityEngine.Object.DontDestroyOnLoad(runnerGo);
            _runner = runnerGo.AddComponent<GameViewRecorderRunner>();

            EnsureAudioTapAttached();
            Debug.Log($"[GameViewRecorder] Đã chuẩn bị quay Game View -> {CurrentRelativePath}");
        }

        public static void StopRecording()
        {
            if (!IsRecording) return;
            _stopRequested = true;

            try
            {
                DrainAudioSamples();
                EmitFramesToCatchUp();

                if (_encoder != null)
                {
                    _encoder.Dispose();
                    _encoder = null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameViewRecorder] Lỗi khi đóng encoder: {ex.Message}");
            }

            IsRecording = false;
            CleanupObjectsAndTextures();

            if (!string.IsNullOrEmpty(CurrentAbsolutePath) && File.Exists(CurrentAbsolutePath))
            {
                var info = new FileInfo(CurrentAbsolutePath);
                LastRecordedRelativePath = CurrentRelativePath;
                LastRecordedSummary = $"{info.Length / 1024f:F1} KB | {EncodedFrames} video frames | {EncodedAudioSamples:N0} audio samples";
                Debug.Log($"[GameViewRecorder] Đã lưu video thành công tại relative path: {CurrentRelativePath} ({LastRecordedSummary})");
            }
            else
            {
                Debug.LogWarning("[GameViewRecorder] Đã dừng quay nhưng không có frame nào được ghi.");
            }
        }

        internal static void OnEndOfFrameStep()
        {
            if (!IsRecording || _stopRequested) return;

            EnsureAudioTapAttached();

            if (!_hasStartedEncoder)
            {
                _warmupFrames++;
                if (_warmupFrames < 2 || Screen.width < 16 || Screen.height < 16)
                {
                    return;
                }

                if (!InitializeEncoder())
                {
                    StopRecording();
                    return;
                }
            }

            DrainAudioSamples();

            double elapsed = GetCurrentClockSeconds();
            if (EncodedFrames > 0 && elapsed < _nextCaptureClock)
            {
                return;
            }

            var captured = ScreenCapture.CaptureScreenshotAsTexture();
            if (captured == null) return;

            if (captured.width == VideoWidth && captured.height == VideoHeight)
            {
                if (_ownedLatestTex != null)
                {
                    UnityEngine.Object.DestroyImmediate(_ownedLatestTex);
                }
                _ownedLatestTex = captured;
                _latestTex = captured;
            }
            else
            {
                EnsureEvenResizeTarget();
                Graphics.Blit(captured, _evenRt);
                var prevActive = RenderTexture.active;
                RenderTexture.active = _evenRt;
                _evenTex.ReadPixels(new Rect(0, 0, VideoWidth, VideoHeight), 0, 0);
                _evenTex.Apply(false);
                RenderTexture.active = prevActive;
                UnityEngine.Object.DestroyImmediate(captured);
                _latestTex = _evenTex;
            }

            _nextCaptureClock = elapsed + (1.0 / TargetFps) * 0.85;
            EmitFramesToCatchUp();
        }

        private static bool InitializeEncoder()
        {
            VideoWidth = Math.Max(2, Screen.width & ~1);
            VideoHeight = Math.Max(2, Screen.height & ~1);
            AudioSampleRate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000;
            AudioChannels = GetSpeakerChannelCount();

            var videoAttrs = new VideoTrackAttributes
            {
                width = (uint)VideoWidth,
                height = (uint)VideoHeight,
                frameRate = new MediaRational { numerator = TargetFps, denominator = 1 },
                bitRateMode = BitrateMode,
                includeAlpha = false
            };

            try
            {
                if (CaptureAudio)
                {
                    var audioAttrs = new AudioTrackAttributes
                    {
                        sampleRate = new MediaRational { numerator = AudioSampleRate, denominator = 1 },
                        channelCount = AudioChannels,
                        language = "en"
                    };
                    _encoder = new MediaEncoder(CurrentAbsolutePath, videoAttrs, audioAttrs);
                }
                else
                {
                    _encoder = new MediaEncoder(CurrentAbsolutePath, videoAttrs);
                }

                _recordStartDspTime = AudioSettings.dspTime;
                _recordStartWallTime = EditorApplication.timeSinceStartup;
                _hasStartedEncoder = true;
                ClearAudioBuffers();
                Debug.Log($"[GameViewRecorder] Bắt đầu ghi hình {VideoWidth}x{VideoHeight}@{TargetFps}fps, Audio={CaptureAudio} ({AudioSampleRate}Hz, {AudioChannels}ch) -> {CurrentRelativePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameViewRecorder] Không thể khởi tạo MediaEncoder: {ex.Message}");
                return false;
            }
        }

        private static void EmitFramesToCatchUp()
        {
            if (_encoder == null || _latestTex == null) return;

            double elapsed = GetCurrentClockSeconds();
            long targetFrames = Math.Max(1, (long)Math.Floor(elapsed * TargetFps) + 1);
            long needed = targetFrames - EncodedFrames;
            if (needed <= 0) return;

            long emitCount = needed > 6 ? 6 : needed;
            for (long i = 0; i < emitCount; i++)
            {
                if (_encoder.AddFrame(_latestTex))
                {
                    EncodedFrames++;
                }
            }
        }

        private static void DrainAudioSamples()
        {
            if (!CaptureAudio || _encoder == null || !_hasStartedEncoder) return;

            _audioScratch.Clear();
            lock (_audioLock)
            {
                while (_pendingAudioChunks.Count > 0)
                {
                    float[] chunk = _pendingAudioChunks.Dequeue();
                    _audioScratch.AddRange(chunk);
                    if (_audioChunkPool.Count < 32)
                    {
                        _audioChunkPool.Push(chunk);
                    }
                }
            }

            if (_audioScratch.Count == 0) return;

            int validCount = _audioScratch.Count - (_audioScratch.Count % AudioChannels);
            if (validCount <= 0) return;

            var nativeSamples = new NativeArray<float>(validCount, Allocator.Temp);
            try
            {
                for (int i = 0; i < validCount; i++)
                {
                    nativeSamples[i] = _audioScratch[i];
                }
                if (_encoder.AddSamples(nativeSamples))
                {
                    EncodedAudioSamples += validCount / AudioChannels;
                }
            }
            finally
            {
                if (nativeSamples.IsCreated)
                {
                    nativeSamples.Dispose();
                }
            }
        }

        internal static void PushAudioSamplesFromThread(float[] data, int channels)
        {
            if (!IsRecording || !_captureAudioCached || !_hasStartedEncoder || _stopRequested || data == null || data.Length == 0)
            {
                return;
            }

            int dstChannels = AudioChannels > 0 ? AudioChannels : 2;
            int safeSrcChannels = channels > 0 ? channels : 1;
            int frames = data.Length / safeSrcChannels;
            if (frames <= 0) return;

            int neededLen = frames * dstChannels;
            float peak = 0f;

            lock (_audioLock)
            {
                if (_pendingAudioChunks.Count > 128) return;

                float[] buf = null;
                while (_audioChunkPool.Count > 0)
                {
                    var candidate = _audioChunkPool.Pop();
                    if (candidate.Length == neededLen)
                    {
                        buf = candidate;
                        break;
                    }
                }
                if (buf == null)
                {
                    buf = new float[neededLen];
                }

                if (safeSrcChannels == dstChannels)
                {
                    Buffer.BlockCopy(data, 0, buf, 0, neededLen * sizeof(float));
                    for (int i = 0; i < neededLen; i++)
                    {
                        float a = buf[i] < 0f ? -buf[i] : buf[i];
                        if (a > peak) peak = a;
                    }
                }
                else
                {
                    for (int f = 0; f < frames; f++)
                    {
                        for (int c = 0; c < dstChannels; c++)
                        {
                            int srcC = c < safeSrcChannels ? c : safeSrcChannels - 1;
                            float sample = data[f * safeSrcChannels + srcC];
                            buf[f * dstChannels + c] = sample;
                            float a = sample < 0f ? -sample : sample;
                            if (a > peak) peak = a;
                        }
                    }
                }

                _pendingAudioChunks.Enqueue(buf);
            }

            AudioPeakLevel = AudioPeakLevel + (peak - AudioPeakLevel) * 0.35f;
        }

        private static double GetCurrentClockSeconds()
        {
            if (CaptureAudio && AudioListenerFound)
            {
                double dspDelta = AudioSettings.dspTime - _recordStartDspTime;
                if (dspDelta > 0) return dspDelta;
            }
            return EditorApplication.timeSinceStartup - _recordStartWallTime;
        }

        private static void EnsureAudioTapAttached()
        {
            if (!CaptureAudio) return;
            if (_audioTap != null && _audioTap.isActiveAndEnabled) return;

            var listener = UnityEngine.Object.FindFirstObjectByType<AudioListener>();
            if (listener == null) return;

            _audioTap = listener.GetComponent<GameViewAudioTap>();
            if (_audioTap == null)
            {
                _audioTap = listener.gameObject.AddComponent<GameViewAudioTap>();
                _audioTap.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        private static void EnsureEvenResizeTarget()
        {
            if (_evenRt == null || _evenRt.width != VideoWidth || _evenRt.height != VideoHeight)
            {
                if (_evenRt != null) _evenRt.Release();
                _evenRt = new RenderTexture(VideoWidth, VideoHeight, 0, RenderTextureFormat.ARGB32);
                _evenRt.Create();
            }
            if (_evenTex == null || _evenTex.width != VideoWidth || _evenTex.height != VideoHeight)
            {
                if (_evenTex != null) UnityEngine.Object.DestroyImmediate(_evenTex);
                _evenTex = new Texture2D(VideoWidth, VideoHeight, TextureFormat.RGBA32, false);
            }
        }

        private static ushort GetSpeakerChannelCount()
        {
            switch (AudioSettings.speakerMode)
            {
                case AudioSpeakerMode.Mono: return 1;
                case AudioSpeakerMode.Stereo: return 2;
                case AudioSpeakerMode.Quad: return 4;
                case AudioSpeakerMode.Surround: return 5;
                case AudioSpeakerMode.Mode5point1: return 6;
                case AudioSpeakerMode.Mode7point1: return 8;
                default: return 2;
            }
        }

        private static void ClearAudioBuffers()
        {
            lock (_audioLock)
            {
                _pendingAudioChunks.Clear();
            }
            _audioScratch.Clear();
        }

        private static void CleanupObjectsAndTextures()
        {
            ClearAudioBuffers();
            _latestTex = null;

            if (_ownedLatestTex != null)
            {
                UnityEngine.Object.DestroyImmediate(_ownedLatestTex);
                _ownedLatestTex = null;
            }
            if (_evenTex != null)
            {
                UnityEngine.Object.DestroyImmediate(_evenTex);
                _evenTex = null;
            }
            if (_evenRt != null)
            {
                _evenRt.Release();
                UnityEngine.Object.DestroyImmediate(_evenRt);
                _evenRt = null;
            }
            if (_runner != null)
            {
                UnityEngine.Object.DestroyImmediate(_runner.gameObject);
                _runner = null;
            }
            if (_audioTap != null)
            {
                UnityEngine.Object.DestroyImmediate(_audioTap);
                _audioTap = null;
            }
        }
    }

    public sealed class GameViewRecorderRunner : MonoBehaviour
    {
        private readonly WaitForEndOfFrame _waitEnd = new WaitForEndOfFrame();

        private System.Collections.IEnumerator Start()
        {
            while (GameViewRecorderSession.IsRecording)
            {
                yield return _waitEnd;
                GameViewRecorderSession.OnEndOfFrameStep();
            }
        }

        private void OnDestroy()
        {
            if (GameViewRecorderSession.IsRecording)
            {
                GameViewRecorderSession.StopRecording();
            }
        }
    }

    public sealed class GameViewAudioTap : MonoBehaviour
    {
        private void OnAudioFilterRead(float[] data, int channels)
        {
            GameViewRecorderSession.PushAudioSamplesFromThread(data, channels);
        }
    }
}
