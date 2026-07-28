using UnityEngine;

/// <summary>
/// Immutable definition of a mission reward. Like <see cref="Mission"/>, a reward
/// asset is shared by every player, so it must never mutate its own fields or hand
/// out a reference to them.
/// </summary>
public abstract class IMissionReward : ScriptableObject
{
    /// <summary>
    /// Describes this reward into <paramref name="draft"/> without granting anything.
    /// The draft is sent to the database with the completion and only applied to the
    /// player once that transaction commits.
    /// </summary>
    public abstract void BuildReward(MissionRewardDraft draft);
    public abstract string GetRewardDescription();
    public abstract Sprite GetIcon();
}
