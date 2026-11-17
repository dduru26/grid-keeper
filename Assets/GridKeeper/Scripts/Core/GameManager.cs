using GridKeeper.AR;
using GridKeeper.Data;
using GridKeeper.World;
using UnityEngine;

namespace GridKeeper.Core
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private BalanceConfig balance;
        [SerializeField] private City city;
        [SerializeField] private ARPlacementController placement;

        public BalanceConfig Balance => balance;
        public City City => city;
        public Wallet Wallet { get; private set; }

        private void Awake()
        {
            Wallet = new Wallet(balance != null ? balance.startingBudget : 900);
            if (city == null) city = FindAnyObjectByType<City>(FindObjectsInactive.Include);
            if (placement == null) placement = FindAnyObjectByType<ARPlacementController>();
        }
    }
}
