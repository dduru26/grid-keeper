using GridKeeper.Data;
using GridKeeper.World;
using UnityEngine;

namespace GridKeeper.Energy
{
    public abstract class Placeable : MonoBehaviour
    {
        public PlaceableDefinition Definition { get; private set; }
        public Tile Tile { get; private set; }
        protected BalanceConfig Balance { get; private set; }

        public void Initialise(PlaceableDefinition definition, Tile tile, BalanceConfig balance)
        {
            Definition = definition;
            Tile = tile;
            Balance = balance;
            OnPlaced();
        }

        protected virtual void OnPlaced()
        {
        }
    }
}
