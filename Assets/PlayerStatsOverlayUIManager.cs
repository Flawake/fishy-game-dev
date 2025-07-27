using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ItemSystem;
using System;
using System.Collections.Generic;

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

    // Store original sprites for each effect slot
    private Dictionary<Image, Sprite> originalEffectSprites = new Dictionary<Image, Sprite>();

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
        UpdateEffectUI(luckpotionImage, luckpotionTimeLeft, hasLuckBoost, luckEffect.itemId, luckEffect.expiry);

        // Update magic watch UI
        bool hasWaitTimeReduction = activeEffects.TryGetValue(SpecialEffectType.WaitTimeReduction, out var waitEffect);
        UpdateEffectUI(magicwatchImage, magicwatchTimeLeft, hasWaitTimeReduction, waitEffect.itemId, waitEffect.expiry);
    }

    private void UpdateEffectUI(Image effectImage, TMP_Text timeText, bool hasEffect, int itemId, DateTime expiry)
    {
        if (hasEffect)
        {
            // Store the original sprite if not already stored
            if (!originalEffectSprites.ContainsKey(effectImage))
                originalEffectSprites[effectImage] = effectImage.sprite;

            // Set the effect image to the sprite corresponding to the itemId
            var itemDef = ItemRegistry.Get(itemId);
            if (itemDef != null && itemDef.Icon != null)
                effectImage.sprite = itemDef.Icon;

            effectImage.gameObject.SetActive(true);
            TimeSpan remainingTime = expiry - DateTime.UtcNow;
            if (remainingTime.TotalSeconds > 0)
            {
                timeText.text = FormatTimeRemaining(remainingTime);
                effectImage.color = Color.white;
            }
            else
            {
                timeText.text = "xx:xx";
                effectImage.color = new Color(40f/255f, 40f/255f, 40f/255f, 1f);
            }
        }
        else
        {
            // Restore the original sprite if it was changed
            if (originalEffectSprites.TryGetValue(effectImage, out var originalSprite))
                effectImage.sprite = originalSprite;

            effectImage.gameObject.SetActive(true);
            timeText.text = "xx:xx";
            effectImage.color = new Color(40f/255f, 40f/255f, 40f/255f, 1f);
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
