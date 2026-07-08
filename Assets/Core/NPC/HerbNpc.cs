using System.Collections;
using UnityEngine;

/// <summary>
/// Put this on the root object of the Herb NPC in every area he can stand in.
/// Herb is only visible in the area the current daily quest points to, and this
/// component is his timer: every 04:00 Amsterdam time he moves to the next area.
///
/// The quest is computed deterministically from the date (see HerbQuestManager),
/// so the server and every client toggle the same Herb without any syncing.
/// </summary>
public class HerbNpc : MonoBehaviour
{
    [Tooltip("The visual root of Herb that gets enabled/disabled when Herb moves, leave empty to use this object's first child")]
    [SerializeField] private GameObject herbRoot;

    private const float RolloverCheckIntervalSeconds = 30f;

    private Area area;
    private int appliedQuestDayNumber = int.MinValue;

    private void Awake()
    {
        area = SceneToAreaMapper.GetAreaFromSceneName(gameObject.scene.name);
        if (herbRoot == null && transform.childCount > 0)
        {
            herbRoot = transform.GetChild(0).gameObject;
        }
    }

    private void OnEnable()
    {
        ApplyCurrentQuest();
        StartCoroutine(WatchForRollover());
    }

    private IEnumerator WatchForRollover()
    {
        while (true)
        {
            yield return new WaitForSeconds(RolloverCheckIntervalSeconds);
            if (HerbQuestManager.CurrentQuestDayNumber() != appliedQuestDayNumber)
            {
                ApplyCurrentQuest();
            }
        }
    }

    private void ApplyCurrentQuest()
    {
        HerbQuest quest = HerbQuestManager.GetCurrentQuest();
        appliedQuestDayNumber = quest.questDayNumber;

        bool herbIsHere = quest.IsValid && quest.area == area;
        if (herbRoot != null)
        {
            herbRoot.SetActive(herbIsHere);
        }
        else
        {
            Debug.LogWarning($"[HerbNpc] No Herb root object assigned in {gameObject.scene.name}");
        }
    }
}
