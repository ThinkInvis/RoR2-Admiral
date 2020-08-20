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

        internal static void Patch() {
            ProjectileCatalog.getAdditionalEntries += ProjectileCatalog_getAdditionalEntries;

            var jppBase = GameObject.Instantiate(Resources.Load<GameObject>("prefabs/networkedobjects/HumanFan"));
            jppBase.GetComponent<PurchaseInteraction>().enabled = false;
            jppBase.GetComponent<RoR2.Hologram.HologramProjector>().enabled = false;
            jppBase.GetComponent<OccupyNearbyNodes>().enabled = false;
            var jppDecayer = jppBase.AddComponent<CaptainBeaconDecayer>();
            jppDecayer.lifetime = 30;
            /*var chl = jppBase.transform.Find("mdlHumanFan").GetComponent<ChildLocator>();
            chl.FindChild("JumpVolume").gameObject.SetActive(true);
            chl.FindChild("LightBack").gameObject.SetActive(true);
            chl.FindChild("LightFront").gameObject.SetActive(true);*/
            jumpPadPrefabBase = PrefabAPI.InstantiateClone(jppBase, "CaptainJumpPad");

            On.RoR2.Projectile.ProjectileImpactExplosion.OnProjectileImpact += ProjectileImpactExplosion_OnProjectileImpact;

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

        private static void ProjectileImpactExplosion_OnProjectileImpact(On.RoR2.Projectile.ProjectileImpactExplosion.orig_OnProjectileImpact orig, ProjectileImpactExplosion self, ProjectileImpactInfo pii) {
            Debug.Log("Projectile prehit");
            if(self.GetComponent<OrbitalJumpPad1ImpactEventFlag>()) {
                Debug.Log("Proj1 impacted");
                if(!NetworkServer.active) return;
                var owner = self.GetComponent<ProjectileController>().owner;
                if(!owner) return;
                var ojph = owner.GetComponent<OrbitalJumpPadHelper>();
                if(!ojph) owner.AddComponent<OrbitalJumpPadHelper>();
                ojph.lastPadBase = GameObject.Instantiate(OrbitalJumpPadSkill.jumpPadPrefabBase, pii.estimatedPointOfImpact, Quaternion.FromToRotation(Vector3.up, pii.estimatedImpactNormal));
                Debug.Log("Spawning fan at " + pii.estimatedPointOfImpact.ToString());
                NetworkServer.Spawn(ojph.lastPadBase);
            } else if(self.GetComponent<OrbitalJumpPad2ImpactEventFlag>()) {
                Debug.Log("Proj2 impacted");
                var owner = self.GetComponent<ProjectileController>().owner;
                if(!owner) return;
                var ojph = owner.GetComponent<OrbitalJumpPadHelper>();
                if(!ojph) owner.AddComponent<OrbitalJumpPadHelper>();
                if(!ojph.lastPadBase) return;
                ojph.lastPadBase.transform.Find("mdlHumanFan").Find("JumpVolume").Find("Target").position = pii.estimatedPointOfImpact;
                Debug.Log("Moving fan target to " + pii.estimatedPointOfImpact.ToString());
                ojph.lastPadBase.GetComponent<ChestBehavior>().Open();
            }
            orig(self, pii);
            Debug.Log("Projectile posthit");
        }

        private static void ProjectileCatalog_getAdditionalEntries(List<GameObject> entries) {
            entries.Add(jumpPadPrefabProj1);
            entries.Add(jumpPadPrefabProj2);
        }

        /*private static void On_CABModifyProjectile(On.EntityStates.Captain.Weapon.CallAirstrikeBase.orig_ModifyProjectile orig, EntityStates.Captain.Weapon.CallAirstrikeBase self, ref RoR2.Projectile.FireProjectileInfo fireProjectileInfo) {
            orig(self, ref fireProjectileInfo);
            bool is1 = self is EntStateJumpPad1;
            bool is2 = self is EntStateJumpPad2;
            if(!(is1 || is2)) return;
            //fireProjectileInfo.projectilePrefab = 
            fireProjectileInfo.damage /= 6f;
        }*/
    }
}
