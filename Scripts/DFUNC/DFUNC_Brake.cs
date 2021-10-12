﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class DFUNC_Brake : UdonSharpBehaviour
{
    [SerializeField] private UdonSharpBehaviour SAVControl;
    [Tooltip("Looping sound to play while brake is active")]
    [SerializeField] private AudioSource Airbrake_snd;
    [SerializeField] private Animator BrakeAnimator;
    [Tooltip("Because you have to hold the break, and the keyboardcontrols script can only send events, this option is here.")]
    [SerializeField] private KeyCode KeyboardControl = KeyCode.B;
    private bool UseLeftTrigger = false;
    [System.NonSerializedAttribute] private float BrakeInput;
    private Rigidbody VehicleRigidbody;
    private bool HasAirBrake;
    [SerializeField] private float AirbrakeStrength = 4f;
    [SerializeField] private float GroundBrakeStrength = 6f;
    [Tooltip("Water brake functionality requires that floatscript is being used")]
    [SerializeField] private float WaterBrakeStrength = 1f;
    [SerializeField] private bool NoPilotAlwaysGroundBrake = true;
    [SerializeField] private float GroundBrakeSpeed = 40f;
    //other functions can set this +1 to disable breaking
    [System.NonSerializedAttribute] public int DisableGroundBrake = 0;
    private SaccEntity EntityControl;
    private float BrakeStrength;
    private int BRAKE_STRING = Animator.StringToHash("brake");
    private bool Braking;
    private bool BrakingLastFrame;
    private float LastDrag = 0;
    private float AirbrakeLerper;
    private float NonLocalActiveDelay;//this var is for adding a min delay for disabling for non-local users to account for lag
    private bool Selected;
    private bool IsOwner;
    private bool Piloting;
    private float NextUpdateTime;
    public void DFUNC_LeftDial() { UseLeftTrigger = true; }
    public void DFUNC_RightDial() { UseLeftTrigger = false; }
    public void SFEXT_L_EntityStart()
    {
        EntityControl = (SaccEntity)SAVControl.GetProgramVariable("EntityControl");
        VehicleRigidbody = EntityControl.GetComponent<Rigidbody>();
        HasAirBrake = AirbrakeStrength != 0;
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        if (localPlayer != null && !localPlayer.isMaster)
        { gameObject.SetActive(false); }
        else
        { gameObject.SetActive(true); }
    }
    public void DFUNC_Selected()
    {
        Selected = true;
    }
    public void DFUNC_Deselected()
    {
        BrakeInput = 0;
        Selected = false;
    }
    public void SFEXT_O_PilotEnter()
    {
        Piloting = true;
        if (!NoPilotAlwaysGroundBrake)
        {
            if ((bool)SAVControl.GetProgramVariable("Floating"))
            {
                BrakeStrength = WaterBrakeStrength;
            }
            else if ((bool)SAVControl.GetProgramVariable("Taxiing"))
            {
                BrakeStrength = GroundBrakeStrength;
            }
        }
    }
    public void SFEXT_O_PilotExit()
    {
        Piloting = false;
        BrakeInput = 0;
        RequestSerialization();
        Selected = false;
        if (!NoPilotAlwaysGroundBrake)
        { BrakeStrength = 0; }
    }
    public void SFEXT_G_Explode()
    {
        BrakeInput = 0;
        BrakeAnimator.SetFloat(BRAKE_STRING, 0);
    }
    public void SFEXT_O_TakeOwnership()
    {
        gameObject.SetActive(true);
    }
    public void SFEXT_O_LoseOwnership()
    {
        gameObject.SetActive(false);
    }
    public void EnableForAnimation()
    {
        if (!(bool)SAVControl.GetProgramVariable("IsOwner"))
        {
            gameObject.SetActive(true);
            NonLocalActiveDelay = 3;
        }
    }
    public void DisableForAnimation()
    {
        BrakeAnimator.SetFloat(BRAKE_STRING, 0);
        BrakeInput = 0;
        AirbrakeLerper = 0;
        if (Airbrake_snd)
        {
            Airbrake_snd.pitch = 0;
            Airbrake_snd.volume = 0;
        }
        gameObject.SetActive(false);
    }
    public void SFEXT_G_TouchDownWater()
    {
        if ((bool)SAVControl.GetProgramVariable("Piloting") || NoPilotAlwaysGroundBrake)
        {
            BrakeStrength = WaterBrakeStrength;
        }
    }
    public void SFEXT_G_TouchDown()
    {
        if ((bool)SAVControl.GetProgramVariable("Piloting") || NoPilotAlwaysGroundBrake)
        {
            BrakeStrength = GroundBrakeStrength;
        }
    }
    private void Update()
    {
        float DeltaTime = Time.deltaTime;
        if ((bool)SAVControl.GetProgramVariable("IsOwner"))
        {
            float Speed = (float)SAVControl.GetProgramVariable("Speed");
            Vector3 CurrentVel = (Vector3)SAVControl.GetProgramVariable("CurrentVel");
            bool Taxiing = (bool)SAVControl.GetProgramVariable("Taxiing");
            if ((bool)SAVControl.GetProgramVariable("Piloting"))
            {
                float KeyboardBrakeInput = 0;
                float VRBrakeInput = 0;

                if (Selected)
                {
                    float Trigger;
                    if (UseLeftTrigger)
                    { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                    else
                    { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }

                    VRBrakeInput = Trigger;
                }

                if (Input.GetKey(KeyboardControl))
                {
                    KeyboardBrakeInput = 1;
                }
                BrakeInput = Mathf.Max(VRBrakeInput, KeyboardBrakeInput);
                if (Taxiing)
                {
                    //ground brake checks if vehicle is on top of a rigidbody, and if it is, brakes towards its speed rather than zero
                    //does not work if owner of vehicle does not own the rigidbody 
                    Rigidbody gdhr = (Rigidbody)SAVControl.GetProgramVariable("GDHitRigidbody");
                    if (gdhr)
                    {
                        float RBSpeed = ((Vector3)SAVControl.GetProgramVariable("CurrentVel") - gdhr.velocity).magnitude;
                        if (BrakeInput > 0 && RBSpeed < GroundBrakeSpeed * BrakeInput && DisableGroundBrake == 0)
                        {
                            VehicleRigidbody.velocity = Vector3.MoveTowards(VehicleRigidbody.velocity, gdhr.GetPointVelocity(EntityControl.CenterOfMass.position), BrakeInput * BrakeStrength * DeltaTime);
                        }
                    }
                    else
                    {
                        if (BrakeInput > 0 && Speed < GroundBrakeSpeed * BrakeInput && DisableGroundBrake == 0)
                        {
                            VehicleRigidbody.velocity = Vector3.MoveTowards(VehicleRigidbody.velocity, Vector3.zero, BrakeInput * BrakeStrength * DeltaTime);
                            // VehicleRigidbody.velocity += -CurrentVel.normalized * BrakeInput * GroundBrakeStrength * DeltaTime;
                        }
                    }
                }
                if (!HasAirBrake)
                {
                    BrakeInput = 0;
                }
                //remove the drag added last frame to add the new value for this frame
                float extradrag = (float)SAVControl.GetProgramVariable("ExtraDrag");
                float newdrag = (AirbrakeStrength * BrakeInput);
                float dragtoadd = -LastDrag + newdrag;
                extradrag += dragtoadd;
                LastDrag = newdrag;
                SAVControl.SetProgramVariable("ExtraDrag", extradrag);

                //send events to other users to tell them to enable the script so they can see the animation
                Braking = BrakeInput > .02f;
                if (Braking)
                {
                    if (!BrakingLastFrame)
                    {
                        if (Airbrake_snd && !Airbrake_snd.isPlaying) { Airbrake_snd.Play(); }
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "EnableForAnimation");
                    }
                    if (Time.time > NextUpdateTime)
                    {
                        RequestSerialization();
                        NextUpdateTime = Time.time + .4f;
                    }
                }
                else
                {
                    if (BrakingLastFrame)
                    {
                        float brk = BrakeInput;
                        BrakeInput = 0;
                        RequestSerialization();
                        BrakeInput = brk;
                    }
                }
                if (AirbrakeLerper < .03 && BrakeInput < .03)
                {
                    if (Airbrake_snd && Airbrake_snd.isPlaying) { Airbrake_snd.Stop(); }
                }
                BrakingLastFrame = Braking;
            }
            else
            {
                //outside of vehicle, ground brake always max
                Rigidbody gdhr = null;
                { gdhr = (Rigidbody)SAVControl.GetProgramVariable("GDHitRigidbody"); }
                if (gdhr)
                {
                    float RBSpeed = ((Vector3)SAVControl.GetProgramVariable("CurrentVel") - gdhr.velocity).magnitude;
                    if (Taxiing && RBSpeed < GroundBrakeSpeed && DisableGroundBrake == 0)
                    {
                        VehicleRigidbody.velocity = Vector3.MoveTowards(VehicleRigidbody.velocity, gdhr.GetPointVelocity(EntityControl.CenterOfMass.position), GroundBrakeStrength * DeltaTime);
                    }
                }
                else
                {
                    if (Taxiing && Speed < GroundBrakeSpeed && DisableGroundBrake == 0)
                    {
                        VehicleRigidbody.velocity = Vector3.MoveTowards(VehicleRigidbody.velocity, Vector3.zero, GroundBrakeStrength * DeltaTime);
                    }
                }
            }
        }
        else
        {
            //this object is enabled for non-owners only while animating
            NonLocalActiveDelay -= DeltaTime;
            if (NonLocalActiveDelay < 0 && AirbrakeLerper < 0.01)
            {
                DisableForAnimation();
                return;
            }
        }
        AirbrakeLerper = Mathf.Lerp(AirbrakeLerper, BrakeInput, 2f * DeltaTime);
        BrakeAnimator.SetFloat(BRAKE_STRING, AirbrakeLerper);
        if (Airbrake_snd)
        {
            Airbrake_snd.pitch = AirbrakeLerper * .2f + .9f;
            Airbrake_snd.volume = AirbrakeLerper * (float)SAVControl.GetProgramVariable("rotlift");
        }
    }
}
