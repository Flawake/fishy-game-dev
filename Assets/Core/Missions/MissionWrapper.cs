using System;
using UnityEngine;

/// <summary>
/// Runtime state for one mission the player has taken on. The Mission asset
/// says what the goal is; this says how far along the player is.
/// </summary>
[Serializable]
public class MissionWrapper
{
    [SerializeField] private Mission mission;
    [SerializeField] private int progress;
    [SerializeField] private bool completed;
    [SerializeField] private bool started;

    public Mission Mission => mission;
    public int Progress => progress;
    public bool Completed => completed;
    public bool Started => started;
    public int MissionID => mission != null ? mission.MissionID : -1;
    public int RequiredProgress => mission != null ? mission.RequiredProgress : 0;

    public event Action<MissionWrapper> ProgressChanged;
    public event Action<MissionWrapper> MissionCompleted;

    public MissionWrapper(Mission mission)
    {
        this.mission = mission;
        started = true;
    }

    public MissionWrapper(Mission mission, int progress)
    {
        this.mission = mission;
        this.progress = progress;
        started = true;
    }

    /// <summary>Hand an event to the mission, which decides whether it matters.</summary>
    public void Deliver(IMissionEvent missionEvent)
    {
        if (completed || mission == null)
        {
            return;
        }

        mission.ProgressMission(missionEvent, this);
    }

    /// <summary>For missions that count occurrences (catch a fish, make a trade).</summary>
    public void AddProgress(int amount = 1)
    {
        SetProgress(progress + amount);
    }

    /// <summary>For missions that track an absolute value (fishdex size).</summary>
    public void SetProgress(int value)
    {
        if (completed || mission == null)
        {
            return;
        }

        int clamped = Mathf.Clamp(value, 0, mission.RequiredProgress);
        if (clamped == progress)
        {
            return;
        }

        progress = clamped;
        ProgressChanged?.Invoke(this);

        if (progress >= mission.RequiredProgress)
        {
            Complete();
        }
    }

    private void Complete()
    {
        completed = true;
        mission.completionReward?.DistributeReward();
        MissionCompleted?.Invoke(this);
    }
}
