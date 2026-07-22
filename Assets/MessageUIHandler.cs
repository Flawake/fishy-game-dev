using System.Collections.Generic;
using UnityEngine;

public class Notification
{
    public string message;
}

public class MessageUIHandler : MonoBehaviour
{
    private const int MaxMessagesRendered = 3;

    [SerializeField] private GameObject messagePrefab;
    [SerializeField] private float spacing = 150f;

    private static readonly List<Notification> notifications = new(10);

    private readonly List<NotificationRenderer> activeMessages = new();

    public static void AddNotification(Notification notification)
    {
        notifications.Add(notification);
    }

    private void Update()
    {
        while (activeMessages.Count < MaxMessagesRendered && notifications.Count > 0)
        {
            GameObject obj = Instantiate(messagePrefab, transform);
            obj.transform.localPosition = new Vector2(messagePrefab.transform.position.x, -activeMessages.Count * spacing);

            NotificationRenderer renderer = obj.GetComponent<NotificationRenderer>();

            renderer.Render(notifications[0]);
            renderer.OnFinished += RemoveMessage;

            activeMessages.Add(renderer);

            notifications.RemoveAt(0);
        }
    }

    private void RemoveMessage(NotificationRenderer renderer)
    {
        renderer.OnFinished -= RemoveMessage;

        activeMessages.Remove(renderer);

        UpdatePositions();
    }

    private void UpdatePositions()
    {
        for (int i = 0; i < activeMessages.Count; i++)
        {
            Vector2 target = new Vector2(messagePrefab.transform.position.x, -i * spacing);
            activeMessages[i].MoveTo(target);
        }
    }
}
