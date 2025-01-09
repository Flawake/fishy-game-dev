using UnityEngine;

[CreateAssetMenu(fileName = "FishConfig", menuName = "Fishes/config", order = 1), System.Serializable]
public class FishConfiguration : ScriptableObject
{
    public int id;
    public new string name;
    //TextAreaAttribute(int minLines, int maxLines);
    [TextArea(3, 10)]
    public string description;
    public Sprite fishImage;
    public int minimumLength;
    public int maximumLength;
    public int avarageLength;
    [Space(20)]
    public float minimumWeightGrams;
    public float maximumWeightGrams;
    public float avarageWeightGrams;
    [Space(20)]
    public int minMartketPrice;
    public int maxMarketPrice;
    [Space(20)]
    //1 is normal, 0.1 is 10 times as rare.
    public float rarityFactor;  
    //0 for always, has to be in format: "HH:MM - HH:MM _ HH:MM - HH:MM ..."
    public string Time;
    //0 for always, has to be in format: "Day:Month - Day:Month _ Day:Month - Day:Month ..."
    public string Date;
    [Space(20)]
    public int breedSuccessRate;
    public int breedPrice;


    public baitType baitType;
    public FishRarity rarity;
    public Locations locations; 
}
