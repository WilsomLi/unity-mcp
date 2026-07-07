using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using Newtonsoft.Json.Linq;

namespace MCPForUnity.Editor.Helpers
{
    /// <summary>
    /// Compatibility shims for UnityEditor.PackageManager APIs that differ between
    /// Unity 2019.4 and Unity 2020.1+.
    ///
    /// - <see cref="PackageInfo.GetAllRegisteredPackages"/> was introduced in 2020.1.
    ///   On 2019.4 we fall back to parsing Packages/packages-lock.json.
    /// - <see cref="Client.Resolve"/> was introduced in 2020.1. On 2019.4 editing the
    ///   manifest already triggers package resolution automatically, so it is a no-op.
    /// </summary>
    internal static class UnityPackageManagerCompat
    {
        public static PackageInfo[] GetAllRegisteredPackages()
        {
#if UNITY_2020_1_OR_NEWER
            return PackageInfo.GetAllRegisteredPackages();
#else
            try
            {
                string lockPath = Path.Combine(
                    Path.GetFullPath("Packages"), "packages-lock.json");
                if (!File.Exists(lockPath))
                    return new PackageInfo[0];

                var root = JObject.Parse(File.ReadAllText(lockPath));
                var deps = root["dependencies"] as JObject;
                if (deps == null)
                    return new PackageInfo[0];

                var result = new List<PackageInfo>();
                foreach (var entry in deps.Properties())
                {
                    string name = entry.Name;
                    var obj = entry.Value as JObject;
                    string version = obj?["version"]?.ToString() ?? string.Empty;

                    var childDeps = new List<PackageInfo>();
                    var depObj = obj?["dependencies"] as JObject;
                    if (depObj != null)
                    {
                        foreach (var d in depObj.Properties())
                            childDeps.Add(MakePackage(d.Name, d.Value.ToString(), null));
                    }

                    result.Add(MakePackage(name, version, childDeps.ToArray()));
                }

                return result.ToArray();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning(
                    "[UnityPackageManagerCompat] Failed to read packages-lock.json: " + ex.Message);
                return new PackageInfo[0];
            }
#endif
        }

        public static void Resolve()
        {
#if UNITY_2020_1_OR_NEWER
            Client.Resolve();
#endif
        }

#if !UNITY_2020_1_OR_NEWER
        private static PackageInfo MakePackage(string name, string version, PackageInfo[] deps)
        {
            var pi = (PackageInfo)System.Runtime.Serialization.FormatterServices
                .GetUninitializedObject(typeof(PackageInfo));
            SetField(pi, "name", name);
            SetField(pi, "version", version);
            SetField(pi, "displayName", name);
            SetField(pi, "dependencies", deps ?? new PackageInfo[0]);
            return pi;
        }

        private static void SetField(object obj, string propName, object value)
        {
            var fi = typeof(PackageInfo).GetField(
                "<" + propName + ">k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (fi != null)
                fi.SetValue(obj, value);
        }
#endif
    }
}
