using ItemSystem;
using UnityEngine;

public class FishdexUIManager : MonoBehaviour
{
    [SerializeField]
    GameObject fishdexObject;
    [SerializeField]
    GameObject fishdexContentHolder;
    [SerializeField]
    GameObject fishdexContentPrefab;

    void BuildFishdex() {
        foreach (Transform child in fishdexContentHolder.transform)
        {
            Destroy(child.gameObject);
        }

        Debug.Log($"{ItemRegistry.GetFullItemsList().Length}");
        foreach (ItemDefinition item in ItemRegistry.GetFullItemsList())
        {
            if (item.GetBehaviour<FishBehaviour>() == null)
            {
                Debug.Log($"Skipping item id: {item.Id}");
                continue;
            }
            GameObject newFishdexFish = Instantiate(fishdexContentPrefab, fishdexContentHolder.transform);
            newFishdexFish.GetComponent<FishdexFishUIBuilder>().BuildFishdexFish(item);
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
            BuildFishdex();
            fishdexObject.SetActive(true);
        }
    }

    public void CloseFishdex()
    {
        fishdexObject.SetActive(false);
    }
}
