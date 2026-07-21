using System.Collections;
using UnityEngine;

public class FishSpotRenderer : MonoBehaviour
{
    [SerializeField]
    SpriteRenderer spriteRenderer;

    internal void Create(FishSpot spot, FishSpotType spotType)
    {
        transform.position = spot.centrePoint;
        transform.localScale = spot.size / 1.5f;
        switch (spotType)
        {
            case FishSpotType.Uninitialized:
                spriteRenderer.color = Color.red;
                break;
            case FishSpotType.Bad:
                spriteRenderer.color = Color.red;
                break;
            case FishSpotType.Normal:
                spriteRenderer.color = Color.green;
                break;
            case FishSpotType.Good:
                spriteRenderer.color = Color.blue;
                break;
            case FishSpotType.Perfect:
                spriteRenderer.color = Color.magenta;
                break;
        }
        StartCoroutine(FadeSpot());
    }

    private IEnumerator FadeSpot()
    {
        Color c = spriteRenderer.color;
        while (true)
        {
            yield return new WaitForSeconds(0.4f);
            for (float alpha = 0.7f; alpha >= 0; alpha -= 0.02f)
            {
                c.a = alpha;
                spriteRenderer.color = c;
                yield return new WaitForSeconds(0.04f);
            }
            
            yield return new WaitForSeconds(5f);
            for (float alpha = 0; alpha < 0.7f; alpha += 0.05f)
            {
                c.a = alpha;
                spriteRenderer.color = c;
                yield return new WaitForSeconds(0.04f);
            }
        }
    }
}
