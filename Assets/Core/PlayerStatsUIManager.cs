using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStatsUIManager : MonoBehaviour
{
    PlayerController controller;
    
    [SerializeField]
    GameObject infoUI;
    
    private void Start()
    {
        controller = GetComponentInParent<PlayerController>();
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
            controller = GetComponentInParent<PlayerController>();
        }
    }

    public void TestFunction()
    {
        Debug.Log("TestFunction");
    }

}
