using R2API;
using R2API.Networking.Interfaces;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using TILER2;
using UnityEngine;
using UnityEngine.Networking;

namespace ThinkInvisible.Admiral {
    public class OrbitalJumpPadSkill : AdmiralModule<OrbitalJumpPadSkill> {
        [AutoItemConfig("Lifetime of the Orbital Jump Pad deployable.",
            AutoItemConfigFlags.DeferForever | AutoItemConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float skillLifetime {get; private set;} = 20f;

        [AutoItemConfig("Cooldown of Orbital Jump Pad.",
            AutoItemConfigFlags.DeferForever | AutoItemConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float skillRecharge {get; private set;} = 30f;

        [AutoItemConfig("Maximum range of both Orbital Jump Pad terminals.",
            AutoItemConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float skillRange {get; private set;} = 100f;

        public override string configDescription => "Adds the Orbital Jump Pad utility skill variant.";
        public override AutoItemConfigFlags enabledConfigFlags => AutoItemConfigFlags.PreventNetMismatch | AutoItemConfigFlags.DeferForever;

        internal SkillDef setupSkillDef;
        internal SkillDef callSkillDef;

        internal GameObject jumpPadPrefabBase;
        internal GameObject jumpPadPrefabProj1;
        internal GameObject jumpPadPrefabProj2;

        public static Vector3 CalculateJumpPadTrajectory(Vector3 source, Vector3 target, float extraPeakHeight) {
            var deltaPos = target - source;
            var yF = deltaPos.y;
            var yPeak = Mathf.Max(Mathf.Max(yF, 0) + extraPeakHeight, yF, 0);
            //everything will be absolutely ruined if gravity goes in any direction other than -y. them's the breaks.
            var g = -UnityEngine.Physics.gravity.y;
            //calculate initial vertical velocity
            float vY0 = Mathf.Sqrt(2f * g * yPeak);
            //calculate total travel time from vertical velocity
            float tF = Mathf.Sqrt(2)/g * (Mathf.Sqrt(g * (yPeak - yF)) + Mathf.Sqrt(g * yPeak));
            //use total travel time to calculate other velocity components
            var vX0 = deltaPos.x/tF;
            var vZ0 = deltaPos.z/tF;
            return new Vector3(vX0, vY0, vZ0);
        }

        internal override void Setup() {
            base.Setup();
            
            LoadoutAPI.AddSkill(typeof(EntStateCallJumpPad));
            LoadoutAPI.AddSkill(typeof(EntStateSetupJumpPad));

            R2API.Networking.NetworkingAPI.RegisterMessageType<MsgSetJumpPadTarget>();

            UnlockablesAPI.AddUnlockable<AdmiralJumpPadAchievement>(false);
            LanguageAPI.Add("ADMIRAL_JUMPPAD_ACHIEVEMENT_NAME", "Captain: Damn The Torpedoes");
            LanguageAPI.Add("ADMIRAL_JUMPPAD_ACHIEVEMENT_DESCRIPTION", "As Captain, nail a very speedy target with an Orbital Probe.");

            ProjectileCatalog.getAdditionalEntries += ProjectileCatalog_getAdditionalEntries;
            var jppBase = GameObject.Instantiate(Resources.Load<GameObject>("prefabs/networkedobjects/HumanFan"));
            jppBase.transform.localScale = new Vector3(0.75f, 0.125f, 0.75f);
            jppBase.GetComponent<PurchaseInteraction>().enabled = false;
            jppBase.GetComponent<RoR2.Hologram.HologramProjector>().enabled = false;
            jppBase.GetComponent<OccupyNearbyNodes>().enabled = false;
            var jppDecayer = jppBase.AddComponent<CaptainBeaconDecayer>();
            jppDecayer.lifetime = skillLifetime;
            jumpPadPrefabBase = PrefabAPI.InstantiateClone(jppBase, "CaptainJumpPad");

            var jppProj1 = GameObject.Instantiate(Resources.Load<GameObject>("prefabs/projectiles/CaptainAirstrikeProjectile1"));
            var iexp = jppProj1.GetComponent<ProjectileImpactExplosion>();
            iexp.blastDamageCoefficient = 0.1f;
            iexp.blastRadius = 5f;
            iexp.lifetime = 0.5f;
            jppProj1.AddComponent<OrbitalJumpPad1ImpactEventFlag>();
            jumpPadPrefabProj1 = PrefabAPI.InstantiateClone(jppProj1, "CaptainJumpPadProjectile1");

            var jppProj2 = GameObject.Instantiate(Resources.Load<GameObject>("prefabs/projectiles/CaptainAirstrikeProjectile1"));
            var iexp2 = jppProj2.GetComponent<ProjectileImpactExplosion>();
            iexp2.blastDamageCoefficient = 0.05f;
            iexp2.blastRadius = 2.5f;
            iexp2.lifetime = 0.5f;
            jppProj2.AddComponent<OrbitalJumpPad2ImpactEventFlag>();
            jumpPadPrefabProj2 = PrefabAPI.InstantiateClone(jppProj2, "CaptainJumpPadProjectile2");

            var nametoken = "ADMIRAL_JUMPPAD_SKILL_NAME";
            var desctoken = "ADMIRAL_JUMPPAD_SKILL_DESC";
            var namestr = "Orbital Jump Pad";
            LanguageAPI.Add(nametoken, namestr);
            LanguageAPI.Add(desctoken, "Request an Orbital Jump Pad from the <style=cIsUtility>UES Safe Travels</style>. Fire once to set the jump pad, then again to set its target (both within <style=cIsUtility>100 m</style>).");
            
            setupSkillDef = ScriptableObject.CreateInstance<SkillDef>();

            setupSkillDef.activationStateMachineName = "Skillswap";
            setupSkillDef.activationState = LoadoutAPI.StateTypeOf<EntStateSetupJumpPad>();
            setupSkillDef.interruptPriority = EntityStates.InterruptPriority.Skill;
            setupSkillDef.baseRechargeInterval = skillRecharge;
            setupSkillDef.baseMaxStock = 1;
            setupSkillDef.rechargeStock = 1;
            setupSkillDef.isBullets = false;
            setupSkillDef.shootDelay = 0f;
            setupSkillDef.beginSkillCooldownOnSkillEnd = true;
            setupSkillDef.requiredStock = 1;
            setupSkillDef.stockToConsume = 1;
            setupSkillDef.isCombatSkill = false;
            setupSkillDef.noSprint = true;
            setupSkillDef.canceledFromSprinting = true;
            setupSkillDef.mustKeyPress = true;
            setupSkillDef.fullRestockOnAssign = true;

            setupSkillDef.skillName = namestr;
            setupSkillDef.skillNameToken = nametoken;
            setupSkillDef.skillDescriptionToken = desctoken;
            setupSkillDef.icon = Resources.Load<Sprite>("@Admiral:Assets/Admiral/Textures/Icons/icon_AdmiralJumpPadSkill.png");

            LoadoutAPI.AddSkillDef(setupSkillDef);

            callSkillDef = ScriptableObject.CreateInstance<SkillDef>();

            callSkillDef.activationStateMachineName = "Weapon";
            callSkillDef.activationState = LoadoutAPI.StateTypeOf<EntStateCallJumpPad>();
            callSkillDef.interruptPriority = EntityStates.InterruptPriority.PrioritySkill;
            callSkillDef.baseRechargeInterval = 0f;
            callSkillDef.baseMaxStock = 2;
            callSkillDef.rechargeStock = 0;
            callSkillDef.isBullets = false;
            callSkillDef.shootDelay = 0.3f;
            callSkillDef.beginSkillCooldownOnSkillEnd = true;
            callSkillDef.requiredStock = 1;
            callSkillDef.stockToConsume = 1;
            callSkillDef.isCombatSkill = false;
            callSkillDef.noSprint = true;
            callSkillDef.canceledFromSprinting = true;
            callSkillDef.mustKeyPress = true;
            callSkillDef.fullRestockOnAssign = true;
            callSkillDef.dontAllowPastMaxStocks = true;

            callSkillDef.skillName = namestr;
            callSkillDef.skillNameToken = nametoken;
            callSkillDef.skillDescriptionToken = desctoken;
            callSkillDef.icon = Resources.Load<Sprite>("@Admiral:Assets/Admiral/Textures/Icons/icon_AdmiralJumpPadSkill.png");

            LoadoutAPI.AddSkillDef(callSkillDef);
        }

        internal override void Install() {
            base.Install();

            On.RoR2.Projectile.ProjectileImpactExplosion.Detonate += ProjectileImpactExplosion_Detonate;

            var csdf = Resources.Load<SkillFamily>("skilldefs/captainbody/CaptainUtilitySkillFamily");
            csdf.AddVariant(setupSkillDef, "ADMIRAL_JUMPPAD_UNLOCKABLE_ID");
        }

        internal override void Uninstall() {
            base.Uninstall();

            On.RoR2.Projectile.ProjectileImpactExplosion.Detonate -= ProjectileImpactExplosion_Detonate;

            var csdf = Resources.Load<SkillFamily>("skilldefs/captainbody/CaptainUtilitySkillFamily");
            csdf.RemoveVariant(setupSkillDef);
        }

        private void ProjectileImpactExplosion_Detonate(On.RoR2.Projectile.ProjectileImpactExplosion.orig_Detonate orig, ProjectileImpactExplosion self) {
            orig(self);
            if(!NetworkServer.active) return;
            if(self.GetComponent<OrbitalJumpPad1ImpactEventFlag>()) {
                var owner = self.GetComponent<ProjectileController>().owner;
                if(!owner) return;
                var ojph = owner.GetComponent<OrbitalJumpPadDeployTracker>();
                if(!ojph) ojph = owner.AddComponent<OrbitalJumpPadDeployTracker>();
                var nobj = GameObject.Instantiate(jumpPadPrefabBase, self.transform.position, self.transform.rotation);
                ojph.lastPadBase = nobj;
                NetworkServer.Spawn(nobj);
            } else if(self.GetComponent<OrbitalJumpPad2ImpactEventFlag>()) {
                var owner = self.GetComponent<ProjectileController>().owner;
                if(!owner) return;
                var ojph = owner.GetComponent<OrbitalJumpPadDeployTracker>();
                if(!ojph || !ojph.lastPadBase) return;
                var jtraj = CalculateJumpPadTrajectory(ojph.lastPadBase.transform.position, self.transform.position, 5f);
                if(!float.IsNaN(jtraj.y)) {
                    new MsgSetJumpPadTarget(ojph.lastPadBase, jtraj).Send(R2API.Networking.NetworkDestination.Clients);
                    ojph.lastPadBase.GetComponent<ChestBehavior>().Open();
                } else
                    GameObject.Destroy(ojph.lastPadBase);
            }
        }

        private void ProjectileCatalog_getAdditionalEntries(List<GameObject> entries) {
            entries.Add(jumpPadPrefabProj1);
            entries.Add(jumpPadPrefabProj2);
        }
        
        private struct MsgSetJumpPadTarget : INetMessage {
            private GameObject _targetJumpPad;
            private Vector3 _velocity;

            public void Serialize(NetworkWriter writer) {
                writer.Write(_targetJumpPad);
                writer.Write(_velocity);
            }

            public void Deserialize(NetworkReader reader) {
                _targetJumpPad = reader.ReadGameObject();
                _velocity = reader.ReadVector3();
            }

            public void OnReceived() {
                if(!_targetJumpPad) return;
                _targetJumpPad.transform.Find("mdlHumanFan").Find("JumpVolume").gameObject.GetComponent<JumpVolume>().jumpVelocity = _velocity;
            }

            public MsgSetJumpPadTarget(GameObject targetJumpPad, Vector3 velocity) {
                _targetJumpPad = targetJumpPad;
                _velocity = velocity;
            }
        }
    }
    
    public class OrbitalJumpPadDeployTracker : MonoBehaviour {
        public GameObject lastPadBase;
    }
    
	public class OrbitalJumpPad1ImpactEventFlag : MonoBehaviour {}

	public class OrbitalJumpPad2ImpactEventFlag : MonoBehaviour {}


    #region Achievement handling
    public class AdmiralJumpPadAchievement : ModdedUnlockableAndAchievement<CustomSpriteProvider> {
        public override string AchievementIdentifier => "ADMIRAL_JUMPPAD_ACHIEVEMENT_ID";
        public override string UnlockableIdentifier => "ADMIRAL_JUMPPAD_UNLOCKABLE_ID";
        public override string PrerequisiteUnlockableIdentifier => "CompleteMainEnding";
        public override string AchievementNameToken => "ADMIRAL_JUMPPAD_ACHIEVEMENT_NAME";
        public override string AchievementDescToken => "ADMIRAL_JUMPPAD_ACHIEVEMENT_DESCRIPTION";
        public override string UnlockableNameToken => "ADMIRAL_JUMPPAD_SKILL_NAME";
        protected override CustomSpriteProvider SpriteProvider => new CustomSpriteProvider("@Admiral:Assets/Admiral/Textures/Icons/icon_AdmiralJumpPadSkill.png");

        public override bool wantsBodyCallbacks => true;

        int projTestInd1 = -1;
        int projTestInd2 = -1;
        int projTestInd3 = -1;

        public override int LookUpRequiredBodyIndex() {
            return BodyCatalog.FindBodyIndex("CaptainBody");
        }

        public override void OnInstall() {
            base.OnInstall();
            RoR2.GlobalEventManager.onServerDamageDealt += GlobalEventManager_onServerDamageDealt;
            On.RoR2.CharacterBody.Awake += CharacterBody_Awake;
			projTestInd1 = ProjectileCatalog.FindProjectileIndex("CaptainAirstrikeProjectile1");
            projTestInd2 = ProjectileCatalog.FindProjectileIndex("CaptainAirstrikeProjectile2");
            projTestInd3 = ProjectileCatalog.FindProjectileIndex("CaptainAirstrikeProjectile3");
        }

        public override void OnUninstall() {
            base.OnUninstall();
            RoR2.GlobalEventManager.onServerDamageDealt -= GlobalEventManager_onServerDamageDealt;
            On.RoR2.CharacterBody.Awake -= CharacterBody_Awake;
        }

        private void CharacterBody_Awake(On.RoR2.CharacterBody.orig_Awake orig, CharacterBody self) {
            orig(self);
            self.gameObject.AddComponent<AverageSpeedTracker>();
        }

        private void GlobalEventManager_onServerDamageDealt(DamageReport obj) {
            if(!meetsBodyRequirement) return;
            if(!obj.victimBody || !obj.damageInfo.attacker || !obj.damageInfo.inflictor || NetworkUser.readOnlyLocalPlayersList.Count == 0 || obj.damageInfo.attacker != NetworkUser.readOnlyLocalPlayersList[0].GetCurrentBody()?.gameObject) return;
            var projInd = ProjectileCatalog.GetProjectileIndex(obj.damageInfo.inflictor);
            if(projInd != projTestInd1 && projInd != projTestInd2 && projInd != projTestInd3) return;
            var ast = obj.victimBody.GetComponent<AverageSpeedTracker>();
            if(!ast) return;
            var vel = ast.QuerySpeed();
            var projdist = (obj.damageInfo.position - obj.damageInfo.inflictor.transform.position).magnitude;
            if(vel > 20f && projdist < 4f)
                Grant();
        }
    }
    
    public class AverageSpeedTracker : MonoBehaviour {
        private readonly List<Vector3> positions = new List<Vector3>();
        private readonly List<float> deltas = new List<float>();
        public float pollingRate = 0.2f;
        private uint _history = 5;
        public uint history {get{return _history;} set{positions.Clear();deltas.Clear();_history=value;} }

        private float stopwatch = 0f;
        
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by UnityEngine")]
        private void FixedUpdate() {
            stopwatch += Time.fixedDeltaTime;
            if(stopwatch > pollingRate) {
                positions.Add(transform.position);
                deltas.Add(stopwatch);
                if(positions.Count >= _history) {
                    positions.RemoveAt(0);
                    deltas.RemoveAt(0);
                }
                stopwatch = 0f;
            }
        }
        public float QuerySpeed() {
            float totalVel = 0f;
            for(var i = 0; i < positions.Count - 1; i++) {
                totalVel += (positions[i+1] - positions[i]).magnitude / deltas[i+1];
            }
            return totalVel;
        }
    }
    #endregion
}