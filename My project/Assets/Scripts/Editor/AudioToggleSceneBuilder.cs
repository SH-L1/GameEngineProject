using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace VoidEater.Editor
{
    public static class AudioToggleSceneBuilder
    {
        private const string BgmPlayerName = "BGM Player";
        private const string CanvasName = "Audio Toggle Canvas";
        private const string OffButtonName = "BGM Off Button";
        private const string OnButtonName = "BGM On Button";

        [MenuItem("Void Eater/Setup BGM Toggle Button")]
        public static void SetupBgmToggleButton()
        {
            AudioSource bgmSource = EnsureBgmPlayer();
            EnsureAudioListener();
            Canvas canvas = EnsureCanvas();
            EnsureEventSystem();

            Button offButton = EnsureButton(canvas.transform, OffButtonName, "BGM OFF", true);
            Button onButton = EnsureButton(canvas.transform, OnButtonName, "BGM ON", false);

            ConfigureToggleButton(offButton, bgmSource, onButton.gameObject, offButton.gameObject, false);
            ConfigureToggleButton(onButton, bgmSource, offButton.gameObject, onButton.gameObject, true);

            if (EditorApplication.isPlaying && bgmSource.clip != null && !bgmSource.isPlaying)
            {
                bgmSource.Play();
            }

            Selection.activeGameObject = offButton.gameObject;
            EditorGUIUtility.PingObject(offButton.gameObject);
            Debug.Log("BGM toggle UI is ready. Assign your music clip to the BGM Player AudioSource if it is empty.");
        }

        private static AudioSource EnsureBgmPlayer()
        {
            GameObject bgmPlayer = GameObject.Find(BgmPlayerName);
            if (bgmPlayer == null)
            {
                bgmPlayer = new GameObject(BgmPlayerName);
                Undo.RegisterCreatedObjectUndo(bgmPlayer, "Create BGM Player");
            }

            AudioSource source = EnsureComponent<AudioSource>(bgmPlayer);
            source.playOnAwake = true;
            source.loop = true;
            source.spatialBlend = 0f;

            if (Mathf.Approximately(source.volume, 1f))
            {
                source.volume = 0.35f;
            }

            if (source.clip == null)
            {
                source.clip = FindFirstBgmClip();
            }

            EditorUtility.SetDirty(source);
            return source;
        }

        private static AudioClip FindFirstBgmClip()
        {
            string[] searchFolders = GetBgmSearchFolders();

            string[] audioClipGuids = AssetDatabase.FindAssets("t:AudioClip", searchFolders);
            if (audioClipGuids.Length == 0)
            {
                return null;
            }

            string clipPath = AssetDatabase.GUIDToAssetPath(audioClipGuids[0]);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
        }

        private static string[] GetBgmSearchFolders()
        {
            if (AssetDatabase.IsValidFolder("Assets/BGM"))
            {
                return new[] { "Assets/BGM" };
            }

            if (AssetDatabase.IsValidFolder("Assets/Audio/BGM"))
            {
                return new[] { "Assets/Audio/BGM" };
            }

            return new[] { "Assets" };
        }

        private static void EnsureAudioListener()
        {
            if (Object.FindFirstObjectByType<AudioListener>() != null)
            {
                return;
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                camera = Object.FindFirstObjectByType<Camera>();
            }

            GameObject listenerObject;
            if (camera != null)
            {
                listenerObject = camera.gameObject;
            }
            else
            {
                listenerObject = new GameObject("Audio Listener");
                Undo.RegisterCreatedObjectUndo(listenerObject, "Create Audio Listener");
            }

            EnsureComponent<AudioListener>(listenerObject);
            EditorUtility.SetDirty(listenerObject);
        }

        private static Canvas EnsureCanvas()
        {
            GameObject canvasObject = GameObject.Find(CanvasName);
            if (canvasObject != null && canvasObject.GetComponent<RectTransform>() == null)
            {
                Undo.DestroyObjectImmediate(canvasObject);
                canvasObject = null;
            }

            if (canvasObject == null)
            {
                canvasObject = new GameObject(
                    CanvasName,
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));
                Undo.RegisterCreatedObjectUndo(canvasObject, "Create Audio Toggle Canvas");
            }

            Canvas canvas = EnsureComponent<Canvas>(canvasObject);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            CanvasScaler scaler = EnsureComponent<CanvasScaler>(canvasObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            EnsureComponent<GraphicRaycaster>(canvasObject);
            EditorUtility.SetDirty(canvasObject);
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem");
                Undo.RegisterCreatedObjectUndo(eventSystemObject, "Create EventSystem");
                eventSystem = eventSystemObject.AddComponent<EventSystem>();
            }

#if ENABLE_INPUT_SYSTEM
            EnsureComponent<InputSystemUIInputModule>(eventSystem.gameObject);
            StandaloneInputModule legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacyModule != null)
            {
                Object.DestroyImmediate(legacyModule);
            }
#else
            EnsureComponent<StandaloneInputModule>(eventSystem.gameObject);
#endif
        }

        private static Button EnsureButton(Transform parent, string name, string label, bool active)
        {
            Transform existing = parent.Find(name);
            GameObject buttonObject = existing != null ? existing.gameObject : null;
            if (buttonObject != null && buttonObject.GetComponent<RectTransform>() == null)
            {
                Undo.DestroyObjectImmediate(buttonObject);
                buttonObject = null;
            }

            if (buttonObject == null)
            {
                buttonObject = new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button));
                Undo.RegisterCreatedObjectUndo(buttonObject, "Create " + name);
                buttonObject.transform.SetParent(parent, false);
            }

            RectTransform rect = EnsureComponent<RectTransform>(buttonObject);
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-32f, -32f);
            rect.sizeDelta = new Vector2(150f, 48f);

            Image image = EnsureComponent<Image>(buttonObject);
            image.color = active ? new Color(0.12f, 0.18f, 0.22f, 0.92f) : new Color(0.22f, 0.14f, 0.12f, 0.92f);

            Button button = EnsureComponent<Button>(buttonObject);
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.88f, 0.96f, 1f, 1f);
            colors.pressedColor = new Color(0.72f, 0.86f, 0.92f, 1f);
            button.colors = colors;

            EnsureButtonLabel(buttonObject.transform, label);
            buttonObject.SetActive(active);
            EditorUtility.SetDirty(buttonObject);
            return button;
        }

        private static void EnsureButtonLabel(Transform parent, string label)
        {
            Transform existing = parent.Find("Label");
            GameObject labelObject = existing != null ? existing.gameObject : null;
            if (labelObject != null && labelObject.GetComponent<RectTransform>() == null)
            {
                Undo.DestroyObjectImmediate(labelObject);
                labelObject = null;
            }

            if (labelObject == null)
            {
                labelObject = new GameObject(
                    "Label",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));
                Undo.RegisterCreatedObjectUndo(labelObject, "Create Button Label");
                labelObject.transform.SetParent(parent, false);
            }

            RectTransform rect = EnsureComponent<RectTransform>(labelObject);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text text = EnsureComponent<Text>(labelObject);
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 20;
            text.color = Color.white;
            text.raycastTarget = false;

            if (text.font == null)
            {
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                    ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            EditorUtility.SetDirty(labelObject);
        }

        private static void ConfigureToggleButton(
            Button button,
            AudioSource source,
            GameObject buttonToShow,
            GameObject buttonToHide,
            bool enableAudio)
        {
            Undo.RecordObject(button, "Configure BGM Toggle Button");
            RemovePersistentListeners(button);

            if (enableAudio)
            {
                UnityEventTools.AddVoidPersistentListener(button.onClick, source.Play);
            }
            else
            {
                UnityEventTools.AddVoidPersistentListener(button.onClick, source.Pause);
            }

            UnityEventTools.AddBoolPersistentListener(button.onClick, buttonToShow.SetActive, true);
            UnityEventTools.AddBoolPersistentListener(button.onClick, buttonToHide.SetActive, false);
            EditorUtility.SetDirty(button);
        }

        private static void RemovePersistentListeners(Button button)
        {
            button.onClick.RemoveAllListeners();

            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                UnityEventTools.RemovePersistentListener(button.onClick, i);
            }
        }

        private static T EnsureComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = Undo.AddComponent<T>(gameObject);
            }

            return component;
        }
    }
}
