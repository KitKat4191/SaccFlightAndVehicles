
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SAV_BombController : UdonSharpBehaviour
    {
        public UdonSharpBehaviour BombLauncherControl;
        [Tooltip("Bomb will explode after this time")]
        public float MaxLifetime = 40;
        [Tooltip("Maximum liftime of bomb is randomized by +- this many seconds on appearance")]
        public float MaxLifetimeRadnomization = 2f;
        [Tooltip("How long to wait to destroy the gameobject after it has exploded, (explosion sound/animation must finish playing)")]
        public float ExplosionLifeTime = 10;
        [Tooltip("Play a random one of these explosion sounds")]
        public AudioSource[] ExplosionSounds;
        [Tooltip("Play a random one of these explosion sounds when hitting water")]
        public AudioSource[] WaterExplosionSounds;
        [Tooltip("Bomb flies forward with this much extra speed, can be used to make guns/shells")]
        public float LaunchSpeed = 0;
        [Tooltip("Spawn bomb at a random angle up to this number")]
        public float AngleRandomization = 1;
        [Tooltip("Distance from plane to enable the missile's collider, to prevent bomb from colliding with own plane")]
        public float ColliderActiveDistance = 30;
        [Tooltip("How much the bomb's nose is pushed towards direction of movement")]
        public float StraightenFactor = .1f;
        [Tooltip("Amount of drag bomb has when moving horizontally/vertically")]
        public float AirPhysicsStrength = .1f;
        private Animator BombAnimator;
        private SaccEntity EntityControl;
        private Rigidbody VehicleRigid;
        private Rigidbody BombRigid;
        [System.NonSerializedAttribute] public bool Exploding = false;
        private bool ColliderActive = false;
        private CapsuleCollider BombCollider;
        private Transform VehicleCenterOfMass;
        private bool hitwater;
        private bool IsOwner;
        private bool initialized;
        private int LifeTimeExplodesSent;
        private bool ColliderAlwaysActive;
        private void Initialize()
        {
            initialized = true;
            EntityControl = (SaccEntity)BombLauncherControl.GetProgramVariable("EntityControl");
            if (EntityControl) { VehicleCenterOfMass = EntityControl.CenterOfMass; }
            BombCollider = GetComponent<CapsuleCollider>();
            BombRigid = GetComponent<Rigidbody>();
            VehicleRigid = EntityControl.VehicleRigidbody;
            transform.rotation = Quaternion.Euler(new Vector3(transform.rotation.eulerAngles.x + (Random.Range(0, AngleRandomization)), transform.rotation.eulerAngles.y + (Random.Range(-(AngleRandomization / 2), (AngleRandomization / 2))), transform.rotation.eulerAngles.z));
            BombAnimator = GetComponent<Animator>();
            ColliderAlwaysActive = ColliderActiveDistance == 0;
        }
        public void AddLaunchSpeed()
        {
            BombRigid.velocity += transform.forward * LaunchSpeed;
        }
        private void OnEnable()
        {
            if (!initialized) { Initialize(); }
            if (ColliderAlwaysActive || !VehicleCenterOfMass) { BombCollider.enabled = true; ColliderActive = true; }
            else { ColliderActive = false; }
            if (EntityControl && EntityControl.InEditor) { IsOwner = true; }
            else
            { IsOwner = (bool)BombLauncherControl.GetProgramVariable("IsOwner"); }
            SendCustomEventDelayedSeconds(nameof(LifeTimeExplode), MaxLifetime + Random.Range(-MaxLifetimeRadnomization, MaxLifetimeRadnomization));
            LifeTimeExplodesSent++;
            SendCustomEventDelayedFrames(nameof(AddLaunchSpeed), 1);//doesn't work if done this frame
        }
        void LateUpdate()
        {
            if (!ColliderActive)
            {
                if (Vector3.Distance(BombRigid.position, VehicleRigid.position) > ColliderActiveDistance)
                {
                    BombCollider.enabled = true;
                    ColliderActive = true;
                }
            }
            if (Exploding) return;
        }
        void FixedUpdate()
        {
            float sidespeed = Vector3.Dot(BombRigid.velocity, transform.right);
            float downspeed = Vector3.Dot(BombRigid.velocity, transform.up);
            BombRigid.AddRelativeTorque(new Vector3(-downspeed, sidespeed, 0) * StraightenFactor, ForceMode.Acceleration);
            BombRigid.AddRelativeForce(new Vector3(-sidespeed * AirPhysicsStrength, -downspeed * AirPhysicsStrength, 0), ForceMode.Acceleration);
        }
        public void LifeTimeExplode()
        {
            //prevent the delayed event from a previous life causing explosion
            if (LifeTimeExplodesSent == 1)
            {
                if (!Exploding && gameObject.activeSelf)//active = not in pool
                { hitwater = false; Explode(); }
            }
            LifeTimeExplodesSent--;
        }
        public void MoveBackToPool()
        {
            BombAnimator.WriteDefaultValues();
            gameObject.SetActive(false);
            transform.SetParent(BombLauncherControl.transform);
            BombCollider.enabled = false;
            if (VehicleCenterOfMass) { ColliderActive = false; }
            BombRigid.constraints = RigidbodyConstraints.None;
            BombRigid.angularVelocity = Vector3.zero;
            transform.localPosition = Vector3.zero;
            Exploding = false;
        }
        private void OnCollisionEnter(Collision other)
        { if (!Exploding) { hitwater = false; Explode(); } }
        private void OnTriggerEnter(Collider other)
        {
            if (other && other.gameObject.layer == 4 /* water */)
            {
                if (!Exploding)
                {
                    hitwater = true;
                    Explode();
                }
            }
        }
        private void Explode()
        {
            if (BombRigid)
            {
                BombRigid.constraints = RigidbodyConstraints.FreezePosition;
                BombRigid.velocity = Vector3.zero;
            }
            Exploding = true;
            if (hitwater && WaterExplosionSounds.Length > 0)
            {
                int rand = Random.Range(0, WaterExplosionSounds.Length);
                WaterExplosionSounds[rand].pitch = Random.Range(.94f, 1.2f);
                WaterExplosionSounds[rand].Play();
            }
            else
            {
                if (ExplosionSounds.Length > 0)
                {
                    int rand = Random.Range(0, ExplosionSounds.Length);
                    ExplosionSounds[rand].pitch = Random.Range(.94f, 1.2f);
                    ExplosionSounds[rand].Play();
                }
            }
            BombCollider.enabled = false;
            if (IsOwner)
            { BombAnimator.SetTrigger("explodeowner"); }
            else { BombAnimator.SetTrigger("explode"); }
            BombAnimator.SetBool("hitwater", hitwater);
            SendCustomEventDelayedSeconds(nameof(MoveBackToPool), ExplosionLifeTime);
        }
    }
}