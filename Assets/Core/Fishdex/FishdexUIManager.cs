using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishdexUIManager : MonoBehaviour
{
    [SerializeField]
    GameObject fishdexObject;
    [SerializeField]
    GameObject fishdexContentHolder;
    [SerializeField]
    GameObject fishdexContentPrefab;

    void buildFishdex() {
        foreach (FishConfiguration fish in ItemsInGame.fishesInGame) {
            GameObject newFishdexFish = Instantiate(fishdexContentPrefab, fishdexContentHolder.transform);
            newFishdexFish.GetComponent<FishdexFishUIBuilder>().BuildFishdexFish(fish);
        }
    }

    public void ToggleFishdex()
    {
        if(fishdexObject.activeInHierarchy)
        {
            fishdexObject.SetActive(false);
        }
        else
        {
            fishdexObject.SetActive(true);
            buildFishdex();
        }
    }

    public void CloseFishdex()
    {
        fishdexObject.SetActive(false);
    }
}
