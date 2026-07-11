using System;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Carries the current daily quest from the server to the clients on the spawned Herb.
///
/// Herb is spawned/despawned by <see cref="HerbSpawnManager"/> via NetworkServer.Spawn.
/// The quest data is set on the server right before spawning, so it is part of Herb's
/// initial state and is available to clients by the time OnStartClient runs. Clients
/// read it here to build the dialog; the server keeps validating against
/// <see cref="HerbQuestService"/> instead.
/// </summary>
[RequireComponent(typeof(NetworkIdentity))]
public class HerbQuestSync : NetworkBehaviour
{
    // Unique id of this quest, decided by the database.
    [SyncVar] public Guid questId;
    // Area enum value Herb is standing in.
    [SyncVar] public int areaId;
    // Fishcoins paid for handing the quest in (decided by the database).
    [SyncVar] public int rewardCoins;

    // The quest fishes, serialized through HerbQuestEntrySerializer.
    private readonly SyncList<HerbQuestEntry> questFishes = new SyncList<HerbQuestEntry>();

    /// <summary>
    /// Fills in the quest on the server. Must be called before NetworkServer.Spawn so the
    /// values are serialized into Herb's spawn state.
    /// </summary>
    [Server]
    public void ServerSetQuest(HerbQuest quest)
    {
        questId = quest.QuestId;
        areaId = (int)quest.area;
        rewardCoins = quest.RewardCoins;

        questFishes.Clear();
        foreach (HerbQuestEntry entry in quest.entries)
        {
            questFishes.Add(new HerbQuestEntry { fishId = entry.fishId, amount = entry.amount });
        }
    }

    /// <summary>
    /// Rebuilds a <see cref="HerbQuest"/> from the synced data for the client dialog.
    /// </summary>
    public HerbQuest BuildQuest()
    {
        HerbQuest quest = new HerbQuest
        {
            questId = questId,
            area = (Area)areaId,
            rewardCoins = rewardCoins,
        };

        foreach (HerbQuestEntry entry in questFishes)
        {
            quest.entries.Add(new HerbQuestEntry { fishId = entry.fishId, amount = entry.amount });
        }
        return quest;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Mirror instantiates dynamically spawned objects into the client's active scene,
        // but Herb belongs to a specific area subscene (NpcMovement pathfinding and rendering
        // rely on gameObject.scene). The client only observes Herb when it is in that area
        // (SceneInterestManagement), so the target scene is loaded here.
        Scene areaScene = SceneManager.GetSceneByName(((Area)areaId).ToString());
        if (areaScene.IsValid() && areaScene.isLoaded && gameObject.scene != areaScene)
        {
            SceneManager.MoveGameObjectToScene(gameObject, areaScene);
        }

        // Hand the quest to the dialog now that the synced data has arrived.
        DialogHerb dialog = GetComponent<DialogHerb>();
        if (dialog != null)
        {
            dialog.BuildForQuest(BuildQuest());
        }
    }
}
