using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using DseCecil;
using DseCecil.Cil;

internal static class VerifyMods
{
    private static int _failures;
    private static HashSet<string> _gameAssemblies;
    private static AssemblyDefinition _game;
    private static Dictionary<string, TypeDefinition> _gameTypes;

    private static readonly string[] BannedTypePrefixes =
    {
        "System.IO.",
        "System.Net.",
        "System.Diagnostics.Process",
        "System.Reflection.Emit.",
        "System.Runtime.Loader.",
        "System.CodeDom.",
        "Microsoft.CSharp.CSharpCodeProvider",
        "System.Runtime.Serialization.Formatters.",
        "System.Runtime.InteropServices.Marshal",
        "System.Runtime.InteropServices.NativeLibrary",
        "System.Runtime.CompilerServices.Unsafe",
        "UnityEngine.ScreenCapture",
        "ES3"
    };

    private static readonly string[] FrameworkAssemblies =
    {
        "mscorlib",
        "netstandard",
        "System",
        "System.Core",
        "System.Xml",
        "System.Xml.Linq",
        "System.Runtime",
        "System.Collections",
        "System.Collections.Immutable"
    };

    private static readonly HashSet<string> InjectedPatchParameters = new HashSet<string>(StringComparer.Ordinal)
    {
        "__instance",
        "__result",
        "__state",
        "__originalMethod",
        "__runOriginal",
        "__args"
    };

    private static int Main(string[] args)
    {
        string root = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
        string gameDir = args.Length > 1 ? args[1] : @"C:\Program Files (x86)\Steam\steamapps\common\Probably Stolen";
        string managed = Path.Combine(gameDir, "Probably Stolen_Data", "Managed");
        string dist = Path.Combine(root, "dist");

        Console.WriteLine("VerifyMods root=" + root);
        Console.WriteLine("VerifyMods managed=" + managed);

        if (!Directory.Exists(dist))
        {
            Fail("dist folder does not exist: " + dist);
            return Finish();
        }

        _gameAssemblies = new HashSet<string>(
            Directory.GetFiles(managed, "*.dll").Select(Path.GetFileNameWithoutExtension),
            StringComparer.OrdinalIgnoreCase);
        _game = AssemblyDefinition.ReadAssembly(Path.Combine(managed, "Assembly-CSharp.dll"));
        _gameTypes = FlattenTypes(_game.MainModule.Types)
            .GroupBy(t => t.FullName)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        string[] manifests = Directory.GetFiles(dist, "manifest.xml", SearchOption.AllDirectories);
        if (manifests.Length == 0)
        {
            Fail("no manifest.xml found under dist");
            return Finish();
        }

        foreach (string manifestPath in manifests.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine();
            Console.WriteLine("== " + manifestPath);
            VerifyOne(manifestPath, dist);
        }

        return Finish();
    }

    private static void VerifyOne(string manifestPath, string dist)
    {
        string folder = Path.GetDirectoryName(manifestPath);
        string folderName = Path.GetFileName(folder);
        XDocument doc;
        try
        {
            doc = XDocument.Load(manifestPath);
        }
        catch (Exception e)
        {
            Fail(folderName + ": manifest.xml cannot be parsed: " + e.Message);
            return;
        }

        XElement root = doc.Root;
        string id = (string)root.Element("ID");
        string name = (string)root.Element("Name");
        string version = (string)root.Element("ModVersion");
        if (string.IsNullOrWhiteSpace(id))
        {
            Fail(folderName + ": missing <ID>");
            return;
        }
        if (!Regex.IsMatch(id, "^[A-Za-z0-9_.-]{1,64}$") || id.StartsWith(".", StringComparison.Ordinal))
        {
            Fail(folderName + ": invalid <ID> " + id);
        }
        else
        {
            Pass(folderName + ": ID " + id);
        }
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(version))
        {
            Fail(folderName + ": <Name> or <ModVersion> missing");
        }

        XElement capabilities = root.Element("Capabilities");
        if (capabilities == null)
        {
            Fail(folderName + ": missing <Capabilities> (use <Capabilities/> for none)");
        }
        else
        {
            int count = capabilities.Elements("Capability").Count();
            if (count == 0)
            {
                Pass(folderName + ": capabilities empty");
            }
            else
            {
                Fail(folderName + ": declares capabilities but this mod must use none: " + string.Join(", ", capabilities.Elements("Capability").Select(e => e.Value).ToArray()));
            }
        }

        XElement assembliesElement = root.Element("Assemblies");
        if (assembliesElement == null)
        {
            Fail(folderName + ": missing <Assemblies>");
            return;
        }
        List<string> assemblies = assembliesElement.Elements("Assembly").Select(e => e.Value.Trim()).Where(v => v.Length > 0).ToList();
        if (assemblies.Count == 0)
        {
            Fail(folderName + ": no <Assembly> entries");
            return;
        }

        List<string> ownAssemblyNames = new List<string>();
        foreach (string assembly in assemblies)
        {
            if (assembly.Contains("..") || Path.IsPathRooted(assembly) || !assembly.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                Fail(folderName + ": invalid assembly path " + assembly);
                continue;
            }
            string dllPath = Path.Combine(folder, assembly.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(dllPath))
            {
                Fail(folderName + ": listed assembly missing: " + assembly);
                continue;
            }
            if (new FileInfo(dllPath).Length <= 0)
            {
                Fail(folderName + ": listed assembly is empty: " + assembly);
                continue;
            }
            ownAssemblyNames.Add(Path.GetFileNameWithoutExtension(dllPath));
            VerifyAssembly(folderName, dllPath);
        }

        if (ownAssemblyNames.Count != assemblies.Count)
        {
            Fail(folderName + ": assembly list did not resolve cleanly");
        }

        foreach (string own in ownAssemblyNames)
        {
            if (_gameAssemblies.Contains(own))
            {
                Fail(folderName + ": assembly name collides with a game assembly: " + own);
            }
        }
    }

    private static void VerifyAssembly(string modName, string dllPath)
    {
        string fileName = Path.GetFileName(dllPath);
        AssemblyDefinition assembly;
        try
        {
            assembly = AssemblyDefinition.ReadAssembly(dllPath);
        }
        catch (Exception e)
        {
            Fail(fileName + ": cannot read assembly: " + e.Message);
            return;
        }

        List<TypeDefinition> types = FlattenTypes(assembly.MainModule.Types).ToList();
        List<TypeDefinition> modTypes = types.Where(t =>
            !t.IsInterface && !t.IsAbstract &&
            t.Interfaces.Any(i => i.InterfaceType.FullName == "IMod")).ToList();
        if (modTypes.Count != 1)
        {
            Fail(fileName + ": expected exactly one IMod implementation, found " + modTypes.Count);
        }
        else
        {
            TypeDefinition mod = modTypes[0];
            bool init = mod.Methods.Any(m => m.Name == "Init" && m.Parameters.Count == 1 &&
                m.Parameters[0].ParameterType.FullName == "ModManifest" && m.ReturnType.FullName == "System.Void");
            bool enable = mod.Methods.Any(m => m.Name == "OnEnable" && m.Parameters.Count == 0 && m.ReturnType.FullName == "System.Void");
            bool disable = mod.Methods.Any(m => m.Name == "OnDisable" && m.Parameters.Count == 0 && m.ReturnType.FullName == "System.Void");
            if (init && enable && disable)
            {
                Pass(fileName + ": IMod implemented by " + mod.FullName);
            }
            else
            {
                Fail(fileName + ": IMod implementation is missing Init/OnEnable/OnDisable signatures");
            }
        }

        foreach (AssemblyNameReference reference in assembly.MainModule.AssemblyReferences)
        {
            if (_gameAssemblies.Contains(reference.Name) || FrameworkAssemblies.Contains(reference.Name, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }
            Fail(fileName + ": unlisted assembly reference " + reference.Name);
        }

        int bannedHits = 0;
        foreach (TypeDefinition type in types)
        {
            foreach (MethodDefinition method in type.Methods)
            {
                if (method.IsPInvokeImpl)
                {
                    Fail(fileName + ": P/Invoke method " + type.FullName + "." + method.Name);
                    bannedHits++;
                }
                if (!method.HasBody)
                {
                    continue;
                }
                foreach (Instruction instruction in method.Body.Instructions)
                {
                    string target = GetMemberName(instruction.Operand);
                    if (target == null)
                    {
                        continue;
                    }
                    if (IsBanned(target))
                    {
                        Fail(fileName + ": banned API " + target + " in " + type.FullName + "." + method.Name);
                        bannedHits++;
                    }
                }
            }
        }
        if (bannedHits == 0)
        {
            Pass(fileName + ": no banned IL APIs");
        }

        int patchCount = 0;
        foreach (TypeDefinition type in types)
        {
            foreach (CustomAttribute attribute in type.CustomAttributes)
            {
                if (attribute.AttributeType.FullName != "HarmonyLib.HarmonyPatch")
                {
                    continue;
                }
                patchCount++;
                VerifyPatch(fileName, type, attribute);
            }
        }
        Pass(fileName + ": HarmonyPatch attributes checked (" + patchCount + ")");
    }

    private static void VerifyPatch(string fileName, TypeDefinition patchType, CustomAttribute attribute)
    {
        string targetTypeName = null;
        string targetMethodName = null;
        if (attribute.ConstructorArguments.Count >= 1 && attribute.ConstructorArguments[0].Value is TypeReference)
        {
            targetTypeName = ((TypeReference)attribute.ConstructorArguments[0].Value).FullName;
        }
        if (attribute.ConstructorArguments.Count >= 2 && attribute.ConstructorArguments[1].Value is string)
        {
            targetMethodName = (string)attribute.ConstructorArguments[1].Value;
        }
        if (targetTypeName == null)
        {
            Fail(fileName + ": " + patchType.FullName + " has a HarmonyPatch without a target type");
            return;
        }
        TypeDefinition targetType;
        if (!_gameTypes.TryGetValue(targetTypeName, out targetType))
        {
            Fail(fileName + ": " + patchType.FullName + " targets unknown game type " + targetTypeName);
            return;
        }
        if (targetMethodName == null)
        {
            return;
        }
        List<MethodDefinition> targets = targetType.Methods.Where(m => m.Name == targetMethodName).ToList();
        if (targets.Count == 0)
        {
            Fail(fileName + ": " + patchType.FullName + " targets missing method " + targetTypeName + "." + targetMethodName);
            return;
        }

        MethodDefinition patchMethod = patchType.Methods.FirstOrDefault(m =>
            m.Name == "Prefix" || m.Name == "Postfix" || m.Name == "Transpiler" || m.Name == "Finalizer");
        if (patchMethod == null)
        {
            return;
        }
        ParameterDefinition[] patchParameters = patchMethod.Parameters.Where(p => !IsInjected(p.Name)).ToArray();
        bool matched = targets.Any(t =>
        {
            foreach (ParameterDefinition p in patchParameters)
            {
                if (!t.Parameters.Any(tp => tp.Name == p.Name && tp.ParameterType.FullName == p.ParameterType.FullName))
                {
                    return false;
                }
            }
            return true;
        });
        if (matched)
        {
            Pass(fileName + ": " + patchType.FullName + " -> " + targetTypeName + "." + targetMethodName);
        }
        else
        {
            Fail(fileName + ": " + patchType.FullName + " parameter names/types do not match any " +
                targetTypeName + "." + targetMethodName + " overload");
        }
    }

    private static bool IsInjected(string name)
    {
        return name != null && (InjectedPatchParameters.Contains(name) || name.StartsWith("___", StringComparison.Ordinal));
    }

    private static string GetMemberName(object operand)
    {
        MethodReference method = operand as MethodReference;
        if (method != null)
        {
            return (method.DeclaringType == null ? "" : method.DeclaringType.FullName + ".") + method.Name;
        }
        FieldReference field = operand as FieldReference;
        if (field != null)
        {
            return (field.DeclaringType == null ? "" : field.DeclaringType.FullName + ".") + field.Name;
        }
        TypeReference type = operand as TypeReference;
        if (type != null)
        {
            return type.FullName;
        }
        return null;
    }

    private static bool IsBanned(string target)
    {
        foreach (string prefix in BannedTypePrefixes)
        {
            if (target.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }
        if (target.StartsWith("System.Reflection.Assembly.Load", StringComparison.Ordinal) ||
            target.StartsWith("System.AppDomain.Load", StringComparison.Ordinal) ||
            target.StartsWith("System.AppDomain.CreateInstance", StringComparison.Ordinal) ||
            target.StartsWith("System.AppDomain.ExecuteAssembly", StringComparison.Ordinal))
        {
            return true;
        }
        return false;
    }

    private static IEnumerable<TypeDefinition> FlattenTypes(IEnumerable<TypeDefinition> types)
    {
        foreach (TypeDefinition type in types)
        {
            yield return type;
            foreach (TypeDefinition nested in FlattenTypes(type.NestedTypes))
            {
                yield return nested;
            }
        }
    }

    private static void Pass(string message)
    {
        Console.WriteLine("PASS " + message);
    }

    private static void Fail(string message)
    {
        _failures++;
        Console.WriteLine("FAIL " + message);
    }

    private static int Finish()
    {
        Console.WriteLine();
        if (_failures == 0)
        {
            Console.WriteLine("RESULT: PASS");
            return 0;
        }
        Console.WriteLine("RESULT: FAIL (" + _failures + " check(s))");
        return 1;
    }
}
