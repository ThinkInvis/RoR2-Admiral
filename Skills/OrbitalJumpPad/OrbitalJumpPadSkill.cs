using EntityStates;
using R2API;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ThinkInvisible.Admiral {
    public class OrbitalJumpPadHelper : MonoBehaviour {
        public GameObject lastPadBase;
    }

	public class OrbitalJumpPad1ImpactEventFlag : MonoBehaviour {}

	public class OrbitalJumpPad2ImpactEventFlag : MonoBehaviour {}

    public static class OrbitalJumpPadSkill {
        internal static SkillDef setupSkillDef;
        internal static SkillDef callSkillDef;

        internal static GameObject jumpPadPrefabBase;
        internal static GameObject jumpPadPrefabProj1;
        internal static GameObject jumpPadPrefabProj2;

        public static Vector3 CalculateJumpPadTrajectory(Vector3 source, Vector3 target, float extraPeakHeight) {
            float ePHCap = Mathf.Max(extraPeakHeight, 0f);
            var deltaPos = target - source;
            var yF = deltaPos.y;
            var yPeak = ePHCap + yF;
            //everything will be absolutely ruined if gravity goes in any direction other than -y. them's the breaks.
            var g = -UnityEngine.Physics.gravity.y;
            //calculate initial vertical velocity
            float vY0 = Mathf.Sqrt(2f * g * yPeak);
            //calculate total travel time from vertical velocity
            float tF = Mathf.Sqrt(2)/g * (Mathf.Sqrt(g * ePHCap) + Mathf.Sqrt(g * yPeak));
            //use total travel time to calculate other velocity components
            var vX0 = deltaPos.x/tF;
            var vZ0 = deltaPos.z/tF;
            return new Vector3(vX0, vY0, vZ0);
        }

        internal static void Patch() {
            ProjectileCatalog.getAdditionalEntries += ProjectileCatalog_getAdditionalEntries;

            var jppBase = GameObject.Instantiate(Resources.Load<GameObject>("prefabs/networkedobjects/HumanFan"));
            jppBase.transform.localScale = new Vector3(0.5f, 0.125f, 0.5f);
            jppBase.GetComponent<PurchaseInteraction>().enabled = false;
            jppBase.GetComponent<RoR2.Hologram.HologramProjector>().enabled = false;
            jppBase.GetComponent<OccupyNearbyNodes>().enabled = false;
            var jppDecayer = jppBase.AddComponent<CaptainBeaconDecayer>();
            jppDecayer.lifetime = 20;
            /*var chl = jppBase.transform.Find("mdlHumanFan").GetComponent<ChildLocator>();
            chl.FindChild("JumpVolume").gameObject.SetActive(true);
            chl.FindChild("LightBack").gameObject.SetActive(true);
            chl.FindChild("LightFront").gameObject.SetActive(true);*/
            jumpPadPrefabBase = PrefabAPI.InstantiateClone(jppBase, "CaptainJumpPad");

            On.RoR2.Projectile.ProjectileImpactExplosion.Detonate += ProjectileImpactExplosion_Detonate;

            //On.EntityStates.Captain.Weapon.CallAirstrikeBase.ModifyProjectile += On_CABModifyProjectile;
            var jppProj1 = GameObject.Instantiate(Resources.Load<GameObject>("prefabs/projectiles/CaptainAirstrikeProjectile1"));
            var iexp = jppProj1.GetComponent<ProjectileImpactExplosion>();
            iexp.blastDamageCoefficient = 0.1f;
            iexp.blastRadius = 5f;
            jppProj1.AddComponent<OrbitalJumpPad1ImpactEventFlag>();
            jumpPadPrefabProj1 = PrefabAPI.InstantiateClone(jppProj1, "CaptainJumpPadProjectile1");

            var jppProj2 = GameObject.Instantiate(Resources.Load<GameObject>("prefabs/projectiles/CaptainAirstrikeProjectile1"));
            var iexp2 = jppProj2.GetComponent<ProjectileImpactExplosion>();
            iexp2.blastDamageCoefficient = 0.05f;
            iexp2.blastRadius = 2.5f;
            jppProj2.AddComponent<OrbitalJumpPad2ImpactEventFlag>();
            jumpPadPrefabProj2 = PrefabAPI.InstantiateClone(jppProj2, "CaptainJumpPadProjectile2");

            var nametoken = "ADMIRAL_JUMPPAD_NAME";
            var desctoken = "ADMIRAL_JUMPPAD_DESC";
            var namestr = "Orbital Jump Pad";
            LanguageAPI.Add(nametoken, namestr);
            LanguageAPI.Add(desctoken, "Request an Orbital Jump Pad from the <style=cIsUtility>UES Safe Travels</style>. Fire once to set the jump pad, then again to set its target.");
            
            setupSkillDef = ScriptableObject.CreateInstance<SkillDef>();

            setupSkillDef.activationStateMachineName = "Skillswap";
            setupSkillDef.activationState = LoadoutAPI.StateTypeOf<EntStateSetupJumpPad>();
            setupSkillDef.interruptPriority = EntityStates.InterruptPriority.Skill;
            setupSkillDef.baseRechargeInterval = 30f;
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
            setupSkillDef.icon = Resources.Load<SkillDef>("skilldefs/captainbody/PrepAirstrike").icon;

            LoadoutAPI.AddSkillDef(setupSkillDef);

            var csdf = Resources.Load<SkillFamily>("skilldefs/captainbody/CaptainUtilitySkillFamily");
            Array.Resize(ref csdf.variants, csdf.variants.Length + 1);
            csdf.variants[csdf.variants.Length - 1] = new SkillFamily.Variant {
                skillDef = setupSkillDef,
                viewableNode = new ViewablesCatalog.Node(nametoken, false, null),
                unlockableName = ""
            };

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
            callSkillDef.icon = Resources.Load<SkillDef>("skilldefs/captainbody/CallAirstrike").icon;

            LoadoutAPI.AddSkillDef(callSkillDef);

        }

        private static void ProjectileImpactExplosion_Detonate(On.RoR2.Projectile.ProjectileImpactExplosion.orig_Detonate orig, ProjectileImpactExplosion self) {
            orig(self);
            if(!NetworkServer.active) return;
            if(self.GetComponent<OrbitalJumpPad1ImpactEventFlag>()) {
                var owner = self.GetComponent<ProjectileController>().owner;
                if(!owner) return;
                var ojph = owner.GetComponent<OrbitalJumpPadHelper>();
                if(!ojph) ojph = owner.AddComponent<OrbitalJumpPadHelper>();
                var nobj = GameObject.Instantiate(OrbitalJumpPadSkill.jumpPadPrefabBase, self.transform.position, self.transform.rotation);
                NetworkServer.Spawn(nobj);
                ojph.lastPadBase = nobj;
            } else if(self.GetComponent<OrbitalJumpPad2ImpactEventFlag>()) {
                var owner = self.GetComponent<ProjectileController>().owner;
                if(!owner) return;
                var ojph = owner.GetComponent<OrbitalJumpPadHelper>();
                if(!ojph) ojph = owner.AddComponent<OrbitalJumpPadHelper>();
                if(!ojph.lastPadBase) return;
                var jtraj = CalculateJumpPadTrajectory(ojph.lastPadBase.transform.position, self.transform.position, self.transform.position.y > ojph.lastPadBase.transform.position.y ? 5f : 0f);
                ojph.lastPadBase.transform.Find("mdlHumanFan").Find("JumpVolume").gameObject.GetComponent<JumpVolume>().jumpVelocity = jtraj;
                ojph.lastPadBase.GetComponent<ChestBehavior>().Open();
            }
        }

        private static void ProjectileCatalog_getAdditionalEntries(List<GameObject> entries) {
            entries.Add(jumpPadPrefabProj1);
            entries.Add(jumpPadPrefabProj2);
        }
    }
}
