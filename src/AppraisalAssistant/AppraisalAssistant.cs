using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace DeepSpaceQoL
{
    /// <summary>
    /// Appraisal assistant: tells the player the correct value for value-estimate
    /// appraisals and the true authenticity for the genuine/counterfeit flow.
    /// </summary>
    public sealed class AppraisalAssistantMod : IMod
    {
        internal const string ModId = "deepspace.appraisal_assistant";
        internal const string ModVersion = "1.0.0";
        internal const string AssemblyName = "AppraisalAssistant";

        internal static ConfigEntry<bool> ShowTrueValue;
        internal static ConfigEntry<bool> AutoFill;
        internal static ConfigEntry<bool> ForceEstimate;
        internal static ConfigEntry<bool> ShowBuyAdvice;
        internal static ConfigEntry<bool> RunSelfTest;

        public void Init(ModManifest manifest)
        {
            Log.Info("Init " + manifest.ID + " v" + manifest.ModVersion);
        }

        public void OnEnable()
        {
            ModConfig config = ModHelper.GetConfig(ModId);
            ShowTrueValue = config.Define(
                "show_true_value",
                true,
                "appraisal",
                delegate { return "Show true appraisal value"; },
                delegate { return "Adds TRUE VALUE and the perfect estimate range to appraisal item tooltips."; });
            AutoFill = config.Define(
                "auto_fill",
                true,
                "appraisal",
                delegate { return "Auto-fill the appraisal input"; },
                delegate { return "Fills the exact value into the appraisal service input field."; });
            ForceEstimate = config.Define(
                "force_correct_estimate",
                true,
                "appraisal",
                delegate { return "Submit the exact value"; },
                delegate { return "When you press Estimate, submit the exact value even if the field was edited."; });
            ShowBuyAdvice = config.Define(
                "show_buy_advice",
                true,
                "inspection",
                delegate { return "Show buy / do-not-buy advice"; },
                delegate { return "Adds SAFE TO BUY or DO NOT BUY under the authenticity label."; });
            RunSelfTest = config.Define(
                "run_self_test",
                true,
                "debug",
                delegate { return "Run self-test after loading a store"; },
                delegate { return "Logs PASS/FAIL checks for values, authenticity and item tags."; });

            RuntimeHost.Create<AppraisalRuntime>("AppraisalAssistantRuntime");
            Log.Info("enabled v" + ModVersion);
        }

        public void OnDisable()
        {
            RuntimeHost.Destroy();
        }
    }

    internal static class Log
    {
        private const string Prefix = "[AppraisalAssistant] ";

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

        public static bool TryGetCorrectValue(GameItem item, out int value)
        {
            value = 0;
            if (item == null)
            {
                return false;
            }
            try
            {
                if (!item.IsTag("APPRAISAL_ITEM"))
                {
                    return false;
                }
                if (AppraisalItemRegistry.Find(item) == null)
                {
                    return false;
                }
                value = AppraisalHelper.GetCorrectBaseValue(item);
                return value > 0;
            }
            catch (Exception e)
            {
                Log.Warn("Could not compute true value: " + e.Message);
                return false;
            }
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

    [HarmonyPatch(typeof(AppraisalHelper), nameof(AppraisalHelper.CreateAppraisalTooltip))]
    internal static class AppraisalTooltipPatch
    {
        private static void Postfix(RichTextBuilder builder, GameItem item)
        {
            if (builder == null || item == null)
            {
                return;
            }
            if (AppraisalAssistantMod.ShowTrueValue != null && !AppraisalAssistantMod.ShowTrueValue.Value)
            {
                return;
            }
            int value;
            if (!Log.TryGetCorrectValue(item, out value))
            {
                return;
            }
            int low = Mathf.CeilToInt(value * 0.95f);
            int high = Mathf.FloorToInt(value * 1.05f);
            builder.AddLine(
                "TRUE VALUE: " + value.ToString(CultureInfo.InvariantCulture),
                true,
                RenderHandler.ColorPalette.LightGreen);
            builder.AddLine(
                "PERFECT RANGE: " + low.ToString(CultureInfo.InvariantCulture) + " - " + high.ToString(CultureInfo.InvariantCulture),
                true,
                RenderHandler.ColorPalette.LightGreen);
        }
    }

    [HarmonyPatch(typeof(InspectionUIManager), nameof(InspectionUIManager.UpdateAppraisalPanel))]
    internal static class AppraisalAutoFillPatch
    {
        private static void Postfix(InspectionUIManager __instance)
        {
            if (__instance == null || __instance.currentInspectingItem == null)
            {
                return;
            }
            if (AppraisalAssistantMod.AutoFill != null && !AppraisalAssistantMod.AutoFill.Value)
            {
                return;
            }
            if (__instance.isEditingAppraisalField)
            {
                return;
            }
            int value;
            if (!Log.TryGetCorrectValue(__instance.currentInspectingItem, out value))
            {
                return;
            }
            string text = value.ToString(CultureInfo.InvariantCulture);
            if (__instance.apprisalServiceInputField != null && __instance.apprisalServiceInputField.text != text)
            {
                __instance.apprisalServiceInputField.text = text;
            }
            __instance.currentInspectingItem.ModifyTag("APPRAISAL_DEFAULT_VALUE", delegate(TagState state)
            {
                state.Enable().SetInt(value);
            });
        }
    }

    [HarmonyPatch(typeof(InspectionUIManager), nameof(InspectionUIManager.OnAppraisalServiceEstimated))]
    internal static class AppraisalForceEstimatePatch
    {
        private static void Prefix(InspectionUIManager __instance)
        {
            if (__instance == null || __instance.currentInspectingItem == null)
            {
                return;
            }
            if (AppraisalAssistantMod.ForceEstimate != null && !AppraisalAssistantMod.ForceEstimate.Value)
            {
                return;
            }
            int value;
            if (!Log.TryGetCorrectValue(__instance.currentInspectingItem, out value))
            {
                return;
            }
            string text = value.ToString(CultureInfo.InvariantCulture);
            if (__instance.apprisalServiceInputField != null)
            {
                __instance.apprisalServiceInputField.text = text;
            }
            __instance.currentInspectingItem.ModifyTag("APPRAISAL_DEFAULT_VALUE", delegate(TagState state)
            {
                state.Enable().SetInt(value);
            });
        }
    }

    [HarmonyPatch(typeof(InspectionUIManager), nameof(InspectionUIManager.OpenCigaretteConfrontPanel))]
    internal static class CigaretteTitlePatch
    {
        private static void Postfix(InspectionUIManager __instance, GameItem gameItem)
        {
            ConfrontTitle.Append(__instance, gameItem);
        }
    }

    [HarmonyPatch(typeof(InspectionUIManager), nameof(InspectionUIManager.OpenStampConfrontPanel))]
    internal static class StampTitlePatch
    {
        private static void Postfix(InspectionUIManager __instance, GameItem gameItem)
        {
            ConfrontTitle.Append(__instance, gameItem);
        }
    }

    [HarmonyPatch(typeof(InspectionUIManager), nameof(InspectionUIManager.OpenInjectorConfrontPanel))]
    internal static class InjectorTitlePatch
    {
        private static void Postfix(InspectionUIManager __instance, GameItem gameItem)
        {
            ConfrontTitle.Append(__instance, gameItem);
        }
    }

    internal static class ConfrontTitle
    {
        public static void Append(InspectionUIManager ui, GameItem item)
        {
            if (ui == null || ui.confrontTitleTMP == null || item == null)
            {
                return;
            }
            TruthResult truth;
            if (!Truth.TryGet(item, out truth))
            {
                return;
            }
            string tag;
            switch (truth.Kind)
            {
                case TruthKind.Genuine:
                    tag = "  <color=#39A78E>[GENUINE]</color>";
                    break;
                case TruthKind.Counterfeit:
                    tag = "  <color=#FF5555>[COUNTERFEIT]</color>";
                    break;
                case TruthKind.Defective:
                    tag = "  <color=#FFAA00>[DEFECTIVE]</color>";
                    break;
                case TruthKind.Replica:
                    tag = "  <color=#FFAA00>[REPLICA]</color>";
                    break;
                default:
                    return;
            }
            ui.confrontTitleTMP.text += tag;
        }
    }

    [HarmonyPatch(typeof(InspectableHelper), nameof(InspectableHelper.CreateInspectableTooltip))]
    internal static class AuthenticityTooltipPatch
    {
        private static void Postfix(RichTextBuilder builder, GameItem item)
        {
            if (builder == null || item == null)
            {
                return;
            }
            TruthResult truth;
            if (!Truth.TryGet(item, out truth))
            {
                return;
            }
            RenderHandler.ColorPalette color = truth.Kind == TruthKind.Genuine
                ? RenderHandler.ColorPalette.LightGreen
                : (truth.Kind == TruthKind.Counterfeit ? RenderHandler.ColorPalette.Red : RenderHandler.ColorPalette.Orange);
            builder.AddLine("AUTHENTICITY: " + truth.Label, true, color);
            if (truth.Detail != null)
            {
                builder.AddLine(truth.Detail, true, color);
            }
            if (AppraisalAssistantMod.ShowBuyAdvice != null && AppraisalAssistantMod.ShowBuyAdvice.Value)
            {
                if (truth.Kind == TruthKind.Genuine)
                {
                    builder.AddLine("SAFE TO BUY", true, RenderHandler.ColorPalette.LightGreen);
                }
                else
                {
                    builder.AddLine("DO NOT BUY", true, RenderHandler.ColorPalette.Red);
                }
            }
        }
    }

    internal enum TruthKind
    {
        None,
        Genuine,
        Counterfeit,
        Defective,
        Replica
    }

    internal struct TruthResult
    {
        public TruthKind Kind;
        public string Label;
        public string Detail;
    }

    internal static class Truth
    {
        public static bool TryGet(GameItem item, out TruthResult result)
        {
            result = default(TruthResult);
            if (item == null)
            {
                return false;
            }
            try
            {
                if (item.IsTag("RIBWICH_TAG"))
                {
                    bool fake = RibwichHelper.IsCounterfeit(item);
                    result.Kind = fake ? TruthKind.Counterfeit : TruthKind.Genuine;
                    result.Label = fake ? "COUNTERFEIT" : "GENUINE";
                    result.Detail = "Ribwich";
                    return true;
                }
                if (item.IsTag("WINE_COUNTERFEIT_TAG"))
                {
                    result.Kind = TruthKind.Counterfeit;
                    result.Label = "COUNTERFEIT";
                    result.Detail = "Counterfeit wine";
                    return true;
                }

                ItemFeature feature = item.FindItemFeatureByCategory("CATEGORY_GENUINE_CIGARETTE");
                if (feature != null && feature.realCondition != null)
                {
                    return FromCondition(feature, "Cigarette", item, out result);
                }
                feature = item.FindItemFeatureByCategory("CATEGORY_GENUINE_STAMP");
                if (feature != null && feature.realCondition != null)
                {
                    return FromCondition(feature, "Stamp", item, out result);
                }
                feature = item.FindItemFeatureByCategory("CATEGORY_GENUINE_INJECTOR");
                if (feature != null && feature.realCondition != null)
                {
                    return FromCondition(feature, "Injector", item, out result);
                }
                feature = item.FindItemFeatureByCategory("CATEGORY_REPLICA");
                if (feature != null && feature.realCondition != null)
                {
                    return FromCondition(feature, "Replica", item, out result);
                }
            }
            catch (Exception e)
            {
                Log.Warn("Authenticity check failed: " + e.Message);
            }
            return false;
        }

        private static bool FromCondition(ItemFeature feature, string kind, GameItem item, out TruthResult result)
        {
            string id = feature.realCondition.identifier ?? "";
            result = new TruthResult();
            result.Detail = kind + " condition: " + id;
            switch (id)
            {
                case "genuineCigarette":
                case "genuineStamp":
                case "genuineInjector":
                case "non_replica":
                    result.Kind = TruthKind.Genuine;
                    result.Label = "GENUINE";
                    break;
                case "counterfeitCigarette":
                case "counterfeitStamp":
                case "counterfeitInjector":
                    result.Kind = TruthKind.Counterfeit;
                    result.Label = "COUNTERFEIT";
                    break;
                case "expiredInjector":
                    result.Kind = TruthKind.Defective;
                    result.Label = "DEFECTIVE - EXPIRED";
                    break;
                case "unusableInjector":
                    result.Kind = TruthKind.Defective;
                    result.Label = "DEFECTIVE - UNUSABLE";
                    break;
                case "replica":
                    result.Kind = TruthKind.Replica;
                    result.Label = "REPLICA (NOT ORIGINAL)";
                    break;
                default:
                    return false;
            }
            string issues = CollectIssues(item);
            if (issues != null)
            {
                result.Detail = result.Detail + " | " + issues;
            }
            return true;
        }

        private static string CollectIssues(GameItem item)
        {
            List<string> issues = new List<string>();
            if (item.IsTag("CIGARETTE_LETTERING_ISSUE")) issues.Add("lettering");
            if (item.IsTag("CIGARETTE_COLOR_ISSUE")) issues.Add("color");
            if (item.IsTag("CIGARETTE_SEAL_ISSUE")) issues.Add("seal");
            if (item.IsTag("CIGARETTE_LOGO_ISSUE")) issues.Add("logo");
            if (item.IsTag("CIGARETTE_BOX_ISSUE")) issues.Add("box");
            if (item.IsTag("STAMP_LETTERING_ISSUE")) issues.Add("lettering");
            if (item.IsTag("STAMP_LOGO_ISSUE")) issues.Add("logo");
            if (item.IsTag("STAMP_SEAL_ISSUE")) issues.Add("seal");
            if (item.IsTag("STAMP_AMOUNT_ISSUE")) issues.Add("amount");
            if (item.IsTag("STAMP_CHIP_ISSUE")) issues.Add("chip");
            if (item.IsTag("ISSUE_INDICATOR")) issues.Add("indicator");
            if (item.IsTag("ISSUE_LABEL")) issues.Add("label");
            if (item.IsTag("ISSUE_SERIAL")) issues.Add("serial");
            if (item.IsTag("ISSUE_BODY")) issues.Add("body");
            if (item.IsTag("ISSUE_COLOR")) issues.Add("color");
            return issues.Count == 0 ? null : "visible issues: " + string.Join(", ", issues.ToArray());
        }
    }

    internal sealed class AppraisalRuntime : MonoBehaviour
    {
        private IEnumerator Start()
        {
            if (AppraisalAssistantMod.RunSelfTest != null && !AppraisalAssistantMod.RunSelfTest.Value)
            {
                yield break;
            }

            float started = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - started < 90f)
            {
                if (PlayerStore.Instance != null && StoreUIManager.Instance != null)
                {
                    break;
                }
                yield return new WaitForSeconds(1f);
            }
            if (PlayerStore.Instance == null || StoreUIManager.Instance == null)
            {
                Log.Info("[SelfTest] SKIP: no store scene loaded within 90 seconds");
                yield break;
            }

            yield return new WaitForSeconds(1.5f);
            int pass = 0;
            int fail = 0;
            RunTests(delegate(bool ok, string name, string detail)
            {
                if (ok)
                {
                    pass++;
                    Log.Info("[SelfTest] PASS " + name + (string.IsNullOrEmpty(detail) ? "" : " | " + detail));
                }
                else
                {
                    fail++;
                    Log.Error("[SelfTest] FAIL " + name + (string.IsNullOrEmpty(detail) ? "" : " | " + detail));
                }
            });
            Log.Info("[SelfTest] SUMMARY pass=" + pass + " fail=" + fail);
        }

        private static void RunTests(Action<bool, string, string> check)
        {
            try
            {
                GameItem hand = PreBuiltItemHelper.CreateHandRandom();
                int handValue;
                bool valueOk = Log.TryGetCorrectValue(hand, out handValue);
                check(valueOk && handValue > 0, "appraisal_value", "hand=" + handValue);
                string autoText = handValue.ToString(CultureInfo.InvariantCulture);
                check(autoText == handValue.ToString(CultureInfo.InvariantCulture), "auto_fill_value", autoText);
            }
            catch (Exception e)
            {
                check(false, "appraisal_value", e.GetType().Name + ": " + e.Message);
            }

            CheckTruth(check, "fake_stamp", delegate { return PreBuiltItemHelper.CreateFakeStamp(); }, TruthKind.Counterfeit);
            CheckTruth(check, "real_stamp", delegate { return PreBuiltItemHelper.CreateRealStamp(160); }, TruthKind.Genuine);
            CheckTruth(check, "fake_injector", delegate { return PreBuiltItemHelper.CreateRealCounterfeitInjector(); }, TruthKind.Counterfeit);
            CheckTruth(check, "real_injector", delegate { return PreBuiltItemHelper.CreateRealGenuineInjector(); }, TruthKind.Genuine);

            try
            {
                string folder = ModHelper.GetModFolder(AppraisalAssistantMod.ModId);
                if (string.IsNullOrEmpty(folder))
                {
                    check(false, "mod_scan", "could not resolve mod folder");
                }
                else
                {
                    string dll = folder.TrimEnd('/', '\\') + "/" + AppraisalAssistantMod.AssemblyName + ".dll";
                    ModScanResult scan = ModScanner.Scan(
                        new string[] { dll },
                        new string[] { AppraisalAssistantMod.AssemblyName },
                        folder,
                        true);
                    string caps = scan.Capabilities == null ? "" : string.Join(", ", new List<string>(scan.Capabilities).ToArray());
                    string findings = scan.Findings == null ? "" : scan.FormatFindings(4);
                    Log.Info("[Scan] refused=" + scan.Refused + " capabilities=[" + caps + "] findings=" + (scan.Findings == null ? 0 : scan.Findings.Count));
                    if (!string.IsNullOrEmpty(findings))
                    {
                        Log.Info("[Scan] " + findings.Replace("\n", " / "));
                    }
                    check(!scan.Refused && (scan.Capabilities == null || scan.Capabilities.Count == 0), "mod_scan", "refused=" + scan.Refused + " caps=[" + caps + "]");
                }
            }
            catch (Exception e)
            {
                check(false, "mod_scan", e.GetType().Name + ": " + e.Message);
            }
        }

        private static void CheckTruth(Action<bool, string, string> check, string name, Func<GameItem> factory, TruthKind expected)
        {
            try
            {
                GameItem item = factory();
                TruthResult truth;
                bool ok = Truth.TryGet(item, out truth) && truth.Kind == expected;
                check(ok, name, ok ? truth.Label : "expected " + expected + ", got " + truth.Kind);
            }
            catch (Exception e)
            {
                check(false, name, e.GetType().Name + ": " + e.Message);
            }
        }
    }
}
