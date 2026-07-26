using System;
using FishyGame.Api;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Server-only manager that owns Herb's presence in the world.
///
/// The daily quest lives on the database server; the game never generates it.
/// Because the database can not notify the game of a change, this manager polls it:
///   * it fetches the current quest on start,
///   * it schedules its next fetch for the moment Herb is due to move (next_move_at),
///   * once that moment has passed it retries every <see cref="RetrySeconds"/> until the
///     database serves a quest with a new date.
///
/// When a new quest arrives it despawns the old Herb and spawns a fresh Herb prefab into
/// the target area's subscene via NetworkServer.Spawn. Clients only observe Herb while
/// they are in that area (SceneInterestManagement), and read the quest off the spawned
/// Herb (see <see cref="HerbQuestSync"/>). Place this on a GameObject in the always-loaded
/// online scene (Container) and assign the Herb prefab.
/// </summary>
public class HerbSpawnManager : MonoBehaviour
{
    [Tooltip("The Herb prefab to spawn. Must also be registered in the NetworkManager's spawnable prefabs.")]
    [SerializeField] private GameObject herbPrefab;

    // How often to retry once Herb is overdue to move but the database has not rolled over yet.
    private const float RetrySeconds = 30f;
    // How often to retry spawning while the target subscene is still loading (server boot race).
    private const float SceneWaitRetrySeconds = 2f;

    private float _nextPollTime;
    private float _nextSceneRetryTime;
    private bool _requestInFlight;

    // Id of the quest currently applied, used to detect a genuinely new quest.
    private Guid _currentQuestId;
    // A quest that still needs Herb spawned (kept while its subscene finishes loading).
    private HerbQuest _pendingSpawnQuest;
    private GameObject _spawnedHerb;

    private void Start()
    {
        // This is server logic only. On a pure client the object still exists (it is part
        // of the online scene) but must stay idle.
        if (!NetworkServer.active)
        {
            enabled = false;
            return;
        }

        if (herbPrefab == null)
        {
            Debug.LogError("[HerbSpawnManager] No Herb prefab assigned, Herb will never spawn");
            enabled = false;
            return;
        }

        // Poll immediately on start.
        _nextPollTime = 0f;
    }

    private void Update()
    {
        // Keep trying to spawn while we are waiting for the target subscene to load.
        if (_pendingSpawnQuest != null && Time.time >= _nextSceneRetryTime)
        {
            TrySpawnHerb();
        }

        if (!_requestInFlight && Time.time >= _nextPollTime)
        {
            SendPoll();
        }
    }

    private void SendPoll()
    {
        _requestInFlight = true;
        DatabaseCommunications.GetCurrentHerbQuest(OnQuestResponse);
    }

    private void OnQuestResponse(ApiResult<CurrentHerbQuestResponse> result)
    {
        _requestInFlight = false;

        // The generated client reports both transport failures and unreadable
        // bodies through Error, so the manual JsonUtility parse is gone.
        if (!result.Success)
        {
            Debug.LogWarning($"[HerbSpawnManager] Quest poll failed ({result.Error}), retrying in {RetrySeconds}s");
            ScheduleRetry();
            return;
        }

        HerbQuest quest = HerbQuestManager.FromResponse(result.Value);
        if (quest == null)
        {
            ScheduleRetry();
            return;
        }

        // A new quest id means Herb moved: swap him over.
        if (quest.QuestId != _currentQuestId)
        {
            ApplyNewQuest(quest);
        }

        ScheduleNextPoll(quest);
    }

    private void ApplyNewQuest(HerbQuest quest)
    {
        _currentQuestId = quest.QuestId;
        // The server validates hand-ins against this.
        HerbQuestService.SetCurrentQuest(quest);
        _pendingSpawnQuest = quest;
        _nextSceneRetryTime = 0f;
        TrySpawnHerb();
    }

    private void TrySpawnHerb()
    {
        HerbQuest quest = _pendingSpawnQuest;
        if (quest == null)
        {
            return;
        }

        string sceneName = quest.area.ToString();
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            // Subscenes load shortly after the server boots; keep trying.
            _nextSceneRetryTime = Time.time + SceneWaitRetrySeconds;
            return;
        }

        // Remove the previous Herb before placing the new one.
        if (_spawnedHerb != null)
        {
            NetworkServer.Destroy(_spawnedHerb);
            _spawnedHerb = null;
        }

        Vector3 spawnPosition = spawnPoint.GetRandomSpawnPoint(sceneName);
        GameObject herb = Instantiate(herbPrefab, spawnPosition, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(herb, scene);

        HerbQuestSync sync = herb.GetComponent<HerbQuestSync>();
        if (sync != null)
        {
            // Set before Spawn so the quest is part of Herb's initial synced state.
            sync.ServerSetQuest(quest);
        }
        else
        {
            Debug.LogWarning("[HerbSpawnManager] Herb prefab has no HerbQuestSync, clients will not see the quest");
        }

        NetworkServer.Spawn(herb);
        _spawnedHerb = herb;
        _pendingSpawnQuest = null;
    }

    private void ScheduleNextPoll(HerbQuest quest)
    {
        double secondsUntilMove = (quest.nextMoveAtUtc - DateTime.UtcNow).TotalSeconds;
        if (secondsUntilMove <= 0)
        {
            // Herb is due to move but the database has not produced the next quest yet.
            _nextPollTime = Time.time + RetrySeconds;
        }
        else
        {
            _nextPollTime = Time.time + (float)secondsUntilMove;
        }
    }

    private void ScheduleRetry()
    {
        _nextPollTime = Time.time + RetrySeconds;
    }
}
