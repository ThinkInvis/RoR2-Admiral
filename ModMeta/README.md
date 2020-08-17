# Admiral

## Description

A sweeping overhaul for Captain. Changes include:

### Vulcan Shotgun

Now autofires. Time between full charge and autofire is configurable -- defaults to 10% of total charge time scaled with attack speed. An alternate config option for fixed time is available.

### Power Tazer (& Beacon: Shocking)

The Shocked status now wears off after a 100% health interval, up from 10% (so health-based 'stunbreak' is effectively disabled).

The Shocked status now deals roughly 20% of the victim's maximum health per second as damage to its allies within 7 meters, targeting and firing randomly.

### Orbital Supply Beacon

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

- Existing language strings that should be overridden will remain unchanged -- most notably Beacon: Rejuvenator still has Resupply's name and description -- until a bug in R2API is fixed.
- Beacon: Rejuvenator doesn't have an area indicator.
- Plans exist to replace Beacon: Hacking with some sort of extra item generation, likely at the cost of more money for each use in the same stage.
- Plans exist for an alternate utility skill which creates temporary jump pads.
- See the GitHub repo for more!

## Changelog

**1.0.0**

- Initial version. Implements unlimited beacons with cooldown+lifetime, the Rejuvenator beacon as a replacement for Resupply, a special AoE max-health-as-damage component to the Shocked status, a tweak to Shocked which greatly increases its stunbreak threshold, and Vulcan Shotgun autofire.