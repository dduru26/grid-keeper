using UnityEngine;

namespace GridKeeper
{
    public static class Require
    {
        public static bool Reference(Object owner, Object value, string field)
        {
            if (value != null) return true;
            Debug.LogError("[Grid Keeper] " + owner.GetType().Name + " on " + owner.name + " is missing " + field + ". Assign it in the Inspector or run Grid Keeper > Setup > Build Main Scene.", owner);
            return false;
        }
    }
}
