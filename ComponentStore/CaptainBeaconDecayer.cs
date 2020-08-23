using RoR2;
using UnityEngine;

namespace ThinkInvisible.Admiral {
    public class CaptainBeaconDecayer : MonoBehaviour {
        public static float lifetimeDropAdjust {get; internal set;} = 4f;

        public float lifetime = 15f;
        public bool silent = false;
        private float stopwatch = 0f;

        private GenericEnergyComponent energyCpt;

        private void Awake() {
            energyCpt = gameObject.GetComponent<GenericEnergyComponent>();
        }

        private void FixedUpdate() {
            stopwatch += Time.fixedDeltaTime;
            if(energyCpt) {
                energyCpt.capacity = lifetime;
                energyCpt.energy = lifetime - stopwatch + lifetimeDropAdjust;
            }
            if(stopwatch >= lifetime + lifetimeDropAdjust) {
                if(!silent) {
                    EffectManager.SpawnEffect(Resources.Load<GameObject>("prefabs/effects/omnieffect/OmniExplosionVFXEngiTurretDeath"),
                        new EffectData {
                            origin = transform.position,
                            scale = 5f
                        }, true);
                }

                Destroy(gameObject);
            }
        }
    }
}