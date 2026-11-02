using System;
using HarmonyLib;
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

    // Normal customers: the game calls this after the walk-in animation ends.
    [HarmonyPatch(typeof(StoreUIManager), nameof(StoreUIManager.OnNextClientArrived))]
    internal static class ClientArrivedPatch
    {
        private static void Postfix()
        {
            AugOverlay.Refresh();
        }
    }

    // Morning/special customers are created directly without the normal walk-in
    // event, so they need their own hook.
    [HarmonyPatch(typeof(PlayerStore), nameof(PlayerStore.SetupWaitingClient))]
    internal static class WaitingClientPatch
    {
        private static void Postfix()
        {
            AugOverlay.Refresh();
        }
    }

    [HarmonyPatch(typeof(PlayerStore), nameof(PlayerStore.DismissCurrentClient))]
    internal static class ClientDismissedPatch
    {
        private static void Postfix()
        {
            AugOverlay.Refresh();
        }
    }

    // Loading a save/new game can clear the customer without going through the
    // dismiss path; hide a marker that belonged to the previous scene.
    [HarmonyPatch(typeof(PlayerStore), nameof(PlayerStore.Reset))]
    internal static class StoreResetPatch
    {
        private static void Postfix()
        {
            // The overlay is DontDestroyOnLoad. Destroying it here avoids keeping
            // old-scene TMP objects/textures alive across a menu -> load transition.
            AugOverlay.ResetForSceneChange();
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
            // The canvas is a separate DontDestroyOnLoad root; destroy it before
            // the holder so disabling/resetting the mod leaves no red overlay.
            AugOverlay.TearDownCanvas();
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
        }
    }

    internal sealed class AugOverlay : MonoBehaviour
    {
        private static AugOverlay _instance;
        private static bool _refreshPending;
        private static GameObject _overlayRoot;

        private Canvas _canvas;
        private TextMeshProUGUI _label;
        private Image _background;
        private string _lastText;

        private void Awake()
        {
            _instance = this;
        }

        private void OnDestroy()
        {
            // If a new overlay replaced this one in the same frame, its canvas is
            // already the static _overlayRoot; do not tear that one down.
            if (_instance == this)
            {
                _instance = null;
                TearDownCanvas();
            }
        }

        internal static void TearDownCanvas()
        {
            if (_overlayRoot != null)
            {
                UnityEngine.Object.Destroy(_overlayRoot);
                _overlayRoot = null;
            }
        }

        private void Update()
        {
            EnsureOverlay();
            if (_label == null)
            {
                return;
            }
            if (_refreshPending)
            {
                _refreshPending = false;
                RefreshNow();
            }
            // Once the overlay is built and the first read succeeded there is
            // nothing to do every frame. Customer changes are event-driven.
            if (!_refreshPending)
            {
                enabled = false;
            }
        }

        public static void Refresh()
        {
            if (_instance == null)
            {
                // The previous overlay is destroyed on a scene transition; build
                // a fresh one from the new scene's Chinese TMP template.
                RuntimeHost.Create<AugOverlay>("AugSpotterRuntime");
                if (_instance == null)
                {
                    _refreshPending = true;
                    return;
                }
            }
            _instance.enabled = true;
            _instance.RefreshNow();
            if (_instance._label != null && !_refreshPending)
            {
                _instance.enabled = false;
            }
        }

        public static void ResetForSceneChange()
        {
            RuntimeHost.Destroy();
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

            GameObject root = new GameObject("AugSpotterOverlay");
            _overlayRoot = root;
            UnityEngine.Object.DontDestroyOnLoad(root);
            _canvas = root.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 32000;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

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

            RefreshNow();
        }

        private void RefreshNow()
        {
            if (_label == null)
            {
                _refreshPending = true;
                return;
            }

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
                // PlayerStore may still be constructing; retry until it is ready.
                SetText("");
                _refreshPending = true;
                return;
            }

            _refreshPending = false;
            if (client != null && client.isAug)
            {
                SetText("<color=#FF4B4B>侦测到改造体  等级 " + client.augTier + "</color>");
            }
            else
            {
                SetText("");
            }
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
