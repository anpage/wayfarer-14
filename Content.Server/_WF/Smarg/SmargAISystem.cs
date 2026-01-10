using System.Numerics;
using Content.Server.NPC.Components;
using Content.Shared.CombatMode;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Server._WF.Smarg;

/// <summary>
/// Handles chaotic smarg AI movement using impulse-based physics.
/// Smargs chase hostile entities but frequently overshoot their targets.
/// </summary>
public sealed class SmargAISystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;

    private static readonly SoundSpecifier DeathSound = new SoundPathSpecifier("/Audio/_WF/Smarg/die.ogg");
    private static readonly SoundSpecifier HuntSound = new SoundCollectionSpecifier("SmargHunt");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SmargAIComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SmargAIComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(EntityUid uid, SmargAIComponent comp, ComponentStartup args)
    {
        comp.NextImpulseTime = _timing.CurTime;
        comp.NextRetargetTime = _timing.CurTime;

        // Enable combat mode so the smarg can attack
        if (TryComp<CombatModeComponent>(uid, out var combatMode))
            _combat.SetInCombatMode(uid, true, combatMode);
    }

    private void OnShutdown(EntityUid uid, SmargAIComponent comp, ComponentShutdown args)
    {
        // Clean up combat component if we added it
        RemComp<NPCMeleeCombatComponent>(uid);

        if (TryComp<CombatModeComponent>(uid, out var combatMode))
            _combat.SetInCombatMode(uid, false, combatMode);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<SmargAIComponent, NpcFactionMemberComponent, PhysicsComponent, MobStateComponent>();

        while (query.MoveNext(out var uid, out var smarg, out var faction, out var physics, out var mobState))
        {
            // Don't process dead smargs
            if (mobState.CurrentState != MobState.Alive)
            {
                _audio.PlayPvs(DeathSound, uid);
                RemComp<NPCMeleeCombatComponent>(uid);
                RemComp<TimedDespawnComponent>(uid);
                RemComp<SmargAIComponent>(uid);
                _physics.SetBodyStatus(uid, physics, BodyStatus.OnGround);
                continue;
            }
            // Check if we need to retarget
            if (curTime >= smarg.NextRetargetTime)
            {
                FindNewTarget(uid, smarg, faction);
                smarg.NextRetargetTime = curTime + TimeSpan.FromSeconds(smarg.RetargetInterval);
            }

            // Check if we need to apply an impulse
            if (curTime >= smarg.NextImpulseTime)
            {
                ApplyImpulse(uid, smarg, physics);
                smarg.NextImpulseTime = curTime + TimeSpan.FromSeconds(smarg.ImpulseInterval);
            }

            // Handle attacking if we have a target in range
            UpdateCombat(uid, smarg);
        }
    }

    private void FindNewTarget(EntityUid uid, SmargAIComponent smarg, NpcFactionMemberComponent faction)
    {
        smarg.Target = null;

        // Get nearby hostiles
        var hostiles = _faction.GetNearbyHostiles((uid, faction, null), smarg.MaxChaseRadius);

        EntityUid? closest = null;
        var closestDist = float.MaxValue;
        var ourPos = _transform.GetWorldPosition(uid);

        foreach (var hostile in hostiles)
        {
            // Skip dead entities
            if (TryComp<MobStateComponent>(hostile, out var mobState) && mobState.CurrentState != MobState.Alive)
                continue;

            var theirPos = _transform.GetWorldPosition(hostile);
            var dist = Vector2.Distance(ourPos, theirPos);

            if (dist < closestDist)
            {
                closestDist = dist;
                closest = hostile;
            }
        }

        smarg.Target = closest;
    }

    private void ApplyImpulse(EntityUid uid, SmargAIComponent smarg, PhysicsComponent physics)
    {
        Vector2 direction;

        if (smarg.Target != null && !Deleted(smarg.Target))
        {
            // Chase the target
            var ourPos = _transform.GetWorldPosition(uid);
            var targetPos = _transform.GetWorldPosition(smarg.Target.Value);
            var delta = targetPos - ourPos;

            if (delta.LengthSquared() > 0.01f)
            {
                direction = delta.Normalized();
            }
            else
            {
                // We're on top of the target, pick a random direction
                direction = _random.NextAngle().ToVec();
            }
        }
        else
        {
            // No target - move in a random direction
            direction = _random.NextAngle().ToVec();
        }

        // Random speed for erratic movement
        var speed = _random.NextFloat(smarg.MinSpeed, smarg.MaxSpeed);

        // Apply velocity directly - no friction compensation means overshooting!
        _physics.SetLinearVelocity(uid, direction * speed, body: physics);

        // Rotate to face movement direction for directional sprites
        _transform.SetWorldRotation(uid, direction.ToWorldAngle());

        // Keep in air to prevent ground friction
        _physics.SetBodyStatus(uid, physics, BodyStatus.InAir);

        // Play hunt sound on each jump
        _audio.PlayPvs(HuntSound, uid);
    }

    private void UpdateCombat(EntityUid uid, SmargAIComponent smarg)
    {
        if (smarg.Target == null || Deleted(smarg.Target))
        {
            // No target - remove combat component
            RemComp<NPCMeleeCombatComponent>(uid);
            return;
        }

        // Check distance to target
        var ourPos = _transform.GetWorldPosition(uid);
        var targetPos = _transform.GetWorldPosition(smarg.Target.Value);
        var distance = Vector2.Distance(ourPos, targetPos);

        if (distance <= smarg.AttackRange)
        {
            // In range - ensure we have combat component so NPCCombatSystem handles attacks
            var combat = EnsureComp<NPCMeleeCombatComponent>(uid);
            combat.Target = smarg.Target.Value;
        }
        else
        {
            // Out of range - remove combat component
            RemComp<NPCMeleeCombatComponent>(uid);
        }
    }
}
