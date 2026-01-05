using ItemSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FishdexUIManager : MonoBehaviour
{

    #region Fishdex
    private const byte PAGE_ONE_FISH_SIZE = 20;
    private const byte PAGE_TWO_FISH_SIZE = 18;
    private const byte FISH_PER_PAGE = PAGE_ONE_FISH_SIZE + PAGE_TWO_FISH_SIZE;

    [SerializeField]
    GameObject fishdexObject;
    [SerializeField]
    GameObject pageOneContentHolder;
    [SerializeField]
    GameObject pageTwoContentHolder;
    [SerializeField]
    GameObject fishdexContentPrefab;

    [SerializeField]
    RectTransform searchButton;
    [SerializeField]
    RectTransform overviewButton;
    [SerializeField]
    GameObject FishOverviewObject;

    Vector2 searchButtonDefaultPos;
    Vector2 overviewButtonDefaultPos;

    private int currentPage = 1;
    private List<ItemDefinition> allFishList = new List<ItemDefinition>();
    private int currentShowingFishIndex = 0;
    private int totalPages = 1;

    #endregion

    #region FishInfo
    [SerializeField]
    FishInfoUIManager fishInfoUIManager;
    [SerializeField]
    GameObject FishInfoObject;
    #endregion

    void Awake()
    {
        searchButtonDefaultPos = searchButton.anchoredPosition;
        overviewButtonDefaultPos = overviewButton.anchoredPosition;
    }

    void BuildFishdex() {
        // Collect all fish
        allFishList.Clear();
        foreach (ItemDefinition item in ItemRegistry.GetFullItemsList())
        {
            if (item.GetBehaviour<FishBehaviour>() == null)
            {
                continue;
            }
            allFishList.Add(item);
        }

        // Sort by rarity (1->5) then by name (a->z)
        allFishList = allFishList.OrderBy(fish => 
        {
            FishBehaviour fishBehaviour = fish.GetBehaviour<FishBehaviour>();
            return fishBehaviour != null ? FishEnumConfig.RarityToInt(fishBehaviour.Rarity) : 0;
        })
        .ThenBy(fish => fish.name)
        .ToList();

        // Calculate total pages
        totalPages = Mathf.Max(1, Mathf.CeilToInt((float)allFishList.Count / FISH_PER_PAGE));
        
        // Reset to first page
        currentPage = 1;
        
        // Generate the current page
        GeneratePage(currentPage);
    }

    private void GeneratePage(int pageNumber)
    {
        // Clear existing content
        foreach (Transform child in pageOneContentHolder.transform)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in pageTwoContentHolder.transform)
        {
            Destroy(child.gameObject);
        }

        int startIndex = (pageNumber - 1) * FISH_PER_PAGE;
        
        for (int i = 0; i < PAGE_ONE_FISH_SIZE; i++)
        {
            int fishIndex = startIndex + i;
            if (fishIndex < allFishList.Count)
            {
                GameObject newFishdexFish = Instantiate(fishdexContentPrefab, pageOneContentHolder.transform);
                newFishdexFish.GetComponent<FishdexFishUIBuilder>().BuildFishdexFish(allFishList[fishIndex]);
            }
        }

        for (int i = 0; i < PAGE_TWO_FISH_SIZE; i++)
        {
            int fishIndex = startIndex + PAGE_ONE_FISH_SIZE + i;
            if (fishIndex < allFishList.Count)
            {
                GameObject newFishdexFish = Instantiate(fishdexContentPrefab, pageTwoContentHolder.transform);
                newFishdexFish.GetComponent<FishdexFishUIBuilder>().BuildFishdexFish(allFishList[fishIndex]);
            }
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
            OverviewButtonClicked();
        }
    }

    public void CloseFishdex()
    {
        fishdexObject.SetActive(false);
    }

    private void ResetButtonPositions()
    {
        searchButton.anchoredPosition = searchButtonDefaultPos;
        overviewButton.anchoredPosition = overviewButtonDefaultPos;
    }

    private void ClearPages()
    {
        FishOverviewObject.SetActive(false);
        FishInfoObject.SetActive(false);
    }

    // Called from button in game
    public void SearchButtonClicked()
    {
        ResetButtonPositions();

        Vector2 pos = searchButton.anchoredPosition;
        pos.x = searchButtonDefaultPos.x - 30;
        searchButton.anchoredPosition = pos;
    }

    // Called from button in game
    public void OverviewButtonClicked()
    {
        ResetButtonPositions();

        Vector2 pos = overviewButton.anchoredPosition;
        pos.x = overviewButtonDefaultPos.x - 30;
        overviewButton.anchoredPosition = pos;

        ClearPages();
        BuildFishdex();
        FishOverviewObject.SetActive(true);
    }

    // Called from button in game
    public void NextPageButtonClicked()
    {
        if (currentPage < totalPages)
        {
            currentPage++;
            GeneratePage(currentPage);
        }
    }

    // Called from button in game
    public void PreviousPageButtonClicked()
    {
        if (currentPage > 1)
        {
            currentPage--;
            GeneratePage(currentPage);
        }
    }

    // Called from button in game
    public void LastPageButtonClicked()
    {
        if (currentPage != totalPages)
        {
            currentPage = totalPages;
            GeneratePage(currentPage);
        }
    }

    // Called from button in game
    public void FirstPageButtonClicked()
    {
        if (currentPage != 1)
        {
            currentPage = 1;
            GeneratePage(currentPage);
        }
    }

    public void ShowFishInfo(ItemDefinition fish)
    {
        currentShowingFishIndex = allFishList.FindIndex(f => f.Id == fish.Id);
        ClearPages();
        ResetButtonPositions();
        FishInfoObject.SetActive(true);

        fishInfoUIManager.LoadFishInfo(fish.Id);
    }

    // Called from button in game
    public void ShowNextFish()
    {
        currentShowingFishIndex = (currentShowingFishIndex + 1) % allFishList.Count;
        ShowFishInfo(allFishList[currentShowingFishIndex]);
    }

    // Called from button in game
    public void ShowPreviousFish()
    {
        currentShowingFishIndex = (currentShowingFishIndex - 1 + allFishList.Count) % allFishList.Count;
        ShowFishInfo(allFishList[currentShowingFishIndex]);
    }
}
