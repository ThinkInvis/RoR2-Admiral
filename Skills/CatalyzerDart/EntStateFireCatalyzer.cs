using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using R2API.Utils;
using EntityStates.Captain.Weapon;
using EntityStates;

namespace ThinkInvisible.Admiral {
    public class EntStateFireCatalyzer : FireTazer {
        public override void OnEnter() {
            var oldPrefab = projectilePrefab;
            projectilePrefab = CatalyzerDartSkill.projectilePrefab;
            base.OnEnter();
            projectilePrefab = oldPrefab;
        }
    }
}
