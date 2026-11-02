using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

internal static class LogicProbe
{
    private static int _fail;
    private static Assembly _game;
    private static Assembly _appraisal;
    private static Type _itemType;
    private static Type _featureType;
    private static Type _conditionType;
    private static Type _tagSystemType;

    private static int Main(string[] args)
    {
        try { return Run(args); }
        catch (Exception e)
        {
            Console.WriteLine("FATAL " + e.GetType().Name + ": " + e.Message);
            Console.WriteLine(e.StackTrace);
            return 1;
        }
    }

    private static int Run(string[] args)
    {
        string deps = args[0];
        string appraisalDll = args[1];
        string augDll = args[2];

        AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs e)
        {
            string name = new AssemblyName(e.Name).Name;
            string path = Path.Combine(deps, name + ".dll");
            return File.Exists(path) ? Assembly.UnsafeLoadFrom(path) : null;
        };

        _game = Assembly.UnsafeLoadFrom(Path.Combine(deps, "Assembly-CSharp.dll"));
        Type modLoader = _game.GetType("ModLoader");
        FieldInfo namesField = modLoader.GetField("gameAssemblyNames", BindingFlags.NonPublic | BindingFlags.Static);
        HashSet<string> names = namesField.GetValue(null) as HashSet<string>;
        if (names == null)
        {
            names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            namesField.SetValue(null, names);
        }
        foreach (string dll in Directory.GetFiles(deps, "*.dll"))
        {
            names.Add(Path.GetFileNameWithoutExtension(dll));
        }

        _appraisal = Assembly.UnsafeLoadFrom(appraisalDll);
        Type augMod = Assembly.UnsafeLoadFrom(augDll).GetType("DeepSpaceQoL.AugSpotterMod");
        MethodInfo isAug = augMod.GetMethod("IsAug", BindingFlags.NonPublic | BindingFlags.Static);
        Type storeClient = _game.GetType("StoreClient");
        object augClient = Activator.CreateInstance(storeClient);
        storeClient.GetField("isAug").SetValue(augClient, true);
        object normalClient = Activator.CreateInstance(storeClient);
        storeClient.GetField("isAug").SetValue(normalClient, false);
        Check((bool)isAug.Invoke(null, new object[] { augClient }), "aug=true");
        Check(!(bool)isAug.Invoke(null, new object[] { normalClient }), "aug=false");
        Check(!(bool)isAug.Invoke(null, new object[] { null }), "aug=null");

        _itemType = _game.GetType("GameItemElement");
        _featureType = _game.GetType("ItemFeature");
        _conditionType = _game.GetType("ItemCondition");
        _tagSystemType = _game.GetType("TagSystem");

        CheckTruth("fake_stamp", "CATEGORY_GENUINE_STAMP", "counterfeitStamp", "STAMP_LOGO_ISSUE", "Counterfeit");
        CheckTruth("real_stamp", "CATEGORY_GENUINE_STAMP", "genuineStamp", null, "Genuine");
        CheckTruth("fake_injector", "CATEGORY_GENUINE_INJECTOR", "counterfeitInjector", "ISSUE_LABEL", "Counterfeit");
        CheckTruth("expired_injector", "CATEGORY_GENUINE_INJECTOR", "expiredInjector", "ISSUE_INDICATOR", "Defective");
        CheckTruth("unusable_injector", "CATEGORY_GENUINE_INJECTOR", "unusableInjector", "ISSUE_BODY", "Defective");
        CheckTruth("replica", "CATEGORY_REPLICA", "replica", null, "Replica");
        CheckTruth("fake_wine", null, null, "WINE_COUNTERFEIT_TAG", "Counterfeit");
        CheckUnknown();
        CheckAppraisalValue();

        Type scanner = _game.GetType("ModScanner");
        MethodInfo scan = scanner.GetMethod("Scan", BindingFlags.Public | BindingFlags.Static);
        Scan(scanner, scan, appraisalDll, "AppraisalAssistant");
        Scan(scanner, scan, augDll, "AugSpotter");

        Console.WriteLine(_fail == 0 ? "RESULT: PASS" : "RESULT: FAIL (" + _fail + ")");
        return _fail == 0 ? 0 : 1;
    }

    private static object MakeItem()
    {
        object item = FormatterServices.GetUninitializedObject(_itemType);
        object features = Activator.CreateInstance(typeof(List<>).MakeGenericType(_featureType));
        _itemType.GetField("itemFeatures").SetValue(item, features);
        object state = Activator.CreateInstance(_tagSystemType);
        object modified = Activator.CreateInstance(_tagSystemType);
        _itemType.GetProperty("state").SetValue(item, state);
        _itemType.GetProperty("modifiedState").SetValue(item, modified);
        return item;
    }

    private static void EnableTag(object item, string tag)
    {
        object modified = _itemType.GetProperty("modifiedState").GetValue(item);
        _tagSystemType.GetMethod("EnableTag", new Type[] { typeof(string) }).Invoke(modified, new object[] { tag });
    }

    private static void AddFeature(object item, string category, string conditionIdentifier)
    {
        object feature = Activator.CreateInstance(_featureType);
        _featureType.GetProperty("category").SetValue(feature, category);
        object condition = Activator.CreateInstance(_conditionType);
        _conditionType.GetField("category").SetValue(condition, category);
        _conditionType.GetField("identifier").SetValue(condition, conditionIdentifier);
        _featureType.GetField("realCondition").SetValue(feature, condition);
        _featureType.GetField("useCondition").SetValue(feature, true);
        ((IList)_itemType.GetField("itemFeatures").GetValue(item)).Add(feature);
    }

    private static void CheckTruth(string name, string category, string condition, string tag, string expectedKind)
    {
        try
        {
            object item = MakeItem();
            if (category != null) AddFeature(item, category, condition);
            if (tag != null) EnableTag(item, tag);
            Type truth = _appraisal.GetType("DeepSpaceQoL.Truth");
            MethodInfo tryGet = truth.GetMethod("TryGet", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            object[] args = new object[] { item, null };
            bool ok = (bool)tryGet.Invoke(null, args);
            string kind = ok ? args[1].GetType().GetField("Kind").GetValue(args[1]).ToString() : "None";
            string label = ok ? (string)args[1].GetType().GetField("Label").GetValue(args[1]) : "";
            Check(ok && kind == expectedKind, name + " kind=" + kind + " label=" + label);
        }
        catch (Exception e)
        {
            Exception inner = e.InnerException ?? e;
            Console.WriteLine("FAIL " + name + " exception: " + inner.GetType().Name + ": " + inner.Message);
            _fail++;
        }
    }

    private static void CheckUnknown()
    {
        try
        {
            object item = MakeItem();
            Type truth = _appraisal.GetType("DeepSpaceQoL.Truth");
            MethodInfo tryGet = truth.GetMethod("TryGet", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            object[] args = new object[] { item, null };
            bool ok = (bool)tryGet.Invoke(null, args);
            Check(!ok, "unknown_item returns false");
        }
        catch (Exception e)
        {
            Console.WriteLine("FAIL unknown_item exception: " + e.Message);
            _fail++;
        }
    }

    private static void CheckAppraisalValue()
    {
        try
        {
            object item = MakeItem();
            EnableTag(item, "APPRAISAL_ITEM");
            EnableTag(item, "HAND_TAG");
            SetStringTag(item, "HAND_MATERIAL", "ALUMINUM_ALLOY");
            SetStringTag(item, "HAND_INTERFACE", "EMG");
            SetStringTag(item, "HAND_INTERFACE_MARKING", "FULL");
            SetStringTag(item, "HAND_CONDITION", "NONE");
            SetStringTag(item, "HAND_ADDON_ARMORED", "NONE");
            SetStringTag(item, "HAND_ADDON_BLADED", "NONE");
            Type log = _appraisal.GetType("DeepSpaceQoL.Log");
            MethodInfo tryGet = log.GetMethod("TryGetCorrectValue", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            object[] args = new object[] { item, 0 };
            bool ok = (bool)tryGet.Invoke(null, args);
            int value = (int)args[1];
            Check(ok && value > 0, "appraisal_value hand=" + value);
        }
        catch (Exception e)
        {
            Exception inner = e.InnerException ?? e;
            Console.WriteLine("FAIL appraisal_value exception: " + inner.GetType().Name + ": " + inner.Message);
            _fail++;
        }
    }

    private static void SetStringTag(object item, string tag, string value)
    {
        object modified = _itemType.GetProperty("modifiedState").GetValue(item);
        object state = _tagSystemType.GetMethod("GetTag", new Type[] { typeof(string) }).Invoke(modified, new object[] { tag });
        state.GetType().GetMethod("Enable").Invoke(state, null);
        state.GetType().GetMethod("SetString").Invoke(state, new object[] { value });
    }

    private static void Scan(Type scanner, MethodInfo scan, string dll, string name)
    {
        try
        {
            object result = scan.Invoke(null, new object[] { new string[] { dll }, new string[] { name }, Path.GetDirectoryName(dll), true });
            Type resultType = result.GetType();
            bool refused = (bool)resultType.GetProperty("Refused").GetValue(result);
            IEnumerable caps = (IEnumerable)resultType.GetField("Capabilities").GetValue(result);
            IList findings = (IList)resultType.GetField("Findings").GetValue(result);
            string error = (string)resultType.GetField("Error").GetValue(result);
            var capList = caps.Cast<object>().Select(o => o.ToString()).ToArray();
            Console.WriteLine("[Scan] " + Path.GetFileName(dll) + " refused=" + refused + " capabilities=[" + string.Join(", ", capList) + "] findings=" + findings.Count + " error=" + (error ?? "null"));
            Check(!refused && capList.Length == 0 && findings.Count == 0, "scan " + Path.GetFileName(dll));
        }
        catch (Exception e)
        {
            Exception inner = e.InnerException ?? e;
            Console.WriteLine("FAIL scan exception: " + inner.GetType().Name + ": " + inner.Message);
            _fail++;
        }
    }

    private static void Check(bool ok, string name)
    {
        if (ok) { Console.WriteLine("PASS " + name); }
        else { Console.WriteLine("FAIL " + name); _fail++; }
    }
}
