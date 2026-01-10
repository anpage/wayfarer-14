using Robust.Shared.GameStates;

namespace Content.Shared._WF.Snark;

/// <summary>
/// Simple component that makes a gun throw its ammo instead of shooting it as projectiles.
/// Throw speed is controlled by the GunComponent's ProjectileSpeed.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ThrowingGunComponent : Component
{
    /// <summary>
    /// Optional custom message to show when out of ammo.
    /// If null, uses the default gun empty message.
    /// </summary>
    [DataField]
    public LocId? EmptyMessage;
}
