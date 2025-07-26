using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ItemSystem;
using System;

public class PlayerStatsOverlayUIManager : MonoBehaviour
{
    [SerializeField]
    Image luckpotionImage;
    [SerializeField]
    TMP_Text luckpotionTimeLeft;
    [SerializeField]
    Image magicwatchImage;
    [SerializeField]
    TMP_Text magicwatchTimeLeft;

    private PlayerData playerData;

    private void Start()
    {
        // Find the PlayerData component in the parent hierarchy
        playerData = GetComponentInParent<PlayerData>();
        if (playerData == null)
        {
            Debug.LogWarning("PlayerStatsOverlayUIManager: Could not find PlayerData component");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (playerData == null)
        {
            return;
        }

        UpdateSpecialEffectsUI();
    }

    private void UpdateSpecialEffectsUI()
    {
        var activeEffects = playerData.GetActiveSpecialEffects();
        
        // Update luck potion UI
        bool hasLuckBoost = activeEffects.TryGetValue(SpecialEffectType.LuckBoost, out var luckEffect);
        UpdateEffectUI(luckpotionImage, luckpotionTimeLeft, hasLuckBoost, luckEffect.expiry);

        // Update magic watch UI
        bool hasWaitTimeReduction = activeEffects.TryGetValue(SpecialEffectType.WaitTimeReduction, out var waitEffect);
        UpdateEffectUI(magicwatchImage, magicwatchTimeLeft, hasWaitTimeReduction, waitEffect.expiry);
    }

    private void UpdateEffectUI(Image effectImage, TMP_Text timeText, bool hasEffect, DateTime expiry)
    {
        if (hasEffect)
        {
            // Show the effect UI
            effectImage.gameObject.SetActive(true);
            
            // Calculate remaining time
            TimeSpan remainingTime = expiry - DateTime.UtcNow;
            
            if (remainingTime.TotalSeconds > 0)
            {
                // Format time display
                string timeDisplay = FormatTimeRemaining(remainingTime);
                timeText.text = timeDisplay;
                
                // Set image color to white when active
                effectImage.color = Color.white;
                
                // Optional: Change color based on remaining time
                if (remainingTime.TotalMinutes < 1)
                {
                    timeText.color = Color.red; // Less than 1 minute remaining
                }
                else if (remainingTime.TotalMinutes < 5)
                {
                    timeText.color = Color.yellow; // Less than 5 minutes remaining
                }
                else
                {
                    timeText.color = Color.white; // Normal time remaining
                }
            }
            else
            {
                // Effect has expired, show "xx:xx" and set image to gray
                timeText.text = "xx:xx";
                effectImage.color = new Color(40f/255f, 40f/255f, 40f/255f, 1f); // RGB(40, 40, 40)
                timeText.color = Color.white;
            }
        }
        else
        {
            // No active effect, show "xx:xx" and set image to gray
            effectImage.gameObject.SetActive(true);
            timeText.text = "xx:xx";
            effectImage.color = new Color(40f/255f, 40f/255f, 40f/255f, 1f); // RGB(40, 40, 40)
            timeText.color = Color.white;
        }
    }

    private string FormatTimeRemaining(TimeSpan timeSpan)
    {
        if (timeSpan.TotalMinutes >= 1)
        {
            // Show minutes and seconds
            return $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }
        else
        {
            // Show only seconds when less than 1 minute
            return $"{timeSpan.Seconds:D1}s";
        }
    }
}
