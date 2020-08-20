using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace ThinkInvisible.Admiral {
	class FakeInventory : Inventory {}

	[RequireComponent(typeof(TeamFilter))]
    class ItemWard : NetworkBehaviour {
		private static bool ignoreFakes = false;
		public static GameObject displayPrefab;
		internal static void Patch() {
			On.RoR2.Inventory.GetItemCount += On_InvGetItemCount;
			On.RoR2.CostTypeCatalog.LunarItemOrEquipmentCostTypeHelper.IsAffordable += LunarItemOrEquipmentCostTypeHelper_IsAffordable;
			On.RoR2.CostTypeCatalog.LunarItemOrEquipmentCostTypeHelper.PayCost += LunarItemOrEquipmentCostTypeHelper_PayCost;
			On.RoR2.CostTypeCatalog.LunarItemOrEquipmentCostTypeHelper.PayOne += LunarItemOrEquipmentCostTypeHelper_PayOne;
			On.RoR2.Inventory.HasAtLeastXTotalItemsOfTier += Inventory_HasAtLeastXTotalItemsOfTier;
			On.RoR2.Inventory.GetTotalItemCountOfTier += Inventory_GetTotalItemCountOfTier;
			On.RoR2.ItemStealController.StolenInventoryInfo.StealItem += StolenInventoryInfo_StealItem;
			On.RoR2.RunReport.Generate += RunReport_Generate;
			On.RoR2.ScrapperController.BeginScrapping += ScrapperController_BeginScrapping;
			On.RoR2.ShrineCleanseBehavior.CleanseInventoryServer += ShrineCleanseBehavior_CleanseInventoryServer;
			On.RoR2.ShrineCleanseBehavior.InventoryIsCleansable += ShrineCleanseBehavior_InventoryIsCleansable;
			On.RoR2.Util.GetItemCountForTeam += Util_GetItemCountForTeam;
			IL.RoR2.PickupPickerController.SetOptionsFromInteractor += PickupPickerController_SetOptionsFromInteractor;

            var cClass = typeof(CostTypeCatalog).GetNestedType("<>c", BindingFlags.NonPublic);
			var subMethod = cClass.GetMethod("<Init>g__PayCostItems|5_1", BindingFlags.NonPublic | BindingFlags.Instance);
            MonoMod.RuntimeDetour.HookGen.HookEndpointManager.Modify(subMethod, (Action<ILContext>)gPayCostItemsHook);
			
			var displayPrefabPrefab = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("prefabs/effects/orbeffects/ItemTransferOrbEffect"));
			displayPrefabPrefab.GetComponent<EffectComponent>().enabled = false;
			displayPrefabPrefab.GetComponent<OrbEffect>().enabled = false;
			displayPrefabPrefab.GetComponent<ItemTakenOrbEffect>().enabled = false;

			displayPrefab = displayPrefabPrefab.InstantiateClone("ItemWardDisplay");
		}

		private static void PickupPickerController_SetOptionsFromInteractor(ILContext il) {
			var c = new ILCursor(il);
			int locIndex = -1;
			c.GotoNext(MoveType.After,
				x => x.MatchLdloc(out locIndex),
				x => x.MatchLdfld<ItemDef>("canRemove"));
			c.Emit(OpCodes.Ldarg_1);
			c.Emit(OpCodes.Ldloc_S, (byte)locIndex);
			c.EmitDelegate<Func<bool,Interactor,ItemDef,bool>>((origDoContinue, iac, def) => {
				var retv = origDoContinue;
				ignoreFakes = true;
				if(iac.GetComponent<CharacterBody>().inventory.GetItemCount(def.itemIndex) <= 0) retv = false;
				ignoreFakes = false;
				return retv;
			});
		}

		private static void gPayCostItemsHook(ILContext il) {
			ILCursor c = new ILCursor(il);
			c.GotoNext(x => x.MatchCallvirt<Inventory>("GetItemCount"));
			c.EmitDelegate<Action>(() => {ignoreFakes = true;});
			c.GotoNext(MoveType.After, x => x.MatchCallvirt<Inventory>("GetItemCount"));
			c.EmitDelegate<Action>(() => {ignoreFakes = false;});
		}

		private static int Util_GetItemCountForTeam(On.RoR2.Util.orig_GetItemCountForTeam orig, TeamIndex teamIndex, ItemIndex itemIndex, bool requiresAlive, bool requiresConnected) {
			ignoreFakes = true;
			var retv = orig(teamIndex, itemIndex, requiresAlive, requiresConnected);
			ignoreFakes = false;
			return retv;
		}

		private static bool ShrineCleanseBehavior_InventoryIsCleansable(On.RoR2.ShrineCleanseBehavior.orig_InventoryIsCleansable orig, Inventory inventory) {
			ignoreFakes = true;
			var retv = orig(inventory);
			ignoreFakes = false;
			return retv;
		}

		private static int ShrineCleanseBehavior_CleanseInventoryServer(On.RoR2.ShrineCleanseBehavior.orig_CleanseInventoryServer orig, Inventory inventory) {
			ignoreFakes = true;
			var retv = orig(inventory);
			ignoreFakes = false;
			return retv;
		}

		private static void ScrapperController_BeginScrapping(On.RoR2.ScrapperController.orig_BeginScrapping orig, ScrapperController self, int intPickupIndex) {
			ignoreFakes = true;
			orig(self, intPickupIndex);
			ignoreFakes = false;
		}

		private static RunReport RunReport_Generate(On.RoR2.RunReport.orig_Generate orig, Run run, GameEndingDef gameEnding) {
			ignoreFakes = true;
			var retv = orig(run, gameEnding);
			ignoreFakes = false;
			return retv;
		}

		private static int StolenInventoryInfo_StealItem(On.RoR2.ItemStealController.StolenInventoryInfo.orig_StealItem orig, object self, ItemIndex itemIndex, int maxStackToSteal) {
			ignoreFakes = true;
			var retv = orig(self, itemIndex, maxStackToSteal);
			ignoreFakes = false;
			return retv;
		}

		private static int Inventory_GetTotalItemCountOfTier(On.RoR2.Inventory.orig_GetTotalItemCountOfTier orig, Inventory self, ItemTier itemTier) {
			ignoreFakes = true;
			var retv = orig(self, itemTier);
			ignoreFakes = false;
			return retv;
		}

		private static bool Inventory_HasAtLeastXTotalItemsOfTier(On.RoR2.Inventory.orig_HasAtLeastXTotalItemsOfTier orig, Inventory self, ItemTier itemTier, int x) {
			ignoreFakes = true;
			var retv = orig(self, itemTier, x);
			ignoreFakes = false;
			return retv;
		}

		private static void LunarItemOrEquipmentCostTypeHelper_PayOne(On.RoR2.CostTypeCatalog.LunarItemOrEquipmentCostTypeHelper.orig_PayOne orig, Inventory inventory) {
			ignoreFakes = true;
			orig(inventory);
			ignoreFakes = false;
		}

		private static void LunarItemOrEquipmentCostTypeHelper_PayCost(On.RoR2.CostTypeCatalog.LunarItemOrEquipmentCostTypeHelper.orig_PayCost orig, CostTypeDef costTypeDef, CostTypeDef.PayCostContext context) {
			ignoreFakes = true;
			orig(costTypeDef, context);
			ignoreFakes = false;
		}

		private static bool LunarItemOrEquipmentCostTypeHelper_IsAffordable(On.RoR2.CostTypeCatalog.LunarItemOrEquipmentCostTypeHelper.orig_IsAffordable orig, CostTypeDef costTypeDef, CostTypeDef.IsAffordableContext context) {
			ignoreFakes = true;
			var retv = orig(costTypeDef, context);
			ignoreFakes = false;
			return retv;
		}

		private void Awake() {
			teamFilter = base.GetComponent<TeamFilter>();
		}

		private void OnEnable() {
			if(rangeIndicator)
				rangeIndicator.gameObject.SetActive(true);
		}

		private static int On_InvGetItemCount(On.RoR2.Inventory.orig_GetItemCount orig, Inventory self, ItemIndex itemIndex) {
			var origVal = orig(self, itemIndex);
			if(self is FakeInventory || !ignoreFakes) return origVal;
			var fakeinv = self.gameObject.GetComponent<FakeInventory>();
			if(!fakeinv) return origVal;
			return origVal - fakeinv.GetItemCount(itemIndex);
		}

		private void OnDisable() {
			if(this.rangeIndicator)
				this.rangeIndicator.gameObject.SetActive(false);
			
			trackedInventories.RemoveAll(x => !x || !x.gameObject);
			for(var i = trackedInventories.Count - 1; i >= 0; i--) {
				DeregInv(trackedInventories[i]);
			}
		}

		private void Update() {
			if(this.rangeIndicator) {
				float num = Mathf.SmoothDamp(this.rangeIndicator.localScale.x, this.radius, ref this.rangeIndicatorScaleVelocity, 0.2f);
				this.rangeIndicator.localScale = new Vector3(num, num, num);
			}

			if(cachedRadius != radius) {
				cachedRadius = radius;
				cachedRadSq = cachedRadius * cachedRadius;
			}

			var totalRotateAmount = -0.125f * (2f * Mathf.PI * Time.time);
			var countAngle = 2f*Mathf.PI/displays.Count;
			var displayRadius = cachedRadius/2f;
			var displayHeight = Mathf.Max(cachedRadius/3f, 1f);
			for(int i = 0; i < displays.Count; i++) {
				var target = new Vector3(Mathf.Cos(countAngle*i+totalRotateAmount)*displayRadius, displayHeight, Mathf.Sin(countAngle*i+totalRotateAmount)*displayRadius);
				var dspv = displayVelocities[i];
				displays[i].transform.localPosition = Vector3.SmoothDamp(displays[i].transform.localPosition, target, ref dspv, 1f);
				displayVelocities[i] = dspv;
			}
		}

		private void FixedUpdate() {
			stopwatch += Time.fixedDeltaTime;
			if(stopwatch > updateTickRate) {
				stopwatch = 0f;
				trackedInventories.RemoveAll(x => !x || !x.gameObject);
				var bodies = (CharacterBody[])UnityEngine.GameObject.FindObjectsOfType<CharacterBody>();
				foreach(var body in bodies) {
					if(body.teamComponent.teamIndex != currentTeam) continue;
					if((body.transform.position - transform.position).sqrMagnitude <= cachedRadSq)
						RegObject(body.gameObject);
					else
						DeregObject(body.gameObject);
				}
			}
		}

		private void RegObject(GameObject go) {
			var inv = go.GetComponent<CharacterBody>()?.inventory;
			if(inv && !trackedInventories.Contains(inv)) {
				trackedInventories.Add(inv);
				var fakeInv = inv.gameObject.GetComponent<FakeInventory>();
				if(!fakeInv) fakeInv = inv.gameObject.AddComponent<FakeInventory>();
				foreach(var kvp in itemcounts) {
					inv.GiveItem(kvp.Key, kvp.Value);
					fakeInv.GiveItem(kvp.Key, kvp.Value);
				}
			}
		}

		private void DeregObject(GameObject go) {
			var inv = go.GetComponent<CharacterBody>()?.inventory;
			if(!inv) return;
			DeregInv(inv);
		}

		private void DeregInv(Inventory inv) {
			if(trackedInventories.Contains(inv)) {
				var fakeInv = inv.gameObject.GetComponent<FakeInventory>();
				foreach(var kvp in itemcounts) {
					inv.RemoveItem(kvp.Key, kvp.Value);
					fakeInv.RemoveItem(kvp.Key, kvp.Value);
				}
				trackedInventories.Remove(inv);
			}
		}

		//TODO: figure out removal (not needed yet). probably needs a custom component or separate list.
		public void AddItem(ItemIndex ind) {
			if(!itemcounts.ContainsKey(ind)) itemcounts[ind] = 1;
			else itemcounts[ind]++;
			var display = UnityEngine.Object.Instantiate(displayPrefab, transform.position, transform.rotation);
			display.transform.Find("BillboardBase").Find("PickupSprite").GetComponent<SpriteRenderer>().sprite = ItemCatalog.GetItemDef(ind).pickupIconSprite;
			display.transform.parent = transform;
			displays.Add(display);
			displayVelocities.Add(new Vector3(0, 0, 0));
			trackedInventories.RemoveAll(x => !x || !x.gameObject);
			foreach(var inv in trackedInventories) {
				var fakeInv = inv.gameObject.GetComponent<FakeInventory>();
				inv.GiveItem(ind);
				fakeInv.GiveItem(ind);
			}
		}

		private const float updateTickRate = 1f;
		private float stopwatch = 0f;

		[SyncVar]
		public float radius = 1f;

		private float cachedRadius;
		private float cachedRadSq;

		public Transform rangeIndicator;
		private TeamFilter teamFilter;
		public TeamIndex currentTeam => teamFilter.teamIndex;

		public Dictionary<ItemIndex, int> itemcounts = new Dictionary<ItemIndex, int>();
		private float rangeIndicatorScaleVelocity;

		private List<GameObject> displays = new List<GameObject>();
		private List<Vector3> displayVelocities = new List<Vector3>();
		private List<Inventory> trackedInventories = new List<Inventory>();
    }
}
