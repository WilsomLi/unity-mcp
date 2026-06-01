using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
#if UNITY_2021_2_OR_NEWER
using McpBuildTargetKey = UnityEditor.Build.NamedBuildTarget;
#else
using McpBuildTargetKey = UnityEditor.BuildTargetGroup;
#endif

namespace MCPForUnity.Editor.Tools.Build
{
    public static class BuildSettingsHelper
    {
        public static object ReadProperty(string property, McpBuildTargetKey namedTarget)
        {
            switch (property.ToLowerInvariant())
            {
                case "product_name":
                    return new { property, value = PlayerSettings.productName };
                case "company_name":
                    return new { property, value = PlayerSettings.companyName };
                case "version":
                    return new { property, value = PlayerSettings.bundleVersion };
                case "bundle_id":
                    return new { property, value = GetApplicationIdentifier(namedTarget) };
                case "scripting_backend":
                    var backend = GetScriptingBackend(namedTarget);
                    return new { property, value = backend == ScriptingImplementation.IL2CPP ? "il2cpp" : "mono" };
                case "defines":
                    return new { property, value = GetScriptingDefineSymbols(namedTarget) };
                case "architecture":
                    var arch = GetArchitecture(namedTarget);
                    string archName;
                    switch (arch)
                    {
                        case 0:
                            archName = "x86_64";
                            break;
                        case 1:
                            archName = "arm64";
                            break;
                        case 2:
                            archName = "universal";
                            break;
                        default:
                            archName = "unknown";
                            break;
                    }
                    return new { property, value = archName, raw = arch };
                default:
                    return null;
            }
        }

        public static string WriteProperty(string property, string value, McpBuildTargetKey namedTarget)
        {
            try
            {
                switch (property.ToLowerInvariant())
                {
                    case "product_name":
                        PlayerSettings.productName = value;
                        return null;
                    case "company_name":
                        PlayerSettings.companyName = value;
                        return null;
                    case "version":
                        PlayerSettings.bundleVersion = value;
                        return null;
                    case "bundle_id":
                        SetApplicationIdentifier(namedTarget, value);
                        return null;
                    case "scripting_backend":
                        var backendValue = value.ToLowerInvariant();
                        if (backendValue != "il2cpp" && backendValue != "mono")
                            return $"Unknown scripting_backend '{value}'. Valid: mono, il2cpp";
                        var impl = backendValue == "il2cpp"
                            ? ScriptingImplementation.IL2CPP
                            : ScriptingImplementation.Mono2x;
                        SetScriptingBackend(namedTarget, impl);
                        return null;
                    case "defines":
                        SetScriptingDefineSymbols(namedTarget, value);
                        return null;
                    case "architecture":
                        int arch;
                        switch (value.ToLowerInvariant())
                        {
                            case "x86_64":
                            case "none":
                            case "default":
                                arch = 0;
                                break;
                            case "arm64":
                                arch = 1;
                                break;
                            case "universal":
                                arch = 2;
                                break;
                            default:
                                arch = -1;
                                break;
                        }
                        if (arch < 0)
                            return $"Unknown architecture '{value}'. Valid: x86_64, arm64, universal";
                        SetArchitecture(namedTarget, arch);
                        return null;
                    default:
                        return $"Unknown property '{property}'. Valid: product_name, company_name, version, bundle_id, scripting_backend, defines, architecture";
                }
            }
            catch (Exception ex)
            {
                return $"Failed to set {property}: {ex.Message}";
            }
        }

        public static readonly IReadOnlyList<string> ValidProperties = new[]
        {
            "product_name", "company_name", "version", "bundle_id",
            "scripting_backend", "defines", "architecture"
        };

#if UNITY_2021_2_OR_NEWER
        private static string GetApplicationIdentifier(McpBuildTargetKey target)
        {
            return PlayerSettings.GetApplicationIdentifier(target);
        }

        private static void SetApplicationIdentifier(McpBuildTargetKey target, string value)
        {
            PlayerSettings.SetApplicationIdentifier(target, value);
        }

        private static ScriptingImplementation GetScriptingBackend(McpBuildTargetKey target)
        {
            return PlayerSettings.GetScriptingBackend(target);
        }

        private static void SetScriptingBackend(McpBuildTargetKey target, ScriptingImplementation value)
        {
            PlayerSettings.SetScriptingBackend(target, value);
        }

        private static string GetScriptingDefineSymbols(McpBuildTargetKey target)
        {
            return PlayerSettings.GetScriptingDefineSymbols(target);
        }

        private static void SetScriptingDefineSymbols(McpBuildTargetKey target, string value)
        {
            PlayerSettings.SetScriptingDefineSymbols(target, value);
        }
#else
        private static string GetApplicationIdentifier(McpBuildTargetKey target)
        {
            return PlayerSettings.GetApplicationIdentifier(target);
        }

        private static void SetApplicationIdentifier(McpBuildTargetKey target, string value)
        {
            PlayerSettings.SetApplicationIdentifier(target, value);
        }

        private static ScriptingImplementation GetScriptingBackend(McpBuildTargetKey target)
        {
            return PlayerSettings.GetScriptingBackend(target);
        }

        private static void SetScriptingBackend(McpBuildTargetKey target, ScriptingImplementation value)
        {
            PlayerSettings.SetScriptingBackend(target, value);
        }

        private static string GetScriptingDefineSymbols(McpBuildTargetKey target)
        {
            return PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
        }

        private static void SetScriptingDefineSymbols(McpBuildTargetKey target, string value)
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, value);
        }
#endif

        private static int GetArchitecture(McpBuildTargetKey target)
        {
            return PlayerSettings.GetArchitecture(target);
        }

        private static void SetArchitecture(McpBuildTargetKey target, int value)
        {
            PlayerSettings.SetArchitecture(target, value);
        }

    }
}
