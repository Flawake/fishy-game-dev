using System.Collections.Generic;
using System.Linq;
using FishyGame.Api;
using Mirror;
using UnityEngine;

/// <summary>
/// Network-serialisable snapshot of one mission's progress, sent to the owning
/// client so the UI has something to draw. The server keeps the real state in
/// <see cref="MissionWrapper"/>.
/// </summary>
public struct MissionState
{
    public int missionID;
    public int progress;
    public bool completed;
}

/// <summary>
/// Per-player mission tracking. Lives on the Player prefab.
/// Progress is server-authoritative: only [Server] code mutates it, and the
/// owning client receives a read-only copy. A modified client cannot award
/// itself mission rewards.
/// </summary>
public class MissionManager : NetworkBehaviour
{
    const int MAX_SIMULTANEOUS_MISSIONS = 3;

    // --- Server-only authoritative state ---
    readonly List<MissionWrapper> activeMissions = new List<MissionWrapper>();

    // Missions whose missions_started row is still being inserted.
    readonly HashSet<int> missionsAwaitingStart = new HashSet<int>();

    // Missions already gone from activeMissions but not yet confirmed complete by
    // the database. They belong to neither list, so they need their own guard.
    readonly HashSet<int> missionsAwaitingCompletion = new HashSet<int>();

    // --- Owner-only mirrors, index-aligned with activeMissions ---
    public readonly SyncList<MissionState> syncedMissions = new();
    public readonly SyncList<int> completedMissionIDs = new();

    [SerializeField] PlayerFishdexFishes fishdex;
    [SerializeField] PlayerDataSyncManager syncManager;
    [SerializeField] PlayerData playerData;
    [SerializeField] PlayerInventory inventory;

    static int[] missionIDSequence = { 0, 1, 2, 3 };

    void Awake()
    {
        // Mission progress is nobody else's business.
        syncMode = SyncMode.Owner;
    }

    public List<int> GetCompletedMissionsIDs()
    {
        return completedMissionIDs.ToList();
    }

    [Client]
    public List<MissionState> ClientGetActiveMissions()
    {
        return syncedMissions.ToList();
    }

    [Server]
    public void SetInitialMissionData(List<ActiveMission> activeMissions, List<int> completedMissions)
    {
        foreach(ActiveMission activeMission in activeMissions)
        {
            this.activeMissions.Add(
                new MissionWrapper(MissionRegistry.Get(activeMission.mission_id), activeMission.mission_progress)
            );
        }

        foreach(MissionWrapper mission in this.activeMissions)
        {
            syncedMissions.Add(new MissionState
            {
                missionID = mission.MissionID,
                progress = mission.Progress,
                completed = mission.Completed,
            });
        }

        foreach(int missionID in completedMissions)
        {
            completedMissionIDs.Add(missionID);
        }
    }

    // ---- Dispatch ---------------------------------------------------------
    //
    // One entry point for every kind of event. Missions that do not care about
    // the payload type ignore it, so this never changes when a mission type is
    // added.

    [Server]
    public void ServerRaise<TEvent>(TEvent missionEvent) where TEvent : IMissionEvent
    {
        // Box once here rather than once per mission.
        IMissionEvent boxed = missionEvent;

        for (int i = activeMissions.Count - 1; i >= 0; i--)
        {
            MissionWrapper wrapper = activeMissions[i];
            int progressBefore = wrapper.Progress;

            wrapper.Deliver(boxed);

            if (wrapper.Progress == progressBefore)
            {
                // This mission does not react to this event type.
                continue;
            }

            if (wrapper.Completed)
            {
                activeMissions.RemoveAt(i);
                syncedMissions.RemoveAt(i);
            }
            else
            {
                syncedMissions[i] = Snapshot(wrapper);
            }

            ServerPersistState(wrapper);
        }
    }

    // ---- Thin, readable call sites for the rest of the game ---------------

    [Server]
    public void FishCaught(int fishID) => ServerRaise(new FishCaughtEvent(fishID));

    [Server]
    public void FishdexUpdated(int totalSpeciesDiscovered) => ServerRaise(new FishdexUpdatedEvent(totalSpeciesDiscovered));

    [Server]
    public void TradeMade() => ServerRaise(new TradeMadeEvent());

    [Server]
    public void FriendMade() => ServerRaise(new FriendMadeEvent());

    [Server]
    public void ItemCollected(int itemID, int amount) => ServerRaise(new ItemCollectedEvent(itemID, amount));

    // ---- Mission lifecycle ------------------------------------------------

    [Server]
    public bool ServerStartMission(int missionID)
    {
        if (activeMissions.Count >= MAX_SIMULTANEOUS_MISSIONS)
        {
            return false;
        }

        if (completedMissionIDs.Contains(missionID)
            || missionsAwaitingStart.Contains(missionID)
            || missionsAwaitingCompletion.Contains(missionID)
            || IsActive(missionID))
        {
            return false;
        }

        if (!MissionRegistry.TryGet(missionID, out Mission mission))
        {
            Debug.LogError($"No mission registered for ID {missionID}.");
            return false;
        }

        MissionWrapper wrapper = new MissionWrapper(mission);

        // Seed from state the player already has, so "reach 25 species" started
        // by someone who already has 20 shows 20/25 rather than 0/25.
        wrapper.Deliver(new FishdexUpdatedEvent(fishdex.DiscoveredSpeciesCount));

        ServerPersistStart(wrapper);

        if (wrapper.Completed)
        {
            // The start is still in flight; its callback sends the completion.
            missionsAwaitingCompletion.Add(missionID);
            return true;
        }

        activeMissions.Add(wrapper);
        syncedMissions.Add(Snapshot(wrapper));
        return true;
    }

    /// <summary>
    /// Sends the completion together with its reward, and hands the reward to the
    /// player only once the database has committed both.
    ///
    /// A rolled back transaction grants nothing and leaves the missions_started row
    /// alone, so the mission comes back intact on the next login. A commit whose
    /// response is lost looks identical from here, but the database already holds
    /// both the completion and the reward, so the player simply sees them after the
    /// next login. Either way nothing is paid twice.
    /// </summary>
    [Server]
    void ServerCompleteMission(MissionWrapper wrapper)
    {
        int missionID = wrapper.MissionID;
        missionsAwaitingCompletion.Add(missionID);

        MissionRewardDraft reward = new MissionRewardDraft(inventory, syncManager);
        wrapper.Mission.completionReward?.BuildReward(reward);

        DatabaseCommunications.CompleteMission(playerData.GetUuid(), missionID, reward, result =>
        {
            if (this == null)
            {
                return;
            }

            missionsAwaitingCompletion.Remove(missionID);

            if (!result.Success || !result.Value)
            {
                Debug.LogError($"Could not complete mission {missionID} in the database: {result.Error}");
                return;
            }

            reward.Apply();
            completedMissionIDs.Add(missionID);
            TargetMissionCompleted(missionID);
        });
    }

    /// <summary>
    /// Inserts the missions_started row. Nothing else may be written for this
    /// mission until the insert lands: progress_mission updates an existing row and
    /// fails without one, start_mission always writes a progress of 0, and a
    /// completion that overtakes the start leaves an orphaned started row behind.
    /// </summary>
    [Server]
    void ServerPersistStart(MissionWrapper wrapper)
    {
        int missionID = wrapper.MissionID;
        missionsAwaitingStart.Add(missionID);

        DatabaseCommunications.StartMission(playerData.GetUuid(), missionID, result =>
        {
            if (this == null)
            {
                return;
            }

            missionsAwaitingStart.Remove(missionID);

            if (!result.Success || !result.Value)
            {
                // Release the completion guard too, otherwise a mission that finished
                // while the failed insert was in flight stays blocked for the session.
                missionsAwaitingCompletion.Remove(missionID);
                Debug.LogError($"Could not start mission {missionID} in the database: {result.Error}");

                // There is no row to progress against, so drop the mission rather than
                // show the player one that can never be saved.
                ServerDropMission(wrapper);
                return;
            }

            // Whatever happened while the insert was in flight is written now.
            ServerPersistState(wrapper);
        });
    }

    /// <summary>
    /// Forgets a mission the database never accepted, keeping the lists index-aligned.
    /// </summary>
    [Server]
    void ServerDropMission(MissionWrapper wrapper)
    {
        int index = activeMissions.IndexOf(wrapper);
        if (index < 0)
        {
            return;
        }

        activeMissions.RemoveAt(index);
        syncedMissions.RemoveAt(index);
    }

    [Server]
    void ServerPersistState(MissionWrapper wrapper)
    {
        if (missionsAwaitingStart.Contains(wrapper.MissionID))
        {
            return;
        }

        if (wrapper.Completed)
        {
            ServerCompleteMission(wrapper);
        }
        else if (wrapper.Progress > 0)
        {
            DatabaseCommunications.ProgressMission(playerData.GetUuid(), wrapper.MissionID, wrapper.Progress);
        }
    }

    public bool IsActive(int missionID)
    {
        foreach (MissionState state in syncedMissions)
        {
            if (state.missionID == missionID)
            {
                return true;
            }
        }

        return false;
    }

    [TargetRpc]
    void TargetMissionCompleted(int missionID)
    {
        // TODO: show the completion toast / reward popup here.
        Debug.Log($"Mission {missionID} completed.");
    }

    static MissionState Snapshot(MissionWrapper wrapper) => new MissionState
    {
        missionID = wrapper.MissionID,
        progress = wrapper.Progress,
        completed = wrapper.Completed,
    };

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    public static void VerifyMissions()
    {
        // Verify if missions are not twice in missionIDSequence
        if (missionIDSequence.Count() != missionIDSequence.ToList().Distinct().Count())
        {
            Debug.LogWarning("Some missions accur multiple times inside missionIDSequence");
        }


        List<Mission> missions = Resources.LoadAll<Mission>("").ToList();

        // Verify that there are no missions with overlapping id's
        List<int> missionIDList = missions.Select(mission => mission.MissionID).ToList();
        if(missionIDList.Distinct().Count() != missions.Count())
        {
            Debug.LogWarning("Some missions have the same missionID");
        }

        // Verify that there are no missions that are not added to missionIDSequence
        foreach(int missionID in missionIDSequence)
        {
            if (!missionIDList.Contains(missionID))
            {
                Debug.LogWarning($"Added mission id {missionID} to the missionIDSequence, but this mission does not exist");
            }
        }

        // Verify that there are no missions inside missionIDSequence that do not exist
        foreach(int missionID in missionIDList)
        {
            if (!missionIDSequence.Contains(missionID))
            {
                Debug.LogWarning($"Added mission with id {missionID}, but this mission is not added to missionIDSequence");
            }
        }
        
        // TODO: Should check that there are no missions with the same requirements
    }
#endif
}
