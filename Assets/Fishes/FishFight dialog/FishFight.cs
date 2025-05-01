using Microsoft.Cci;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class FishFight : MonoBehaviour
{
    private enum FishReelDifficulty
    {
        normal,
        hard,
        impossible
    }

    private float startedFightTime = 0;

    private bool right_pressed;
    private bool left_pressed;

    [SerializeField] GameObject fishFightDialog;
    [SerializeField] GameObject fightSlider;
    [SerializeField] RectTransform fightSliderTransform;
    [SerializeField] Slider progressBar;
    [SerializeField] RectTransform redRight;
    [SerializeField] RectTransform redLeft;
    [SerializeField] RectTransform fishFightArea;
    [SerializeField] Material fishFightMaterial;

    FishingManager fishingManager;
    PlayerData playerData;

    CurrentFish currentFishOnHook = null;
    PlayerControls playerControls;

    float rodPower;
    float fishPower;
    float totalFishPull;
    float sailRandom;
    float idle;
    float progress;
    int rarity;
    int gameSize;
    float realGameSize;
    float relativePos;
    [SerializeField]
    float sensitivity = 0.5f;
    public bool initialized = false;

    int minFishingTimeSeconds;

    int RandomGameSize(int minFishingTime)
    {
        int random = Random.Range(100, 200);
        return random * minFishingTime;
    }

    void FightDone(FishingManager.EndFishingReason reason)
    {
        if (reason == FishingManager.EndFishingReason.lostFish)
        {
            fightSlider.transform.localPosition = new Vector2(relativePos, fightSlider.transform.localPosition.y);
            fishingManager.EndFishing(FishingManager.EndFishingReason.lostFish);
        }
        else if (reason == FishingManager.EndFishingReason.caughtFish)
        {
            fishingManager.EndFishing(FishingManager.EndFishingReason.caughtFish);
        }
    }

    public float CalculateFishPower(int fishSize) {
        //Returns number between 0.97 and 1.06
        float randomFishPowerMultiplier = (11f + Random.Range(0f, 1f) - 0.33f) / 11f;
        //Add 50 as a offset, so that the fight power of the fish and rod do not become too small
        return 50 + (fishSize * randomFishPowerMultiplier);
    }

    public float CalculateRodPower(int rodStrength, float fishPower)
    {
        if(rodStrength / 2 > fishPower)
        {
            return 50 + fishPower * 3;
        }
        return 50 + rodStrength;
    }

    public void StartFight(CurrentFish currentFish, int minFishingTime)
    {
        playerData = GetComponentInParent<PlayerData>();
        Debug.Log($"FishLength: {currentFish.length}");
        Debug.Log($"RodStrength: {playerData.GetSelectedRod().strength}");

        gameSize = RandomGameSize(minFishingTime);

        totalFishPull = 0;
        sailRandom = Random.Range(0f, 1f);
        sensitivity = Random.Range(0f, 1f);
        idle = 0;
        progress = 0;
        realGameSize = 0;
        relativePos = 50f;

        currentFishOnHook = currentFish;

        fishPower = CalculateFishPower(currentFishOnHook.length);
        rodPower = CalculateRodPower(playerData.GetSelectedRod().strength, fishPower);

        rarity = FishEnumConfig.RatityToInt(currentFishOnHook.rarity);
        redLeft.sizeDelta = new Vector2((1.0f / 8.0f * rarity + 1.0f / 8.0f) / 2.0f * fishFightArea.rect.width, 50);
        redRight.sizeDelta = new Vector2((1.0f / 8.0f * rarity + 1.0f / 8.0f) / 2.0f * fishFightArea.rect.width, 50);
        fishFightMaterial.SetFloat("_Rarity", rarity);
        progressBar.value = progressBar.minValue;
        minFishingTimeSeconds = minFishingTime;
        
        startedFightTime = Time.time;
        initialized = true;
        StartCoroutine(startMiniGame());
    }

    public void EndFight()
    {
        initialized = false;
    }

    public void OnRightArrowKey(InputAction.CallbackContext rightKey)
    {
        if (!fishFightDialog.activeInHierarchy)
        {
            return;
        }

        if (rightKey.started)
        {
            right_pressed = true;
        }
        else if (rightKey.canceled)
        {
            right_pressed = false;
        }
    }

    public void OnLeftArrowKey(InputAction.CallbackContext leftKey)
    {
        if (!fishFightDialog.activeInHierarchy)
        {
            return;
        }

        if (leftKey.started)
        {
            left_pressed = true;
        }
        else if (leftKey.canceled)
        {
            left_pressed = false;
        }
    }

    private void Update()
    {
        if (!initialized)
            return;

        fightSlider.transform.localPosition = new Vector2(relativePos, fightSlider.transform.localPosition.y);
        if (fightSlider.transform.localPosition.x < redLeft.rect.width + redLeft.transform.localPosition.x || fightSlider.transform.localPosition.x > redRight.transform.localPosition.x - redRight.rect.width)
        {
            //Progress bar should sink 3 times as fast in the red as it grows in the green.
            progress -= (progressBar.maxValue * Time.deltaTime / minFishingTimeSeconds) * 100 * 3;
        }
        else
        {
            progress += (progressBar.maxValue * Time.deltaTime / minFishingTimeSeconds) * 100;
        }

        progressBar.value = progress / 100;

        // Make sure that the progress bar is filled AND the time is enough, accumelated float errors and frame timing can mess up the totalfishingtime and stop a few ms too early, which the anti-cheat than catches.
        if (progress / 100 > progressBar.maxValue && Time.time - startedFightTime > minFishingTimeSeconds)
        {
            if(Time.time - startedFightTime < minFishingTimeSeconds)
            {
                Debug.LogWarning($"That was a lil to early ({minFishingTimeSeconds - (Time.time - startedFightTime)})");
                Debug.LogWarning($"minFishingTimeSeconds: {minFishingTimeSeconds}, startedFightTime: {startedFightTime}, Time: {Time.time}");
            }
            FightDone(FishingManager.EndFishingReason.caughtFish);
        }
        //minValue is 0, but we start with 0 so that would make us instantly fail. Check if it is less than 0 instead.
        else if (progress < 0)
        {
            FightDone(FishingManager.EndFishingReason.lostFish);
        }
    }

    private IEnumerator startMiniGame() {
        var nextFrameTime = Time.realtimeSinceStartup + 0.035f;

        while (initialized)
        {
            processMinigameTick();

            // Calculate how long to wait
            var currentTime = Time.realtimeSinceStartup;
            var waitTime = nextFrameTime - currentTime;

            if (waitTime > 0)
            {
                yield return new WaitForSecondsRealtime(waitTime);
            }
            else
            {
                // If we're running behind, yield null to at least let other coroutines run
                yield return null;
            }

            nextFrameTime += 0.035f;
        }
    }

    private void processMinigameTick()
    {
        float fishPull; 
        float fishPullOption1 = Random.Range(-0.5f, 0.5f);
        float fishPullOption2 = Random.Range(-0.5f, 0.5f);
        float power;

        realGameSize++;

        // Choose the stronger fish pull
        fishPull = Mathf.Abs(fishPullOption1) > Mathf.Abs(fishPullOption2) 
            ? fishPullOption1 
            : fishPullOption2;
        
        // Reset or increase idle counter based on player input
        if(left_pressed || right_pressed)
        {
            idle = 0;
        }
        else
        {
            idle++;
        }
        
        // Scale fish pull by progress
        if(progress < 40)
        {
            fishPull *= progress / 40;
        }
        // Scale fish pull by ídleness
        if (idle > 40)
        {
            fishPull *= idle / 40;
        }
        
        // 50% change to flip the direction if the current pull vector is opposite to the total pull vector
        if (Random.Range(0f, 1f) < 0.5f && (fishPull < 0 && totalFishPull > 0) || (fishPull > 0 && totalFishPull < 0))
        {
            fishPull = -fishPull;
        }
        
        // Determine power based on fish vs rod power, and randomly scale totalFishPull
        if(fishPower > rodPower * 1.2f)
        {
            power = fishPower / rodPower * 2.4f;
            if((left_pressed || right_pressed) && Random.Range(0f, 1f) < 0.12f + rarity / 60)
            {
                totalFishPull *= -2.2f;
            } 
        }
        else if(fishPower > rodPower)
        {
            power = fishPower / rodPower * 1.8f;
            if ((left_pressed || right_pressed) && Random.Range(0f, 1f) < 0.08f + rarity / 60)
            {
                totalFishPull *= -1.6f;
            }
        }
        else
        {
            power = fishPower / rodPower * 0.8f;
            if ((left_pressed || right_pressed) && Random.Range(0f, 1f) < 0.04f + rarity / 70)
            {
                totalFishPull *= -0.4f;
            }
        }
        
        // Minimum power
        if(power < 0.1f)
        {
            power = 0.1f;
        }
        
        // Modify power based on rarity and game progress
        power *= 0.7f + rarity / 3;
        power += realGameSize / (float)gameSize / 3;
        
        //apply current fishPull to the totalFishPull
        totalFishPull += fishPull * power;
        totalFishPull *= (7 + sailRandom - 0.5f) / 7;

        // Handle player input
        if (left_pressed && !right_pressed)
        {
            relativePos -= 0.4f - rarity / 40f + sensitivity / 4;
            totalFishPull *= 0.8f;
            if(totalFishPull > 0 && Random.Range(0f, 1f) < 0.08f)
            {
                totalFishPull *= -0.8f;
            }
        }
        else if (!left_pressed && right_pressed)
        {
            relativePos += 0.4f - rarity / 40f + sensitivity / 4;
            totalFishPull *= 0.8f;
            if (totalFishPull < 0 && Random.Range(0f, 1f) < 0.08f)
            {
                totalFishPull *= -0.8f;
            }
        }
        
        // Finally, update the minigame position
        relativePos += totalFishPull * 4;
    }

    private void Start()
    {
        fishingManager = GetComponentInParent<FishingManager>();
        playerData = GetComponentInParent<PlayerData>();
    }

    private void FixedUpdate()
    {
        if (!initialized)
        {
            return;
        }

        if(relativePos + fightSliderTransform.rect.width / 2 > fishFightArea.rect.width / 2)
        {
            relativePos = (fishFightArea.rect.width / 2) + (-fightSliderTransform.rect.width / 2);
            FightDone(FishingManager.EndFishingReason.lostFish);
        }
        else if (relativePos - fightSliderTransform.rect.width / 2 < -fishFightArea.rect.width / 2)
        {
            relativePos = (-fishFightArea.rect.width / 2) + (fightSliderTransform.rect.width / 2);
            FightDone(FishingManager.EndFishingReason.lostFish);
        }
    }

    private void OnEnable()
    {
        playerControls = new PlayerControls();
        playerControls.Player.FishFightLeft.started += OnLeftArrowKey;
        playerControls.Player.FishFightLeft.canceled += OnLeftArrowKey;
        playerControls.Player.FishFightLeft.Enable();
        playerControls.Player.FishFightRight.started += OnRightArrowKey;
        playerControls.Player.FishFightRight.canceled += OnRightArrowKey;
        playerControls.Player.FishFightRight.Enable();
    }

    private void OnDisable()
    {
        playerControls.Player.FishFightLeft.Disable();
        playerControls.Player.FishFightRight.Disable();
    }
}
