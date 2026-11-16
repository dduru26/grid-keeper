using System.Collections.Generic;
using System.IO;
using GridKeeper.Data;
using GridKeeper.Energy;
using GridKeeper.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GridKeeper.EditorTools
{
    public sealed class ContentSet
    {
        public BalanceConfig Balance;
        public CityLayout Layout;
        public PlaceableDefinition Solar;
        public PlaceableDefinition Wind;
        public PlaceableDefinition Battery;
        public readonly Dictionary<char, GameObject> BuildingPrefabs = new Dictionary<char, GameObject>();
        public GameObject TreePrefab;
        public Material GrassA;
        public Material GrassB;
        public Material Hill;
        public Material HillSide;
        public Material Water;
        public Material GhostValid;
        public Material GhostInvalid;
    }

    public static class ContentBuilder
    {
        private const string Root = "Assets/GridKeeper";
        private const string MatFolder = Root + "/Art/Materials";
        private const string DataFolder = Root + "/Data";
        private const string SourcePrefabs = Root + "/Prefabs/Sources";
        private const string BuildingPrefabs = Root + "/Prefabs/Buildings";

        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        [MenuItem("Grid Keeper/Setup/Build Content")]
        public static void BuildFromMenu()
        {
            BuildAll();
            Debug.Log("[Grid Keeper] Content built.");
        }

        public static ContentSet BuildAll()
        {
            foreach (string f in new[] { MatFolder, DataFolder, SourcePrefabs, BuildingPrefabs }) Directory.CreateDirectory(f);
            AssetDatabase.Refresh();
            Materials.Clear();

            var set = new ContentSet
            {
                Balance = GetOrCreateAsset<BalanceConfig>(DataFolder + "/BalanceConfig.asset"),
                Layout = GetOrCreateAsset<CityLayout>(DataFolder + "/BrightwaterLayout.asset"),
                GrassA = Lit("GrassA", "#BAD7B2"),
                GrassB = Lit("GrassB", "#B2D0A9"),
                Hill = Lit("HillTop", "#9CC493"),
                HillSide = Lit("HillSide", "#86B07E"),
                Water = Lit("Bay", "#8EC6DC", 0.6f),
                GhostValid = Ghost("GhostValid", new Color(1f, 1f, 1f, 0.55f)),
                GhostInvalid = Ghost("GhostInvalid", new Color(0.84f, 0.27f, 0.23f, 0.6f))
            };

            set.Solar = Definition("Solar", SourceKind.Solar, "Solar panel", 150, BuildSolar(), SceneBuilder.SolarAmber, 8f, 0f, 0f);
            set.Wind = Definition("Wind", SourceKind.Wind, "Wind turbine", 200, BuildTurbine(), SceneBuilder.WindTeal, 6f, 0f, 0f);
            set.Battery = Definition("Battery", SourceKind.Battery, "Battery", 120, BuildBattery(), SceneBuilder.BatteryViolet, 0f, 30f, 8f);

            set.BuildingPrefabs[CityLayout.House] = BuildHouse(BuildingDef("House", BuildingKind.House, 1.6f, HouseCurve()));
            set.BuildingPrefabs[CityLayout.Shop] = BuildShop(BuildingDef("Shop", BuildingKind.Shop, 2.4f, ShopCurve()));
            set.BuildingPrefabs[CityLayout.Factory] = BuildFactory(BuildingDef("Factory", BuildingKind.Factory, 6f, FactoryCurve()));
            set.BuildingPrefabs[CityLayout.Hospital] = BuildHospital(BuildingDef("Hospital", BuildingKind.Hospital, 3f, AnimationCurve.Constant(0f, 24f, 1f)));
            set.TreePrefab = BuildTree();

            AssetDatabase.SaveAssets();
            return set;
        }

        private static GameObject BuildSolar()
        {
            var root = new GameObject("SolarPanel");
            Part(root.transform, "Post", LowPolyMeshes.Cylinder8, Lit("Steel", "#8F98A3"), new Vector3(0f, 0f, 0f), new Vector3(0.004f, 0.012f, 0.004f));
            var tilt = new GameObject("Tilt").transform;
            tilt.SetParent(root.transform, false);
            tilt.localPosition = new Vector3(0f, 0.012f, 0f);
            tilt.localRotation = Quaternion.Euler(-28f, 0f, 0f);
            Part(tilt, "Frame", LowPolyMeshes.Box, Lit("SolarFrame", "#F2A516"), new Vector3(0f, -0.0012f, 0f), new Vector3(0.042f, 0.0016f, 0.032f));
            Part(tilt, "Cells", LowPolyMeshes.Box, Lit("SolarCells", "#2D3E6E", 0.75f), Vector3.zero, new Vector3(0.038f, 0.0012f, 0.028f));
            Part(tilt, "Seam", LowPolyMeshes.Box, Lit("SolarSeam", "#5C6E9E"), new Vector3(0f, 0.0011f, 0f), new Vector3(0.0012f, 0.0003f, 0.028f));
            root.AddComponent<SolarPanel>();
            return SavePrefab(root, SourcePrefabs + "/SolarPanel.prefab");
        }

        private static GameObject BuildTurbine()
        {
            var root = new GameObject("WindTurbine");
            Material white = Lit("TurbineWhite", "#F3F5F6");
            Material teal = Lit("TurbineTeal", "#10A0A3");
            Part(root.transform, "Base", LowPolyMeshes.Cylinder8, teal, Vector3.zero, new Vector3(0.012f, 0.003f, 0.012f));
            Part(root.transform, "Tower", LowPolyMeshes.Cylinder8, white, Vector3.zero, new Vector3(0.0045f, 0.062f, 0.0045f));
            Part(root.transform, "Nacelle", LowPolyMeshes.Box, teal, new Vector3(0f, 0.06f, 0.001f), new Vector3(0.006f, 0.0055f, 0.012f));

            var rotor = new GameObject("Rotor").transform;
            rotor.SetParent(root.transform, false);
            rotor.localPosition = new Vector3(0f, 0.0628f, -0.0055f);
            Part(rotor, "Hub", LowPolyMeshes.Cone6, white, Vector3.zero, new Vector3(0.005f, 0.005f, 0.005f), new Vector3(-90f, 0f, 0f));
            for (int i = 0; i < 3; i++)
            {
                var arm = new GameObject("Blade " + (i + 1)).transform;
                arm.SetParent(rotor, false);
                arm.localRotation = Quaternion.Euler(0f, 0f, i * 120f);
                Part(arm, "Blade", LowPolyMeshes.Box, white, new Vector3(0f, 0.002f, 0f), new Vector3(0.0032f, 0.03f, 0.0008f));
                Part(arm, "Tip", LowPolyMeshes.Box, teal, new Vector3(0f, 0.027f, -0.0001f), new Vector3(0.0034f, 0.005f, 0.001f));
            }

            var turbine = root.AddComponent<WindTurbine>();
            SetField(turbine, "rotor", rotor);
            return SavePrefab(root, SourcePrefabs + "/WindTurbine.prefab");
        }

        private static GameObject BuildBattery()
        {
            var root = new GameObject("Battery");
            Material violet = Lit("BatteryViolet", "#7B5CE6");
            Material cap = Lit("BatteryCap", "#A58FF0");
            Material slot = Lit("BatterySlot", "#2A2340");
            Material level = Unlit("BatteryLevel", new Color(0.82f, 0.76f, 1f));
            Part(root.transform, "Body", LowPolyMeshes.Box, violet, Vector3.zero, new Vector3(0.034f, 0.022f, 0.026f));
            Part(root.transform, "Cap", LowPolyMeshes.Box, cap, new Vector3(0f, 0.022f, 0f), new Vector3(0.036f, 0.003f, 0.028f));
            Part(root.transform, "Terminal +", LowPolyMeshes.Cylinder8, Lit("Steel", "#8F98A3"), new Vector3(-0.009f, 0.025f, 0f), new Vector3(0.004f, 0.003f, 0.004f));
            Part(root.transform, "Terminal -", LowPolyMeshes.Cylinder8, Lit("Steel", "#8F98A3"), new Vector3(0.009f, 0.025f, 0f), new Vector3(0.004f, 0.003f, 0.004f));
            Part(root.transform, "Slot", LowPolyMeshes.Box, slot, new Vector3(0f, 0.004f, -0.0131f), new Vector3(0.008f, 0.014f, 0.0008f));
            var pivot = new GameObject("Charge Pivot").transform;
            pivot.SetParent(root.transform, false);
            pivot.localPosition = new Vector3(0f, 0.005f, -0.0136f);
            Part(pivot, "Level", LowPolyMeshes.Box, level, Vector3.zero, new Vector3(0.006f, 0.012f, 0.0006f));
            pivot.localScale = new Vector3(1f, 0.001f, 1f);

            var battery = root.AddComponent<GridKeeper.Energy.Battery>();
            SetField(battery, "chargeBar", pivot);
            return SavePrefab(root, SourcePrefabs + "/Battery.prefab");
        }

        private static GameObject BuildHouse(BuildingDefinition def)
        {
            var root = new GameObject("House");
            Part(root.transform, "Walls", LowPolyMeshes.Box, Lit("HouseWalls", "#F4EDE1"), Vector3.zero, new Vector3(0.03f, 0.02f, 0.028f));
            Part(root.transform, "Roof", LowPolyMeshes.Prism, Lit("RoofTerracotta", "#C97B5A"), new Vector3(0f, 0.02f, 0f), new Vector3(0.034f, 0.014f, 0.032f), new Vector3(0f, 90f, 0f));
            Part(root.transform, "Door", LowPolyMeshes.Box, Lit("Door", "#7A5A48"), new Vector3(0.007f, 0f, -0.0142f), new Vector3(0.006f, 0.011f, 0.0006f));
            var windows = new List<Renderer>
            {
                Window(root.transform, new Vector3(-0.007f, 0.009f, -0.0142f), 0.007f, 0.006f),
                Window(root.transform, new Vector3(0.0151f, 0.009f, 0.002f), 0.006f, 0.006f, 90f),
                Window(root.transform, new Vector3(-0.0151f, 0.009f, 0.002f), 0.006f, 0.006f, 90f)
            };
            return FinishBuilding(root, def, windows);
        }

        private static GameObject BuildShop(BuildingDefinition def)
        {
            var root = new GameObject("Shop");
            Part(root.transform, "Walls", LowPolyMeshes.Box, Lit("ShopWalls", "#DCE7F0"), Vector3.zero, new Vector3(0.04f, 0.02f, 0.03f));
            Part(root.transform, "Roof", LowPolyMeshes.Box, Lit("ShopRoof", "#9FB4C6"), new Vector3(0f, 0.02f, 0f), new Vector3(0.042f, 0.003f, 0.032f));
            Part(root.transform, "Awning", LowPolyMeshes.Prism, Lit("Awning", "#E9967A"), new Vector3(0f, 0.013f, -0.017f), new Vector3(0.038f, 0.004f, 0.006f), new Vector3(0f, 0f, 0f));
            Part(root.transform, "Sign", LowPolyMeshes.Box, Lit("ShopSign", "#3A4A5C"), new Vector3(0f, 0.016f, -0.0152f), new Vector3(0.02f, 0.003f, 0.0006f));
            var windows = new List<Renderer>
            {
                Window(root.transform, new Vector3(-0.009f, 0.003f, -0.0152f), 0.014f, 0.009f),
                Window(root.transform, new Vector3(0.011f, 0.003f, -0.0152f), 0.01f, 0.009f)
            };
            return FinishBuilding(root, def, windows);
        }

        private static GameObject BuildFactory(BuildingDefinition def)
        {
            var root = new GameObject("Factory");
            Material walls = Lit("FactoryWalls", "#C9C9C2");
            Material roof = Lit("FactoryRoof", "#8E9893");
            Part(root.transform, "Hall", LowPolyMeshes.Box, walls, Vector3.zero, new Vector3(0.042f, 0.018f, 0.04f));
            for (int i = 0; i < 3; i++)
            {
                float x = -0.014f + i * 0.014f;
                Part(root.transform, "Sawtooth " + (i + 1), LowPolyMeshes.Prism, roof, new Vector3(x, 0.018f, 0f), new Vector3(0.014f, 0.008f, 0.04f));
            }
            Part(root.transform, "Chimney", LowPolyMeshes.Cylinder8, Lit("Brick", "#A0644C"), new Vector3(0.014f, 0f, 0.012f), new Vector3(0.007f, 0.042f, 0.007f));
            Part(root.transform, "Chimney Band", LowPolyMeshes.Cylinder8, Lit("TurbineWhite", "#F3F5F6"), new Vector3(0.014f, 0.036f, 0.012f), new Vector3(0.0075f, 0.003f, 0.0075f));
            Part(root.transform, "Gate", LowPolyMeshes.Box, Lit("Gate", "#5E666B"), new Vector3(-0.01f, 0f, -0.0202f), new Vector3(0.012f, 0.011f, 0.0006f));
            var windows = new List<Renderer>
            {
                Window(root.transform, new Vector3(0.004f, 0.009f, -0.0202f), 0.006f, 0.005f),
                Window(root.transform, new Vector3(0.013f, 0.009f, -0.0202f), 0.006f, 0.005f),
                Window(root.transform, new Vector3(0.0212f, 0.008f, -0.006f), 0.006f, 0.005f, 90f),
                Window(root.transform, new Vector3(0.0212f, 0.008f, 0.006f), 0.006f, 0.005f, 90f)
            };
            return FinishBuilding(root, def, windows);
        }

        private static GameObject BuildHospital(BuildingDefinition def)
        {
            var root = new GameObject("Hospital");
            Material white = Lit("HospitalWalls", "#F7F7F5");
            Material cross = Lit("HospitalCross", "#3FA672");
            Part(root.transform, "Block", LowPolyMeshes.Box, white, Vector3.zero, new Vector3(0.044f, 0.026f, 0.04f));
            Part(root.transform, "Wing", LowPolyMeshes.Box, white, new Vector3(0f, 0.026f, 0.004f), new Vector3(0.028f, 0.008f, 0.026f));
            Part(root.transform, "Roof Trim", LowPolyMeshes.Box, Lit("ShopRoof", "#9FB4C6"), new Vector3(0f, 0.026f, 0f), new Vector3(0.046f, 0.0015f, 0.042f));
            Part(root.transform, "Cross V", LowPolyMeshes.Box, cross, new Vector3(0f, 0.034f, 0.004f), new Vector3(0.004f, 0.001f, 0.014f));
            Part(root.transform, "Cross H", LowPolyMeshes.Box, cross, new Vector3(0f, 0.034f, 0.004f), new Vector3(0.014f, 0.001f, 0.004f));
            Part(root.transform, "Entrance", LowPolyMeshes.Box, Lit("Door", "#7A5A48"), new Vector3(0f, 0f, -0.0202f), new Vector3(0.01f, 0.01f, 0.0006f));
            var windows = new List<Renderer>();
            for (int row = 0; row < 2; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    if (row == 0 && (col == 1 || col == 2)) continue;
                    float x = -0.0165f + col * 0.011f;
                    windows.Add(Window(root.transform, new Vector3(x, 0.006f + row * 0.01f, -0.0202f), 0.006f, 0.005f));
                }
            }
            return FinishBuilding(root, def, windows);
        }

        private static GameObject BuildTree()
        {
            var root = new GameObject("Trees");
            Material trunk = Lit("Trunk", "#8A6A50");
            Material leaves = Lit("Leaves", "#6FA86A");
            Material leavesDark = Lit("LeavesDark", "#5B9258");
            Vector3[] spots = { new Vector3(-0.01f, 0f, -0.008f), new Vector3(0.011f, 0f, 0.006f), new Vector3(-0.006f, 0f, 0.013f) };
            float[] sizes = { 1f, 0.8f, 0.65f };
            for (int i = 0; i < spots.Length; i++)
            {
                float s = sizes[i];
                Part(root.transform, "Trunk " + (i + 1), LowPolyMeshes.Cylinder8, trunk, spots[i], new Vector3(0.003f, 0.008f, 0.003f) * s);
                Part(root.transform, "Crown " + (i + 1), LowPolyMeshes.Cone6, i % 2 == 0 ? leaves : leavesDark, spots[i] + new Vector3(0f, 0.006f * s, 0f), new Vector3(0.016f, 0.026f, 0.016f) * s);
            }
            return SavePrefab(root, BuildingPrefabs + "/Trees.prefab");
        }

        private static Renderer Window(Transform parent, Vector3 position, float width, float height, float yaw = 0f)
        {
            GameObject window = Part(parent, "Window", LowPolyMeshes.Box, WindowLit, position, new Vector3(width, height, 0.0006f), new Vector3(0f, yaw, 0f));
            return window.GetComponent<Renderer>();
        }

        public static Material WindowLit => Emissive("WindowLit", "#FFD27A", 1.6f);
        public static Material WindowDark => Lit("WindowDark", "#3A4048", 0.4f);

        private static GameObject FinishBuilding(GameObject root, BuildingDefinition def, List<Renderer> windows)
        {
            var building = root.AddComponent<GridKeeper.World.Building>();
            building.Setup(def, windows.ToArray(), WindowLit, WindowDark);
            return SavePrefab(root, BuildingPrefabs + "/" + root.name + ".prefab");
        }

        public static GameObject Part(Transform parent, string name, Mesh mesh, Material material, Vector3 position, Vector3 size, Vector3 euler = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = size;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return go;
        }

        private static PlaceableDefinition Definition(string file, SourceKind kind, string name, int cost, GameObject prefab, Color accent, float rated, float capacity, float rate)
        {
            var def = GetOrCreateAsset<PlaceableDefinition>(DataFolder + "/" + file + ".asset");
            def.Configure(kind, name, cost, prefab, accent, rated, capacity, rate);
            EditorUtility.SetDirty(def);
            return def;
        }

        private static BuildingDefinition BuildingDef(string file, BuildingKind kind, float peak, AnimationCurve curve)
        {
            var def = GetOrCreateAsset<BuildingDefinition>(DataFolder + "/" + file + ".asset");
            def.Configure(kind, file, peak, curve);
            EditorUtility.SetDirty(def);
            return def;
        }

        private static AnimationCurve HouseCurve()
        {
            return Curve((0f, 0.35f), (5f, 0.3f), (7f, 0.6f), (9f, 0.35f), (13f, 0.3f), (17f, 0.55f), (19f, 1f), (21.5f, 0.95f), (23f, 0.5f), (24f, 0.35f));
        }

        private static AnimationCurve ShopCurve()
        {
            return Curve((0f, 0.08f), (7f, 0.1f), (8.5f, 0.85f), (13f, 1f), (19f, 0.8f), (20.5f, 0.1f), (24f, 0.08f));
        }

        private static AnimationCurve FactoryCurve()
        {
            return Curve((0f, 0.15f), (6f, 0.15f), (7f, 1f), (17f, 1f), (18f, 0.15f), (24f, 0.15f));
        }

        private static AnimationCurve Curve(params (float time, float value)[] keys)
        {
            var curve = new AnimationCurve();
            foreach (var (time, value) in keys) curve.AddKey(time, value);
            for (int i = 0; i < curve.length; i++) AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            for (int i = 0; i < curve.length; i++) AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            return curve;
        }

        public static Material Lit(string name, string hex, float smoothness = 0.15f)
        {
            string key = "Lit/" + name;
            if (Materials.TryGetValue(key, out Material cached)) return cached;
            string path = MatFolder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            ColorUtility.TryParseHtmlString(hex, out Color color);
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", 0f);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            Materials[key] = mat;
            return mat;
        }

        private static Material Emissive(string name, string hex, float intensity)
        {
            string key = "Emissive/" + name;
            if (Materials.TryGetValue(key, out Material cached)) return cached;
            Material mat = Lit(name, hex, 0.5f);
            ColorUtility.TryParseHtmlString(hex, out Color color);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * intensity);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(mat);
            Materials[key] = mat;
            return mat;
        }

        private static Material Unlit(string name, Color color)
        {
            string key = "Unlit/" + name;
            if (Materials.TryGetValue(key, out Material cached)) return cached;
            string path = MatFolder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            Materials[key] = mat;
            return mat;
        }

        private static Material Ghost(string name, Color color)
        {
            Material mat = Unlit(name, color);
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
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = ShadowCastingMode.On;
                r.receiveShadows = true;
            }
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void SetField(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
