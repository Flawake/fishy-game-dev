using UnityEngine;

public class DontFishWhenActive : MonoBehaviour
{
    void OnEnable() {
        GetComponentInParent<PlayerController>().IncreaseObjectsPreventingFishing();
    }


    void OnDisable()
    {
        GetComponentInParent<PlayerController>().DecreaseObjectsPreventingFishing();
    }

}
