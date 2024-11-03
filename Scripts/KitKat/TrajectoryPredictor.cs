
using UnityEngine;
using UdonSharp;

namespace SaccFlightAndVehicles.KitKat
{
    [AddComponentMenu("")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TrajectoryPredictor : UdonSharpBehaviour
    {
        #region SERIALIZED FIELDS
        
        [Header("Dependencies")]
        [SerializeField] private SAV_HUDController linkedHUDControl;
        [SerializeField] private Camera atgCamera;
        
        [Space]
        [SerializeField] private Transform linkedHudVelocityVector;
        [SerializeField] private Transform hudCcip;
        [SerializeField] private Transform topOfCcipLine;
        
        [Space]
        [SerializeField] private Rigidbody vehicleRigidbody;
        [SerializeField] private Rigidbody bombRigidbody;
        
        [Header("Settings")]
        [SerializeField] private float lineOffset = -9;
        [SerializeField] private float atgCamZoom = 0.005f;
        [SerializeField] private float secondsBetweenRaycast = 1;
        [SerializeField] private float bombLifeTime = 8;

        #endregion // SERIALIZED FIELDS

        #region PRIVATE FIELDS

        private float _stepsPerSecond;
        private bool _hitdetect;
        private Vector3 _groundZero;
        private float _fixedDeltaTime;
        private int _stepsToPredict;
        
        private Vector3 _gravity;
        
        private float _drag;
        private float _dragConstant;
        
        private int _trajectoryResolution;

        private Transform _hudControlTransform;
        private Transform _atgCameraTransform;
        
        private float _distanceFromHead;
        
        #endregion // PRIVATE FIELDS

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        
        private void OnEnable()
        {
            if (!_initialized) Init();
        }

        private void OnDisable()
        {
            if (hudCcip) hudCcip.gameObject.SetActive(false);
            if (_atgCameraTransform) _atgCameraTransform.rotation = Quaternion.identity;
        }

        
        private bool _initialized;
        private void Init()
        {
            if (_initialized) return;
            _initialized = true;

            _gravity = Physics.gravity;
            _fixedDeltaTime = Time.fixedDeltaTime;
            
            _stepsPerSecond = 1 / _fixedDeltaTime;

            _drag = bombRigidbody.drag;
            if (_drag <= 0) { Debug.LogErrorFormat("<size=16>[<color=cyan>KitKat</color>] <color=white>{0}</color> : <color=red>{1}</color></size>", name, "There is no drag on the bomb prefab. Please ensure you are using a nonzero drag value."); }
            
            _dragConstant = 1 - _drag * _fixedDeltaTime;

            _stepsToPredict = (int)(_stepsPerSecond * secondsBetweenRaycast);
            _trajectoryResolution = (int)(bombLifeTime / secondsBetweenRaycast);
            
            _hudControlTransform = linkedHUDControl.transform;
            _distanceFromHead = linkedHUDControl.distance_from_head;

            if (atgCamera) _atgCameraTransform = atgCamera.transform;
        }

        
        private void LateUpdate()
        {
            PredictTheFuture(transform.position, vehicleRigidbody.velocity);
            UpdateHud();
        }
        
        private void PredictTheFuture(Vector3 startPos, Vector3 startVelocity)
        {
            Vector3 constants = ((startVelocity * _drag - _gravity) * _dragConstant + _gravity * _dragConstant) / _drag;
            Vector3 nextVelocity = (Mathf.Pow(_dragConstant, _stepsToPredict - 1) * (constants * _drag - _gravity * _dragConstant) + _gravity) / _drag;
            Vector3 nextPos = _fixedDeltaTime * (Mathf.Pow(_dragConstant, _stepsToPredict) * (constants * _drag - _dragConstant * _gravity) + _gravity * ((_dragConstant - 1) * _stepsToPredict + _dragConstant) - constants * _drag) / ((_dragConstant - 1) * _drag) + startPos;

            _hitdetect = false;
            for (int i = 1; i < _trajectoryResolution; i++)
            {
                constants = ((nextVelocity * _drag - _gravity) * _dragConstant + _gravity * _dragConstant) / _drag;
                nextVelocity = (Mathf.Pow(_dragConstant, _stepsToPredict - 1) * (constants * _drag - _gravity * _dragConstant) + _gravity) / _drag;
                Vector3 lastPredictedPos = nextPos;
                nextPos = _fixedDeltaTime * (Mathf.Pow(_dragConstant, _stepsToPredict) * (constants * _drag - _dragConstant * _gravity) + _gravity * ((_dragConstant - 1) * _stepsToPredict + _dragConstant) - constants * _drag) / ((_dragConstant - 1) * _drag) + lastPredictedPos;

                if (!Physics.Raycast(lastPredictedPos, nextPos - lastPredictedPos, out RaycastHit hit, (nextPos - lastPredictedPos).magnitude + 2)) continue;

                _hitdetect = true;
                _groundZero = hit.point;
                return;
            }
        }
        
        private void UpdateHud()
        {
            Vector3 ccipLookDir = _groundZero - _hudControlTransform.position;
            
            float dirAngleCorr = Vector3.SignedAngle(
                Vector3.ProjectOnPlane(ccipLookDir, Vector3.up), 
                Vector3.ProjectOnPlane(linkedHudVelocityVector.forward, Vector3.up), Vector3.up);
            
            ccipLookDir = Quaternion.AngleAxis(dirAngleCorr, Vector3.up) * ccipLookDir;

            Quaternion lookAtPlaneUp = Quaternion.LookRotation(-ccipLookDir, Vector3.up);

            hudCcip.gameObject.SetActive(_hitdetect);

            if (_hitdetect)
            {
                hudCcip.position = _hudControlTransform.position + ccipLookDir.normalized;
                hudCcip.localPosition = hudCcip.localPosition.normalized * _distanceFromHead;
                hudCcip.rotation = lookAtPlaneUp;

                topOfCcipLine.SetPositionAndRotation(
                    linkedHudVelocityVector.position + (Vector3.ProjectOnPlane(Vector3.up, linkedHudVelocityVector.forward) * lineOffset),
                    lookAtPlaneUp);
            }

            if (atgCamera)
            {
                _atgCameraTransform.rotation = Quaternion.LookRotation(ccipLookDir, Vector3.up);
                atgCamera.fieldOfView = Mathf.Clamp(ccipLookDir.magnitude * atgCamZoom, 1, 60);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
    }
}