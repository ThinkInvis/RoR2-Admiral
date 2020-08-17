# Admiral

## Description

A sweeping overhaul for Captain. Changes include:

### Vulcan Shotgun

Now autofires. Time between full charge and autofire is configurable -- defaults to 20% of total charge time scaled with attack speed. An alternate config option for fixed time is available.

No longer has damage falloff, making the charged fire mode more useful.

Reduced pellet count from 8 to 6. The intent of this mod is to buff the rest of Captain's kit, removing the need for such an overpowered primary.

### Power Tazer (& Beacon: Shocking)

The Shocked status now wears off after a 100% health interval, up from 10% (so health-based 'stunbreak' is effectively disabled).

The Shocked status now deals roughly 20% of the victim's maximum health per second as damage to its allies within 15 meters, targeting and firing randomly.

### Orbital Probe

Now usable in every Hidden Realm, except for Bazaar.

### Orbital Supply Beacon

Now usable in every Hidden Realm, except for Bazaar.

All beacons (except Hacking, for now) are no longer limited to one use per stage. Instead, they have individual cooldowns and lifetimes.

#### Beacon: Healing

Lasts 20 seconds. Recharges in 40 seconds.

#### Beacon: Shocking

Lasts 8 seconds. Recharges in 30 seconds.

#### Beacon: Resupply

REPLACED with Beacon: Rejuvenator.

Lasts 20 seconds. Recharges in 60 seconds.

Beacon: Rejuvenator gives all allies standing nearby the new Stimmed buff, which provides +50% skill recharge rate. Will also recharge beacons, so Rejuvenator's own cooldown is higher to compensate.

#### Beacon: Hacking

Unchanged... for now.

## Issues/TODO

- Beacon: Rejuvenator doesn't have an area indicator.
- Plans exist to replace Beacon: Hacking with some sort of extra item generation, likely at the cost of more money for each use in the same stage.
- Plans exist for an alternate utility skill which creates temporary jump pads.
- Trying to find a way to allow canceling beacon/probe by recasting the skill.
- See the GitHub repo for more!

## Changelog

**1.1.0**

- Implemented language token overrides (R2API bug was patched).
- Reduced Vulcan Shotgun pellet count from 8 to 6.
- Removed damage falloff from Vulcan Shotgun.
- Minor project reorganization.

**1.0.0**

- Initial version. Implements unlimited beacons with cooldown+lifetime, the Rejuvenator beacon as a replacement for Resupply, a special AoE max-health-as-damage component to the Shocked status, a tweak to Shocked which greatly increases its stunbreak threshold, and Vulcan Shotgun autofire.
- Late notes as of 1.1.0: Initial version also made orbital skills usable in hidden realms (except bazaar).