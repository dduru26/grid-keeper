using System.Runtime.InteropServices;

namespace GridKeeper.Interaction
{
    public static class Haptics
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void GK_HapticTick();
        [DllImport("__Internal")] private static extern void GK_HapticSelection();
        [DllImport("__Internal")] private static extern void GK_HapticError();
        [DllImport("__Internal")] private static extern void GK_HapticSuccess();

        public static void Tick() => GK_HapticTick();
        public static void Selection() => GK_HapticSelection();
        public static void Error() => GK_HapticError();
        public static void Success() => GK_HapticSuccess();
#else
        public static void Tick() { }
        public static void Selection() { }
        public static void Error() { }
        public static void Success() { }
#endif
    }
}
