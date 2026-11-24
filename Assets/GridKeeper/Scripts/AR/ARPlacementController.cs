using System;
using System.Collections.Generic;
using GridKeeper.Interaction;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace GridKeeper.AR
{
    public enum PlacementState
    {
        Scanning,
        ReadyToPlace,
        Placed
    }

    [RequireComponent(typeof(ARRaycastManager))]
    [RequireComponent(typeof(ARPlaneManager))]
    [RequireComponent(typeof(ARAnchorManager))]
    public class ARPlacementController : MonoBehaviour
    {
        [SerializeField, Tooltip("Root of the town. It stays hidden until the player places it.")]
        private Transform cityRoot;

        [SerializeField, Tooltip("Footprint shown on the detected surface while scanning.")]
        private PlacementReticle reticle;

        [SerializeField, Tooltip("Hide the dot field once the town is placed so the table stays calm.")]
        private bool hidePlanesAfterPlacement = true;

        public event Action<PlacementState> StateChanged;
        public event Action<Transform> CityPlaced;

        public PlacementState State { get; private set; } = PlacementState.Scanning;
        public Transform CityRoot => cityRoot;

        private static readonly List<ARRaycastHit> Hits = new List<ARRaycastHit>();

        private ARRaycastManager raycastManager;
        private ARPlaneManager planeManager;
        private ARAnchorManager anchorManager;
        private Camera arCamera;

        private void Awake()
        {
            raycastManager = GetComponent<ARRaycastManager>();
            planeManager = GetComponent<ARPlaneManager>();
            anchorManager = GetComponent<ARAnchorManager>();

            XROrigin origin = GetComponent<XROrigin>();
            arCamera = origin != null && origin.Camera != null ? origin.Camera : Camera.main;

            Require.Reference(this, cityRoot, nameof(cityRoot));
            Require.Reference(this, reticle, nameof(reticle));

            if (cityRoot != null) cityRoot.gameObject.SetActive(false);
        }

        private void Start()
        {
            StateChanged?.Invoke(State);
        }

        private void Update()
        {
            if (State == PlacementState.Placed) return;

            bool hasTarget = TryGetCentreHit(out Pose pose, out ARPlane plane);
            if (reticle != null) reticle.Show(hasTarget, pose);
            SetState(hasTarget ? PlacementState.ReadyToPlace : PlacementState.Scanning);

            if (hasTarget && PointerInput.TryGetPress(out _))
            {
                Place(pose, plane);
            }
        }

        private bool TryGetCentreHit(out Pose pose, out ARPlane plane)
        {
            pose = default;
            plane = null;

            Vector2 centre = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            if (!raycastManager.Raycast(centre, Hits, TrackableType.PlaneWithinPolygon)) return false;

            foreach (ARRaycastHit hit in Hits)
            {
                ARPlane candidate = planeManager.GetPlane(hit.trackableId);
                if (candidate == null || candidate.alignment != PlaneAlignment.HorizontalUp) continue;

                plane = candidate;
                pose = new Pose(hit.pose.position, FacingCamera(hit.pose.position));
                return true;
            }

            return false;
        }

        private Quaternion FacingCamera(Vector3 position)
        {
            if (arCamera == null) return Quaternion.identity;
            Vector3 toCamera = arCamera.transform.position - position;
            toCamera.y = 0f;
            if (toCamera.sqrMagnitude < 0.0001f) return Quaternion.identity;
            return Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
        }

        private void Place(Pose pose, ARPlane plane)
        {
            if (cityRoot == null)
            {
                Debug.LogError("[Grid Keeper] ARPlacementController has no city root to place.");
                return;
            }

            Transform anchorTransform = CreateAnchor(pose, plane);
            cityRoot.SetParent(anchorTransform, false);
            cityRoot.localPosition = Vector3.zero;
            cityRoot.localRotation = Quaternion.identity;
            cityRoot.gameObject.SetActive(true);

            if (reticle != null) reticle.Show(false, pose);
            if (hidePlanesAfterPlacement) SetPlanesVisible(false);

            SetState(PlacementState.Placed);
            CityPlaced?.Invoke(cityRoot);
        }

        private Transform CreateAnchor(Pose pose, ARPlane plane)
        {
            ARAnchor anchor = plane != null ? anchorManager.AttachAnchor(plane, pose) : null;
            if (anchor != null) return anchor.transform;

            var fallback = new GameObject("City Anchor");
            fallback.transform.SetPositionAndRotation(pose.position, pose.rotation);
            fallback.AddComponent<ARAnchor>();
            return fallback.transform;
        }

        private void SetPlanesVisible(bool visible)
        {
            planeManager.enabled = visible;
            foreach (ARPlane trackedPlane in planeManager.trackables)
            {
                trackedPlane.gameObject.SetActive(visible);
            }
        }

        private void SetState(PlacementState next)
        {
            if (State == next) return;
            State = next;
            StateChanged?.Invoke(State);
        }
    }
}
