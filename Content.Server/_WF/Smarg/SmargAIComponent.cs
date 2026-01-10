using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._WF.Smarg;

/// <summary>
/// Component for chaotic smarg AI movement.
/// Uses impulse-based physics chasing with overshooting behavior.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class SmargAIComponent : Component
{
    /// <summary>
    /// How often the smarg gets an impulse toward its target.
    /// Lower values = more responsive but still chaotic.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ImpulseInterval = 0.5f;

    /// <summary>
    /// Minimum speed for impulses.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MinSpeed = 4f;

    /// <summary>
    /// Maximum speed for impulses.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxSpeed = 7f;

    /// <summary>
    /// How far the smarg will search for targets.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxChaseRadius = 15f;

    /// <summary>
    /// How often the smarg picks a new target.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float RetargetInterval = 1f;

    /// <summary>
    /// Distance at which the smarg will attempt to bite.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float AttackRange = 1f;

    /// <summary>
    /// Current target entity.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? Target;

    /// <summary>
    /// Next time to apply an impulse.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    [AutoPausedField]
    public TimeSpan NextImpulseTime;

    /// <summary>
    /// Next time to pick a new target.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    [AutoPausedField]
    public TimeSpan NextRetargetTime;
}
