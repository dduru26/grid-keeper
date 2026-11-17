using GridKeeper.Data;
using GridKeeper.Energy;
using GridKeeper.World;
using UnityEngine;

namespace GridKeeper.Construction
{
    public class SourceFactory : MonoBehaviour
    {
        [SerializeField] private BalanceConfig balance;

        public Placeable Create(PlaceableDefinition definition, Tile tile)
        {
            if (definition == null || definition.Prefab == null || tile == null || !tile.IsBuildable) return null;

            GameObject instance = Instantiate(definition.Prefab, tile.transform);
            instance.name = definition.DisplayName;
            instance.transform.localPosition = tile.SurfaceLocal;
            instance.transform.localRotation = Quaternion.identity;

            var placeable = instance.GetComponent<Placeable>();
            if (placeable == null)
            {
                Debug.LogError("[Grid Keeper] Prefab " + definition.Prefab.name + " has no Placeable component.");
                Destroy(instance);
                return null;
            }

            placeable.Initialise(definition, tile, balance);
            tile.SetOccupant(placeable);
            if (tile.City != null) tile.City.Register(placeable);
            return placeable;
        }

        public void Remove(Tile tile)
        {
            if (tile == null || tile.Occupant == null) return;
            Placeable placeable = tile.Occupant;
            tile.SetOccupant(null);
            if (tile.City != null) tile.City.Unregister(placeable);
            Destroy(placeable.gameObject);
        }
    }
}
