# Admiral Changelog

**2.5.1**

- Prefab setup is now more mod-compatible in many instances (prefabs will no longer awaken during plugin load).
- Updated R2API dependency to 4.3.21.
- Updated TILER2 dependency to 7.1.0.

**2.5.0**

- Balance pass.
	- Beacon: Special Order:
		- Now provides 6 items + 3 per stage by default (was 5 items + 1 per stage).
		- Now evenly distributes its item selection by category (damage/healing/utility). Can be disabled in config.
	- Beacon: Rejuvenator:
		- Is now an interactable with unlimited non-renewable-per-player charges. Can be configured back to old behavior, or to have a limited number of shared charges.
	- Valiant Blaster:
		- Nerfed charge shot damage (2400% --> 1800%).
- Fixed causing an extraneous language reload during game load.
- Added Risk Of Options integration to all config entries.
- Fixed achievements/unlockables (now uses new vanilla system instead of R2API.UnlockableAPI).
- Updated for latest RoR2 version.
- Updated R2API dependency to 4.3.5 (still outdated, but less so).
- Updated TILER2 dependency to 7.0.1.

**2.4.0**

- Temporary beacons now have a default-disabled config to prevent them from replacing vanilla skills, with which they can now coexist.
- All temporary beacon skills are now named T.Beacon instead of Beacon to differentiate them from vanilla skills.
- Added T.Beacon: Stasis, which time-freezes enemies temporarily (no movement incl. falling, no damage in or out).
- Experimental balance change: Orbital Jump Pad now has 2 base stock, double restock, 30 --> 50 sec cooldown, 100 --> 80 m range.
	- Added a config option to revert the 2 stock/double restock portion of this change.
- Updated TILER2 dependency to 5.0.3.

**2.3.3**

- Compatibility update for Risk of Rain 2 Expansion 1 (SotV).
- Updated R2API dependency to 4.0.11 (fixing an ancient incompatibility with R2API 3.0.43 in the process).
- Updated BepInEx dependency to 5.4.1902.
- Updated TILER2 dependency to 5.0.2.

**2.3.2**

- BeaconRebalance CDR resistance no longer prevents beacons from recharging normally when ramped all the way up.
- BeaconRebalance now provides separate config options for CDR and restock resistance.
- Some BeaconRebalance config options are no longer flagged as DeferForever and can be changed while the game is running.

**2.3.1**

- Maintenance for RoR2 updates: Anniversary through PC Patch v1.1.1.4.

**2.3.0**

- Implements changes from TILER2 3.0.0.

**2.2.3**

- Added a missing R2API submodule dependency.

**2.2.2**

- Implements changes from TILER2 2.2.3.
	- Beacon: Special Order will no longer select items which are in FakeInventory.blacklist.

**2.2.1**

- Charging Valiant Blaster now completely prevents sprinting (whether through an autosprint mod or mashing the sprint key).

**2.2.0**

- Added variant Primary skill + unlock achievement: Valiant Blaster!

**2.1.1**

- Updated for RoR2 1.0.1.1 and R2API 2.5.14.
	- Fixed OrbitalSkillsAnywhere not causing any changes.
	- Fixed OrbitalJumpPad detonation effects (jump pad spawn/target set) not working.
	- Fixed BeaconRebalance not preventing skill overrides to UsedUpSkillDef.
- Fixed ShotgunRebalance pellet count config not applying.
- Fixed Beacon: Special Order being able to hack T1 chests and shrines and increase its ItemWard item count on completion.
- Fixed Beacon: Special Order display radius being ~50% smaller than the actual radius.

**2.1.0**

- Post-playtest balance/QoL update for Catalyzer Dart and Orbital Jump Pad:
- Orbital Jump Pad felt awkward to use for several reasons, and its VFX was bugged. Several other design TODOs have also been resolved.
	- Orbital Jump Pad and its target now take 0.5s to land, down from 2s.
	- Orbital Jump Pads now display an arc towards their destination (can be disabled clientside).
	- Stepping into an Orbital Jump Pad now provides fall damage protection until you land or hit something (same as Acrid jump).
	- Orbital Jump Pads are now limited to two per player at a time. The oldest one will be destroyed early if a third is placed.
	- Orbital Jump Pad particles are now properly scaled down along with the fan model.
- Catalyzer Dart's damage was underwhelming, partially due to the scarcity and low duration of most DoTs -- and the opportunity cost of losing the non-DoTs. Since these changes are default-config-only, they won't automatically apply if you've already installed the mod.
	- Buffed default Catalyzer Dart DoT damage from 150% to 300%.
	- Buffed default Catalyzer Dart non-DoT damage from 1x200% to 1x500%.

**2.0.2**

- Fixed temporary beacons missing VFX/SFX.
- Fixed temporary beacons not appearing for multiplayer clients (hopefully).
- Removed some stray debug logging.

**2.0.1**

- Fixed a hard crash caused by Shocked lightning hits.

**2.0.0**

- Refactored into a proper module system. Large swathes of the mod can now be disabled and/or configured, and should have multiplayer sync (bugs notwithstanding).
- Added dependency to TILER2.
- Migrated FakeInventory and ItemWard to TILER2.
- Beacon: Special Order is now compatible with TinkersSatchel's Mostly Tame Mimic.

**1.5.3**

- Orbital Jump Pad now works correctly in multiplayer.
- Beacon: Special Order now properly displays floating items for multiplayer clients.
- For modders: Major breaking changes to ItemWard and FakeInventory. These may be migrated to TILER2 in the near future.

**1.5.2**

- Fixed a bug that was causing Acrid's special ability to error out... somehow.
- Added a config option for disabling Beacon overrides (ALL changes except usability in Hidden Realms).
- Bumped R2API dependency to v2.5.7.

**1.5.1**

- Loosened requirements for both unlock achievements.
- Beacons are now 50% resistant to cooldown reduction and completely resistant to max stock increase.
- Reduced Beacon: Rejuvenator cooldown to compensate for CDR resistance.

**1.5.0**

- Added variant Secondary skill + unlock achievement: Catalyzer Dart!
- Added an achievement for unlocking Orbital Jump Pad.
- Added custom icon to Orbital Jump Pad.
- Minor tweak to make Orbital Jump Pad trajectory more reliable.
- Beacon: Shocking now has greatly increased fire rate (1 / 1.5 s).
- Internal: ItemWard now supports item removal.

**1.4.0**

- Added variant Utility skill: Orbital Jump Pad!
- All beacons now use the original beacons' energy indicator as a lifetime indicator.
- Added some extra zombie protection to Beacon: Special Order (original patch wasn't enough).

**1.3.1**

- Beacon: Special Order can no longer give items to enemies.
- Beacon: Special Order can no longer give items to dead bodies caused by beacon impact (which doesn't work and could cause console errors + lower itemcount on the beacon).
- Fixed shock health threshold override not being aggressive enough.

**1.3.0**

- Beacon: Hacking has been replaced with Beacon: Special Order! Provides 5 random, temporary items (+1 per stage cleared) from the Tier 1 chest drop list.

**1.2.0**

- Fixed inability to disable Vulcan Shotgun autofire.
- Beacon: Rejuvenator now has a range indicator.
- Slightly buffed Beacon: Rejuvenator range to bring it in line with existing beacons (7 m --> 10 m).
- Beacon lifetime is now extended to account for the ~4 sec drop animation.

**1.1.0**

- Implemented language token overrides (R2API bug was patched).
- Reduced Vulcan Shotgun pellet count from 8 to 6.
- Removed damage falloff from Vulcan Shotgun.
- Minor project reorganization.

**1.0.0**

- Initial version. Implements unlimited beacons with cooldown+lifetime, the Rejuvenator beacon as a replacement for Resupply, a special AoE max-health-as-damage component to the Shocked status, a tweak to Shocked which greatly increases its stunbreak threshold, and Vulcan Shotgun autofire.
- Late notes as of 1.1.0: Initial version also made orbital skills usable in hidden realms (except bazaar).