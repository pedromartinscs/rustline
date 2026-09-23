using Rustline.Gameplay.Weapons;

namespace Rustline.Presentation
{
    public static class WeaponHudInfoFormatter
    {
        public static string Resource(WeaponDefinition2D definition, PlayerWeaponAmmo2D.Snapshot ammo)
        {
            if (definition == null) return string.Empty;
            if (ammo.Policy == WeaponAmmoPolicy2D.Infinite) return "∞";
            if (ammo.Policy == WeaponAmmoPolicy2D.Magazine)
                return $"{ammo.Rounds} / {ammo.Capacity} ×{ammo.ReserveMagazines}";
            return string.Empty;
        }

        public static string Identity(WeaponDefinition2D definition,
            WeaponFireMode2D fireMode, WeaponShotMode2D shotMode)
        {
            if (definition == null) return "HANDS";
            if (definition.WeaponId == "longwatch_dmr")
                return fireMode == WeaponFireMode2D.Automatic
                    ? "LONGWATCH DMR (AUTO)" : "LONGWATCH DMR (SEMI)";
            if (definition.WeaponId == "latch_9")
                return shotMode == WeaponShotMode2D.Bouncing
                    ? "LATCH-9 (PHOTON FIELD)" : "LATCH-9 (PLASMA)";
            return definition.DisplayName.ToUpperInvariant();
        }
    }
}
