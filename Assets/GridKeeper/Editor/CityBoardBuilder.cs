using GridKeeper.Data;
using GridKeeper.World;
using UnityEditor;
using UnityEngine;

namespace GridKeeper.EditorTools
{
    public static class CityBoardBuilder
    {
        private const float GrassHeight = 0.008f;
        private const float HillHeight = 0.022f;
        private const float WaterHeight = 0.004f;

        public static City Build(ContentSet content)
        {
            CityLayout layout = content.Layout;
            float size = layout.TileSize;
            int width = layout.Width;
            int depth = layout.Depth;

            var root = new GameObject(layout.TownName);
            var city = root.AddComponent<City>();
            var so = new SerializedObject(city);
            so.FindProperty("layout").objectReferenceValue = layout;
            so.ApplyModifiedPropertiesWithoutUndo();

            var board = new GameObject("Board").transform;
            board.SetParent(root.transform, false);

            Material soil = ContentBuilder.Lit("Soil", "#9C8A72");
            ContentBuilder.Part(board, "Plinth", LowPolyMeshes.Box, soil, new Vector3(0f, -0.006f, 0f), new Vector3(width * size + 0.008f, 0.006f, depth * size + 0.008f));

            for (int z = 0; z < depth; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    char cell = layout.At(x, z);
                    Vector3 position = new Vector3((x - (width - 1) * 0.5f) * size, 0f, ((depth - 1) * 0.5f - z) * size);
                    CreateTile(board, content, cell, new Vector2Int(x, z), position, size);
                }
            }

            return city;
        }

        private static void CreateTile(Transform board, ContentSet content, char cell, Vector2Int coords, Vector3 position, float size)
        {
            var go = new GameObject("Tile " + coords.x + "," + coords.y);
            go.transform.SetParent(board, false);
            go.transform.localPosition = position;

            TileKind kind;
            float surface;
            switch (cell)
            {
                case CityLayout.Hill:
                    kind = TileKind.Hill;
                    surface = HillHeight;
                    ContentBuilder.Part(go.transform, "Hill", LowPolyMeshes.Box, content.HillSide, Vector3.zero, new Vector3(size, HillHeight - 0.002f, size));
                    ContentBuilder.Part(go.transform, "Hill Top", LowPolyMeshes.Box, content.Hill, new Vector3(0f, HillHeight - 0.002f, 0f), new Vector3(size * 0.96f, 0.002f, size * 0.96f));
                    break;
                case CityLayout.Water:
                    kind = TileKind.Water;
                    surface = WaterHeight;
                    ContentBuilder.Part(go.transform, "Water", LowPolyMeshes.Box, content.Water, Vector3.zero, new Vector3(size, WaterHeight, size));
                    break;
                default:
                    kind = cell == CityLayout.Grass ? TileKind.Grass : TileKind.Built;
                    surface = GrassHeight;
                    Material grass = (coords.x + coords.y) % 2 == 0 ? content.GrassA : content.GrassB;
                    ContentBuilder.Part(go.transform, "Grass", LowPolyMeshes.Box, grass, Vector3.zero, new Vector3(size, GrassHeight, size));
                    break;
            }

            var tile = go.AddComponent<Tile>();
            tile.Setup(coords, kind, surface);
            var collider = go.GetComponent<BoxCollider>();
            collider.center = new Vector3(0f, surface * 0.5f, 0f);
            collider.size = new Vector3(size, surface, size);

            GameObject prefab = null;
            if (cell == CityLayout.Tree) prefab = content.TreePrefab;
            else content.BuildingPrefabs.TryGetValue(cell, out prefab);
            if (prefab == null) return;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, go.transform);
            instance.transform.localPosition = new Vector3(0f, surface, 0f);
        }
    }
}
