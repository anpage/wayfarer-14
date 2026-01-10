using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared._WF.Snark;

/// <summary>
/// Makes guns with ThrowingGunComponent throw their ammo instead of shooting projectiles.
/// </summary>
public sealed class ThrowingGunSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ThrowingGunComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<ThrowingGunComponent, TakeAmmoEvent>(OnTakeAmmo);
    }

    private void OnAttemptShoot(EntityUid uid, ThrowingGunComponent component, ref AttemptShootEvent args)
    {
        args.ThrowItems = true;
    }

    private void OnTakeAmmo(EntityUid uid, ThrowingGunComponent component, TakeAmmoEvent args)
    {
        // Set custom empty message if configured
        if (component.EmptyMessage != null && args.Ammo.Count == 0)
        {
            args.Reason = Loc.GetString(component.EmptyMessage);
        }
    }
}
