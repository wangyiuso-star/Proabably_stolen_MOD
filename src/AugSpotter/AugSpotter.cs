using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeepSpaceQoL
{
    /// <summary>
    /// Aug spotter: shows a persistent, readable marker for the customer at the
    /// counter when that customer is really an Aug (disguised augmented human).
    /// </summary>
    public sealed class AugSpotterMod : IMod
    {
        internal const string ModId = "deepspace.aug_spotter";
        internal const string ModVersion = "1.0.0";
        internal const string AssemblyName = "AugSpotter";


        public void Init(ModManifest manifest)
        {
            Log.Info("Init " + manifest.ID + " v" + manifest.ModVersion);
        }

        public void OnEnable()
        {
            RuntimeHost.Create<AugOverlay>("AugSpotterRuntime");
            Log.Info("enabled v" + ModVersion);
        }

        public void OnDisable()
        {
            RuntimeHost.Destroy();
        }

        internal static bool IsAug(StoreClient client)
        {
            return client != null && client.isAug;
        }
    }

    internal static class Log
    {
        private const string Prefix = "[AugSpotter] ";

        public static void Info(string message)
        {
            Debug.Log(Prefix + message);
        }

        public static void Warn(string message)
        {
            Debug.LogWarning(Prefix + message);
        }

        public static void Error(string message)
        {
            Debug.LogError(Prefix + message);
        }
    }

    internal static class RuntimeHost
    {
        private static GameObject _root;

        public static void Create<T>(string name) where T : MonoBehaviour
        {
            if (_root != null)
            {
                return;
            }
            _root = new GameObject(name);
            UnityEngine.Object.DontDestroyOnLoad(_root);
            _root.AddComponent<T>();
        }

        public static void Destroy()
        {
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
        }
    }

    internal sealed class AugOverlay : MonoBehaviour
    {
        private Canvas _canvas;
        private TextMeshProUGUI _label;
        private Image _background;
        private string _lastText;
        private float _playerProbeAt;

        private void Update()
        {
            EnsureOverlay();
            UpdateLabel();

        }

        private void EnsureOverlay()
        {
            if (_canvas != null)
            {
                return;
            }
            StoreUIManager storeUi = StoreUIManager.Instance;
            if (storeUi == null || storeUi.versionTMP == null)
            {
                return;
            }
            InspectionUIManager inspectionUi = InspectionUIManager.Instance;
            if (inspectionUi == null || inspectionUi.confrontTitleTMP == null)
            {
                return;
            }

            GameObject root = new GameObject("AugSpotterOverlay");
            UnityEngine.Object.DontDestroyOnLoad(root);
            _canvas = root.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 32000;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Clone a TMP text that already renders Chinese in this scene. This
            // copies the exact font asset, fallback list, material and shader;
            // building a fresh TMP text with a hand-picked font can lose the
            // fallback atlas and render only Latin/digits.
            TextMeshProUGUI template = null;
            if (storeUi.storeCalendar != null)
            {
                template = storeUi.storeCalendar.dayTMP;
            }
            if (template == null)
            {
                template = inspectionUi.confrontTitleTMP;
            }
            if (template == null)
            {
                return;
            }

            GameObject textObject = UnityEngine.Object.Instantiate(template.gameObject, root.transform);
            textObject.name = "Label";
            textObject.SetActive(true);
            _label = textObject.GetComponent<TextMeshProUGUI>();
            if (_label == null)
            {
                UnityEngine.Object.Destroy(textObject);
                return;
            }
            _label.fontSize = 24f;
            _label.enableAutoSizing = false;
            _label.alignment = TextAlignmentOptions.TopLeft;
            _label.enableWordWrapping = false;
            _label.raycastTarget = false;
            _label.color = Color.white;
            _label.margin = Vector4.zero;
            _label.text = "";

            RectTransform rect = _label.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(38f, -112f);
            rect.sizeDelta = new Vector2(700f, 80f);

            GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
            backgroundObject.transform.SetParent(root.transform, false);
            _background = backgroundObject.GetComponent<Image>();
            _background.color = new Color(0f, 0f, 0f, 0.55f);
            _background.raycastTarget = false;
            RectTransform backgroundRect = _background.rectTransform;
            backgroundRect.anchorMin = new Vector2(0f, 1f);
            backgroundRect.anchorMax = new Vector2(0f, 1f);
            backgroundRect.pivot = new Vector2(0f, 1f);
            backgroundRect.anchoredPosition = new Vector2(24f, -104f);
            backgroundRect.sizeDelta = new Vector2(752f, 96f);
            backgroundObject.transform.SetSiblingIndex(0);

            // The store scene can exist before PlayerStore finishes constructing.
            // Wait a few seconds and then probe at a low rate so a transient
            // construction failure cannot spam the log or corrupt the singleton.
            _playerProbeAt = Time.realtimeSinceStartup + 5f;
        }

        private void UpdateLabel()
        {
            if (_label == null)
            {
                return;
            }

            float now = Time.realtimeSinceStartup;
            if (now < _playerProbeAt)
            {
                return;
            }
            _playerProbeAt = now + 0.5f;

            StoreClient client = null;
            try
            {
                PlayerStore store = PlayerStore.Instance;
                if (store != null && store.currentClientInstance != null)
                {
                    client = store.currentClientInstance.GetClientBlueprint();
                }
            }
            catch (Exception)
            {
                // PlayerStore is still being constructed or the scene is changing.
                SetText("");
                _playerProbeAt = now + 2f;
                return;
            }

            string text = "";
            if (client != null && client.isAug)
            {
                text = "<color=#FF4B4B>侦测到改造体  等级 " + client.augTier + "</color>";
            }
            SetText(text);
        }

        private void SetText(string text)
        {
            if (text == _lastText)
            {
                return;
            }
            _lastText = text;
            _label.text = text;
            bool visible = text.Length > 0;
            _label.enabled = visible;
            if (_background != null)
            {
                _background.enabled = visible;
            }
        }

    }
}
