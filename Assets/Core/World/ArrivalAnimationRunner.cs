using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class ArrivalAnimationRunner : MonoBehaviour
{
    public bool IsRunning { get; private set; }

    public void StartArrivalAnimation(WorldTravel.CustomSpawnInstruction instruction, Area area)
    {
        if (!NetworkClient.active || IsRunning)
        {
            return;
        }
        StopAllCoroutines();
        switch (instruction)
        {
            case WorldTravel.CustomSpawnInstruction.WalkOusideBakery:
                StartCoroutine(PlayWalkOutsideBakery(area));
                break;
            case WorldTravel.CustomSpawnInstruction.None:
            default:
                StartCoroutine(FadeInOnly());
                break;
        }
    }

    IEnumerator PlayWalkOutsideBakery(Area area)
    {
        IsRunning = true;
        PlayerController controller = GetComponent<PlayerController>();
        if (controller == null)
        {
            IsRunning = false;
            yield break;
        }

        // Fade in while computing and walking
        Coroutine fade = StartCoroutine(FadeIn(0.52f));

        // Compute path from current position to arrival target if configured
        Vector3? target = SpawnPointProvider.TryGetArrivalTarget(area, WorldTravel.CustomSpawnInstruction.WalkOusideBakery);
        if (target.HasValue)
        {
            PathFinding pathFinder = SceneObjectCache.GetPathFinding(gameObject.scene);
            bool done = false;
            List<Vector2> result = null;
            pathFinder.QueueNewPath(transform.position, target.Value, gameObject, (List<Vector2> path) =>
            {
                result = path;
                done = true;
            });
            // wait for callback
            while (!done)
            {
                yield return null;
            }
            if (result != null && result.Count > 0)
            {
                controller.SetScriptedPath(result);
                // briefly wait until path is consumed
                float timeout = 3f;
                float t = 0f;
                while (controller.HasPendingScriptedPath() && t < timeout)
                {
                    t += Time.deltaTime;
                    yield return null;
                }
            }
        }

        if (fade != null)
        {
            yield return fade;
        }
        IsRunning = false;
        controller.EndTravelLock();
    }

    IEnumerator FadeInOnly()
    {
        yield return FadeIn(0.52f);
    }

    IEnumerator FadeIn(float duration)
    {
        SpriteRenderer[] renders = GetComponentsInChildren<SpriteRenderer>(true);
        // set alpha to 0
        foreach (var r in renders)
        {
            Color c = r.color; c.a = 0f; r.color = c;
        }
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / duration);
            foreach (var r in renders)
            {
                Color c = r.color; c.a = a; r.color = c;
            }
            yield return null;
        }
    }
} 