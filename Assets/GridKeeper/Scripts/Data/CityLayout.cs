using UnityEngine;

namespace GridKeeper.Data
{
    [CreateAssetMenu(menuName = "Grid Keeper/City Layout", fileName = "CityLayout")]
    public class CityLayout : ScriptableObject
    {
        public const char Grass = '.';
        public const char Hill = 'h';
        public const char Water = 'w';
        public const char Tree = 'T';
        public const char House = 'H';
        public const char Shop = 'S';
        public const char Factory = 'F';
        public const char Hospital = 'P';

        [SerializeField] private string townName = "Brightwater";
        [SerializeField, Min(0.01f)] private float tileSize = 0.05f;
        [SerializeField, Tooltip("First row is the back of the board, last row is the front.")]
        private string[] rows =
        {
            "hhh..hhh",
            "h.T..T.h",
            ".H..H...",
            "...S..F.",
            ".H..P.H.",
            "..S.....",
            ".H..T.H.",
            "wwwwwwww"
        };

        public string TownName => townName;
        public float TileSize => tileSize;
        public int Depth => rows.Length;
        public int Width => rows.Length == 0 ? 0 : rows[0].Length;

        public char At(int x, int z)
        {
            if (z < 0 || z >= rows.Length) return Water;
            string row = rows[z];
            return x < 0 || x >= row.Length ? Water : row[x];
        }
    }
}
