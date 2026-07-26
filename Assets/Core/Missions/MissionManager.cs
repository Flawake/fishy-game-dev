using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEditor.SceneManagement;
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
/// Per-player mission tracking. Lives on the Player prefab next to
/// PlayerFishdexFishes and PlayerDataSyncManager.
///
/// Progress is server-authoritative: only [Server] code mutates it, and the
/// owning client receives a read-only copy. A modified client cannot award
/// itself mission rewards.
/// </summary>
public class MissionManager : NetworkBehaviour
{
    const int MAX_SIMULTANEOUS_MISSIONS = 3;

    // --- Server-only authoritative state ---
    readonly List<MissionWrapper> activeMissions = new List<MissionWrapper>();

    // --- Owner-only mirrors, index-aligned with activeMissions ---
    public readonly SyncList<MissionState> syncedMissions = new();
    public readonly SyncList<int> completedMissionIDs = new();

    [SerializeField] PlayerFishdexFishes fishdex;

    static int[] missionIDSequence = { 0, 1, 2, 3 };

    void Awake()
    {
        // Mission progress is nobody else's business.
        syncMode = SyncMode.Owner;
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
                completedMissionIDs.Add(wrapper.MissionID);
                TargetMissionCompleted(wrapper.MissionID);
            }
            else
            {
                syncedMissions[i] = Snapshot(wrapper);
            }
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

        if (completedMissionIDs.Contains(missionID) || IsActive(missionID))
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

        if (wrapper.Completed)
        {
            completedMissionIDs.Add(wrapper.MissionID);
            TargetMissionCompleted(wrapper.MissionID);
            return true;
        }

        activeMissions.Add(wrapper);
        syncedMissions.Add(Snapshot(wrapper));
        return true;
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

    // TODO: load active mission progress from the database on login and persist
    // it on change, the way PlayerFishdexFishes/DatabaseCommunications does.

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
        
        // Should check that there are no missions with the same requirements
    }
#endif
}
