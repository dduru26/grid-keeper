using System;
using System.Collections.Generic;
using GridKeeper.Data;
using GridKeeper.Energy;
using UnityEngine;

namespace GridKeeper.World
{
    public class City : MonoBehaviour
    {
        private static readonly Vector2Int[] Directions =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        [SerializeField] private CityLayout layout;

        public event Action<Placeable> PlaceableAdded;
        public event Action<Placeable> PlaceableRemoved;

        public CityLayout Layout => layout;
        public IReadOnlyList<Building> Buildings => buildings;
        public IReadOnlyList<Placeable> Placeables => placeables;
        public IReadOnlyList<IGenerator> Generators => generators;
        public IReadOnlyList<IStorage> Storage => storage;

        private readonly List<Building> buildings = new List<Building>();
        private readonly List<Placeable> placeables = new List<Placeable>();
        private readonly List<IGenerator> generators = new List<IGenerator>();
        private readonly List<IStorage> storage = new List<IStorage>();
        private readonly List<Building> sorted = new List<Building>();
        private readonly Dictionary<Vector2Int, Tile> tiles = new Dictionary<Vector2Int, Tile>();

        private void Awake()
        {
            Index();
        }

        public void Index()
        {
            tiles.Clear();
            foreach (Tile tile in GetComponentsInChildren<Tile>(true))
            {
                tile.Bind(this);
                tiles[tile.Coords] = tile;
            }

            buildings.Clear();
            buildings.AddRange(GetComponentsInChildren<Building>(true));
        }

        public Tile GetTile(Vector2Int coords)
        {
            return tiles.TryGetValue(coords, out Tile tile) ? tile : null;
        }

        public IEnumerable<Tile> Neighbours(Tile tile)
        {
            foreach (Vector2Int direction in Directions)
            {
                Tile neighbour = GetTile(tile.Coords + direction);
                if (neighbour != null) yield return neighbour;
            }
        }

        public float DemandAt(float hour, float multiplier)
        {
            float total = 0f;
            foreach (Building building in buildings)
            {
                total += building.DemandAt(hour);
            }
            return total * multiplier;
        }

        public int ApplySupply(float suppliedMW, float hour, float multiplier)
        {
            sorted.Clear();
            sorted.AddRange(buildings);
            sorted.Sort((a, b) => Priority(a).CompareTo(Priority(b)));

            float remaining = suppliedMW + 0.001f;
            int dark = 0;
            foreach (Building building in sorted)
            {
                float need = building.DemandAt(hour) * multiplier;
                bool lit = need <= remaining;
                if (lit) remaining -= need;
                else dark++;
                building.SetLit(lit);
            }
            return dark;
        }

        public void LightAll()
        {
            foreach (Building building in buildings) building.SetLit(true);
        }

        private static int Priority(Building building)
        {
            if (building.Definition == null) return 9;
            switch (building.Definition.Kind)
            {
                case BuildingKind.Hospital: return 0;
                case BuildingKind.House: return 1;
                case BuildingKind.Shop: return 2;
                default: return 3;
            }
        }

        public void Register(Placeable placeable)
        {
            if (placeables.Contains(placeable)) return;
            placeables.Add(placeable);
            if (placeable is IGenerator generator) generators.Add(generator);
            if (placeable is IStorage store) storage.Add(store);
            PlaceableAdded?.Invoke(placeable);
        }

        public void Unregister(Placeable placeable)
        {
            if (!placeables.Remove(placeable)) return;
            if (placeable is IGenerator generator) generators.Remove(generator);
            if (placeable is IStorage store) storage.Remove(store);
            PlaceableRemoved?.Invoke(placeable);
        }

        public int Count(SourceKind kind)
        {
            int count = 0;
            foreach (Placeable placeable in placeables)
            {
                if (placeable.Definition != null && placeable.Definition.Kind == kind) count++;
            }
            return count;
        }
    }
}
