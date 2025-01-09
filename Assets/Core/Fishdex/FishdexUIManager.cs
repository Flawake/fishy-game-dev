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
            BuildFishdex();
        }
    }

    public void CloseFishdex()
    {
        fishdexObject.SetActive(false);
    }
}
