using UnityEngine;

[CreateAssetMenu(fileName = "Sound", menuName = "SoundCollection")]
public class SoundSO : ScriptableObject
{
    public AudioClip[] clips;

    [Range(0f, 1f)]
    public float volume = 1f;

    [Range(0.5f, 2f)]
    public float pitch = 1f;

    public bool randomClip = true;

    public AudioClip GetClip()
    {
        if (clips == null || clips.Length == 0)
            return null;

        if (randomClip)
            return clips[Random.Range(0, clips.Length)];

        return clips[0];
    }
}