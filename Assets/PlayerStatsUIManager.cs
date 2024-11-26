using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStatsUIManager : MonoBehaviour
{
    playerController controller;
    
    [SerializeField]
    GameObject infoUI;
    
    private void Start()
    {
        controller = GetComponentInParent<playerController>();
    }

    public void ToggleStore()
    {
        if(infoUI.activeInHierarchy == false)
        {
            infoUI.SetActive(true);
            EnsurePlayerController();
            controller.IncreaseObjectsPreventingMovement();
            controller.IncreaseObjectsPreventingFishing();
        }
        else
        {
            infoUI.SetActive(false);
            controller.DecreaseObjectsPreventingMovement();
            controller.IncreaseObjectsPreventingFishing();
        }
    }

    //Called from button in game
    public void CloseStore()
    {
        EnsurePlayerController();
        controller.DecreaseObjectsPreventingMovement();
        controller.DecreaseObjectsPreventingFishing();
        infoUI.SetActive(false);
    }
    
    void EnsurePlayerController()
    {
        if (controller == null)
        {
            controller = GetComponentInParent<playerController>();
        }
    }

    public void TestFunction()
    {
        Debug.Log("TestFunction");
    }

}
