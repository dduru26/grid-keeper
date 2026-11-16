using GridKeeper.Data;
using GridKeeper.Energy;
using UnityEngine;

namespace GridKeeper.World
{
    [RequireComponent(typeof(BoxCollider))]
    public class Tile : MonoBehaviour
    {
        [SerializeField] private Vector2Int coords;
        [SerializeField] private TileKind kind;
        [SerializeField, Tooltip("Local height of the top surface where sources sit.")]
        private float surfaceHeight = 0.01f;

        public Vector2Int Coords => coords;
        public TileKind Kind => kind;
        public bool IsHill => kind == TileKind.Hill;
        public Placeable Occupant { get; private set; }
        public City City { get; private set; }
        public bool IsBuildable => (kind == TileKind.Grass || kind == TileKind.Hill) && Occupant == null;
        public Vector3 SurfaceLocal => new Vector3(0f, surfaceHeight, 0f);

        public void Setup(Vector2Int newCoords, TileKind newKind, float newSurfaceHeight)
        {
            coords = newCoords;
            kind = newKind;
            surfaceHeight = newSurfaceHeight;
        }

        public void Bind(City owner)
        {
            City = owner;
        }

        public void SetOccupant(Placeable placeable)
        {
            Occupant = placeable;
        }
    }
}
