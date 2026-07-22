using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class NotificationRenderer : MonoBehaviour
{
    [SerializeField] private TMP_Text notificationText;
    [SerializeField] private CanvasGroup canvasGroup;

    public event Action<NotificationRenderer> OnFinished;

    public void Render(Notification notification)
    {
        notificationText.text = notification.message;

        canvasGroup.alpha = 1f;

        StartCoroutine(FadeNotification());
    }

    public void MoveTo(Vector2 target)
    {
        StopCoroutine(nameof(MoveRoutine));
        StartCoroutine(MoveRoutine(target));
    }

    private IEnumerator MoveRoutine(Vector2 target)
    {
        RectTransform rect = GetComponent<RectTransform>();

        Vector2 start = rect.anchoredPosition;
        float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            rect.anchoredPosition = Vector2.Lerp(start, target, elapsed / duration);
            yield return null;
        }

        rect.anchoredPosition = target;
    }

    private IEnumerator FadeNotification()
    {
        yield return new WaitForSeconds(5f);

        float fadeDuration = 1.5f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }

        OnFinished?.Invoke(this);

        Destroy(gameObject);
    }
}
