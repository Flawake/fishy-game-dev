using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStatsUIManager : MonoBehaviour
{
    PlayerController controller;
    private PlayerData _playerData;
    
    [SerializeField]
    GameObject infoUI;
    
    private void Start()
    {
        controller = GetComponentInParent<PlayerController>();
    }

    public void ToggleStore(PlayerData playerData)
    {
        if (EnsurePlayer(playerData))
        {
            Debug.Log("User name:" + _playerData.GetUsername());
            if(infoUI.activeInHierarchy == false) OpenStore();
            else CloseStore();
        }
    }

    private void OpenStore()
    {
        infoUI.SetActive(true);
        controller.IncreaseObjectsPreventingMovement();
        controller.IncreaseObjectsPreventingFishing();
    }
    
    //Called from button in game
    public void CloseStore()
    {
        controller.DecreaseObjectsPreventingMovement();
        controller.DecreaseObjectsPreventingFishing();
        infoUI.SetActive(false);
    }
    
    bool EnsurePlayer(PlayerData playerData)
    {
        _playerData = playerData;
        controller = GetComponentInParent<PlayerController>();
        if (_playerData != null && controller != null) return true;
        Console.WriteLine("Player data and or Controller not found");
        return false;
    }

    public void TestFunction()
    {
        Debug.Log("TestFunction");
    }

}
