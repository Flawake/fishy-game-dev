using System.Collections;
using ItemSystem;
using UnityEngine;

public class ShowCaughtFishOnPlayer : MonoBehaviour
{
    [SerializeField]
    SpriteRenderer spriteRenderer;
    [SerializeField]
    SpriteRenderer bloom;

    Vector3 spriteStartPos;

    void Awake()
    {
        spriteStartPos = transform.localPosition;
    }

    public void ShowFish(int fishID)
    {
        transform.localPosition = spriteStartPos;
        spriteRenderer.color = Color.white;
        bloom.color = Color.white;
        enableRenderers();
        spriteRenderer.sprite = ItemRegistry.Get(fishID).Icon;
        StartCoroutine(HideFish());
    }

    IEnumerator HideFish()
    {
        yield return new WaitForSeconds(3f);
        Color c = spriteRenderer.material.color;
        for (float alpha = 1; alpha >= 0; alpha -= 0.05f)
        {
            c.a = alpha;
            spriteRenderer.color = c;
            bloom.color = c;
            Vector3 position = transform.localPosition;
            position.y += 0.04f;
            transform.localPosition = position;
            yield return new WaitForSeconds(0.02f);
        }
        disableRenderers();

    }

    private void enableRenderers()
    {
        spriteRenderer.enabled = true;
        bloom.enabled = true;
    }

    private void disableRenderers()
    {
        spriteRenderer.enabled = false;
        bloom.enabled = false;
    }
}
