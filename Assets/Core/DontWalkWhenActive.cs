using UnityEngine;

public class DontWalkWhenActive : MonoBehaviour
{
    void OnEnable() {
        GetComponentInParent<PlayerController>().IncreaseObjectsPreventingMovement();
    }


    void OnDisable()
    {
        GetComponentInParent<PlayerController>().DecreaseObjectsPreventingMovement();
    }

}
