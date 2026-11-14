using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;

namespace GridKeeper.EditorTools
{
    public static class ProjectConfigurator
    {
        private const string BundleId = "com.brimstudios.gridkeeper";
        private const string ARKitLoader = "UnityEngine.XR.ARKit.ARKitLoader";
        private const string SimulationLoader = "UnityEngine.XR.Simulation.SimulationLoader";
        private const string XRSettingsPath = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";

        private static readonly string[] RendererPaths =
        {
            "Assets/Settings/Mobile_Renderer.asset",
            "Assets/Settings/PC_Renderer.asset"
        };

        [MenuItem("Grid Keeper/Setup/Configure Player Settings")]
        public static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Brim Studios";
            PlayerSettings.productName = "Grid Keeper";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, BundleId);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.cameraUsageDescription = "Grid Keeper uses the camera to place the town of Brightwater on your table.";
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.bundleVersion = "0.1.0";
            AssetDatabase.SaveAssets();
            Debug.Log("[Grid Keeper] Player settings configured.");
        }

        [MenuItem("Grid Keeper/Setup/Configure XR Loaders")]
        public static void ConfigureXR()
        {
            XRGeneralSettingsPerBuildTarget perTarget = GetOrCreateXRSettings();
            AssignLoader(perTarget, BuildTargetGroup.iOS, ARKitLoader);
            AssignLoader(perTarget, BuildTargetGroup.Standalone, SimulationLoader);
            EditorUtility.SetDirty(perTarget);
            AssetDatabase.SaveAssets();
            Debug.Log("[Grid Keeper] XR loaders: iOS=ARKit, Standalone=XR Simulation.");
        }

        [MenuItem("Grid Keeper/Setup/Configure URP For AR")]
        public static void ConfigureRenderers()
        {
            foreach (string path in RendererPaths)
            {
                var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
                if (data == null)
                {
                    Debug.LogWarning("[Grid Keeper] Renderer not found at " + path);
                    continue;
                }

                data.depthPrimingMode = DepthPrimingMode.Disabled;

                if (!data.rendererFeatures.Exists(f => f is ARBackgroundRendererFeature))
                {
                    var feature = ScriptableObject.CreateInstance<ARBackgroundRendererFeature>();
                    feature.name = "ARBackgroundRendererFeature";
                    AssetDatabase.AddObjectToAsset(feature, data);
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);

                    var so = new SerializedObject(data);
                    SerializedProperty features = so.FindProperty("m_RendererFeatures");
                    SerializedProperty map = so.FindProperty("m_RendererFeatureMap");
                    features.arraySize++;
                    features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;
                    map.arraySize++;
                    map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                EditorUtility.SetDirty(data);
                Debug.Log("[Grid Keeper] AR background feature on " + Path.GetFileName(path));
            }

            AssetDatabase.SaveAssets();
        }

        [MenuItem("Grid Keeper/Setup/Switch To iOS")]
        public static void SwitchToIOS()
        {
            bool supported = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS);
            Debug.Log("[Grid Keeper] iOS module installed: " + supported);
            if (!supported) return;
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
        }

        [MenuItem("Grid Keeper/Setup/Run All")]
        public static void RunAll()
        {
            ConfigurePlayer();
            ConfigureXR();
            ConfigureRenderers();
        }

        private static XRGeneralSettingsPerBuildTarget GetOrCreateXRSettings()
        {
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget settings);
            if (settings == null)
            {
                string[] found = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget");
                if (found.Length > 0)
                {
                    settings = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(AssetDatabase.GUIDToAssetPath(found[0]));
                }
            }

            if (settings == null)
            {
                Directory.CreateDirectory("Assets/XR");
                settings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(settings, XRSettingsPath);
            }

            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, settings, true);
            return settings;
        }

        private static void AssignLoader(XRGeneralSettingsPerBuildTarget perTarget, BuildTargetGroup group, string loader)
        {
            if (!perTarget.HasManagerSettingsForBuildTarget(group))
            {
                perTarget.CreateDefaultManagerSettingsForBuildTarget(group);
            }

            XRGeneralSettings general = perTarget.SettingsForBuildTarget(group);
            general.InitManagerOnStart = true;
            XRManagerSettings manager = general.Manager;
            if (!XRPackageMetadataStore.IsLoaderAssigned(loader, group))
            {
                bool ok = XRPackageMetadataStore.AssignLoader(manager, loader, group);
                Debug.Log("[Grid Keeper] Assign " + loader + " to " + group + ": " + ok);
            }
        }
    }
}
