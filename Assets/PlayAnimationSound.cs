using Mirror;
using UnityEngine;

namespace Sounds {
public class PlayAnimationSound : MonoBehaviour
{
    public enum AnimationSound
    {
        Walk,
    }

    [SerializeField] AudioSource audioSource;
    public AnimationSound soundType;

    public void PlaySound()
    {
        if(!NetworkClient.active)
        {
            return;
        }
        SoundSO sound = null;
        switch(soundType)
        {
            case AnimationSound.Walk:
                sound = SoundManager.instance.GetWalkSound(SoundManager.CurrentClientArea);
                break;
            default:
                break;
        }
        SoundManager.instance.Play(sound, audioSource);
    }
}
}
