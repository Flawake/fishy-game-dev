using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GlobalCompetitionSystem
{
    public class PersonalResultsUIManager : MonoBehaviour
    {
        [SerializeField] private TMP_Text placementText;
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text playerResultText;
        [SerializeField] private TMP_Text prizeAmountText;
        [SerializeField] private Image prizeSpriteImage;
    
        [SerializeField] private Sprite coinsSprite;
        [SerializeField] private Sprite bucksSprite;
    

        public void SetResults(int rank, string playerName, int playerResult, int prizeAmount, StoreManager.CurrencyType prize)
        {
            placementText.text = rank.ToString();
            playerNameText.text = playerName;
            playerResultText.text = playerResult.ToString();
            prizeAmountText.text = prizeAmount.ToString();
            prizeSpriteImage.sprite = prize switch
            {
                StoreManager.CurrencyType.COINS => coinsSprite,
                StoreManager.CurrencyType.BUCKS => bucksSprite,
                _ => throw new NotSupportedException($"Prize type {prize} is not supported")
            };

            // Hide prize display if no prize for this rank
            if (prizeAmount == 0)
            {
                prizeSpriteImage.enabled = false;
                prizeAmountText.text = "";
            }
        }
    }
}
