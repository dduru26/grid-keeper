using UnityEngine;

namespace GridKeeper.AR
{
    public class PlacementReticle : MonoBehaviour
    {
        [SerializeField, Tooltip("Pulses per second while the reticle is visible.")]
        private float pulseSpeed = 2.5f;

        [SerializeField, Range(0f, 0.2f), Tooltip("How much the reticle grows and shrinks while pulsing.")]
        private float pulseAmount = 0.04f;

        private Vector3 baseScale = Vector3.one;
        private bool initialised;

        private void Awake()
        {
            Initialise();
        }

        private void Initialise()
        {
            if (initialised) return;
            baseScale = transform.localScale;
            initialised = true;
        }

        public void Show(bool visible, Pose pose)
        {
            Initialise();

            if (visible)
            {
                transform.SetPositionAndRotation(pose.position, pose.rotation);
            }

            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }
        }

        private void Update()
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) * pulseAmount;
            transform.localScale = baseScale * pulse;
        }
    }
}
