using System.IO;
using GridKeeper.AR;
using GridKeeper.UI;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace GridKeeper.EditorTools
{
    public static class SceneBuilder
    {
        private const string Root = "Assets/GridKeeper";
        private const string ScenePath = Root + "/Scenes/Main.unity";
        private const string TexturesFolder = Root + "/Art/Textures";
        private const string MaterialsFolder = Root + "/Art/Materials";
        private const string PrefabsFolder = Root + "/Prefabs/AR";
        private const string UIFolder = Root + "/UI";

        public static readonly Color SolarAmber = Hex("#F2A516");
        public static readonly Color WindTeal = Hex("#10A0A3");
        public static readonly Color BatteryViolet = Hex("#7B5CE6");
        public static readonly Color ShortfallRed = Hex("#D6453B");
        public static readonly Color GroundGreen = Hex("#BAD7B2");

        [MenuItem("Grid Keeper/Setup/Build Main Scene")]
        public static void Build()
        {
            EnsureFolders();

            Texture2D dot = GetOrCreateTexture(TexturesFolder + "/PlaneDot.png", 64, DrawDot);
            Texture2D frame = GetOrCreateTexture(TexturesFolder + "/ReticleFrame.png", 256, DrawDashedFrame);

            Material planeMat = GetOrCreateUnlit(MaterialsFolder + "/PlaneDots.mat", new Color(1f, 1f, 1f, 0.85f), dot, new Vector2(25f, 25f), true);
            Material lineMat = GetOrCreateUnlit(MaterialsFolder + "/PlaneEdge.mat", new Color(1f, 1f, 1f, 0.35f), null, Vector2.one, true);
            Material reticleMat = GetOrCreateUnlit(MaterialsFolder + "/Reticle.mat", SolarAmber, frame, Vector2.one, true);
            Material groundMat = GetOrCreateLit(MaterialsFolder + "/Ground.mat", GroundGreen);

            GameObject planePrefab = GetOrCreatePlanePrefab(planeMat, lineMat);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateLight();

            Selection.activeObject = null;
            EditorApplication.ExecuteMenuItem("GameObject/XR/AR Session");
            Selection.activeObject = null;
            EditorApplication.ExecuteMenuItem("GameObject/XR/XR Origin (Mobile AR)");

            XROrigin origin = Object.FindAnyObjectByType<XROrigin>();
            if (origin == null)
            {
                Debug.LogError("[Grid Keeper] XR Origin menu item failed.");
                return;
            }

            origin.name = "XR Origin";
            Camera cam = origin.Camera;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 30f;

            var planeManager = origin.gameObject.AddComponent<ARPlaneManager>();
            planeManager.planePrefab = planePrefab;
            planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            origin.gameObject.AddComponent<ARRaycastManager>();
            origin.gameObject.AddComponent<ARAnchorManager>();

            PlacementReticle reticle = CreateReticle(reticleMat);
            Transform city = CreateCityPlaceholder(groundMat);

            var placement = origin.gameObject.AddComponent<ARPlacementController>();
            SetField(placement, "cityRoot", city);
            SetField(placement, "reticle", reticle);

            CreateUI(placement);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[Grid Keeper] Main scene built at " + ScenePath);
        }

        private static void CreateLight()
        {
            var go = new GameObject("Sun");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = new Color(1f, 0.97f, 0.92f);
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.6f;
            go.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
        }

        private static PlacementReticle CreateReticle(Material material)
        {
            var root = new GameObject("Placement Reticle");
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Footprint";
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(root.transform, false);
            quad.transform.localPosition = new Vector3(0f, 0.002f, 0f);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = new Vector3(0.44f, 0.44f, 1f);
            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var reticle = root.AddComponent<PlacementReticle>();
            root.SetActive(false);
            return reticle;
        }

        private static Transform CreateCityPlaceholder(Material groundMat)
        {
            var city = new GameObject("Brightwater");
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Board";
            Object.DestroyImmediate(board.GetComponent<Collider>());
            board.transform.SetParent(city.transform, false);
            board.transform.localScale = new Vector3(0.4f, 0.012f, 0.4f);
            board.transform.localPosition = new Vector3(0f, 0.006f, 0f);
            board.GetComponent<MeshRenderer>().sharedMaterial = groundMat;
            return city.transform;
        }

        private static void CreateUI(ARPlacementController placement)
        {
            var themePath = UIFolder + "/GridKeeperTheme.tss";
            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themePath);
            var panelPath = UIFolder + "/GameUIPanel.asset";
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelPath);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, panelPath);
            }

            panel.themeStyleSheet = theme;
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(828, 1792);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 0.5f;
            EditorUtility.SetDirty(panel);

            var go = new GameObject("Game UI");
            var document = go.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UIFolder + "/GameUI.uxml");
            var prompt = go.AddComponent<PlacementPromptView>();
            SetField(prompt, "placement", placement);
        }

        private static GameObject GetOrCreatePlanePrefab(Material planeMat, Material lineMat)
        {
            string path = PrefabsFolder + "/DotPlane.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject("DotPlane");
            go.AddComponent<ARPlane>();
            go.AddComponent<MeshFilter>();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = planeMat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = lineMat;
            line.widthMultiplier = 0.003f;
            line.useWorldSpace = false;
            line.loop = true;
            line.shadowCastingMode = ShadowCastingMode.Off;
            go.AddComponent<ARPlaneMeshVisualizer>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static Material GetOrCreateUnlit(string path, Color color, Texture2D texture, Vector2 tiling, bool transparent)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.SetColor("_BaseColor", color);
            if (texture != null)
            {
                mat.SetTexture("_BaseMap", texture);
                mat.SetTextureScale("_BaseMap", tiling);
            }

            if (transparent) MakeTransparent(mat);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material GetOrCreateLit(string path, Color color)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", 0.15f);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void MakeTransparent(Material mat)
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        private delegate Color PixelPainter(int x, int y, int size);

        private static Texture2D GetOrCreateTexture(string path, int size, PixelPainter painter)
        {
            if (!File.Exists(path))
            {
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        tex.SetPixel(x, y, painter(x, y, size));
                    }
                }

                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);

                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.mipmapEnabled = true;
                importer.filterMode = FilterMode.Trilinear;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Color DrawDot(int x, int y, int size)
        {
            float centre = (size - 1) * 0.5f;
            float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centre, centre));
            float alpha = Mathf.Clamp01((size * 0.09f - distance) / 1.5f);
            return new Color(1f, 1f, 1f, alpha);
        }

        private static Color DrawDashedFrame(int x, int y, int size)
        {
            const int thickness = 7;
            const int corner = 48;
            const int dash = 18;
            const int gap = 12;

            bool onLeft = x < thickness;
            bool onRight = x >= size - thickness;
            bool onBottom = y < thickness;
            bool onTop = y >= size - thickness;
            if (!(onLeft || onRight || onBottom || onTop)) return Color.clear;

            int along = (onLeft || onRight) ? y : x;
            bool inCorner = along < corner || along >= size - corner;
            bool inDash = (along - corner) % (dash + gap) < dash;
            return inCorner || inDash ? Color.white : Color.clear;
        }

        private static void SetField(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError("[Grid Keeper] Missing field " + field + " on " + target.GetType().Name);
                return;
            }

            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolders()
        {
            foreach (string folder in new[] { Root + "/Scenes", TexturesFolder, MaterialsFolder, PrefabsFolder, UIFolder })
            {
                Directory.CreateDirectory(folder);
            }

            AssetDatabase.Refresh();
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }
    }
}
