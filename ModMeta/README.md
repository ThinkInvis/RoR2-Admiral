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

### Catalyzer Dart

A new Secondary skill variant (same slot as Power Tazer). Fires a dart which removes all debuffs from an enemy and deals 300% of the remaining DoT damage, plus 1x500% base damage for each non-DoT debuff.

Unlock by getting 6 Shocked kills originating from the same enemy. *Cannot be unlocked while Shock Status Tweaks is disabled*.

Configurable:
- Cooldown of the Catalyzer Dart skill. Defaults to 8 sec.
- Fraction of remaining DoT damage dealt. Defaults to 300%.
- Fraction of base damage dealt per non-DoT debuff. Defaults to 1x500%.

### Orbital Jump Pad

A new Utility skill variant (same slot as Orbital Probe). Summons a jump pad and its target within a limited range, displaying a preview of the jump's trajectory. Anyone, friend or foe, can trigger the jump pad within its lifetime. Stepping onto an Orbital Jump Pad provides fall damage protection until landing or hitting something.

Unlock by near-directly hitting a fast-moving target with an Orbital Probe.

Configurable:
- Cooldown of the Orbital Jump Pad skill. Defaults to 30 sec.
- Lifetime of jump pads. Defaults to 20 sec.
- Maximum range of the Orbital Jump Pad skill. Defaults to 100 m.
- Whether to show trajectory previews (clientside). Defaults to enabled.

### Beacon Rebalance

Makes beacons no longer limited to one use per stage. Instead, they have individual cooldowns and lifetimes (latter indicated using the energy meter). Also adds a 50% resistance to cooldown reduction (incl. Bandolier, Brainstalks...), and complete resistance to max stock increases.

Configurable:
- Strength of cooldown reduction resistance. Defaults to 50%.

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

REPLACED with Beacon: Rejuvenator. Beacon: Rejuvenator gives all allies standing nearby the new Stimmed buff, which provides +50% skill recharge rate. Will also recharge beacons, so Rejuvenator's own cooldown is higher to compensate.

Configurable:
- Cannot be individually disabled.
- Cooldown and lifetime. Defaults to 20 sec, 50 sec.
- Additional skill recharge rate provided by Stimmed. Defaults to 50%.

#### Beacon: Hacking

REPLACED with Beacon: Special Order. Beacon: Special Order gives all allies standing nearby a set of temporary items rolled on the basic chest table, starting with 5 and increasing by 1 for every stage cleared. These items cannot be used in 3D Printers nor in Scrappers, and they're removed when the beacon breaks down or when you leave its range.

Configurable:
- Cannot be individually disabled.
- Cooldown and lifetime. Defaults to 20 sec, 40 sec.
- Radius of the ItemWard. Defaults to 10 m.
- Number of items provided on the first stage. Defaults to 5.
- Number of items provided per additional stage cleared. Defaults to 1.
- Rarity of the items provided. Defaults to identical to a basic chest, sans equipment (80 common : 20 uncommon : 1 rare).

## Issues/TODO

- Loadout selection of new skills is not remembered between launches of the game.
- Catalyzer Dart could do with some visual changes.
- Trying to find a way to allow canceling beacon/probe by recasting the skill.
- See the GitHub repo for more!

## Changelog

The 5 latest updates are listed below. For a full changelog, see: https://github.com/ThinkInvis/RoR2-Admiral/blob/master/changelog.md

**2.1.0**

- Post-playtest balance/QoL update for Catalyzer Dart and Orbital Jump Pad:
- Orbital Jump Pad felt awkward to use for several reasons, and its VFX was bugged. Several other design TODOs have also been resolved.
	- Orbital Jump Pad and its target now take 0.5s to land, down from 2s.
	- Orbital Jump Pads now display an arc towards their destination (can be disabled clientside).
	- Stepping into an Orbital Jump Pad now provides fall damage protection until you land or hit something (same as Acrid jump).
	- Orbital Jump Pads are now limited to two per player at a time. The oldest one will be destroyed early if a third is placed.
	- Orbital Jump Pad particles are now properly scaled down along with the fan model.
- Catalyzer Dart's seemed underwhelming after extended use, partially due to the scarcity and low duration of most DoTs -- and the opportunity cost of losing the non-DoTs. Since these changes are default-config-only, they won't automatically apply if you've already installed the mod.
	- Buffed default Catalyzer Dart DoT damage from 150% to 300%.
	- Buffed default Catalyzer Dart non-DoT damage from 1x200% to 1x500%.

**2.0.2**

- Fixed temporary beacons missing VFX/SFX.
- Fixed temporary beacons not appearing for multiplayer clients (hopefully).
- Removed some stray debug logging.

**2.0.1**

- Fixed a hard crash caused by Shocked lightning hits.

**2.0.0**

- Buffed Beacon: Shocking (fire rate increased from 1/1.5 sec to 1/0.95 sec, cooldown reduced from 30 sec to 24 sec).
- Refactored into a proper module system. Large swathes of the mod can now be disabled and/or configured, and should have multiplayer sync (bugs notwithstanding).
- Added dependency to TILER2 2.0.0.
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