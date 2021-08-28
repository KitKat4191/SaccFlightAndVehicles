
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SaccAirVehicle : UdonSharpBehaviour
{
    public SaccEntity EntityControl;
    public Transform PlaneMesh;
    public int OnboardPlaneLayer = 19;
    public Transform GroundEffectEmpty;
    public Transform PitchMoment;
    public Transform YawMoment;
    public Transform GroundDetector;
    [UdonSynced(UdonSyncMode.None)] public float Health = 23f;
    public Transform GunRecoilEmpty;
    public float GunRecoil = 150;
    public bool RepeatingWorld = true;
    public float RepeatingWorldDistance = 20000;
    [SerializeField] private bool SwitchHandsJoyThrottle = false;
    public bool HasAfterburner = true;
    [SerializeField] private KeyCode AfterBurnerKey = KeyCode.T;
    public float ThrottleAfterburnerPoint = 0.8f;
    public bool VTOLOnly = false;
    public bool HasVTOLAngle = false;
    [Header("Response:")]
    public float ThrottleStrength = 20f;
    public bool VerticalThrottle = false;
    public float ThrottleSensitivity = 6f;
    public float AfterburnerThrustMulti = 1.5f;
    public float AccelerationResponse = 4.5f;
    public float EngineSpoolDownSpeedMulti = .5f;
    public float AirFriction = 0.0004f;
    public float PitchStrength = 5f;
    public float PitchThrustVecMulti = 0f;
    public float PitchFriction = 24f;
    public float PitchConstantFriction = 0f;
    public float PitchResponse = 20f;
    public float ReversingPitchStrengthMulti = 2;
    public float YawStrength = 3f;
    public float YawThrustVecMulti = 0f;
    public float YawFriction = 15f;
    public float YawConstantFriction = 0f;
    public float YawResponse = 20f;
    public float ReversingYawStrengthMulti = 2.4f;
    public float RollStrength = 450f;
    public float RollThrustVecMulti = 0f;
    public float RollFriction = 90f;
    public float RollConstantFriction = 0f;
    public float RollResponse = 20f;
    public float ReversingRollStrengthMulti = 1.6f;//reversing = AoA > 90
    public float PitchDownStrMulti = .8f;
    public float PitchDownLiftMulti = .8f;
    public float InertiaTensorRotationMulti = 1;
    public bool InvertITRYaw = false;
    public float AdverseYaw = 0;
    public float AdverseRoll = 0;
    public float RotMultiMaxSpeed = 220f;
    //public float StickInputPower = 1.7f;
    public float VelStraightenStrPitch = 0.035f;
    public float VelStraightenStrYaw = 0.045f;
    public float MaxAngleOfAttackPitch = 25f;
    public float MaxAngleOfAttackYaw = 40f;
    public float AoaCurveStrength = 2f;//1 = linear, >1 = convex, <1 = concave
    public float HighPitchAoaMinControl = 0.2f;
    public float HighYawAoaMinControl = 0.2f;
    public float HighPitchAoaMinLift = 0.2f;
    public float HighYawAoaMinLift = 0.2f;
    public float TaxiRotationSpeed = 35f;
    public float TaxiRotationResponse = 2.5f;
    public float Lift = 0.00015f;
    public float SidewaysLift = .17f;
    public float MaxLift = 10f;
    public float VelLift = 1f;
    public float VelLiftMax = 10f;
    public float MaxGs = 40f;
    public float GDamage = 10f;
    public float GroundEffectMaxDistance = 7;
    public float GroundEffectStrength = 4;
    public float GroundEffectLiftMax = 9999999;
    public float GLimiter = 12f;
    public float AoALimiter = 15f;
    [Header("Response VTOL:")]
    public float VTOLAngleTurnRate = 90f;
    public float VTOLDefaultValue = 0;
    public bool VTOLAllowAfterburner = false;
    public float VTOLThrottleStrengthMulti = .7f;
    public float VTOLMinAngle = 0;
    public float VTOLMaxAngle = 90;
    public float VTOLPitchThrustVecMulti = .3f;
    public float VTOLYawThrustVecMulti = .3f;
    public float VTOLRollThrustVecMulti = .07f;
    public float VTOLLoseControlSpeed = 120;
    public float VTOLGroundEffectStrength = 4;
    [Header("Other:")]
    [Tooltip("Adjusts all values that would need to be adjusted if you changed the mass automatically on Start(). Including all wheel colliders")]
    [SerializeField] private bool AutoAdjustValuesToMass = true;
    public float SeaLevel = -10f;
    public Vector3 Wind;
    public float WindGustStrength = 15;
    public float WindGustiness = 0.03f;
    public float WindTurbulanceScale = 0.0001f;
    public float SoundBarrierStrength = 0.0003f;
    public float SoundBarrierWidth = 20f;
    [UdonSynced(UdonSyncMode.None)] public float Fuel = 7200;
    public float FuelConsumption = 2;
    public float FuelConsumptionABMulti = 3f;
    public float RefuelTime = 25;
    public float RepairTime = 30;
    public float RespawnDelay = 10;
    public float InvincibleAfterSpawn = 2.5f;
    [Tooltip("Meters. 12192 = 40,000 feet")]
    public float AtmosphereThinningStart = 12192f; //40,000 feet
    [Tooltip("Meters. 19812 = 65,000 feet feet")]
    public float AtmosphereThinningEnd = 19812; //65,000 feet
    public bool DisablePhysicsAndInputs;
    [System.NonSerializedAttribute] public float AllGs;


    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.Linear)] public float EngineOutput = 0f;
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.Linear)] public Vector3 CurrentVel = Vector3.zero;
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.Linear)] public float VertGs = 1f;
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.Linear)] public float AngleOfAttack;//MAX of yaw & pitch aoa //used by effectscontroller and hudcontroller
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.None)] public bool Occupied = false; //this is true if someone is sitting in pilot seat
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.Linear)] public float VTOLAngle;

    [System.NonSerializedAttribute] public Animator VehicleAnimator;
    [System.NonSerializedAttribute] public ConstantForce VehicleConstantForce;
    [System.NonSerializedAttribute] public Rigidbody VehicleRigidbody;
    [System.NonSerializedAttribute] public Transform VehicleTransform;
    private VRC.SDK3.Components.VRCObjectSync VehicleObjectSync;
    private GameObject VehicleGameObj;
    [System.NonSerializedAttribute] public Transform CenterOfMass;
    private float LerpedRoll;
    private float LerpedPitch;
    private float LerpedYaw;
    [System.NonSerializedAttribute] public bool ThrottleGripLastFrame = false;
    [System.NonSerializedAttribute] public bool JoystickGripLastFrame = false;
    [System.NonSerializedAttribute] public bool LTriggerLastFrame = false;
    [System.NonSerializedAttribute] public bool RTriggerLastFrame = false;
    Quaternion JoystickZeroPoint;
    Quaternion PlaneRotLastFrame;
    [System.NonSerializedAttribute] public float PlayerThrottle;
    private float TempThrottle;
    private float ThrottleZeroPoint;
    private float ThrottlePlayspaceLastFrame;
    [System.NonSerializedAttribute] public float ThrottleInput = 0f;
    private float roll = 0f;
    private float pitch = 0f;
    private float yaw = 0f;
    [System.NonSerializedAttribute] public float FullHealth;
    [System.NonSerializedAttribute] public bool Taxiing = false;
    [System.NonSerializedAttribute] public bool Floating = false;
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.Linear)] public Vector3 RotationInputs;
    [System.NonSerializedAttribute] public bool Piloting = false;
    [System.NonSerializedAttribute] public bool Passenger = false;
    [System.NonSerializedAttribute] public bool InEditor = true;
    [System.NonSerializedAttribute] public bool InVR = false;
    [System.NonSerializedAttribute] public Vector3 LastFrameVel = Vector3.zero;
    [System.NonSerializedAttribute] public VRCPlayerApi localPlayer;
    [System.NonSerializedAttribute] public float AtmoshpereFadeDistance;
    [System.NonSerializedAttribute] public float AtmosphereHeightThing;
    [System.NonSerializedAttribute] public float Atmosphere = 1;
    [System.NonSerializedAttribute] public float rotlift;
    [System.NonSerializedAttribute] public float AngleOfAttackPitch;
    [System.NonSerializedAttribute] public float AngleOfAttackYaw;
    private float AoALiftYaw;
    private float AoALiftPitch;
    private Vector3 Pitching;
    private Vector3 Yawing;
    [System.NonSerializedAttribute] public float Taxiinglerper;
    [System.NonSerializedAttribute] public float ExtraDrag = 1;
    [System.NonSerializedAttribute] public float ExtraLift = 1;
    private float ReversingPitchStrength;
    private float ReversingYawStrength;
    private float ReversingRollStrength;
    private float ReversingPitchStrengthZero;
    private float ReversingYawStrengthZero;
    private float ReversingRollStrengthZero;
    private float ReversingPitchStrengthZeroStart;
    private float ReversingYawStrengthZeroStart;
    private float ReversingRollStrengthZeroStart;
    [System.NonSerializedAttribute] public float Speed;
    [System.NonSerializedAttribute] public float AirSpeed;
    [System.NonSerializedAttribute] public bool IsOwner = false;
    private Vector3 FinalWind;//includes Gusts
    [System.NonSerializedAttribute] public Vector3 AirVel;
    private float StillWindMulti;//multiplies the speed of the wind by the speed of the plane when taxiing to prevent still planes flying away
    private int ThrustVecGrounded;
    private float SoundBarrier;
    [System.NonSerializedAttribute] public float FullFuel;
    private float LowFuel;
    private float LowFuelDivider;
    private float LastResupplyTime = 5;//can't resupply for the first 10 seconds after joining, fixes potential null ref if sending something to PlaneAnimator on first frame
    [System.NonSerializedAttribute] public float FullGunAmmo;
    //use these for whatever, Only MissilesIncomingHeat is used by the prefab
    [System.NonSerializedAttribute] public int MissilesIncomingHeat = 0;
    [System.NonSerializedAttribute] public int MissilesIncomingRadar = 0;
    [System.NonSerializedAttribute] public int MissilesIncomingOther = 0;
    [System.NonSerializedAttribute] public Vector3 Spawnposition;
    [System.NonSerializedAttribute] public Vector3 Spawnrotation;
    private int OutsidePlaneLayer;
    [System.NonSerializedAttribute] public bool DoAAMTargeting;
    bool LandedOnWater = false;
    private float VelLiftStart;
    private int Planelayer;
    private float VelLiftMaxStart;
    private bool HasAirBrake;//set to false if air brake strength is 0
    private float HandDistanceZLastFrame;
    private float EngineAngle;
    private float PitchThrustVecMultiStart;
    private float YawThrustVecMultiStart;
    private float RollThrustVecMultiStart;
    [System.NonSerializedAttribute] public bool VTOLenabled;
    [System.NonSerializedAttribute] public float VTOLAngleInput;
    private float VTOL90Degrees;//1=(90 degrees OR maxVTOLAngle if it's lower than 90) used for transition thrust values 
    private float ThrottleNormalizer;
    private float VTOLAngleDivider;
    private float ABNormalizer;
    private float EngineOutputLastFrame;
    float VTOLAngle90;
    bool HasWheelColliders = false;
    private float vtolangledif;
    Vector3 VTOL180 = new Vector3(0, 0.01f, -1);//used as a rotation target for VTOL adjustment. Slightly below directly backward so that rotatetowards rotates on the correct axis
    private bool GunRecoilEmptyNULL = true;
    [System.NonSerializedAttribute] public float ThrottleStrengthAB;
    [System.NonSerializedAttribute] public float FuelConsumptionAB;
    private bool VTolAngle90Plus;
    [System.NonSerializedAttribute] public bool AfterburnerOn;
    [System.NonSerializedAttribute] public bool PitchDown;//air is hitting plane from the top
    private float GDamageToTake;
    private Vector3 SpawnPos;
    private Quaternion SpawnRot;


    [System.NonSerializedAttribute] public int NumActiveFlares;
    [System.NonSerializedAttribute] public int NumActiveChaff;
    [System.NonSerializedAttribute] public int NumActiveOtherCM;
    //this stuff can be used by DFUNCs
    //if these == 0 then they are not disabled. Being an int allows more than one extension to disable it at a time
    [System.NonSerializedAttribute] public float Limits = 1;
    [System.NonSerializedAttribute] public int OverrideConstantForce = 0;
    [System.NonSerializedAttribute] public Vector3 CFRelativeForceOverride;
    [System.NonSerializedAttribute] public Vector3 CFRelativeTorqueOverride;
    [System.NonSerializedAttribute] public int DisableGearToggle = 0;
    [System.NonSerializedAttribute] public int DisableTaxiRotation = 0;
    [System.NonSerializedAttribute] public int DisableGroundDetection = 0;
    [System.NonSerializedAttribute] public int ThrottleOverridden = 0;
    [System.NonSerializedAttribute] public float ThrottleOverride;
    [System.NonSerializedAttribute] public int JoystickOverridden = 0;
    [System.NonSerializedAttribute] public Vector3 JoystickOverride;



    [System.NonSerializedAttribute] public int ReSupplied = 0;

    private int AAMLAUNCHED_STRING = Animator.StringToHash("aamlaunched");
    private int RADARLOCKED_STRING = Animator.StringToHash("radarlocked");
    private int AFTERBURNERON_STRING = Animator.StringToHash("afterburneron");
    private int RESUPPLY_STRING = Animator.StringToHash("resupply");
    private int HOOKDOWN_STRING = Animator.StringToHash("hookdown");
    private int LOCALPILOT_STRING = Animator.StringToHash("localpilot");
    private int LOCALPASSENGER_STRING = Animator.StringToHash("localpassenger");
    private int OCCUPIED_STRING = Animator.StringToHash("occupied");
    private int REAPPEAR_STRING = Animator.StringToHash("reappear");
    public void SFEXT_L_EntityStart()
    {
        VehicleGameObj = EntityControl.gameObject;
        VehicleTransform = EntityControl.GetComponent<Transform>();
        VehicleRigidbody = EntityControl.GetComponent<Rigidbody>();
        VehicleConstantForce = EntityControl.GetComponent<ConstantForce>();


        localPlayer = Networking.LocalPlayer;
        if (localPlayer == null)
        {
            InEditor = true;
            Piloting = true;
            IsOwner = true;
        }
        else
        {
            InEditor = false;
            InVR = localPlayer.IsUserInVR();
            if (localPlayer.isMaster)
            {
                VehicleRigidbody.angularDrag = 0;
                IsOwner = true;
            }
            else { VehicleRigidbody.angularDrag = .5f; }
        }

        //delete me when ObjectSync.Respawn works in editor again
        SpawnPos = VehicleTransform.position;
        SpawnRot = VehicleTransform.rotation;
        //
        WheelCollider[] wc = PlaneMesh.GetComponentsInChildren<WheelCollider>(true);
        if (wc.Length != 0) { HasWheelColliders = true; }

        if (AutoAdjustValuesToMass)
        {
            //values that should feel the same no matter the weight of the aircraft
            float RBMass = VehicleRigidbody.mass;
            ThrottleStrength *= RBMass;
            PitchStrength *= RBMass;
            PitchFriction *= RBMass;
            YawStrength *= RBMass;
            YawFriction *= RBMass;
            RollStrength *= RBMass;
            RollFriction *= RBMass;
            Lift *= RBMass;
            MaxLift *= RBMass;
            VelLiftMax *= RBMass;
            VelStraightenStrPitch *= RBMass;
            VelStraightenStrYaw *= RBMass;
            foreach (WheelCollider wheel in wc)
            {
                JointSpring SusiSpring = wheel.suspensionSpring;
                SusiSpring.spring *= RBMass;
                SusiSpring.damper *= RBMass;
                wheel.suspensionSpring = SusiSpring;
            }
        }
        Planelayer = PlaneMesh.gameObject.layer;//get the layer of the plane as set by the world creator
        OutsidePlaneLayer = PlaneMesh.gameObject.layer;
        VehicleAnimator = EntityControl.GetComponent<Animator>();
        //set these values at start in case they haven't been set correctly in editor


        FullHealth = Health;
        FullFuel = Fuel;

        VelLiftMaxStart = VelLiftMax;
        VelLiftStart = VelLift;

        PitchThrustVecMultiStart = PitchThrustVecMulti;
        YawThrustVecMultiStart = YawThrustVecMulti;
        RollThrustVecMultiStart = RollThrustVecMulti;

        //these two are only used in editor
        Spawnposition = VehicleTransform.position;
        Spawnrotation = VehicleTransform.rotation.eulerAngles;

        CenterOfMass = EntityControl.CenterOfMass;
        VehicleRigidbody.centerOfMass = VehicleTransform.InverseTransformDirection(CenterOfMass.position - VehicleTransform.position);//correct position if scaled
        VehicleRigidbody.inertiaTensorRotation = Quaternion.SlerpUnclamped(Quaternion.identity, VehicleRigidbody.inertiaTensorRotation, InertiaTensorRotationMulti);
        if (InvertITRYaw)
        {
            Vector3 ITR = VehicleRigidbody.inertiaTensorRotation.eulerAngles;
            ITR.x *= -1;
            VehicleRigidbody.inertiaTensorRotation = Quaternion.Euler(ITR);
        }

        if (AtmosphereThinningStart > AtmosphereThinningEnd) { AtmosphereThinningEnd = (AtmosphereThinningStart + 1); }
        AtmoshpereFadeDistance = (AtmosphereThinningEnd + SeaLevel) - (AtmosphereThinningStart + SeaLevel); //for finding atmosphere thinning gradient
        AtmosphereHeightThing = (AtmosphereThinningStart + SeaLevel) / (AtmoshpereFadeDistance); //used to add back the height to the atmosphere after finding gradient

        //used to set each rotation axis' reversing behaviour to inverted if 0 thrust vectoring, and not inverted if thrust vectoring is non-zero.
        //the variables are called 'Zero' because they ask if thrustvec is set to 0.
        if (VTOLOnly)//Never do this for heli-like vehicles
        {
            ReversingPitchStrengthZero = 1;
            ReversingYawStrengthZero = 1;
            ReversingRollStrengthZero = 1;
        }
        else
        {
            ReversingPitchStrengthZeroStart = ReversingPitchStrengthZero = PitchThrustVecMulti == 0 ? -ReversingPitchStrengthMulti : 1;
            ReversingYawStrengthZeroStart = ReversingYawStrengthZero = YawThrustVecMulti == 0 ? -ReversingYawStrengthMulti : 1;
            ReversingRollStrengthZeroStart = ReversingRollStrengthZero = RollThrustVecMulti == 0 ? -ReversingRollStrengthMulti : 1;
        }


        if (VTOLOnly || HasVTOLAngle) { VTOLenabled = true; }
        VTOL90Degrees = Mathf.Min(90 / VTOLMaxAngle, 1);

        if (!HasAfterburner) { ThrottleAfterburnerPoint = 1; }
        ThrottleNormalizer = 1 / ThrottleAfterburnerPoint;
        ABNormalizer = 1 / (1 - ThrottleAfterburnerPoint);

        FuelConsumptionAB = (FuelConsumption * FuelConsumptionABMulti) - FuelConsumption;
        ThrottleStrengthAB = (ThrottleStrength * AfterburnerThrustMulti) - ThrottleStrength;

        vtolangledif = VTOLMaxAngle - VTOLMinAngle;
        VTOLAngleDivider = VTOLAngleTurnRate / vtolangledif;
        VTOLAngle = VTOLAngleInput = VTOLDefaultValue;

        if (GroundEffectEmpty == null)
        {
            Debug.LogWarning("GroundEffectEmpty not found, using CenterOfMass instead");
            GroundEffectEmpty = CenterOfMass;
        }

        VehicleObjectSync = (VRC.SDK3.Components.VRCObjectSync)EntityControl.gameObject.GetComponent(typeof(VRC.SDK3.Components.VRCObjectSync));

        if (GunRecoilEmpty != null)
        {
            GunRecoilEmptyNULL = false;
        }
        LowFuel = 200;//FullFuel * .13888888f;//to match the old default settings
        LowFuelDivider = 1 / LowFuel;

        //thrust is lerped towards VTOLThrottleStrengthMulti by VTOLAngle, unless VTOLMaxAngle is greater than 90 degrees, then it's lerped by 90=1
        VTolAngle90Plus = VTOLMaxAngle > 90;
    }
    private void LateUpdate()
    {
        float DeltaTime = Time.deltaTime;

        if (IsOwner)//works in editor or ingame
        {
            if (!EntityControl.dead)
            {
                //G/crash Damage
                Health -= Mathf.Max((GDamageToTake) * DeltaTime * GDamage, 0f);//take damage of GDamage per second per G above MaxGs
                GDamageToTake = 0;
                if (Health <= 0f)//plane is ded
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Explode");
                }
            }
            else { GDamageToTake = 0; }


            if (DisableGroundDetection == 0)
            {
                if (Floating)
                {
                    if (!LandedOnWater)
                    {
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(TouchDownWater));
                    }
                }

                if ((Physics.Raycast(GroundDetector.position, -GroundDetector.up, .44f, 2049 /* Default and Environment */, QueryTriggerInteraction.Ignore)))
                {
                    if (!Taxiing)
                    {
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(TouchDown));
                    }
                }
                else
                {
                    if (!Floating && Taxiing)
                    {
                        LandedOnWater = false;
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(TakeOff));
                    }
                }
            }


            //synced variables because rigidbody values aren't accessable by non-owner players
            CurrentVel = VehicleRigidbody.velocity;
            Speed = CurrentVel.magnitude;
            bool PlaneMoving = false;
            if (Speed > .1f)//don't bother doing all this for planes that arent moving and it therefore wont even effect
            {
                PlaneMoving = true;//check this bool later for more optimizations
                WindAndAoA();
            }

            if (Piloting)
            {
                //gotta do these this if we're piloting but it didn't get done(specifically, hovering extremely slowly in a VTOL craft will cause control issues we don't)
                if (!PlaneMoving)
                { WindAndAoA(); PlaneMoving = true; }
                if (RepeatingWorld)
                {
                    if (CenterOfMass.position.z > RepeatingWorldDistance)
                    {
                        Vector3 vehpos = VehicleTransform.position;
                        vehpos.z -= RepeatingWorldDistance * 2;
                        VehicleTransform.position = vehpos;
                    }
                    else if (CenterOfMass.position.z < -RepeatingWorldDistance)
                    {
                        Vector3 vehpos = VehicleTransform.position;
                        vehpos.z += RepeatingWorldDistance * 2;
                        VehicleTransform.position = vehpos;
                    }
                    else if (CenterOfMass.position.x > RepeatingWorldDistance)
                    {
                        Vector3 vehpos = VehicleTransform.position;
                        vehpos.x -= RepeatingWorldDistance * 2;
                        VehicleTransform.position = vehpos;
                    }
                    else if (CenterOfMass.position.x < -RepeatingWorldDistance)
                    {
                        Vector3 vehpos = VehicleTransform.position;
                        vehpos.x += RepeatingWorldDistance * 2;
                        VehicleTransform.position = vehpos;
                    }
                }

                if (!DisablePhysicsAndInputs)
                {
                    //collect inputs
                    int Wi = Input.GetKey(KeyCode.W) ? 1 : 0; //inputs as ints
                    int Si = Input.GetKey(KeyCode.S) ? -1 : 0;
                    int Ai = Input.GetKey(KeyCode.A) ? -1 : 0;
                    int Di = Input.GetKey(KeyCode.D) ? 1 : 0;
                    int Qi = Input.GetKey(KeyCode.Q) ? -1 : 0;
                    int Ei = Input.GetKey(KeyCode.E) ? 1 : 0;
                    int upi = Input.GetKey(KeyCode.UpArrow) ? 1 : 0;
                    int downi = Input.GetKey(KeyCode.DownArrow) ? -1 : 0;
                    int lefti = Input.GetKey(KeyCode.LeftArrow) ? -1 : 0;
                    int righti = Input.GetKey(KeyCode.RightArrow) ? 1 : 0;
                    bool Shift = Input.GetKey(KeyCode.LeftShift);
                    bool Ctrl = Input.GetKey(KeyCode.LeftControl);
                    int Shifti = Shift ? 1 : 0;
                    float LGrip = 0;
                    float RGrip = 0;
                    int LeftControli = Ctrl ? 1 : 0;
                    if (!InEditor)
                    {
                        LGrip = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryHandTrigger");
                        RGrip = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryHandTrigger");
                    }
                    //MouseX = Input.GetAxisRaw("Mouse X");
                    //MouseY = Input.GetAxisRaw("Mouse Y");
                    Vector3 JoystickPosYaw;
                    Vector3 JoystickPos;
                    Vector2 VRPitchRoll;

                    float ThrottleGrip;
                    float JoyStickGrip;
                    if (SwitchHandsJoyThrottle)
                    {
                        JoyStickGrip = LGrip;
                        ThrottleGrip = RGrip;
                    }
                    else
                    {
                        ThrottleGrip = LGrip;
                        JoyStickGrip = RGrip;
                    }
                    //VR Joystick                
                    if (JoyStickGrip > 0.75)
                    {
                        Quaternion PlaneRotDif = VehicleTransform.rotation * Quaternion.Inverse(PlaneRotLastFrame);//difference in plane's rotation since last frame
                        PlaneRotLastFrame = VehicleTransform.rotation;
                        JoystickZeroPoint = PlaneRotDif * JoystickZeroPoint;//zero point rotates with the plane so it appears still to the pilot
                        if (!JoystickGripLastFrame)//first frame you gripped joystick
                        {
                            PlaneRotDif = Quaternion.identity;
                            if (SwitchHandsJoyThrottle)
                            { JoystickZeroPoint = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation; }//rotation of the controller relative to the plane when it was pressed
                            else
                            { JoystickZeroPoint = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation; }
                        }
                        //difference between the plane and the hand's rotation, and then the difference between that and the JoystickZeroPoint
                        Quaternion JoystickDifference;
                        if (SwitchHandsJoyThrottle)
                        { JoystickDifference = (Quaternion.Inverse(VehicleTransform.rotation) * localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation) * Quaternion.Inverse(JoystickZeroPoint); }
                        else { JoystickDifference = (Quaternion.Inverse(VehicleTransform.rotation) * localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation) * Quaternion.Inverse(JoystickZeroPoint); }

                        JoystickPosYaw = (JoystickDifference * VehicleTransform.forward);//angles to vector
                        JoystickPosYaw.y = 0;
                        JoystickPos = (JoystickDifference * VehicleTransform.up);
                        VRPitchRoll = new Vector2(JoystickPos.x, JoystickPos.z) * 1.41421f;

                        JoystickGripLastFrame = true;
                        //making a circular joy stick square
                        //pitch and roll
                        if (Mathf.Abs(VRPitchRoll.x) > Mathf.Abs(VRPitchRoll.y))
                        {
                            if (Mathf.Abs(VRPitchRoll.x) > 0)
                            {
                                float temp = VRPitchRoll.magnitude / Mathf.Abs(VRPitchRoll.x);
                                VRPitchRoll *= temp;
                            }
                        }
                        else if (Mathf.Abs(VRPitchRoll.y) > 0)
                        {
                            float temp = VRPitchRoll.magnitude / Mathf.Abs(VRPitchRoll.y);
                            VRPitchRoll *= temp;
                        }
                        //yaw
                        if (Mathf.Abs(JoystickPosYaw.x) > Mathf.Abs(JoystickPosYaw.z))
                        {
                            if (Mathf.Abs(JoystickPosYaw.x) > 0)
                            {
                                float temp = JoystickPosYaw.magnitude / Mathf.Abs(JoystickPosYaw.x);
                                JoystickPosYaw *= temp;
                            }
                        }
                        else if (Mathf.Abs(JoystickPosYaw.z) > 0)
                        {
                            float temp = JoystickPosYaw.magnitude / Mathf.Abs(JoystickPosYaw.z);
                            JoystickPosYaw *= temp;
                        }

                    }
                    else
                    {
                        JoystickPosYaw.x = 0;
                        VRPitchRoll = Vector3.zero;
                        JoystickGripLastFrame = false;
                    }

                    if (HasAfterburner)
                    {
                        if (AfterburnerOn)
                        { PlayerThrottle = Mathf.Clamp(PlayerThrottle + ((Shifti - LeftControli) * .5f * DeltaTime), 0, 1f); }
                        else
                        { PlayerThrottle = Mathf.Clamp(PlayerThrottle + ((Shifti - LeftControli) * .5f * DeltaTime), 0, .8f); }
                    }
                    else
                    { PlayerThrottle = Mathf.Clamp(PlayerThrottle + ((Shifti - LeftControli) * .5f * DeltaTime), 0, 1f); }
                    //VR Throttle
                    if (ThrottleGrip > 0.75)
                    {
                        Vector3 handdistance;
                        if (SwitchHandsJoyThrottle)
                        { handdistance = VehicleTransform.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position; }
                        else { handdistance = VehicleTransform.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position; }
                        handdistance = VehicleTransform.InverseTransformDirection(handdistance);

                        Vector3 PlaySpaceDistance = transform.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).position;
                        PlaySpaceDistance = VehicleTransform.InverseTransformDirection(PlaySpaceDistance);

                        float HandThrottleAxis;
                        if (VerticalThrottle)
                        {
                            HandThrottleAxis = handdistance.y;
                            /*    - (PlaySpaceDistance.y - ThrottlePlayspaceLastFrame);
                              ThrottlePlayspaceLastFrame = PlaySpaceDistance.y; */
                        }
                        else
                        {
                            HandThrottleAxis = handdistance.z;
                            /*     - (PlaySpaceDistance.y - ThrottlePlayspaceLastFrame);
                               ThrottlePlayspaceLastFrame = PlaySpaceDistance.z; */
                        }

                        if (!ThrottleGripLastFrame)
                        {
                            ThrottleZeroPoint = HandThrottleAxis;
                            TempThrottle = PlayerThrottle;
                            HandDistanceZLastFrame = 0;
                        }
                        float ThrottleDifference = ThrottleZeroPoint - HandThrottleAxis;
                        ThrottleDifference *= ThrottleSensitivity;
                        bool VTOLandAB_Disallowed = (!VTOLAllowAfterburner && VTOLAngle != 0);/*don't allow VTOL AB disabled planes, false if attemping to*/

                        //Detent function to prevent you going into afterburner by accident (bit of extra force required to turn on AB (actually hand speed))
                        if (((HandDistanceZLastFrame - HandThrottleAxis) * ThrottleSensitivity > .05f)/*detent overcome*/ && !VTOLandAB_Disallowed && Fuel > LowFuel || ((PlayerThrottle > ThrottleAfterburnerPoint/*already in afterburner*/&& !VTOLandAB_Disallowed) || !HasAfterburner))
                        {
                            PlayerThrottle = Mathf.Clamp(TempThrottle + ThrottleDifference, 0, 1);
                        }
                        else
                        {
                            PlayerThrottle = Mathf.Clamp(TempThrottle + ThrottleDifference, 0, ThrottleAfterburnerPoint);
                        }
                        HandDistanceZLastFrame = HandThrottleAxis;
                        ThrottleGripLastFrame = true;
                    }
                    else
                    {
                        ThrottleGripLastFrame = false;
                    }

                    if ((DisableTaxiRotation == 0) && (Taxiing))
                    {
                        AngleOfAttack = 0;//prevent stall sound and aoavapor when on ground
                                          //rotate if trying to yaw
                        Taxiinglerper = Mathf.Lerp(Taxiinglerper, RotationInputs.y * TaxiRotationSpeed * Time.smoothDeltaTime, TaxiRotationResponse * DeltaTime);
                        VehicleTransform.Rotate(Vector3.up, Taxiinglerper);

                        StillWindMulti = Mathf.Min(Speed / 10, 1);
                        ThrustVecGrounded = 0;
                    }
                    else
                    {
                        StillWindMulti = 1;
                        ThrustVecGrounded = 1;
                        Taxiinglerper = 0;
                    }
                    //keyboard control for afterburner
                    if (Input.GetKeyDown(AfterBurnerKey) && HasAfterburner && (VTOLAngle == 0 || VTOLAllowAfterburner))
                    {
                        if (AfterburnerOn)
                            PlayerThrottle = ThrottleAfterburnerPoint;
                        else
                            PlayerThrottle = 1;
                    }
                    //Cruise PI Controller
                    if (ThrottleOverridden > 0 && !ThrottleGripLastFrame && !Shift && !Ctrl)
                    {
                        ThrottleInput = PlayerThrottle = ThrottleOverride;
                    }
                    else//if cruise control disabled, use inputs
                    {
                        if (!InVR)
                        {
                            float LTrigger = 0;
                            float RTrigger = 0;
                            if (!InEditor)
                            {
                                LTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
                                RTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
                            }
                            if (LTrigger > .05f)//axis throttle input for people who wish to use it //.05 deadzone so it doesn't take effect for keyboard users with something plugged in
                            { ThrottleInput = LTrigger; }
                            else { ThrottleInput = PlayerThrottle; }
                        }
                        else { ThrottleInput = PlayerThrottle; }
                    }

                    Vector2 Throttles = UnpackThrottles(ThrottleInput);
                    Fuel = Mathf.Max(Fuel -
                                        ((Mathf.Max(Throttles.x, 0.25f) * FuelConsumption)
                                            + (Throttles.y * FuelConsumptionAB)) * DeltaTime, 0);


                    if (Fuel < LowFuel) { ThrottleInput = ThrottleInput * (Fuel * LowFuelDivider); }//decrease max throttle as fuel runs out

                    if (HasAfterburner)
                    {
                        if (ThrottleInput > ThrottleAfterburnerPoint && !AfterburnerOn)
                        {
                            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetAfterburnerOn");
                        }
                        else if (ThrottleInput <= ThrottleAfterburnerPoint && AfterburnerOn)
                        {
                            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetAfterburnerOff");
                        }
                    }
                    if (JoystickOverridden > 0 && !JoystickGripLastFrame)//joystick override enabled, and player not holding joystick
                    {
                        RotationInputs = JoystickOverride;
                    }
                    else//joystick override disabled, player has control
                    {
                        if (!InVR)
                        {
                            //allow stick flight in desktop mode
                            Vector2 LStickPos = new Vector2(0, 0);
                            Vector2 RStickPos = new Vector2(0, 0);
                            if (!InEditor)
                            {
                                LStickPos.x = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickHorizontal");
                                LStickPos.y = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical");
                                RStickPos.x = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");
                                RStickPos.y = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical");
                            }
                            VRPitchRoll = LStickPos;
                            JoystickPosYaw.x = RStickPos.x;
                            //make stick input square
                            if (Mathf.Abs(VRPitchRoll.x) > Mathf.Abs(VRPitchRoll.y))
                            {
                                if (Mathf.Abs(VRPitchRoll.x) > 0)
                                {
                                    float temp = VRPitchRoll.magnitude / Mathf.Abs(VRPitchRoll.x);
                                    VRPitchRoll *= temp;
                                }
                            }
                            else if (Mathf.Abs(VRPitchRoll.y) > 0)
                            {
                                float temp = VRPitchRoll.magnitude / Mathf.Abs(VRPitchRoll.y);
                                VRPitchRoll *= temp;
                            }
                        }

                        RotationInputs.x = Mathf.Clamp(VRPitchRoll.y + Wi + Si + downi + upi, -1, 1) * Limits;
                        RotationInputs.y = Mathf.Clamp(Qi + Ei + JoystickPosYaw.x, -1, 1) * Limits;
                        //roll isn't subject to flight limits
                        RotationInputs.z = Mathf.Clamp(((VRPitchRoll.x + Ai + Di + lefti + righti) * -1), -1, 1);
                    }

                    //ability to adjust input to be more precise at low amounts. 'exponant'
                    /* RotationInputs.x = RotationInputs.x > 0 ? Mathf.Pow(RotationInputs.x, StickInputPower) : -Mathf.Pow(Mathf.Abs(RotationInputs.x), StickInputPower);
                    RotationInputs.y = RotationInputs.y > 0 ? Mathf.Pow(RotationInputs.y, StickInputPower) : -Mathf.Pow(Mathf.Abs(RotationInputs.y), StickInputPower);
                    RotationInputs.z = RotationInputs.z > 0 ? Mathf.Pow(RotationInputs.z, StickInputPower) : -Mathf.Pow(Mathf.Abs(RotationInputs.z), StickInputPower); */

                    //if moving backwards, controls invert (if thrustvectoring is set to 0 strength for that axis)
                    if ((Vector3.Dot(AirVel, VehicleTransform.forward) > 0))//normal, moving forward
                    {
                        ReversingPitchStrength = 1;
                        ReversingYawStrength = 1;
                        ReversingRollStrength = 1;
                    }
                    else//moving backward. The 'Zero' values are set in start(). Explanation there.
                    {
                        ReversingPitchStrength = ReversingPitchStrengthZero;
                        ReversingYawStrength = ReversingYawStrengthZero;
                        ReversingRollStrength = ReversingRollStrengthZero;
                    }

                    pitch = Mathf.Clamp(RotationInputs.x, -1, 1) * PitchStrength * ReversingPitchStrength;
                    yaw = Mathf.Clamp(-RotationInputs.y, -1, 1) * YawStrength * ReversingYawStrength;
                    roll = RotationInputs.z * RollStrength * ReversingRollStrength;


                    if (pitch > 0)
                    {
                        pitch *= PitchDownStrMulti;
                    }

                    //wheel colliders are broken, this workaround stops the plane from being 'sticky' when you try to start moving it.
                    if (Speed < .2 && HasWheelColliders && ThrottleInput > 0)
                    {
                        if (VTOLAngle > VTOL90Degrees)
                        { VehicleRigidbody.velocity = VehicleTransform.forward * -.25f; }
                        else
                        { VehicleRigidbody.velocity = VehicleTransform.forward * .25f; }
                    }

                    if (VTOLenabled)
                    {
                        if (!(VTOLAngle == VTOLAngleInput && VTOLAngleInput == 0) || VTOLOnly)//only SetVTOLValues if it'll do anything
                        { SetVTOLValues(); }
                    }
                }
            }
            else
            {
                //brake is always on if the plane is on the ground
                if (!DisablePhysicsAndInputs)
                {
                    if (Taxiing)
                    {
                        StillWindMulti = Mathf.Min(Speed * .1f, 1);
                    }
                    else { StillWindMulti = 1; }
                }
            }

            if (!DisablePhysicsAndInputs)
            {
                //Lerp the inputs for 'engine response', throttle decrease response is slower than increase (EngineSpoolDownSpeedMulti)
                if (EngineOutput < ThrottleInput)
                { EngineOutput = Mathf.Lerp(EngineOutput, ThrottleInput, AccelerationResponse * DeltaTime); }
                else
                { EngineOutput = Mathf.Lerp(EngineOutput, ThrottleInput, AccelerationResponse * EngineSpoolDownSpeedMulti * DeltaTime); }

                float sidespeed = 0;
                float downspeed = 0;
                float SpeedLiftFactor = 0;

                if (PlaneMoving)//optimization
                {
                    //used to create air resistance for updown and sideways if your movement direction is in those directions
                    //to add physics to plane's yaw and pitch, accel angvel towards velocity, and add force to the plane
                    //and add wind
                    sidespeed = Vector3.Dot(AirVel, VehicleTransform.right);
                    downspeed = -Vector3.Dot(AirVel, VehicleTransform.up);

                    PitchDown = (downspeed < 0) ? true : false;//air is hitting plane from above
                    if (PitchDown)
                    {
                        downspeed *= PitchDownLiftMulti;
                        SpeedLiftFactor = Mathf.Min(AirSpeed * AirSpeed * Lift, MaxLift * PitchDownLiftMulti);
                    }
                    else
                    {
                        SpeedLiftFactor = Mathf.Min(AirSpeed * AirSpeed * Lift, MaxLift);
                    }
                    rotlift = Mathf.Min(AirSpeed / RotMultiMaxSpeed, 1);//using a simple linear curve for increasing control as you move faster

                    //thrust vectoring airplanes have a minimum rotation control
                    float minlifttemp = rotlift * Mathf.Min(AoALiftPitch, AoALiftYaw);
                    pitch *= Mathf.Max(PitchThrustVecMulti * ThrustVecGrounded, minlifttemp);
                    yaw *= Mathf.Max(YawThrustVecMulti * ThrustVecGrounded, minlifttemp);
                    roll *= Mathf.Max(RollThrustVecMulti * ThrustVecGrounded, minlifttemp);

                    //rotation inputs are done, now we can set the minimum lift/drag when at high aoa, this should be higher than 0 because if it's 0 you will have 0 drag when at 90 degree AoA.
                    AoALiftPitch = Mathf.Clamp(AoALiftPitch, HighPitchAoaMinLift, 1);
                    AoALiftYaw = Mathf.Clamp(AoALiftYaw, HighYawAoaMinLift, 1);

                    //Lerp the inputs for 'rotation response'
                    LerpedRoll = Mathf.Lerp(LerpedRoll, roll, RollResponse * DeltaTime);
                    LerpedPitch = Mathf.Lerp(LerpedPitch, pitch, PitchResponse * DeltaTime);
                    LerpedYaw = Mathf.Lerp(LerpedYaw, yaw, YawResponse * DeltaTime);
                }
                else
                {
                    VelLift = pitch = yaw = roll = 0;
                }

                if ((PlaneMoving) && OverrideConstantForce == 0)
                {
                    //Create a Vector3 Containing the thrust, and rotate and adjust strength based on VTOL value
                    //engine output is multiplied so that max throttle without afterburner is max strength (unrelated to vtol)
                    Vector3 FinalInputAcc = new Vector3(-sidespeed * SidewaysLift * SpeedLiftFactor * AoALiftYaw,// X Sideways
                            (downspeed * ExtraLift * PitchDownLiftMulti * SpeedLiftFactor * AoALiftPitch),// Y Up
                            0);//Z Forward

                    float GroundEffectAndVelLift = 0;

                    Vector2 Outputs = UnpackThrottles(EngineOutput);
                    float Thrust = (Mathf.Min(Outputs.x)//Throttle
                    * ThrottleStrength
                    + Mathf.Max((Outputs.y), 0)//Afterburner throttle
                    * ThrottleStrengthAB);


                    if (VTOLenabled)
                    {
                        //float thrust = EngineOutput * ThrottleStrength * AfterburnerThrottle * AfterburnerThrustMulti * Atmosphere;
                        float VTOLAngle2 = VTOLMinAngle + (vtolangledif * VTOLAngle);//vtol angle in degrees

                        Vector3 VTOLInputAcc;                                                     //rotate and scale Vector for VTOL thrust
                        if (VTOLOnly)//just use regular thrust strength if vtol only, as there should be no transition to plane flight
                        {
                            VTOLInputAcc = Vector3.RotateTowards(Vector3.forward, VTOL180, VTOLAngle2 * Mathf.Deg2Rad, 0) * Thrust;
                        }
                        else//vehicle can transition from plane-like flight to helicopter-like flight, with different thrust values for each, with a smooth transition between them
                        {
                            float downthrust = Thrust * VTOLThrottleStrengthMulti;
                            VTOLInputAcc = Vector3.RotateTowards(Vector3.forward, VTOL180, VTOLAngle2 * Mathf.Deg2Rad, 0) * Mathf.Lerp(Thrust, Thrust * VTOLThrottleStrengthMulti, VTolAngle90Plus ? VTOLAngle90 : VTOLAngle);
                        }
                        //add ground effect to the VTOL thrust
                        GroundEffectAndVelLift = GroundEffect(true, GroundEffectEmpty.position, -VehicleTransform.TransformDirection(VTOLInputAcc), VTOLGroundEffectStrength, 1);
                        VTOLInputAcc *= GroundEffectAndVelLift;

                        //Add Airplane Ground Effect
                        GroundEffectAndVelLift = GroundEffect(false, GroundEffectEmpty.position, -VehicleTransform.up, GroundEffectStrength, SpeedLiftFactor);
                        //add lift and thrust

                        FinalInputAcc += VTOLInputAcc;
                        FinalInputAcc.y += GroundEffectAndVelLift;
                        FinalInputAcc *= Atmosphere;
                    }
                    else//Simpler version for non-VTOL craft
                    {
                        GroundEffectAndVelLift = GroundEffect(false, GroundEffectEmpty.position, -VehicleTransform.up, GroundEffectStrength, SpeedLiftFactor);

                        FinalInputAcc.y += GroundEffectAndVelLift;
                        FinalInputAcc.z += Thrust;
                        FinalInputAcc *= Atmosphere;
                    }

                    float outputdif = (EngineOutput - EngineOutputLastFrame);
                    float ADVYaw = outputdif * AdverseYaw;
                    float ADVRoll = outputdif * AdverseRoll;
                    EngineOutputLastFrame = EngineOutput;
                    //used to add rotation friction
                    Vector3 localAngularVelocity = transform.InverseTransformDirection(VehicleRigidbody.angularVelocity);


                    //roll + rotational frictions
                    Vector3 FinalInputRot = new Vector3((-localAngularVelocity.x * PitchFriction * rotlift * AoALiftPitch * AoALiftYaw * Atmosphere) - (localAngularVelocity.x * PitchConstantFriction),// X Pitch
                        (-localAngularVelocity.y * YawFriction * rotlift * AoALiftPitch * AoALiftYaw) + ADVYaw * Atmosphere - (localAngularVelocity.y * YawConstantFriction),// Y Yaw
                            ((LerpedRoll + (-localAngularVelocity.z * RollFriction * rotlift * AoALiftPitch * AoALiftYaw)) + ADVRoll * Atmosphere) - (localAngularVelocity.z * RollConstantFriction));// Z Roll

                    //create values for use in fixedupdate (control input and straightening forces)
                    Pitching = ((((VehicleTransform.up * LerpedPitch) + (VehicleTransform.up * downspeed * VelStraightenStrPitch * AoALiftPitch * rotlift)) * Atmosphere));
                    Yawing = ((((VehicleTransform.right * LerpedYaw) + (VehicleTransform.right * -sidespeed * VelStraightenStrYaw * AoALiftYaw * rotlift)) * Atmosphere));

                    VehicleConstantForce.relativeForce = FinalInputAcc;
                    VehicleConstantForce.relativeTorque = FinalInputRot;
                }
                else
                {
                    VehicleConstantForce.relativeForce = CFRelativeForceOverride;
                    VehicleConstantForce.relativeTorque = CFRelativeTorqueOverride;
                }
            }

            SoundBarrier = (-Mathf.Clamp(Mathf.Abs(Speed - 343) / SoundBarrierWidth, 0, 1) + 1) * SoundBarrierStrength;
        }
        else//non-owners need to know these values
        {
            Speed = AirSpeed = CurrentVel.magnitude;//wind speed is local anyway, so just use ground speed for non-owners
            rotlift = Mathf.Min(Speed / RotMultiMaxSpeed, 1);//so passengers can hear the airbrake
            //AirVel = VehicleRigidbody.velocity - Wind;//wind isn't synced so this will be wrong
            //AirSpeed = AirVel.magnitude;
        }
    }
    private void FixedUpdate()
    {
        if (IsOwner)
        {
            float DeltaTime = Time.fixedDeltaTime;
            //lerp velocity toward 0 to simulate air friction
            Vector3 VehicleVel = VehicleRigidbody.velocity;
            VehicleRigidbody.velocity = Vector3.Lerp(VehicleVel, FinalWind * StillWindMulti * Atmosphere, ((((AirFriction + SoundBarrier) * ExtraDrag)) * 90) * DeltaTime);
            //apply pitching using pitch moment
            VehicleRigidbody.AddForceAtPosition(Pitching, PitchMoment.position, ForceMode.Force);//deltatime is built into ForceMode.Force
            //apply yawing using yaw moment
            VehicleRigidbody.AddForceAtPosition(Yawing, YawMoment.position, ForceMode.Force);
            //calc Gs
            float gravity = 9.81f * DeltaTime;
            LastFrameVel.y -= gravity; //add gravity
            AllGs = Vector3.Distance(LastFrameVel, VehicleVel) / gravity;
            GDamageToTake += Mathf.Max((AllGs - MaxGs), 0);

            Vector3 Gs3 = VehicleTransform.InverseTransformDirection(VehicleVel - LastFrameVel);
            VertGs = Gs3.y / gravity;
            LastFrameVel = VehicleVel;
        }
    }
    public void Explode()//all the things players see happen when the vehicle explodes
    {
        EntityControl.dead = true;
        PlayerThrottle = 0;
        ThrottleInput = 0;
        EngineOutput = 0;
        MissilesIncomingHeat = 0;
        MissilesIncomingRadar = 0;
        MissilesIncomingOther = 0;
        VTOLAngle = VTOLDefaultValue;
        VTOLAngleInput = VTOLDefaultValue;
        if (HasAfterburner) { SetAfterburnerOff(); }
        Fuel = FullFuel;
        Atmosphere = 1;//planemoving optimization requires this to be here

        EntityControl.SendEventToExtensions("SFEXT_G_Explode");

        SendCustomEventDelayedSeconds(nameof(ReAppear), RespawnDelay);
        SendCustomEventDelayedSeconds(nameof(NotDead), RespawnDelay + InvincibleAfterSpawn);

        if (IsOwner)
        {
            VehicleRigidbody.velocity = Vector3.zero;
            VehicleRigidbody.angularVelocity = Vector3.zero;
            VehicleRigidbody.drag = 9999;
            VehicleRigidbody.angularDrag = 9999;
            Health = FullHealth;//turns off low health smoke
            Fuel = FullFuel;
            AoALiftPitch = 0;
            AoALiftYaw = 0;
            AngleOfAttack = 0;
            VelLift = VelLiftStart;
            VTOLAngle90 = 0;
            SendCustomEventDelayedSeconds("MoveToSpawn", RespawnDelay - 3);

            EntityControl.SendEventToExtensions("SFEXT_O_Explode");
        }

        //pilot and passengers are dropped out of the plane
        if ((Piloting || Passenger) && !InEditor)
        {
            EntityControl.ExitStation();
        }
    }
    public void ReAppear()
    {
        VehicleAnimator.SetTrigger("reappear");
        VehicleRigidbody.drag = 0;
        VehicleRigidbody.angularDrag = 0;
    }
    public void NotDead()
    {
        Health = FullHealth;
        EntityControl.dead = false;
    }
    public void MoveToSpawn()
    {
        PlayerThrottle = 0;//for editor test mode
        EngineOutput = 0;//^
        //these could get set after death by lag, probably
        MissilesIncomingHeat = 0;
        MissilesIncomingRadar = 0;
        MissilesIncomingOther = 0;
        Health = FullHealth;
        if (InEditor) { VehicleTransform.SetPositionAndRotation(SpawnPos, SpawnRot); }
        VehicleObjectSync.Respawn();//this works if done just locally;
        EntityControl.SendEventToExtensions("SFEXT_O_MoveToSpawn");
    }
    public void TouchDown()
    {
        //Debug.Log("TouchDown");
        Taxiing = true;
        EntityControl.SendEventToExtensions("SFEXT_G_TouchDown");
    }
    public void TouchDownWater()
    {
        //Debug.Log("TouchDownWater");
        LandedOnWater = true;
        Taxiing = true;
        EntityControl.SendEventToExtensions("SFEXT_G_TouchDownWater");
    }
    public void TakeOff()
    {
        //Debug.Log("TakeOff");
        Taxiing = false;
        EntityControl.SendEventToExtensions("SFEXT_G_TakeOff");
    }
    public void SetAfterburnerOn()
    {
        AfterburnerOn = true;
        VehicleAnimator.SetBool(AFTERBURNERON_STRING, true);

        if (IsOwner)
        {
            EntityControl.SendEventToExtensions("SFEXT_O_AfterburnerOn");
        }
    }
    public void SetAfterburnerOff()
    {
        AfterburnerOn = false;

        VehicleAnimator.SetBool(AFTERBURNERON_STRING, false);

        if (IsOwner)
        {
            EntityControl.SendEventToExtensions("SFEXT_O_AfterburnerOff");
        }
    }
    private void ToggleAfterburner()
    {
        if (!AfterburnerOn && ThrottleInput > ThrottleAfterburnerPoint)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetAfterburnerOn));
        }
        else if (AfterburnerOn)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetAfterburnerOff));
        }
    }
    public void SFEXT_O_ReSupply()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ReSupply));
    }
    public void ReSupply()
    {
        ReSupplied = 0;//used to know if other scripts resupplied
        if ((Fuel < FullFuel - 10 || Health != FullHealth))
        {
            ReSupplied++;//used to only play the sound if we're actually repairing/getting ammo/fuel
        }
        EntityControl.SendEventToExtensions("SFEXT_G_ReSupply");//extensions increase the ReSupplied value too

        LastResupplyTime = Time.time;

        if (IsOwner)
        {
            Fuel = Mathf.Min(Fuel + (FullFuel / RefuelTime), FullFuel);
            Health = Mathf.Min(Health + (FullHealth / RepairTime), FullHealth);
        }
        VehicleAnimator.SetTrigger(RESUPPLY_STRING);
    }
    public void SFEXT_O_RespawnButton()//called when using respawn button
    {
        if (!Occupied && !EntityControl.dead)
        {
            Networking.SetOwner(localPlayer, EntityControl.gameObject);
            EntityControl.TakeOwnerShipOfExtensions();
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ResetStatus");
            IsOwner = true;
            Atmosphere = 1;//planemoving optimization requires this to be here
                           //synced variables
            Health = FullHealth;
            Fuel = FullFuel;
            VTOLAngle = VTOLDefaultValue;
            VTOLAngleInput = VTOLDefaultValue;
            VehicleObjectSync.Respawn();//this works if done just locally
            VehicleRigidbody.angularVelocity = Vector3.zero;//editor needs this}
        }
    }
    public void ResetStatus()//called globally when using respawn button
    {
        if (HasAfterburner) { SetAfterburnerOff(); }
        //these two make it invincible and unable to be respawned again for 5s
        EntityControl.dead = true;
        SendCustomEventDelayedSeconds(nameof(NotDead), InvincibleAfterSpawn);
        EntityControl.SendEventToExtensions("SFEXT_G_RespawnButton");
    }
    public void SFEXT_G_BulletHit()
    {
        if (InEditor || IsOwner)
        {
            Health -= 10;
        }
    }
    public void SFEXT_P_PassengerEnter()
    {
        Passenger = true;

        VehicleAnimator.SetBool(LOCALPASSENGER_STRING, true);
        SetPlaneLayerInside();
    }
    public void SFEXT_P_PassengerExit()
    {
        Passenger = false;
        localPlayer.SetVelocity(CurrentVel);
        MissilesIncomingHeat = 0;
        MissilesIncomingRadar = 0;
        MissilesIncomingOther = 0;
        VehicleAnimator.SetInteger("missilesincoming", 0);
        VehicleAnimator.SetBool("localpassenger", false);

        SetPlaneLayerOutside();
    }
    public void SFEXT_O_TakeOwnership()
    {
        IsOwner = true;
        VehicleRigidbody.velocity = CurrentVel;
        VehicleRigidbody.angularDrag = 0;
    }
    public void SFEXT_O_LoseOwnership()
    {
        IsOwner = false;
        //VRChat doesn't set Angular Velocity to 0 when you're not the owner of a rigidbody,
        //causing spazzing, the script handles angular drag it itself, so when we're not owner of the plane, set this value non-zero to stop spazzing
        VehicleRigidbody.angularDrag = .5f;
    }
    public void SFEXT_O_PilotEnter()
    {
        //setting this as a workaround because it doesnt work reliably in Start()
        if (!InEditor)
        {
            if (localPlayer.IsUserInVR()) { InVR = true; }//move me to start when they fix the bug
                                                          //https://feedback.vrchat.com/vrchat-udon-closed-alpha-bugs/p/vrcplayerapiisuserinvr-for-the-local-player-is-not-returned-correctly-when-calle
        }

        EngineOutput = 0;
        ThrottleInput = 0;
        PlayerThrottle = 0;
        VehicleRigidbody.angularDrag = 0;//set to something nonzero when you're not owner to prevent juddering motion on collisions
        VTOLAngleInput = VTOLAngle;

        Piloting = true;
        if (EntityControl.dead) { Health = FullHealth; }//dead is true for the first 5 seconds after spawn, this might help with spontaneous explosions

        VehicleAnimator.SetBool(LOCALPILOT_STRING, true);

        //hopefully prevents explosions when you enter the plane
        VehicleRigidbody.velocity = CurrentVel;
        VertGs = 0;
        AllGs = 0;
        LastFrameVel = CurrentVel;

        SetPlaneLayerInside();
    }
    public void SFEXT_G_PilotEnter()
    {
        Occupied = true;
        VehicleAnimator.SetBool(OCCUPIED_STRING, true);
        EntityControl.dead = false;//Plane stops being invincible if someone gets in, also acts as redundancy incase someone missed the notdead event
    }
    public void SFEXT_G_PilotExit()
    {
        Occupied = false;
        SetAfterburnerOff();
    }
    public void SFEXT_O_PilotExit()
    {
        //zero control values
        roll = 0;
        pitch = 0;
        yaw = 0;
        LerpedPitch = 0;
        LerpedRoll = 0;
        LerpedYaw = 0;
        RotationInputs = Vector3.zero;
        ThrottleInput = 0;
        //reset everything
        Piloting = false;
        Taxiinglerper = 0;
        ThrottleGripLastFrame = false;
        JoystickGripLastFrame = false;
        LTriggerLastFrame = false;
        RTriggerLastFrame = false;
        DoAAMTargeting = false;
        MissilesIncomingHeat = 0;
        MissilesIncomingRadar = 0;
        MissilesIncomingOther = 0;
        VehicleAnimator.SetBool(LOCALPILOT_STRING, false);
        localPlayer.SetVelocity(CurrentVel);

        //set plane's layer back
        SetPlaneLayerOutside();
    }
    public void SetPlaneLayerInside()
    {
        if (PlaneMesh != null)
        {
            Transform[] children = PlaneMesh.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                child.gameObject.layer = OnboardPlaneLayer;
            }
        }
    }
    public void SetPlaneLayerOutside()
    {
        if (PlaneMesh != null)
        {
            Transform[] children = PlaneMesh.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                child.gameObject.layer = Planelayer;
            }
        }
    }
    private void WindAndAoA()
    {
        if (DisablePhysicsAndInputs) { return; }
        Atmosphere = Mathf.Clamp(-(CenterOfMass.position.y / AtmoshpereFadeDistance) + 1 + AtmosphereHeightThing, 0, 1);
        float TimeGustiness = Time.time * WindGustiness;
        float gustx = TimeGustiness + (VehicleTransform.position.x * WindTurbulanceScale);
        float gustz = TimeGustiness + (VehicleTransform.position.z * WindTurbulanceScale);
        FinalWind = Vector3.Normalize(new Vector3((Mathf.PerlinNoise(gustx + 9000, gustz) - .5f), /* (Mathf.PerlinNoise(gustx - 9000, gustz - 9000) - .5f) */0, (Mathf.PerlinNoise(gustx, gustz + 9999) - .5f))) * WindGustStrength;
        FinalWind = (FinalWind + Wind) * Atmosphere;
        AirVel = VehicleRigidbody.velocity - (FinalWind * StillWindMulti);
        AirSpeed = AirVel.magnitude;
        Vector3 VecForward = VehicleTransform.forward;
        AngleOfAttackPitch = Vector3.SignedAngle(VecForward, AirVel, VehicleTransform.right);
        AngleOfAttackYaw = Vector3.SignedAngle(VecForward, AirVel, VehicleTransform.up);

        //angle of attack stuff, pitch and yaw are calculated seperately
        //pitch and yaw each have a curve for when they are within the 'MaxAngleOfAttack' and a linear version up to 90 degrees, which are Max'd (using Mathf.Clamp) for the final result.
        //the linear version is used for high aoa, and is 0 when at 90 degrees, and 1(multiplied by HighAoaMinControl) at 0. When at more than 90 degrees, the control comes back with the same curve but the inputs are inverted. (unless thrust vectoring is enabled) The invert code is elsewhere.
        AoALiftPitch = Mathf.Min(Mathf.Abs(AngleOfAttackPitch) / MaxAngleOfAttackPitch, Mathf.Abs(Mathf.Abs(AngleOfAttackPitch) - 180) / MaxAngleOfAttackPitch);//angle of attack as 0-1 float, for backwards and forwards
        AoALiftPitch = -AoALiftPitch + 1;
        AoALiftPitch = -Mathf.Pow((1 - AoALiftPitch), AoaCurveStrength) + 1;//give it a curve

        float AoALiftPitchMin = Mathf.Min(Mathf.Abs(AngleOfAttackPitch) / 90, Mathf.Abs(Mathf.Abs(AngleOfAttackPitch) - 180) / 90);//linear version to 90 for high aoa
        AoALiftPitchMin = Mathf.Clamp((-AoALiftPitchMin + 1) * HighPitchAoaMinControl, 0, 1);
        AoALiftPitch = Mathf.Clamp(AoALiftPitch, AoALiftPitchMin, 1);

        AoALiftYaw = Mathf.Min(Mathf.Abs(AngleOfAttackYaw) / MaxAngleOfAttackYaw, Mathf.Abs((Mathf.Abs(AngleOfAttackYaw) - 180)) / MaxAngleOfAttackYaw);
        AoALiftYaw = -AoALiftYaw + 1;
        AoALiftYaw = -Mathf.Pow((1 - AoALiftYaw), AoaCurveStrength) + 1;//give it a curve

        float AoALiftYawMin = Mathf.Min(Mathf.Abs(AngleOfAttackYaw) / 90, Mathf.Abs(Mathf.Abs(AngleOfAttackYaw) - 180) / 90);//linear version to 90 for high aoa
        AoALiftYawMin = Mathf.Clamp((-AoALiftPitchMin + 1) * HighYawAoaMinControl, 0, 1);
        AoALiftYaw = Mathf.Clamp(AoALiftYaw, AoALiftYawMin, 1);

        AngleOfAttack = Mathf.Max(Mathf.Abs(AngleOfAttackPitch), Mathf.Abs(AngleOfAttackYaw));
    }
    private float GroundEffect(bool VTOL, Vector3 Position, Vector3 Direction, float GEStrength, float speedliftfac)
    {
        //Ground effect, extra lift caused by air pressure when close to the ground
        RaycastHit GE;
        if (Physics.Raycast(Position, Direction, out GE, GroundEffectMaxDistance, 2065 /* Default, Water and Environment */, QueryTriggerInteraction.Collide))
        {
            float GroundEffect = ((-GE.distance + GroundEffectMaxDistance) / GroundEffectMaxDistance) * GEStrength;
            if (VTOL) { return 1 + GroundEffect; }
            GroundEffect *= ExtraLift;
            VelLift = VelLiftStart + GroundEffect;
            VelLiftMax = Mathf.Max(VelLiftMaxStart, VTOL ? 99999f : GroundEffectLiftMax);
        }
        else//set non-groundeffect'd vel lift values
        {
            if (VTOL) { return 1; }
            VelLift = VelLiftStart;
            VelLiftMax = VelLiftMaxStart;
        }
        return Mathf.Min(speedliftfac * AoALiftPitch * VelLift, VelLiftMax);
    }
    private void SetVTOLValues()
    {
        VTOLAngle = Mathf.MoveTowards(VTOLAngle, VTOLAngleInput, VTOLAngleDivider * Time.smoothDeltaTime);
        float SpeedForVTOL = (Mathf.Min(Speed / VTOLLoseControlSpeed, 1));
        if (VTOLAngle > 0 && SpeedForVTOL != 1 || VTOLOnly)
        {
            if (VTOLOnly)
            {
                VTOLAngle90 = 1;
                PitchThrustVecMulti = 1;
                YawThrustVecMulti = 1;
                RollThrustVecMulti = 1;
            }
            else
            {
                VTOLAngle90 = Mathf.Min(VTOLAngle / VTOL90Degrees, 1);//used to lerp values as vtol angle goes towards 90 degrees instead of max vtol angle which can be above 90

                float SpeedForVTOL_Inverse_xVTOL = ((SpeedForVTOL * -1) + 1) * VTOLAngle90;
                //the thrust vec values are linearly scaled up the slow you go while in VTOL, from 0 at VTOLLoseControlSpeed
                PitchThrustVecMulti = Mathf.Lerp(PitchThrustVecMultiStart, VTOLPitchThrustVecMulti, SpeedForVTOL_Inverse_xVTOL);
                YawThrustVecMulti = Mathf.Lerp(YawThrustVecMultiStart, VTOLYawThrustVecMulti, SpeedForVTOL_Inverse_xVTOL);
                RollThrustVecMulti = Mathf.Lerp(RollThrustVecMultiStart, VTOLRollThrustVecMulti, SpeedForVTOL_Inverse_xVTOL);

                ReversingPitchStrengthZero = 1;
                ReversingYawStrengthZero = 1;
                ReversingRollStrengthZero = 1;
            }

            if (!VTOLAllowAfterburner)
            {
                if (AfterburnerOn)
                { PlayerThrottle = ThrottleAfterburnerPoint; }
            }
        }
        else
        {
            PitchThrustVecMulti = PitchThrustVecMultiStart;
            YawThrustVecMulti = YawThrustVecMultiStart;
            RollThrustVecMulti = RollThrustVecMultiStart;

            ReversingPitchStrengthZero = ReversingPitchStrengthZeroStart;
            ReversingYawStrengthZero = ReversingYawStrengthZeroStart;
            ReversingRollStrengthZero = ReversingRollStrengthZeroStart;
        }
    }
    public void SetTargeted()
    {
        VehicleAnimator.SetTrigger("radarlocked");
    }
    public Vector2 UnpackThrottles(float Throttle)
    {
        //x = throttle amount (0-1), y = afterburner amount (0-1)
        return new Vector2(Mathf.Min(Throttle, ThrottleAfterburnerPoint) * ThrottleNormalizer,
        Mathf.Max((Mathf.Max(Throttle, ThrottleAfterburnerPoint) - ThrottleAfterburnerPoint) * ABNormalizer, 0));
    }
}