# Admiral Changelog

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