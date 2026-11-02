using System;
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
                delegate { return "显示真实价值"; },
                delegate { return "在鉴定物品提示中加入真值与精确区间。"; });
            AutoFill = config.Define(
                "auto_fill",
                true,
                "appraisal",
                delegate { return "自动填入鉴定价格"; },
                delegate { return "打开鉴定面板时，自动把正确价值填入输入框。"; });
            ForceEstimate = config.Define(
                "force_correct_estimate",
                true,
                "appraisal",
                delegate { return "提交时使用正确价格"; },
                delegate { return "按下估算时，即使输入框被改过，也以正确价值提交。"; });

            Log.Info("enabled v" + ModVersion);
        }

        public void OnDisable()
        {
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
                "真值：" + value.ToString(CultureInfo.InvariantCulture),
                true,
                RenderHandler.ColorPalette.LightGreen);
            builder.AddLine(
                "精确区间：" + low.ToString(CultureInfo.InvariantCulture) + " - " + high.ToString(CultureInfo.InvariantCulture),
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

    [HarmonyPatch(typeof(InspectionUIManager), nameof(InspectionUIManager.OnInspectItem))]
    internal static class AppraisalInspectResetPatch
    {
        private static void Postfix(InspectionUIManager __instance, GameItem gameItem)
        {
            if (__instance == null || gameItem == null || !gameItem.IsTag("APPRAISAL_ITEM"))
            {
                return;
            }
            // 同一顾客的第二件鉴定品可能沿用上一个输入框状态；
            // 每件物品开始检查时都重置，确保各自使用自己的价值。
            __instance.isEditingAppraisalField = false;
            int value;
            if (!Log.TryGetCorrectValue(gameItem, out value))
            {
                return;
            }
            if (__instance.apprisalServiceInputField != null)
            {
                __instance.apprisalServiceInputField.text = value.ToString(CultureInfo.InvariantCulture);
            }
            gameItem.ModifyTag("APPRAISAL_DEFAULT_VALUE", delegate(TagState state)
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
                    tag = "  <color=#39A78E>[真品]</color>";
                    break;
                case TruthKind.Counterfeit:
                    tag = "  <color=#FF5555>[假货]</color>";
                    break;
                case TruthKind.Defective:
                    tag = "  <color=#FFAA00>[有缺陷]</color>";
                    break;
                case TruthKind.Replica:
                    tag = "  <color=#FFAA00>[复制品]</color>";
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
            builder.AddLine("真伪判定：" + truth.Label, true, color);
            if (truth.Reason != null)
            {
                builder.AddLine("原因：" + truth.Reason, true, color);
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
        public string Reason;
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
                    result.Label = fake ? "假货" : "真品";
                    result.Reason = fake ? RibwichReason(item) : null;
                    return true;
                }
                if (item.IsTag("WINE_COUNTERFEIT_TAG"))
                {
                    result.Kind = TruthKind.Counterfeit;
                    result.Label = "假货";
                    result.Reason = "带有假酒标记";
                    return true;
                }

                ItemFeature feature = item.FindItemFeatureByCategory("CATEGORY_GENUINE_CIGARETTE");
                if (feature != null && feature.realCondition != null)
                {
                    return FromCondition(feature, item, out result);
                }
                feature = item.FindItemFeatureByCategory("CATEGORY_GENUINE_STAMP");
                if (feature != null && feature.realCondition != null)
                {
                    return FromCondition(feature, item, out result);
                }
                feature = item.FindItemFeatureByCategory("CATEGORY_GENUINE_INJECTOR");
                if (feature != null && feature.realCondition != null)
                {
                    return FromCondition(feature, item, out result);
                }
                feature = item.FindItemFeatureByCategory("CATEGORY_REPLICA");
                if (feature != null && feature.realCondition != null)
                {
                    return FromCondition(feature, item, out result);
                }
            }
            catch (Exception e)
            {
                Log.Warn("Authenticity check failed: " + e.Message);
            }
            return false;
        }

        private static bool FromCondition(ItemFeature feature, GameItem item, out TruthResult result)
        {
            string id = feature.realCondition.identifier ?? "";
            result = new TruthResult();
            switch (id)
            {
                case "genuineCigarette":
                case "genuineStamp":
                case "genuineInjector":
                case "non_replica":
                    result.Kind = TruthKind.Genuine;
                    result.Label = "真品";
                    return true;
                case "counterfeitCigarette":
                case "counterfeitStamp":
                case "counterfeitInjector":
                    result.Kind = TruthKind.Counterfeit;
                    result.Label = "假货";
                    result.Reason = CounterfeitReason(item);
                    return true;
                case "expiredInjector":
                    result.Kind = TruthKind.Defective;
                    result.Label = "有缺陷";
                    result.Reason = "已过期" + ReasonSuffix(item);
                    return true;
                case "unusableInjector":
                    result.Kind = TruthKind.Defective;
                    result.Label = "有缺陷";
                    result.Reason = "无法使用" + ReasonSuffix(item);
                    return true;
                case "replica":
                    result.Kind = TruthKind.Replica;
                    result.Label = "复制品";
                    result.Reason = "复制品，非原版";
                    return true;
                default:
                    return false;
            }
        }

        private static string CounterfeitReason(GameItem item)
        {
            string issues = CollectIssues(item);
            return string.IsNullOrEmpty(issues) ? "与正品特征不符" : issues;
        }

        private static string ReasonSuffix(GameItem item)
        {
            string issues = CollectIssues(item);
            return string.IsNullOrEmpty(issues) ? "" : "（" + issues + "）";
        }

        private static string RibwichReason(GameItem item)
        {
            List<string> reasons = new List<string>();
            try
            {
                int month = item.GetTagReadonly("RIBWICH_PRODUCTION_MONTH_TAG").GetInt();
                int year = item.GetTagReadonly("RIBWICH_PRODUCTION_YEAR_TAG").GetInt();
                int facility = item.GetTagReadonly("RIBWICH_FACILITY_NUMBER_TAG").GetInt();
                int logo = item.GetTagReadonly("RIBWICH_DAVRE_LOGO_TAG").GetInt();
                if (!RibwichHelper.IsValidProductionDate(month, year)) reasons.Add("生产日期异常");
                if (!RibwichHelper.IsValidFacilityNumber(facility)) reasons.Add("设施编号异常");
                if (logo != RibwichHelper.RIBWICH_DAVRE_LOGO_CORRECT) reasons.Add("商标异常");
                if (RibwichHelper.IsOpened(item)) reasons.Add("已开封");
            }
            catch (Exception)
            {
            }
            if (reasons.Count == 0) reasons.Add("包装与正品不符");
            return string.Join("、", reasons.ToArray());
        }

        private static string CollectIssues(GameItem item)
        {
            List<string> issues = new List<string>();
            if (item.IsTag("CIGARETTE_LETTERING_ISSUE")) issues.Add("字母印刷问题");
            if (item.IsTag("CIGARETTE_COLOR_ISSUE")) issues.Add("颜色问题");
            if (item.IsTag("CIGARETTE_SEAL_ISSUE")) issues.Add("防伪标问题");
            if (item.IsTag("CIGARETTE_LOGO_ISSUE")) issues.Add("商标问题");
            if (item.IsTag("CIGARETTE_BOX_ISSUE")) issues.Add("包装盒问题");
            if (item.IsTag("STAMP_LETTERING_ISSUE")) issues.Add("字母印刷问题");
            if (item.IsTag("STAMP_LOGO_ISSUE")) issues.Add("商标问题");
            if (item.IsTag("STAMP_SEAL_ISSUE")) issues.Add("防伪标问题");
            if (item.IsTag("STAMP_AMOUNT_ISSUE")) issues.Add("面额问题");
            if (item.IsTag("STAMP_CHIP_ISSUE")) issues.Add("芯片问题");
            if (item.IsTag("ISSUE_INDICATOR")) issues.Add("指示灯问题");
            if (item.IsTag("ISSUE_LABEL")) issues.Add("标签问题");
            if (item.IsTag("ISSUE_SERIAL")) issues.Add("序列号问题");
            if (item.IsTag("ISSUE_BODY")) issues.Add("瓶体问题");
            if (item.IsTag("ISSUE_COLOR")) issues.Add("颜色问题");
            return issues.Count == 0 ? null : string.Join("、", issues.ToArray());
        }
    }

}
