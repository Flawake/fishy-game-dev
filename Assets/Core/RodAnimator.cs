using Mirror;
using System.Collections;
using UnityEngine;

public class RodAnimator : NetworkBehaviour
{
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Animator animator;

    [ClientRpc]
    public void RpcThrowRod(Vector2 dir)
    {
        ThrowRod(dir);
    }

    [Client]
    public void ThrowRod(Vector2 dir)
    {
        spriteRenderer.enabled = true;
        animator.enabled = true;

        dir.Normalize();
        float angle = (Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

        // Adjust angle to 0-360 range and set 0° at the top
        angle = 90 - angle;
        if (angle < 0) {
            angle += 360;
        }

        Debug.Log($"angle: {angle}");


        if (angle >= 337.5 || angle < 22.5) {
            animator.Play("rod_throw_in_up", 0, 0f);
        }
        else if (angle >= 22.5 && angle < 67.5)
        {
            animator.Play("rod_throw_in_right_up", 0, 0f);
        }
        else if (angle >= 67.5 && angle < 112.5)
        {
            animator.Play("rod_throw_in_right", 0, 0f);
        }
        else if (angle >= 112.5 && angle < 157.5)
        {
            animator.Play("rod_throw_in_right_down", 0, 0f);
        }
        else if (angle >= 157.5 && angle < 202.5)
        {
            animator.Play("rod_throw_in_down", 0, 0f);
        }
        else if (angle >= 202.5 && angle < 247.5)
        {
            animator.Play("rod_throw_in_left_down", 0, 0f);
        }
        else if(angle >= 247.5 && angle < 292.5)
        {
            animator.Play("rod_throw_in_left", 0, 0f);
        }
        else if(angle >= 292.5 && angle < 337.5)
        {
            animator.Play("rod_throw_in_left_up", 0, 0f);
        }
        else
        {
            Debug.LogWarning($"This code should never execute, angle was: {dir}");
        }
    }

    [ClientRpc]
    public void RpcDisableRod() { 
        DisableRod();
    }

    public void DisableRod() { 
        spriteRenderer.enabled = false;
        animator.enabled = false;
    }
}
