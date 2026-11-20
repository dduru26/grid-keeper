using GridKeeper.Data;
using UnityEngine;

namespace GridKeeper.World
{
    public class Building : MonoBehaviour
    {
        [SerializeField] private BuildingDefinition definition;
        [SerializeField] private Renderer[] windows = new Renderer[0];
        [SerializeField] private Material litWindow;
        [SerializeField] private Material darkWindow;

        public BuildingDefinition Definition => definition;
        public bool IsLit { get; private set; } = true;

        public void Setup(BuildingDefinition newDefinition, Renderer[] newWindows, Material lit, Material dark)
        {
            definition = newDefinition;
            windows = newWindows;
            litWindow = lit;
            darkWindow = dark;
        }

        public float DemandAt(float hour)
        {
            return definition == null ? 0f : definition.DemandAt(hour);
        }

        public void UseLitMaterial(Material material)
        {
            litWindow = material;
            if (!IsLit) return;
            foreach (Renderer window in windows)
            {
                if (window != null) window.sharedMaterial = material;
            }
        }

        public void SetLit(bool lit)
        {
            if (IsLit == lit) return;
            IsLit = lit;
            Material material = lit ? litWindow : darkWindow;
            if (material == null) return;
            foreach (Renderer window in windows)
            {
                if (window != null) window.sharedMaterial = material;
            }
        }
    }
}
