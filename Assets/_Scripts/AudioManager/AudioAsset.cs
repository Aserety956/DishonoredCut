using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Sound Data", fileName = "Sound_")]
public class SoundData : ScriptableObject
{
    public enum SpatialMode { TwoD, ThreeD }

    [Header("Clips (choose random)")]
    public AudioClip[] clips;

    [Header("Volume")]
    [Range(0f, 1f)] public float volume = 1f;

    [Header("Pitch")]
    public bool randomPitch = true;
    [Range(0.1f, 3f)] public float pitchMin = 0.95f;
    [Range(0.1f, 3f)] public float pitchMax = 1.05f;

    [Header("Loop")]
    public bool loop = false;

    [Header("Spatial")]
    public SpatialMode spatial = SpatialMode.TwoD;
    [Range(0f, 1f)] public float spatialBlend = 1f; // 0 = 2D, 1 = 3D
    public float minDistance = 1.5f;
    public float maxDistance = 20f;

    public AudioClip GetRandomClip()
    {
        if (clips == null || clips.Length == 0) return null;
        int i = Random.Range(0, clips.Length);
        return clips[i];
    }

    public float GetPitch()
    {
        if (!randomPitch) return 1f;
        return Random.Range(pitchMin, pitchMax);
    }

    private void OnValidate()
    {
        if (spatial == SpatialMode.TwoD) spatialBlend = 0f;
        if (spatial == SpatialMode.ThreeD) spatialBlend = 1f;

        if (pitchMin > pitchMax) pitchMin = pitchMax;
        if (minDistance < 0.01f) minDistance = 0.01f;
        if (maxDistance < minDistance) maxDistance = minDistance;
    }
}
