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

        internal static ConfigEntry<bool> ShowOrdinary;
        internal static ConfigEntry<bool> RunSelfTest;

        public void Init(ModManifest manifest)
        {
            Log.Info("Init " + manifest.ID + " v" + manifest.ModVersion);
        }

        public void OnEnable()
        {
            ModConfig config = ModHelper.GetConfig(ModId);
            ShowOrdinary = config.Define(
                "show_not_an_aug",
                true,
                "display",
                delegate { return "Show NOT AN AUG for ordinary customers"; },
                delegate { return "When off, the overlay is hidden for ordinary customers and only appears for Augs."; });
            RunSelfTest = config.Define(
                "run_self_test",
                true,
                "debug",
                delegate { return "Run self-test after loading a store"; },
                delegate { return "Logs PASS/FAIL checks for Aug detection and the mod scanner."; });

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
        private string _lastText;
        private bool _selfTestDone;
        private float _started;

        private void Awake()
        {
            _started = Time.realtimeSinceStartup;
        }

        private void Update()
        {
            EnsureOverlay();
            UpdateLabel();

            if (!_selfTestDone && Time.realtimeSinceStartup - _started > 2f)
            {
                _selfTestDone = true;
                if (AugSpotterMod.RunSelfTest == null || AugSpotterMod.RunSelfTest.Value)
                {
                    RunSelfTest();
                }
            }
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

            GameObject root = new GameObject("AugSpotterOverlay");
            UnityEngine.Object.DontDestroyOnLoad(root);
            _canvas = root.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 32000;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(root.transform, false);
            _label = textObject.AddComponent<TextMeshProUGUI>();
            if (storeUi.versionTMP.font != null)
            {
                _label.font = storeUi.versionTMP.font;
            }
            else if (TMP_Settings.defaultFontAsset != null)
            {
                _label.font = TMP_Settings.defaultFontAsset;
            }
            if (storeUi.versionTMP.fontSharedMaterial != null)
            {
                _label.fontSharedMaterial = storeUi.versionTMP.fontSharedMaterial;
            }
            _label.fontSize = 24f;
            _label.alignment = TextAlignmentOptions.TopLeft;
            _label.enableWordWrapping = false;
            _label.raycastTarget = false;
            _label.color = Color.white;

            RectTransform rect = _label.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(38f, -112f);
            rect.sizeDelta = new Vector2(700f, 80f);

            GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
            backgroundObject.transform.SetParent(root.transform, false);
            Image background = backgroundObject.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.55f);
            background.raycastTarget = false;
            RectTransform backgroundRect = background.rectTransform;
            backgroundRect.anchorMin = new Vector2(0f, 1f);
            backgroundRect.anchorMax = new Vector2(0f, 1f);
            backgroundRect.pivot = new Vector2(0f, 1f);
            backgroundRect.anchoredPosition = new Vector2(24f, -104f);
            backgroundRect.sizeDelta = new Vector2(752f, 96f);
            backgroundObject.transform.SetSiblingIndex(0);
        }

        private void UpdateLabel()
        {
            if (_label == null)
            {
                return;
            }

            StoreClient client = null;
            if (PlayerStore.Instance != null && PlayerStore.Instance.currentClientInstance != null)
            {
                client = PlayerStore.Instance.currentClientInstance.GetClientBlueprint();
            }

            string text;
            if (client == null)
            {
                text = "";
            }
            else if (client.isAug)
            {
                text = "<color=#FF4B4B>AUG DETECTED</color>  -  Tier " + client.augTier;
            }
            else if (AugSpotterMod.ShowOrdinary != null && AugSpotterMod.ShowOrdinary.Value)
            {
                text = "<color=#5BE37D>NOT AN AUG</color>";
            }
            else
            {
                text = "";
            }

            if (text != _lastText)
            {
                _lastText = text;
                _label.text = text;
            }
        }

        private void RunSelfTest()
        {
            int pass = 0;
            int fail = 0;
            try
            {
                StoreClient aug = new StoreClient();
                aug.isAug = true;
                aug.augTier = 2;
                Check(AugSpotterMod.IsAug(aug), "aug_detection", "isAug=true", ref pass, ref fail);

                StoreClient normal = new StoreClient();
                normal.isAug = false;
                Check(!AugSpotterMod.IsAug(normal), "ordinary_detection", "isAug=false", ref pass, ref fail);

                Check(!AugSpotterMod.IsAug(null), "null_client", "null -> false", ref pass, ref fail);

                string folder = ModHelper.GetModFolder(AugSpotterMod.ModId);
                if (string.IsNullOrEmpty(folder))
                {
                    Check(false, "mod_scan", "could not resolve mod folder", ref pass, ref fail);
                }
                else
                {
                    string dll = folder.TrimEnd('/', '\\') + "/" + AugSpotterMod.AssemblyName + ".dll";
                    ModScanResult scan = ModScanner.Scan(
                        new string[] { dll },
                        new string[] { AugSpotterMod.AssemblyName },
                        folder,
                        true);
                    string caps = scan.Capabilities == null ? "" : string.Join(", ", new System.Collections.Generic.List<string>(scan.Capabilities).ToArray());
                    Log.Info("[Scan] refused=" + scan.Refused + " capabilities=[" + caps + "] findings=" + (scan.Findings == null ? 0 : scan.Findings.Count));
                    if (scan.Findings != null && scan.Findings.Count > 0)
                    {
                        Log.Info("[Scan] " + scan.FormatFindings(4).Replace("\n", " / "));
                    }
                    Check(!scan.Refused && (scan.Capabilities == null || scan.Capabilities.Count == 0), "mod_scan", "refused=" + scan.Refused + " caps=[" + caps + "]", ref pass, ref fail);
                }
            }
            catch (Exception e)
            {
                fail++;
                Log.Error("[SelfTest] FAIL exception | " + e.GetType().Name + ": " + e.Message);
            }
            Log.Info("[SelfTest] SUMMARY pass=" + pass + " fail=" + fail);
        }

        private static void Check(bool ok, string name, string detail, ref int pass, ref int fail)
        {
            if (ok)
            {
                pass++;
                Log.Info("[SelfTest] PASS " + name + " | " + detail);
            }
            else
            {
                fail++;
                Log.Error("[SelfTest] FAIL " + name + " | " + detail);
            }
        }
    }
}
