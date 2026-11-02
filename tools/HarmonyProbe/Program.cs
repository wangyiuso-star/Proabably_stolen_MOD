using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;

internal static class HarmonyProbe
{
    private static int Main(string[] args)
    {
        string managed = args[0];
        string[] mods = args.Skip(1).ToArray();
        AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs e)
        {
            string name = new AssemblyName(e.Name).Name;
            string path = Path.Combine(managed, name + ".dll");
            return File.Exists(path) ? Assembly.UnsafeLoadFrom(path) : null;
        };

        Assembly.UnsafeLoadFrom(Path.Combine(managed, "0Harmony.dll"));
        foreach (string mod in mods)
        {
            Console.WriteLine("== " + mod);
            Assembly assembly = Assembly.UnsafeLoadFrom(mod);
            Harmony harmony = new Harmony("probe." + Path.GetFileNameWithoutExtension(mod));
            try
            {
                harmony.PatchAll(assembly);
            }
            catch (Exception e)
            {
                Console.WriteLine("FAIL PatchAll: " + e.GetType().Name + ": " + e.Message);
                if (e.InnerException != null)
                {
                    Console.WriteLine("  inner: " + e.InnerException.GetType().Name + ": " + e.InnerException.Message);
                }
                return 1;
            }
            var patched = harmony.GetPatchedMethods().Select(m => m.DeclaringType.FullName + "." + m.Name).OrderBy(s => s).ToArray();
            Console.WriteLine("patched " + patched.Length + " method(s)");
            foreach (string p in patched)
            {
                Console.WriteLine("  " + p);
            }
            if (patched.Length == 0 && !Path.GetFileNameWithoutExtension(mod).Equals("AugSpotter", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("FAIL: expected at least one patch");
                return 1;
            }
            harmony.UnpatchAll(harmony.Id);
        }
        Console.WriteLine("RESULT: PASS");
        return 0;
    }
}
