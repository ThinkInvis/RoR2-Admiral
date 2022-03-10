using R2API;
using R2API.Networking.Interfaces;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using System.Collections.Generic;
using TILER2;
using UnityEngine;
using UnityEngine.Networking;

namespace ThinkInvisible.Admiral {
    public class OrbitalJumpPadSkill : T2Module<OrbitalJumpPadSkill> {
        [AutoConfig("Lifetime of the Orbital Jump Pad deployable.",
            AutoConfigFlags.DeferForever | AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float skillLifetime {get; private set;} = 20f;

        [AutoConfig("Cooldown of Orbital Jump Pad.",
            AutoConfigFlags.DeferForever | AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float skillRecharge {get; private set;} = 30f;

        [AutoConfig("Maximum range of both Orbital Jump Pad terminals.",
            AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float skillRange {get; private set;} = 100f;

        [AutoConfig("If true, arcs previewing Orbital Jump Pad trajectory will appear.")]
        public bool showArcs {get; private set;} = true;

        [AutoConfig("If true, Orbital Jump Pad will have a base stock of two and recharge two at once.",
            AutoConfigFlags.DeferForever | AutoConfigFlags.PreventNetMismatch)]
        public bool doubleStock { get; private set; } = false;

        public override string enabledConfigDescription => "Adds the Orbital Jump Pad utility skill variant.";
        public override AutoConfigFlags enabledConfigFlags => AutoConfigFlags.PreventNetMismatch | AutoConfigFlags.DeferForever;

        internal SkillDef setupSkillDef;
        internal SkillDef callSkillDef;

        internal UnlockableDef unlockable;

        internal GameObject jumpPadPrefabBase;
        internal GameObject jumpPadPrefabProj1;
        internal GameObject jumpPadPrefabProj2;

        public override void SetupAttributes() {
            base.SetupAttributes();
            
            var callEntState = ContentAddition.AddEntityState<EntStateCallJumpPad>(out _);
            var setupEntState = ContentAddition.AddEntityState<EntStateSetupJumpPad>(out _);

            R2API.Networking.NetworkingAPI.RegisterMessageType<MsgSetJumpPadTarget>();

            unlockable = UnlockableAPI.AddUnlockable<AdmiralJumpPadAchievement>();
            LanguageAPI.Add("ADMIRAL_JUMPPAD_ACHIEVEMENT_NAME", "Captain: Damn The Torpedoes");
            LanguageAPI.Add("ADMIRAL_JUMPPAD_ACHIEVEMENT_DESCRIPTION", "As Captain, nail a very speedy target with an Orbital Probe.");

            var jppBase = GameObject.Instantiate(LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/HumanFan"));
            jumpPadPrefabBase = PrefabAPI.InstantiateClone(ModifyJumpPadPrefab(jppBase), "CaptainJumpPad", true);

            var jppProj1 = GameObject.Instantiate(LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/CaptainAirstrikeProjectile1"));
            jumpPadPrefabProj1 = PrefabAPI.InstantiateClone(ModifyAirstrike1Prefab(jppProj1), "CaptainJumpPadProjectile1", true);
            ContentAddition.AddProjectile(jumpPadPrefabProj1);

            var jppProj2 = GameObject.Instantiate(LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/CaptainAirstrikeProjectile1"));
            jumpPadPrefabProj2 = PrefabAPI.InstantiateClone(ModifyAirstrike2Prefab(jppProj2), "CaptainJumpPadProjectile2", true);
            ContentAddition.AddProjectile(jumpPadPrefabProj2);

            var nametoken = "ADMIRAL_JUMPPAD_SKILL_NAME";
            var desctoken = "ADMIRAL_JUMPPAD_SKILL_DESC";
            var namestr = "Orbital Jump Pad";
            LanguageAPI.Add(nametoken, namestr);
            LanguageAPI.Add(desctoken, "Request an Orbital Jump Pad from the <style=cIsUtility>UES Safe Travels</style>. Fire once to set the jump pad, then again to set its target (both within <style=cIsUtility>100 m</style>).");
            
            setupSkillDef = ScriptableObject.CreateInstance<SkillDef>();

            setupSkillDef.activationStateMachineName = "Skillswap";
            setupSkillDef.activationState = setupEntState;
            setupSkillDef.interruptPriority = EntityStates.InterruptPriority.Skill;
            setupSkillDef.baseRechargeInterval = skillRecharge;
            setupSkillDef.baseMaxStock = doubleStock ? 2 : 1;
            setupSkillDef.rechargeStock = doubleStock ? 2 : 1;
            setupSkillDef.beginSkillCooldownOnSkillEnd = true;
            setupSkillDef.requiredStock = 1;
            setupSkillDef.stockToConsume = 1;
            setupSkillDef.isCombatSkill = false;
            setupSkillDef.cancelSprintingOnActivation = true;
            setupSkillDef.canceledFromSprinting = true;
            setupSkillDef.mustKeyPress = true;
            setupSkillDef.fullRestockOnAssign = true;

            setupSkillDef.skillName = namestr;
            setupSkillDef.skillNameToken = nametoken;
            setupSkillDef.skillDescriptionToken = desctoken;
            setupSkillDef.icon = AdmiralPlugin.resources.LoadAsset<Sprite>("Assets/Admiral/Textures/Icons/icon_AdmiralJumpPadSkill.png");

            ContentAddition.AddSkillDef(setupSkillDef);

            callSkillDef = ScriptableObject.CreateInstance<SkillDef>();

            callSkillDef.activationStateMachineName = "Weapon";
            callSkillDef.activationState = callEntState;
            callSkillDef.interruptPriority = EntityStates.InterruptPriority.PrioritySkill;
            callSkillDef.baseRechargeInterval = 0f;
            callSkillDef.baseMaxStock = 2;
            callSkillDef.rechargeStock = 0;
            callSkillDef.beginSkillCooldownOnSkillEnd = true;
            callSkillDef.requiredStock = 1;
            callSkillDef.stockToConsume = 1;
            callSkillDef.isCombatSkill = false;
            callSkillDef.cancelSprintingOnActivation = true;
            callSkillDef.canceledFromSprinting = true;
            callSkillDef.mustKeyPress = true;
            callSkillDef.fullRestockOnAssign = true;
            callSkillDef.dontAllowPastMaxStocks = true;

            callSkillDef.skillName = namestr;
            callSkillDef.skillNameToken = nametoken;
            callSkillDef.skillDescriptionToken = desctoken;
            callSkillDef.icon = AdmiralPlugin.resources.LoadAsset<Sprite>("Assets/Admiral/Textures/Icons/icon_AdmiralJumpPadSkill.png");

            ContentAddition.AddSkillDef(callSkillDef);
        }

        private void ProjectileExplosion_DetonateServer(On.RoR2.Projectile.ProjectileExplosion.orig_DetonateServer orig, ProjectileExplosion self) {
            orig(self);
            if(!NetworkServer.active) return;
            if(self.GetComponent<OrbitalJumpPad1ImpactEventFlag>()) {
                var owner = self.GetComponent<ProjectileController>().owner;
                if(!owner) return;
                var ojph = owner.GetComponent<OrbitalJumpPadDeployTracker>();
                if(!ojph) ojph = owner.AddComponent<OrbitalJumpPadDeployTracker>();
                var nobj = GameObject.Instantiate(jumpPadPrefabBase, self.transform.position, self.transform.rotation);
                if(ojph.prevPadBase) GameObject.Destroy(ojph.prevPadBase);
                ojph.prevPadBase = ojph.lastPadBase;
                ojph.lastPadBase = nobj;
                NetworkServer.Spawn(nobj);
            } else if(self.GetComponent<OrbitalJumpPad2ImpactEventFlag>()) {
                var owner = self.GetComponent<ProjectileController>().owner;
                if(!owner) return;
                var ojph = owner.GetComponent<OrbitalJumpPadDeployTracker>();
                if(!ojph || !ojph.lastPadBase) return;
                var jtraj = CalculateJumpPadTrajectory(ojph.lastPadBase.transform.position, self.transform.position, 5f);
                if(!float.IsNaN(jtraj.Item1.y)) {
                    new MsgSetJumpPadTarget(ojph.lastPadBase, jtraj.Item1, self.transform.position).Send(R2API.Networking.NetworkDestination.Clients);
                    ojph.lastPadBase.GetComponent<ChestBehavior>().Open();
                } else
                    GameObject.Destroy(ojph.lastPadBase);
            }
        }

        public override void Install() {
            base.Install();

            On.RoR2.JumpVolume.OnTriggerStay += JumpVolume_OnTriggerStay;
            On.RoR2.Projectile.ProjectileExplosion.DetonateServer += ProjectileExplosion_DetonateServer;

            var csdf = LegacyResourcesAPI.Load<SkillFamily>("skilldefs/captainbody/CaptainUtilitySkillFamily");
            csdf.AddVariant(setupSkillDef, unlockable);
        }

        public override void Uninstall() {
            base.Uninstall();

            On.RoR2.JumpVolume.OnTriggerStay -= JumpVolume_OnTriggerStay;
            On.RoR2.Projectile.ProjectileExplosion.DetonateServer -= ProjectileExplosion_DetonateServer;

            var csdf = LegacyResourcesAPI.Load<SkillFamily>("skilldefs/captainbody/CaptainUtilitySkillFamily");
            csdf.RemoveVariant(setupSkillDef);
        }
        
        public static (Vector3, float) CalculateJumpPadTrajectory(Vector3 source, Vector3 target, float extraPeakHeight) {
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
            return (new Vector3(vX0, vY0, vZ0), tF);
        }

        public static Vector3[] CalculateJumpPadPoints(Vector3 source, Vector3 target, float extraPeakHeight, int displayPointsToGenerate) {
            var deltaPos = target - source;
            var yF = deltaPos.y;
            var yPeak = Mathf.Max(Mathf.Max(yF, 0) + extraPeakHeight, yF, 0);
            var g = -UnityEngine.Physics.gravity.y;
            float vY0 = Mathf.Sqrt(2f * g * yPeak);
            float tF = Mathf.Sqrt(2)/g * (Mathf.Sqrt(g * (yPeak - yF)) + Mathf.Sqrt(g * yPeak));
            var vX0 = deltaPos.x/tF;
            var vZ0 = deltaPos.z/tF;

            var velocity = new Vector3(vX0, vY0, vZ0);

            //calculate points for display
            var generatedPoints = new Vector3[displayPointsToGenerate];
            var timePerPoint = tF/(displayPointsToGenerate - 1f);
            for(int i = 0; i < displayPointsToGenerate; i++) {
                generatedPoints[i] = Trajectory.CalculatePositionAtTime(source, velocity, timePerPoint * i);
            }

            return generatedPoints;
        }

        private GameObject ModifyJumpPadPrefab(GameObject origPrefab) {
            ///////
            ////vfx
            //main obj scale
            origPrefab.transform.localScale = new Vector3(0.75f, 0.125f, 0.75f);
            
            //particle systems scale
            var jvolTsf = origPrefab.transform.Find("mdlHumanFan").Find("JumpVolume");
            var loopPsysTsf = jvolTsf.Find("LoopParticles").Find("Particle System");
            loopPsysTsf.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            var loopPsysMain = loopPsysTsf.gameObject.GetComponent<ParticleSystem>().main;
            loopPsysMain.startSpeed = new ParticleSystem.MinMaxCurve(6f);

            var loopFdustTsf = jvolTsf.Find("LoopParticles").Find("ForwardDust");
            loopFdustTsf.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            var loopFdustMain = loopFdustTsf.gameObject.GetComponent<ParticleSystem>().main;
            loopFdustMain.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 12f);

            var actFdustTsf = jvolTsf.Find("ActivateParticles").Find("ForwardDust");
            actFdustTsf.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            var actFdustMain = actFdustTsf.gameObject.GetComponent<ParticleSystem>().main;
            actFdustMain.startSpeed = new ParticleSystem.MinMaxCurve(0.75f, 15f);

            var actCircleTsf = jvolTsf.Find("ActivateParticles").Find("Circle");
            actCircleTsf.localScale = new Vector3(0.25f, 0.25f, 0.125f);

            //add LineRenderer
            var lineRen = jvolTsf.gameObject.AddComponent<LineRenderer>();
            //var lineRenMtlSnagFrom = GameObject.Instantiate(LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainHealingWard"));
            //lineRen.material = lineRenMtlSnagFrom.transform.Find("Indicator").Find("IndicatorRing").gameObject.GetComponent<MeshRenderer>().material;
            lineRen.material = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<Material>("materials/matBlueprintsOk"));
            //GameObject.Destroy(lineRenMtlSnagFrom);
            lineRen.material.SetColor("_TintColor", new Color(2f, 0.2f, 10f, 3f));
            lineRen.positionCount = 32;
            List<Keyframe> kfmArr = new List<Keyframe>();
            for(int i = 0; i < lineRen.positionCount; i++) {
                kfmArr.Add(new Keyframe(i/32f, (1f-MiscUtil.Wrap(i/8f,0f,1f))*0.875f));
            }
            lineRen.widthCurve = new AnimationCurve{keys=kfmArr.ToArray()};
            //lineRen.startColor = new Color(0.25f, 0.01f, 2f, 0.4f);
            //lineRen.endColor = new Color(0.25f, 0.01f, 2f, 0.1f);

            ////////////
            ////behavior
            origPrefab.GetComponent<PurchaseInteraction>().enabled = false;
            origPrefab.GetComponent<RoR2.Hologram.HologramProjector>().enabled = false;
            origPrefab.GetComponent<OccupyNearbyNodes>().enabled = false;
            jvolTsf.gameObject.AddComponent<TemporaryFallProtectionProvider>();
            var jppDecayer = origPrefab.AddComponent<CaptainBeaconDecayer>();
            jppDecayer.lifetime = skillLifetime;

            return origPrefab;
        }
        
        private GameObject ModifyAirstrike1Prefab(GameObject origPrefab) {
            var iexp = origPrefab.GetComponent<ProjectileImpactExplosion>();
            iexp.blastDamageCoefficient = 0.1f;
            iexp.blastRadius = 5f;
            iexp.lifetime = 0.5f;
            origPrefab.AddComponent<OrbitalJumpPad1ImpactEventFlag>();
            return origPrefab;
        }

        private GameObject ModifyAirstrike2Prefab(GameObject origPrefab) {
            var iexp = origPrefab.GetComponent<ProjectileImpactExplosion>();
            iexp.blastDamageCoefficient = 0.05f;
            iexp.blastRadius = 2.5f;
            iexp.lifetime = 0.5f;
            origPrefab.AddComponent<OrbitalJumpPad2ImpactEventFlag>();
            return origPrefab;
        }
        
        private void JumpVolume_OnTriggerStay(On.RoR2.JumpVolume.orig_OnTriggerStay orig, JumpVolume self, Collider other) {
            orig(self, other);
            var fpProv = self.GetComponent<TemporaryFallProtectionProvider>();
            var cb = other.GetComponent<CharacterBody>();
            if(!fpProv || !cb || !cb.characterMotor) return;
            var fpRecep = other.GetComponent<TemporaryFallDamageProtection>();
            if(!fpRecep) fpRecep = other.gameObject.AddComponent<TemporaryFallDamageProtection>();
            fpRecep.Apply();
        }

        private struct MsgSetJumpPadTarget : INetMessage {
            private GameObject _targetJumpPad;
            private Vector3 _velocity;
            private Vector3 _targetPos;

            public void Serialize(NetworkWriter writer) {
                writer.Write(_targetJumpPad);
                writer.Write(_velocity);
                writer.Write(_targetPos);
            }

            public void Deserialize(NetworkReader reader) {
                _targetJumpPad = reader.ReadGameObject();
                _velocity = reader.ReadVector3();
                _targetPos = reader.ReadVector3();
            }

            public void OnReceived() {
                if(!_targetJumpPad) return;
                var jumpVolObj = _targetJumpPad.transform.Find("mdlHumanFan").Find("JumpVolume").gameObject;
                jumpVolObj.GetComponent<JumpVolume>().jumpVelocity = _velocity;
                var lRen = jumpVolObj.GetComponent<LineRenderer>();
                if(OrbitalJumpPadSkill.instance.showArcs)
                    lRen.SetPositions(CalculateJumpPadPoints(jumpVolObj.transform.position, _targetPos, 5f, 32));
                else
                    lRen.enabled = false;
            }

            public MsgSetJumpPadTarget(GameObject targetJumpPad, Vector3 velocity, Vector3 targetPos) {
                _targetJumpPad = targetJumpPad;
                _velocity = velocity;
                _targetPos = targetPos;
            }
        }
    }
    
    public class TemporaryFallDamageProtection : NetworkBehaviour {
        private CharacterBody attachedBody;
        bool hasProtection = false;
        bool disableNextFrame = false;
        bool disableN2f = false;
        private void FixedUpdate() {
            if(disableN2f) {
                disableN2f = false;
                disableNextFrame = true;
            } else if(disableNextFrame) {
                disableNextFrame = false;
                hasProtection = false;
                attachedBody.bodyFlags &= ~CharacterBody.BodyFlags.IgnoreFallDamage;
            } else if(hasProtection) {
                if(attachedBody.characterMotor.Motor.GroundingStatus.IsStableOnGround && !attachedBody.characterMotor.Motor.LastGroundingStatus.IsStableOnGround) {
                    disableN2f = true;
                }
            }
        }
        private void Awake() {
            attachedBody = GetComponent<CharacterBody>();
            attachedBody.characterMotor.onMovementHit += CharacterMotor_onMovementHit;
        }

        public void Apply() {
            hasProtection = true;
            attachedBody.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
        }

        private void CharacterMotor_onMovementHit(ref CharacterMotor.MovementHitInfo movementHitInfo) {
            if(hasProtection && !disableN2f && !disableNextFrame) {
                disableN2f = true;
            }
        }
    }
    public class TemporaryFallProtectionProvider : MonoBehaviour {}

    public class OrbitalJumpPadDeployTracker : MonoBehaviour {
        public GameObject lastPadBase;
        public GameObject prevPadBase;
    }
    
	public class OrbitalJumpPad1ImpactEventFlag : MonoBehaviour {}
	public class OrbitalJumpPad2ImpactEventFlag : MonoBehaviour {}

    #region Achievement handling
    public class AdmiralJumpPadAchievement : RoR2.Achievements.BaseAchievement, IModdedUnlockableDataProvider {
        public string AchievementIdentifier => "ADMIRAL_JUMPPAD_ACHIEVEMENT_ID";
        public string UnlockableIdentifier => "ADMIRAL_JUMPPAD_UNLOCKABLE_ID";
        public string PrerequisiteUnlockableIdentifier => "CompleteMainEnding";
        public string AchievementNameToken => "ADMIRAL_JUMPPAD_ACHIEVEMENT_NAME";
        public string AchievementDescToken => "ADMIRAL_JUMPPAD_ACHIEVEMENT_DESCRIPTION";
        public string UnlockableNameToken => "ADMIRAL_JUMPPAD_SKILL_NAME";

        public override bool wantsBodyCallbacks => true;

        public Sprite Sprite => AdmiralPlugin.resources.LoadAsset<Sprite>("Assets/Admiral/Textures/Icons/icon_AdmiralJumpPadSkill.png");

        public System.Func<string> GetHowToUnlock => () => Language.GetStringFormatted("UNLOCK_VIA_ACHIEVEMENT_FORMAT", new[] {
            Language.GetString(AchievementNameToken), Language.GetString(AchievementDescToken)});

        public System.Func<string> GetUnlocked => () => Language.GetStringFormatted("UNLOCKED_FORMAT", new[] {
            Language.GetString(AchievementNameToken), Language.GetString(AchievementDescToken)});

        int projTestInd1 = -1;
        int projTestInd2 = -1;
        int projTestInd3 = -1;

        public AdmiralJumpPadAchievement() : base() {
        }

        public override BodyIndex LookUpRequiredBodyIndex() {
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