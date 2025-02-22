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

    private bool right_pressed;
    private bool left_pressed;

    [SerializeField] GameObject fishFightDialog;
    [SerializeField] GameObject fightSlider;
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
    float sail;
    float sailRandom;
    int idle;
    float progress;
    int rarity;
    int gameSize;
    int realGameSize;
    float relativePos;
    [SerializeField]
    float sensitivity = 0.5f;
    public bool initialized = false;

    int minFishingTimeSeconds;

    void RandomGameSize(int minFishingTime)
    {
        int random = Random.Range(100, 200);
        gameSize = random * minFishingTime;
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

    public void StartFight(CurrentFish currentFish, int minFishingTime)
    {
        playerData = GetComponentInParent<PlayerData>();

        RandomGameSize(minFishingTime);

        sail = 0;
        sailRandom = Random.Range(0f, 1f);
        idle = 0;
        progress = 0;
        realGameSize = 0;
        relativePos = 50f;

        currentFishOnHook = currentFish;

        //Returns number between 0.97 and 1.06
        float randomFishPowerMultiplier = (11f + Random.Range(0f, 1f) - 0.33f) / 11f;
        //Add 50 as a offset, so that the fight power of the fish and rod do not become too small
        fishPower = 50 + (currentFishOnHook.length * randomFishPowerMultiplier);
        rodPower = 50 + playerData.GetSelectedRod().strength;

        rarity = FishEnumConfig.RatityToInt(currentFishOnHook.rarity);
        redLeft.sizeDelta = new Vector2((1.0f / 8.0f * rarity + 1.0f / 8.0f) / 2.0f * fishFightArea.rect.width, 50);
        redRight.sizeDelta = new Vector2((1.0f / 8.0f * rarity + 1.0f / 8.0f) / 2.0f * fishFightArea.rect.width, 50);
        fishFightMaterial.SetFloat("_Rarity", rarity);
        progressBar.value = progressBar.minValue;
        minFishingTimeSeconds = minFishingTime;
        
        initialized = true;
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

        if (progress / 100 > progressBar.maxValue)
        {
            FightDone(FishingManager.EndFishingReason.caughtFish);
        }
        //minValue is 0, but we start with 0 so that would make us instantly fail. Check if it is less than 0 instead.
        else if (progress < 0)
        {
            FightDone(FishingManager.EndFishingReason.lostFish);
        }
    }

    void ReelInFish()
    {
        float newSail;
        float sail1 = Random.Range(0f, 1f) - 0.5f;
        float sail2 = Random.Range(0f, 1f) - 0.5f;
        float power;
        realGameSize++;

        if(Mathf.Abs(sail1) > Mathf.Abs(sail2))
        {
            newSail = sail1;
        }
        else
        {
            newSail = sail2;
        }
        if(left_pressed || right_pressed)
        {
            idle = 0;
        }
        else
        {
            idle++;
        }
        if(progress < 40)
        {
            newSail = newSail * progress / 40;
        }
        if(idle > 40)
        {
            newSail = newSail * idle / 40;
        }
        if (Random.Range(0f, 1f) < 0.5f && (newSail < 0 && sail > 0 || newSail > 0 && sail < 0))
        {
            newSail = -newSail;
        }
        if(fishPower > rodPower * 1.2f)
        {
            power = fishPower / rodPower * 2.4f;
            if((left_pressed || right_pressed) && Random.Range(0f, 1f) < 0.12f + rarity / 60)
            {
                sail = -2.2f * sail;
            } 
        }
        else if(fishPower > rodPower)
        {
            power = fishPower / rodPower * 1.8f;
            if ((left_pressed || right_pressed) && Random.Range(0f, 1f) < 0.08f + rarity / 60)
            {
                sail = -1.6f * sail;
            }
        }
        else
        {
            power = fishPower / rodPower * 0.8f;
            if ((left_pressed || right_pressed) && Random.Range(0f, 1f) < 0.04f + rarity / 70)
            {
                sail = -0.4f * sail;
            }
        }
        if(power < 0.1f)
        {
            power = 0.1f;
        }
        power *= 0.7f + rarity / 3;
        power += realGameSize / gameSize / 3;
        //
        power *= 3f;
        sail += newSail * power;
        sail = sail * (7 + sailRandom - 0.5f) / 7;
        if (left_pressed && !right_pressed)
        {
            relativePos -= 1f - rarity / 20f + sensitivity;
            sail *= 0.8f;
            if(sail > 0 && Random.Range(0f, 1f) < 0.08f)
            {
                sail = -0.8f * sail;
            }
        }
        else if (!left_pressed && right_pressed)
        {
            relativePos += 1f - rarity / 20f + sensitivity;
            sail *= 0.8f;
            if (sail < 0 && Random.Range(0f, 1f) < 0.08f)
            {
                sail = -0.8f * sail;
            }
        }
        relativePos += sail;
    }

    private void Start()
    {
        fishingManager = GetComponentInParent<FishingManager>();
        playerData = GetComponentInParent<PlayerData>();
    }

    private void FixedUpdate()
    {
        if (!initialized)
            return;
        ReelInFish();

        if(relativePos + fightSlider.GetComponent<RectTransform>().rect.width / 2 > fishFightArea.rect.width / 2)
        {
            relativePos = (fishFightArea.rect.width / 2) + (-fightSlider.GetComponent<RectTransform>().rect.width / 2);
            FightDone(FishingManager.EndFishingReason.lostFish);
        }
        else if (relativePos - fightSlider.GetComponent<RectTransform>().rect.width / 2 < -fishFightArea.rect.width / 2)
        {
            relativePos = (-fishFightArea.rect.width / 2) + (fightSlider.GetComponent<RectTransform>().rect.width / 2);
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
