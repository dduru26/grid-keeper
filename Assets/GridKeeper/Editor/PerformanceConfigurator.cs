using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GridKeeper.EditorTools
{
    public static class PerformanceConfigurator
    {
        private const string MobileAsset = "Assets/Settings/Mobile_RPAsset.asset";

        [MenuItem("Grid Keeper/Setup/Apply Mobile Performance Settings")]
        public static void Apply()
        {
            ConfigurePipeline();
            ConfigurePlayer();
            ConfigureAudio();
            ConfigureTextures();
            AssetDatabase.SaveAssets();
            Debug.Log("[Grid Keeper] Mobile performance settings applied.");
        }

        private static void ConfigurePipeline()
        {
            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(MobileAsset);
            urp.supportsHDR = false;
            urp.msaaSampleCount = 2;
            urp.renderScale = 1f;
            urp.shadowDistance = 1.5f;
            urp.useSRPBatcher = true;

            var so = new SerializedObject(urp);
            so.FindProperty("m_AdditionalLightsRenderingMode").intValue = 0;
            so.FindProperty("m_MainLightShadowmapResolution").intValue = 1024;
            so.FindProperty("m_RequireDepthTexture").boolValue = false;
            so.FindProperty("m_RequireOpaqueTexture").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(urp);
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.iOS, Il2CppCompilerConfiguration.Master);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.iOS, ManagedStrippingLevel.Low);
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.gcIncremental = true;
            PlayerSettings.accelerometerFrequency = 0;
            PlayerSettings.SetMobileMTRendering(NamedBuildTarget.iOS, true);
        }

        private static void ConfigureAudio()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/GridKeeper/Audio" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                bool loop = path.Contains("ambient") || path.Contains("wind_loop");
                importer.forceToMono = true;
                importer.loadInBackground = loop;
                var settings = importer.defaultSampleSettings;
                settings.loadType = loop ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = loop ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
                settings.quality = 0.5f;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
        }

        private static void ConfigureTextures()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/GridKeeper/Art" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.mipmapEnabled = true;
                importer.maxTextureSize = 256;
                var ios = importer.GetPlatformTextureSettings("iPhone");
                ios.overridden = true;
                ios.maxTextureSize = 256;
                ios.format = TextureImporterFormat.ASTC_4x4;
                importer.SetPlatformTextureSettings(ios);
                importer.SaveAndReimport();
            }
        }
    }
}
