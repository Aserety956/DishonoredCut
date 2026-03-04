using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I { get; private set; }

    [Header("Mixer (optional)")]
    [SerializeField] private AudioMixerGroup ambienceGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("Ambience Source (for looping/2D)")]
    [SerializeField] private AudioSource ambienceSource;

    [Header("SFX Pool")]
    [SerializeField] private int sfxPoolSize = 8;

    private readonly List<AudioSource> sfxPool = new();

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);

        if (ambienceSource == null)
        {
            ambienceSource = gameObject.AddComponent<AudioSource>();
            ambienceSource.playOnAwake = false;
        }
        ambienceSource.outputAudioMixerGroup = ambienceGroup;

        for (int i = 0; i < sfxPoolSize; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.outputAudioMixerGroup = sfxGroup;
            sfxPool.Add(src);
        }
    }

    // ===== Ambience =====

    /// <summary>Запускает амбиент (обычно loop, 2D). Если sound.loop=false — всё равно будет проигран один раз.</summary>
    public void PlayAmbience(SoundData sound)
    {
        if (sound == null) return;

        var clip = sound.GetRandomClip();
        if (clip == null) return;

        ambienceSource.Stop();

        ambienceSource.clip = clip;
        ambienceSource.volume = sound.volume;
        ambienceSource.pitch = sound.GetPitch();
        ambienceSource.loop = sound.loop;

        // для амбиента почти всегда 2D
        ambienceSource.spatialBlend = 0f;

        ambienceSource.Play();
    }

    public void StopAmbience()
    {
        ambienceSource.Stop();
        ambienceSource.clip = null;
    }

    // ===== SFX =====

    public void Play(SoundData sound, Vector3 worldPos)
    {
        if (sound == null) return;

        var clip = sound.GetRandomClip();
        if (clip == null) return;

        // Если это loop — лучше не через пул one-shot (чтобы не зависло).
        // В твоих кейсах SFX (треск) — точно не loop.
        if (sound.loop)
        {
            // Для loop-SFX проще выделять отдельный источник/объект.
            // Пока просто проигнорируем, чтобы не создать баг.
            Debug.LogWarning($"Sound '{sound.name}' is looped. Use PlayAmbience or a dedicated looping source.");
            return;
        }

        var src = GetFreeSfxSource();
        src.transform.position = worldPos;

        src.clip = clip;
        src.volume = sound.volume;
        src.pitch = sound.GetPitch();
        src.loop = false;

        src.spatialBlend = (sound.spatial == SoundData.SpatialMode.TwoD) ? 0f : 1f;
        if (src.spatialBlend > 0f)
        {
            src.minDistance = sound.minDistance;
            src.maxDistance = sound.maxDistance;
        }

        src.Play();
    }

    public void Play2D(SoundData sound)
    {
        // Удобный хелпер для 2D SFX (клики UI и т.п.)
        Play(sound, transform.position);
    }

    private AudioSource GetFreeSfxSource()
    {
        for (int i = 0; i < sfxPool.Count; i++)
            if (!sfxPool[i].isPlaying)
                return sfxPool[i];

        return sfxPool[0];
    }
}
