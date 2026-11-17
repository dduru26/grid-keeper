using System.Collections.Generic;
using GridKeeper.Data;
using GridKeeper.Energy;
using GridKeeper.World;
using UnityEngine;

namespace GridKeeper.Construction
{
    public class GhostPreview : MonoBehaviour
    {
        [SerializeField] private Material validMaterial;
        [SerializeField] private Material invalidMaterial;
        [SerializeField] private float bobHeight = 0.004f;
        [SerializeField] private float bobSpeed = 3f;

        private readonly Dictionary<PlaceableDefinition, GameObject> cache = new Dictionary<PlaceableDefinition, GameObject>();
        private GameObject current;
        private Renderer[] currentRenderers;
        private Vector3 baseLocal;

        public void Show(PlaceableDefinition definition, Tile tile, bool valid)
        {
            Hide();
            if (definition == null || tile == null) return;

            current = GetGhost(definition);
            current.transform.SetParent(tile.transform, false);
            baseLocal = tile.SurfaceLocal;
            current.transform.localPosition = baseLocal;
            current.transform.localRotation = Quaternion.identity;
            currentRenderers = current.GetComponentsInChildren<Renderer>(true);
            Tint(valid);
            current.SetActive(true);
        }

        public void ShowRemoval(Tile tile)
        {
            Hide();
            if (tile == null || tile.Occupant == null) return;

            current = GetGhost(tile.Occupant.Definition);
            current.transform.SetParent(tile.transform, false);
            baseLocal = tile.SurfaceLocal + Vector3.up * 0.002f;
            current.transform.localPosition = baseLocal;
            current.transform.localRotation = Quaternion.identity;
            current.transform.localScale = Vector3.one * 1.08f;
            currentRenderers = current.GetComponentsInChildren<Renderer>(true);
            Tint(false);
            current.SetActive(true);
        }

        public void Hide()
        {
            if (current == null) return;
            current.SetActive(false);
            current.transform.SetParent(transform, false);
            current.transform.localScale = Vector3.one;
            current = null;
        }

        private void Update()
        {
            if (current == null) return;
            float bob = (Mathf.Sin(Time.time * bobSpeed) * 0.5f + 0.5f) * bobHeight;
            current.transform.localPosition = baseLocal + Vector3.up * bob;
        }

        private void Tint(bool valid)
        {
            Material material = valid ? validMaterial : invalidMaterial;
            foreach (Renderer r in currentRenderers)
            {
                var shared = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < shared.Length; i++) shared[i] = material;
                r.sharedMaterials = shared;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private GameObject GetGhost(PlaceableDefinition definition)
        {
            if (cache.TryGetValue(definition, out GameObject ghost) && ghost != null) return ghost;

            ghost = Instantiate(definition.Prefab, transform);
            ghost.name = definition.DisplayName + " Ghost";
            foreach (Placeable placeable in ghost.GetComponentsInChildren<Placeable>(true))
            {
                if (placeable is WindTurbine turbine) turbine.SetWind(0.6f);
                else placeable.enabled = false;
            }
            foreach (Collider c in ghost.GetComponentsInChildren<Collider>(true)) Destroy(c);
            ghost.SetActive(false);
            cache[definition] = ghost;
            return ghost;
        }
    }
}
