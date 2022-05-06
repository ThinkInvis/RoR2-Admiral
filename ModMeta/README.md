# Admiral

## Description

A sweeping overhaul for Captain. This mod is split into several mostly-independent modules, including:

### Vulcan Shotgun Rebalance

Removes damage falloff from Vulcan Shotgun, making the charged fire mode more useful; but also reduces its pellet count from 8 to 6, improving balance when compared with Admiral's buffs to the rest of Captain's kit.

Configurable:
- Pellet count. Defaults to 6.

### Vulcan Shotgun Autofire

Causes Vulcan Shotgun to autofire under client authority (doesn't need multiplayer sync).

Configurable:
- Dynamic time between full charge and autofire, scaled with attack speed. Defaults to 20%.
- Fixed minimum time between full charge and autofire. Defaults to 0 seconds.

### Orbital Skills Everywhere

Makes Orbital Probe and Beacons (whether new or original) usable in every Hidden Realm, except for Bazaar.

### Shock Status Tweaks

Greatly increases the damage threshold for breaking the Shocked status, to the point where it's effectively removed. Also causes Shocked to deal ~20% of the victim's maximum health per second as damage to its allies within 15 meters, targeting and firing randomly.

Configurable:
- Fire chance per frame, damage, proc coefficient, and range of Shocked AoE. Defaults to 3.3% chance, 2% max health, 0.1 proc coefficient, 15 m range.
- Whether to apply the stunbreak threshold tweak. Defaults to enabled.

### Valiant Blaster

A new Primary skill variant (same slot as Vulcan Shotgun). Rapidly fires a combo of up to 3 slow-moving explosive orbs for 1x500%, 1x500%, and 1x800% damage. Fully-charged shots move faster, have much larger blast radius, and deal 1x1800% damage. Firing a 3rd or charged shot will push you backwards, then force a stand-still reload. Waiting for the combo to timeout performs a faster, mobile reload.

Unlock by getting 600 TOTAL hits with Vulcan Shotgun.

Configurable:
- Whether reload speed increases with attack speed.
- Minimum time required to fully charge (allows firing uncharged shots with high attack speed). Defaults to 0.5 sec.

### Catalyzer Dart

A new Secondary skill variant (same slot as Power Tazer). Fires a dart which removes all debuffs from an enemy and deals 300% of the remaining DoT damage, plus 1x500% base damage for each non-DoT debuff.

Unlock by getting 6 Shocked kills originating from the same enemy. *Cannot be unlocked while Shock Status Tweaks is disabled*.

Configurable:
- Cooldown of the Catalyzer Dart skill. Defaults to 8 sec.
- Fraction of remaining DoT damage dealt. Defaults to 300%.
- Fraction of base damage dealt per non-DoT debuff. Defaults to 1x500%.

### Orbital Jump Pad

A new Utility skill variant (same slot as Orbital Probe). Summons a jump pad and its target within a limited range, displaying a preview of the jump's trajectory. Anyone, friend or foe, can trigger the jump pad within its lifetime. Stepping onto an Orbital Jump Pad provides fall damage protection until landing or hitting something. Provides 2 stock at a time to allow for a return path or extra mobility as needed -- choose wisely.

Unlock by near-directly hitting a fast-moving target with an Orbital Probe.

Configurable:
- Cooldown of the Orbital Jump Pad skill. Defaults to 50 sec.
- Lifetime of jump pads. Defaults to 20 sec.
- Maximum range of the Orbital Jump Pad skill. Defaults to 80 m.
- Whether to have 2 base stock and recharge, versus 1. Defaults to enabled.
- Whether to show trajectory previews (clientside). Defaults to enabled.

### Beacon Rebalance

Makes beacons no longer limited to one use per stage. Instead, they have individual cooldowns and lifetimes (latter indicated using the energy meter). Also adds a 50% resistance to cooldown reduction (incl. Bandolier, Brainstalks...), and complete resistance to max stock increases.

Configurable:
- Strength of cooldown reduction resistance. Defaults to 50%.
- Whether to keep vanilla beacons available. Defaults to disabled.

#### Beacon: Healing

Configurable:
- Cannot be individually disabled.
- Cooldown and lifetime. Defaults to 20 sec, 40 sec.

#### Beacon: Shocking

Now fires once every 0.95 seconds (up from Lots Slower) to compensate for the lower uptime. Will also benefit from Shock Status Tweaks if enabled.

Configurable:
- Cannot be individually disabled.
- Cooldown and lifetime. Defaults to 8 sec, 24 sec.
- Time between pulses. Defaults to 0.95 sec.

#### Beacon: Resupply

REPLACED with Beacon: Rejuvenator. Beacon: Rejuvenator can be interacted with to gain the new non-renewable Stimmed buff, which provides +50% skill recharge rate. Will also recharge beacons, so Rejuvenator's own cooldown is higher to compensate.

Configurable:
- Cannot be individually disabled.
- Cooldown and lifetime. Defaults to 20 sec, 50 sec.
- Additional skill recharge rate provided by Stimmed. Defaults to 50%.
- Option to revert to old behavior of being a buff ward instead of an interactable dispenser.
- Option to limit number of shared charges to 3 (infinite by default).

#### Beacon: Hacking

REPLACED with Beacon: Special Order. Beacon: Special Order gives all allies standing nearby a set of temporary items rolled on the basic category chest tables, starting with 6 and increasing by 3 for every stage cleared. These items cannot be used in 3D Printers nor in Scrappers, and they're removed when the beacon breaks down or when you leave its range.

Configurable:
- Cannot be individually disabled.
- Cooldown and lifetime. Defaults to 20 sec, 40 sec.
- Radius of the ItemWard. Defaults to 10 m.
- Number of items provided on the first stage. Defaults to 6.
- Number of items provided per additional stage cleared. Defaults to 3.
- Rarity of the items provided. Defaults to identical to a basic chest, sans equipment (80 common : 20 uncommon : 1 rare).
- Whether to try to evenly distribute items across Damage/Healing/Utility categories.

#### Beacon: Stasis

New skill which does not replace a vanilla Beacon. Beacon: Stasis time-freezes all nearby enemies, preventing them from moving, falling, dealing damage, or taking damage. Does not work on enemies with freeze resistance.

Configurable:
- Cannot be individually disabled.
- Cooldown and lifetime. Defaults to 15 sec, 40 sec.
- Radius of the stasis zone. Defaults to 15 m.

## Issues/TODO

- Causes a stray explosion sound during game startup.
- Loadout selection of new skills is not remembered between launches of the game.
- Valiant Blaster needs unique firing animations.
- Catalyzer Dart could do with some visual changes.
- Trying to find a way to allow canceling beacon/probe by recasting the skill.
- T.Beacon: Stasis uses placeholder icons, and is a little loud + too close to freeze VFX.
- See the GitHub repo for more!

## Changelog

The 5 latest updates are listed below. For a full changelog, see: https://github.com/ThinkInvis/RoR2-Admiral/blob/master/changelog.md

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