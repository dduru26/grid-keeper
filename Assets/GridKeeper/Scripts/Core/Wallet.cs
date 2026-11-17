using System;

namespace GridKeeper.Core
{
    public sealed class Wallet
    {
        public event Action<float> Changed;

        public float Balance { get; private set; }

        public Wallet(float startingBalance)
        {
            Balance = startingBalance;
        }

        public bool CanAfford(int amount)
        {
            return Balance >= amount;
        }

        public bool TrySpend(int amount)
        {
            if (!CanAfford(amount)) return false;
            Balance -= amount;
            Changed?.Invoke(Balance);
            return true;
        }

        public void Add(float amount)
        {
            if (amount <= 0f) return;
            Balance += amount;
            Changed?.Invoke(Balance);
        }

        public void Reset(float balance)
        {
            Balance = balance;
            Changed?.Invoke(Balance);
        }
    }
}
