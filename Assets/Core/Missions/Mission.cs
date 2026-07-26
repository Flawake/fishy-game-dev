using UnityEngine;

/// <summary>
/// Immutable definition of a mission. A Mission asset is
/// shared by every player, so it must NEVER store runtime progress — that
/// lives in <see cref="MissionWrapper"/>.
/// </summary>
public abstract class Mission : ScriptableObject
{
    public int MissionID;
    public string MissionTitle;
    public string MissionDescription;

    [SerializeReference]
    public IMissionReward completionReward;

    [Min(1)]
    [Tooltip("Progress value at which this mission counts as complete.")]
    public int RequiredProgress = 1;

    /// <summary>
    /// Untyped entry point used by <see cref="MissionManager"/>. Implemented
    /// once, in <see cref="Mission{TEvent}"/>; subclasses never see this.
    /// </summary>
    public abstract void ProgressMission(IMissionEvent missionEvent, MissionWrapper state);
    public abstract Sprite GetMissionIcon();
}

public abstract class Mission<TEvent> : Mission where TEvent : IMissionEvent
{
    public sealed override void ProgressMission(IMissionEvent missionEvent, MissionWrapper state)
    {
        // Events the mission does not care about are silently ignored.
        if (missionEvent is TEvent typedEvent)
        {
            ProgressMission(typedEvent, state);
        }
    }

    protected abstract void ProgressMission(TEvent missionEvent, MissionWrapper state);
}
