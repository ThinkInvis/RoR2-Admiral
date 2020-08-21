# Admiral

## Description

A sweeping overhaul for Captain. Changes include:

### Vulcan Shotgun

Now autofires. Time between full charge and autofire is configurable -- defaults to 20% of total charge time scaled with attack speed. An alternate config option for fixed time is available.

No longer has damage falloff, making the charged fire mode more useful.

Reduced pellet count from 8 to 6. The intent of this mod is to buff the rest of Captain's kit, removing the need for such an overpowered primary.

### Power Tazer (& Beacon: Shocking)

The Shocked status now wears off after a 10,000% health interval, up from 10% (so health-based 'stunbreak' is effectively disabled).

The Shocked status now deals roughly 20% of the victim's maximum health per second as damage to its allies within 15 meters, targeting and firing randomly.

### Catalyzer Dart

A new Secondary skill variant (same slot as Power Tazer). Fires a dart which removes all debuffs from an enemy and deals 150% of the remaining DoT damage + 1x200% per non-DoT debuff.

Unlock by getting 10 Shocked kills originating from the same enemy.

### Orbital Probe

Now usable in every Hidden Realm, except for Bazaar.

### Orbital Jump Pad

A new Utility skill variant (same slot as Orbital Probe). Summons a jump pad and its target within 100 m. Lasts 20 seconds, recharges in 30 seconds; anyone, friend or foe, can trigger the jump pad within this time.

Unlock by hitting a fast-moving target with an Orbital Probe.

### Orbital Supply Beacon

Now usable in every Hidden Realm, except for Bazaar.

All beacons are no longer limited to one use per stage. Instead, they have individual cooldowns and lifetimes. The energy meter above some original beacons now exists on all of them as an indicator of time remaining.

#### Beacon: Healing

Lasts 20 seconds. Recharges in 40 seconds.

#### Beacon: Shocking

Lasts 8 seconds. Recharges in 30 seconds.

Now fires once every 1.5 seconds (up from Lots Slower) to compensate for the lower uptime.

Benefits from the same changes that Power Tazer does, due to sharing the Shocked status.

#### Beacon: Resupply

REPLACED with Beacon: Rejuvenator.

Lasts 20 seconds. Recharges in 60 seconds.

Beacon: Rejuvenator gives all allies standing nearby the new Stimmed buff, which provides +50% skill recharge rate. Will also recharge beacons, so Rejuvenator's own cooldown is higher to compensate.

#### Beacon: Hacking

REPLACED with Beacon: Special Order.

Lasts 20 seconds. Recharges in 40 seconds.

Beacon: Special Order gives all allies standing nearby a set of temporary items rolled on the basic chest table, starting with 5 and increasing by 1 for every stage cleared. These items cannot be used in 3D Printers nor in Scrappers, and they're removed when the beacon breaks down or when you leave its range.

## Issues/TODO

- Orbital Jump Pad VFX is unfinished (particles are too big).
- Catalyzer Dart could also do with some visual changes.
- Stepping on an Orbital Jump Pad can deal unavoidable falling damage; may attempt to make a 'landing pad' object to mitigate this.
- Trying to find a way to allow canceling beacon/probe by recasting the skill.
- Looking into implementing a module system for disabling certain parts of the mod.
- See the GitHub repo for more!

## Changelog

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