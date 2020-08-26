﻿using UnityEngine;
using RoR2.Skills;
using RoR2;
using R2API;
using EntityStates.CaptainSupplyDrop;
using UnityEngine.Networking;
using TILER2;
using static TILER2.SkillUtil;

namespace ThinkInvisible.Admiral {
    public class EquipBeacon : AdmiralModule<EquipBeacon> {
        [AutoItemConfig("Lifetime of the Beacon: Rejuvenator deployable.",
            AutoItemConfigFlags.DeferForever | AutoItemConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float skillLifetime {get; private set;} = 20f;

        [AutoItemConfig("Cooldown of Beacon: Rejuvenator.",
            AutoItemConfigFlags.DeferForever | AutoItemConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float skillRecharge {get; private set;} = 50f;

        [AutoItemConfig("Additional fraction of skill recharge rate to provide from the Stimmed buff.",
            AutoItemConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float rechargeRate {get; private set;} = 0.5f;

        public override string configDescription => "Contains config for the Beacon: Resupply submodule of Modules.BeaconRebalance.";
        public override bool addEnabledConfig => false;

        private GameObject rejuvWardPrefab;

        private SkillFamily skillFamily1;
        private SkillFamily skillFamily2;
        private SkillDef origSkillDef;
        internal SkillDef skillDef;
        internal GameObject beaconPrefab;
        public BuffIndex stimmedBuffIndex {get; private set;}

        internal override void Setup() {
            base.Setup();
            skillFamily1 = Resources.Load<SkillFamily>("skilldefs/captainbody/CaptainSupplyDrop1SkillFamily");
            skillFamily2 = Resources.Load<SkillFamily>("skilldefs/captainbody/CaptainSupplyDrop2SkillFamily");

            origSkillDef = Resources.Load<SkillDef>("skilldefs/captainbody/CallSupplyDropEquipmentRestock");
            skillDef = SkillUtil.CloneSkillDef(origSkillDef);

            skillDef.rechargeStock = 1;
            skillDef.baseRechargeInterval = skillRecharge;
            skillDef.skillName = "AdmiralSupplyDropRejuvenator";
            skillDef.skillNameToken = "ADMIRAL_SUPPLY_REJUVENATOR_NAME";
            skillDef.skillDescriptionToken = "ADMIRAL_SUPPLY_REJUVENATOR_DESCRIPTION";
            skillDef.activationState = LoadoutAPI.StateTypeOf<EntStateCallSupplyDropRejuvenator>();

            LanguageAPI.Add(skillDef.skillNameToken, "Beacon: Rejuvenator");
            LanguageAPI.Add(skillDef.skillDescriptionToken,
                "<style=cIsUtility>Temporary beacon</style>. <style=cIsUtility>Buff</style> all nearby allies with <style=cIsUtility>+50% skill recharge rate</style>.");

            LoadoutAPI.AddSkillDef(skillDef);

            stimmedBuffIndex = BuffAPI.Add(new CustomBuff("Stimmed", "textures/itemicons/texSyringeIcon", Color.red, false, false));

            var beaconPrefabPrefab = GameObject.Instantiate(Resources.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainSupplyDrop, EquipmentRestock"));
            beaconPrefabPrefab.GetComponent<ProxyInteraction>().enabled = false;
            beaconPrefabPrefab.GetComponent<GenericEnergyComponent>().enabled = true;
            var eqprestDecayer = beaconPrefabPrefab.AddComponent<CaptainBeaconDecayer>();
            eqprestDecayer.lifetime = skillLifetime;
            beaconPrefabPrefab.GetComponent<EntityStateMachine>().mainStateType = LoadoutAPI.StateTypeOf<EntStateRejuvenatorMainState>();
            beaconPrefab = beaconPrefabPrefab.InstantiateClone("AdmiralSupplyDrop, Rejuvenator");
            GameObject.Destroy(beaconPrefabPrefab);

            //Cobble together an indicator ring from the healing ward prefab
            var chwPrefab = GameObject.Instantiate(Resources.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainHealingWard"));
            chwPrefab.GetComponent<HealingWard>().enabled = false;
            var indic = chwPrefab.transform.Find("Indicator");
            var wardDecayer = chwPrefab.AddComponent<CaptainBeaconDecayer>();
            wardDecayer.lifetime = skillLifetime - CaptainBeaconDecayer.lifetimeDropAdjust; //ward appears after drop
            wardDecayer.silent = true;
            var eqprestWard = chwPrefab.AddComponent<BuffWard>();
            eqprestWard.buffType = stimmedBuffIndex;
            eqprestWard.buffDuration = 1f;
            eqprestWard.radius = 10f;
            eqprestWard.interval = 1f;
            eqprestWard.rangeIndicator = indic;

            indic.Find("IndicatorRing").GetComponent<MeshRenderer>().material.SetColor("_TintColor", new Color(1f, 0.5f, 0f, 1f));
            var chwHsPsRen = indic.Find("HealingSymbols").GetComponent<ParticleSystemRenderer>();
            chwHsPsRen.material.SetTexture("_MainTex", Resources.Load<Texture>("textures/bufficons/texBuffTeslaIcon"));
            chwHsPsRen.material.SetColor("_TintColor", new Color(2f, 0.05f, 0f, 1f));
            chwHsPsRen.trailMaterial.SetColor("_TintColor", new Color(2f, 0.05f, 0f, 1f));

            var chwFlashPsMain = indic.Find("Flashes").GetComponent<ParticleSystem>().main;
            chwFlashPsMain.startColor = new Color(0.5f, 0.25f, 0f, 1f);
            
            rejuvWardPrefab = chwPrefab.InstantiateClone("CaptainRejuvWard");
            GameObject.Destroy(chwPrefab);
        }

        internal override void Install() {
            base.Install();
            skillFamily1.ReplaceVariant(origSkillDef, skillDef);
            skillFamily2.ReplaceVariant(origSkillDef, skillDef);
            On.RoR2.Skills.SkillDef.OnFixedUpdate += On_SkillDefFixedUpdate;
        }

        internal override void Uninstall() {
            base.Uninstall();
            skillFamily1.ReplaceVariant(skillDef, origSkillDef);
            skillFamily2.ReplaceVariant(skillDef, origSkillDef);
            On.RoR2.Skills.SkillDef.OnFixedUpdate -= On_SkillDefFixedUpdate;
        }

        private void On_SkillDefFixedUpdate(On.RoR2.Skills.SkillDef.orig_OnFixedUpdate orig, RoR2.Skills.SkillDef self, GenericSkill skillSlot) {
            if(skillSlot.characterBody.HasBuff(stimmedBuffIndex))
                skillSlot.RunRecharge(Time.fixedDeltaTime * rechargeRate);
            orig(self, skillSlot);
        }

        public class EntStateCallSupplyDropRejuvenator : EntityStates.Captain.Weapon.CallSupplyDropEquipmentRestock {
            public override void OnEnter() {
                Debug.Log(supplyDropPrefab);
                supplyDropPrefab = EquipBeacon.instance.beaconPrefab;
                Debug.Log(supplyDropPrefab);
                base.OnEnter();
                Debug.Log(supplyDropPrefab);
            }
        }

        public class EntStateRejuvenatorMainState : EquipmentRestockMainState {
            public override void OnEnter() {
                base.OnEnter();
                if(!NetworkServer.active) return;
			    var buffZoneInstance = UnityEngine.Object.Instantiate<GameObject>(EquipBeacon.instance.rejuvWardPrefab, outer.commonComponents.transform.position, outer.commonComponents.transform.rotation);
			    buffZoneInstance.GetComponent<TeamFilter>().teamIndex = teamFilter.teamIndex;
			    NetworkServer.Spawn(buffZoneInstance);
            }

            public override Interactability GetInteractability(Interactor activator) {
                return Interactability.Disabled;
            }

            public override bool shouldShowEnergy => true;
        }
    }
}
