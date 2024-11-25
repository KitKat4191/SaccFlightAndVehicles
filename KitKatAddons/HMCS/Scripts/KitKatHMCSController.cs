
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using UnityEngine.UI;

namespace SaccFlightAndVehicles.KitKatAddons.HMCS
{
    [AddComponentMenu("")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class KitKatHMCSController : UdonSharpBehaviour
    {
        #region CONSTANTS
        
        private const float METERS_PER_SECOND_IN_KNOTS_CONVERSION = 1.9438445f;
        private const float METERS_PER_FOOT = 3.28084f;
        
        #endregion // CONSTANTS

        #region SERIALIZED FIELDS

        [Header("HUD Text Element References:")]
        [SerializeField] private Text HUDText_G;
        [SerializeField] private Text HUDText_mach;
        [SerializeField] private Text HUDText_altitude;
        [SerializeField] private Text HUDText_knots;
        [SerializeField] private Text HUDText_knotsairspeed;
        [SerializeField] private Text HUDText_angleofattack;

        [Header("HUD Elements:")]
        [Tooltip("Hud element that shows heading.")]
        [SerializeField] private Transform HeadingIndicator;
        [SerializeField] private Transform Healthbar;

        [Header("HMCS Settings:")]
        [Tooltip("Enable this if you don't want the HUD to ever be disabled by the limits.")]
        [SerializeField] private bool persistentHUD = false;

        [SerializeField] private bool doBottomAngle = true;
        [SerializeField] private float bottomDisableAngle = 55;

        [SerializeField] private float xAngleMax = 107;
        [SerializeField] private float xAngleMinimum = 0;
        [Tooltip("The yaw angle off the nose to disable the HMD at. The bigger the number the further away to the side you'll have to look for the HMD to be enabled again.")]
        [SerializeField] private float yDisableAngle = 25;

        [Header("Dash Limit Settings:")]
        [Tooltip("Wether or not to have an additional box the HMD will be disabled in.")]
        [SerializeField] private bool doDashLimit = true;
        [Tooltip("The pitch angle up from straight down where the HMD will be disabled. If set to 10 the HMD will be disabled when looking basically straight down.")]
        [SerializeField] private float dashX = 68;
        [Tooltip("The yaw angle off the nose to disable the HMD at. The bigger the number the further away to the side you'll have to look for the HMD to be enabled again.")]
        [SerializeField] private float dashY = 31;

        [Space(10)]
        [Tooltip("This will only disable the HMD when looking forward with the disable angle. Looking down will still keep the HMD active. Useful for cockpits that have extreme visibility down.")]
        [SerializeField] private bool simpleLimit = false;
        [Tooltip("This is the angle off the nose of the plane that the HUD will be disabled at.")]
        [SerializeField] private float disableAngle = 20;

        [Header("Limits Setup:")]
        [Tooltip("This number shows the yaw angle of your look direction.")]
        [SerializeField] private float _yAngleHMCS;
        [Tooltip("This number shows the pitch angle of your look direction.")]
        [SerializeField] private float _xAngleHMCS;

        [Header("Debug:")]
        [SerializeField] private bool printDebugMessages;
        
        #endregion // SERIALIZED FIELDS

        [PublicAPI]
        public bool HMDHidden;

        #region PRIVATE FIELDS
        
        private GameObject _child;
        private SaccEntity _entityControl;
        private Transform _centerOfMass;
        private SaccAirVehicle _saccAirVehicle;
        
        private float _fullHealth;
        private float _seaLevel;
        private float _vertGs;
        private float _maxGs = 0;

        private float _smoothDeltaTime;
        private float _timeBetweenHUDTextUpdate = 0;
        
        private Vector3 _playerLookDirection;

        private VRCPlayerApi _localPlayer;
        
        #endregion // PRIVATE FIELDS


        private void Start()
        {
            _localPlayer = Networking.LocalPlayer;
            
            _child = gameObject.transform.GetChild(0).gameObject;

            _child.SetActive(persistentHUD);

            _entityControl = GetComponentInParent<SaccEntity>();
            if (!_entityControl)
            {
                LogError("SaccEntity could not be found.");
                Die();
                return;
            }
            
            _centerOfMass = _entityControl.CenterOfMass;

            _saccAirVehicle = _entityControl.GetComponentInChildren<SaccAirVehicle>();
            if (!_saccAirVehicle)
            {
                LogError("SaccAirVehicle could not be found.");
                Die();
                return;
            }

            _fullHealth = _saccAirVehicle.FullHealth;
            _seaLevel = _saccAirVehicle.SeaLevel;
        }

        private void OnEnable() => _maxGs = 0f;
        private void OnDisable() => HMDHidden = true;

        private void LateUpdate()
        {
            HMDHidden = !_child.activeSelf;

            var trackingData = _localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            transform.SetPositionAndRotation(trackingData.position, trackingData.rotation);

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            if (!persistentHUD)
            {
                var entityTransform = _entityControl.transform;
                var entityUp = entityTransform.up;

                var thisForward = transform.forward;
                
                _xAngleHMCS = Vector3.Angle(
                    thisForward,
                    -entityUp
                );

                _yAngleHMCS = Mathf.Abs(Vector3.SignedAngle(
                    Vector3.ProjectOnPlane(
                        thisForward,
                        entityUp),
                    entityTransform.forward, 
                    entityUp)
                );

                if (simpleLimit)
                {
                    _child.SetActive(Vector3.Angle(thisForward, entityTransform.forward) >= disableAngle);
                }
                else
                {
                    _child.SetActive(!
                        ((doBottomAngle && _xAngleHMCS < bottomDisableAngle) || 
                        _yAngleHMCS < yDisableAngle && xAngleMinimum < _xAngleHMCS && _xAngleHMCS < xAngleMax || 
                        (doDashLimit && _yAngleHMCS < dashY && _xAngleHMCS < dashX)));
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            
            if (HeadingIndicator)
            {
                _playerLookDirection = transform.rotation.eulerAngles;
                HeadingIndicator.localRotation = Quaternion.Euler(new Vector3(0, -_playerLookDirection.y, 0));
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            if (Healthbar)
                Healthbar.localScale = new Vector3(Mathf.Max(_saccAirVehicle.Health, 0) / _fullHealth, 1 , 1);

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            // Update numbers on HUD roughly 3 times per second.
            _smoothDeltaTime = Time.smoothDeltaTime;
            if (_timeBetweenHUDTextUpdate <= 0.3f)
            {
                _timeBetweenHUDTextUpdate += _smoothDeltaTime;
                return;
            }

            // Update text
            _timeBetweenHUDTextUpdate = 0;

            _vertGs = _saccAirVehicle.VertGs;

            if (HUDText_G)
            {
                if (Mathf.Abs(_maxGs) < Mathf.Abs(_vertGs))
                {
                    _maxGs = _vertGs;
                }

                HUDText_G.text = string.Concat(
                    _vertGs.ToString("F1"),
                    "\n", _maxGs.ToString("F1"));
            }

            var speed = _saccAirVehicle.Speed;

            if (HUDText_mach)
                HUDText_mach.text = (speed / 343f).ToString("F2");

            if (HUDText_altitude)
            {
                HUDText_altitude.text = string.Concat(
                    (_saccAirVehicle.CurrentVel.y * 60 * METERS_PER_FOOT).ToString("F0"),
                    "\n",
                    ((_centerOfMass.position.y - _seaLevel) * METERS_PER_FOOT).ToString("F0"));
            }

            if (HUDText_knots)
                HUDText_knots.text = (speed * METERS_PER_SECOND_IN_KNOTS_CONVERSION).ToString("F0");

            if (HUDText_knotsairspeed)
                HUDText_knotsairspeed.text = (_saccAirVehicle.AirSpeed * METERS_PER_SECOND_IN_KNOTS_CONVERSION).ToString("F0");

            if (HUDText_angleofattack)
            {
                HUDText_angleofattack.text =
                    speed < 2 
                    ? string.Empty 
                    : _saccAirVehicle.AngleOfAttack.ToString("F0");
            }
        }

        /// <summary>
        /// Uses Debug.LogErrorFormat to print a custom error message that stands out and is easier to read.
        /// </summary>
        private void LogError(string message)
        {
            if (!printDebugMessages) return;

            Debug.LogErrorFormat("<size=16>[<color=cyan>KitKat</color>] <color=white>{0}</color> : <color=red>{1}</color></size>", name, message);
        }

        /// <summary>
        /// Least violent way to halt the program I know of.
        /// </summary>
        private void Die() => ((string)null).ToString();
    }
}