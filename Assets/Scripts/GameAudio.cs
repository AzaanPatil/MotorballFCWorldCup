using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GameAudio : MonoBehaviour
{
    [Header("Gameplay SFX")]
    public AudioClip ballHitClip;
    [Range(0f, 1f)] public float ballHitVolume = 1f;

    public AudioClip vehicleCollisionClip;
    [Range(0f, 1f)] public float vehicleCollisionVolume = 0.8f;

    public AudioClip goalHornClip;
    [Range(0f, 1f)] public float goalHornVolume = 1f;

    public AudioClip kickoffWhistleClip;
    [Range(0f, 1f)] public float kickoffWhistleVolume = 0.9f;

    public AudioClip boostClip;
    [Range(0f, 1f)] public float boostVolume = 0.6f;

    public AudioClip tankFireClip;
    [Range(0f, 1f)] public float tankFireVolume = 1f;

    public AudioClip explosionClip;
    [Range(0f, 1f)] public float explosionVolume = 1f;

    [Header("Crowd")]
    public AudioClip crowdAmbienceClip;
    [Range(0f, 1f)] public float crowdAmbienceVolume = 0.3f;

    public AudioClip goalCheerClip;
    [Range(0f, 1f)] public float goalCheerVolume = 1f;

    public AudioClip awayGoalReactionClip;
    [Range(0f, 1f)] public float awayGoalReactionVolume = 0.6f;

    public AudioClip[] crowdClips;
    [Range(0f, 1f)] public float crowdVolume = 1f;

    [Header("Menu")]
    public AudioClip menuMusicClip;
    [Range(0f, 1f)] public float menuMusicVolume = 0.8f;

    public AudioClip buttonClickClip;
    [Range(0f, 1f)] public float buttonClickVolume = 0.8f;

    [Header("Sources")]
    public AudioSource sfxSource;
    public AudioSource musicSource;
    public AudioSource crowdSource;

    void Awake()
    {
        if (sfxSource   == null) sfxSource   = GetComponent<AudioSource>();
        if (musicSource == null) musicSource  = gameObject.AddComponent<AudioSource>();
        if (crowdSource == null) crowdSource  = gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
        crowdSource.loop = true;
    }

    // ── Gameplay ──────────────────────────────────────────────────────────────

    public void PlayBallHit()           => Play(ballHitClip,           ballHitVolume);
    public void PlayVehicleCollision()  => Play(vehicleCollisionClip,  vehicleCollisionVolume);
    public void PlayGoal()              => Play(goalHornClip,           goalHornVolume);
    public void PlayKickoff()           => Play(kickoffWhistleClip,    kickoffWhistleVolume);
    public void PlayBoost()             => Play(boostClip,             boostVolume);
    public void PlayTankFire()          => Play(tankFireClip,          tankFireVolume);
    public void PlayExplosion()         => Play(explosionClip,         explosionVolume);

    // ── Crowd ─────────────────────────────────────────────────────────────────

    public void StartCrowdAmbience()
    {
        if (crowdSource == null || crowdAmbienceClip == null) return;
        crowdSource.clip   = crowdAmbienceClip;
        crowdSource.volume = crowdAmbienceVolume;
        crowdSource.Play();
    }

    public void PlayGoalCheer()
    {
        if (crowdSource == null || goalCheerClip == null) return;
        crowdSource.volume = goalCheerVolume;
        crowdSource.PlayOneShot(goalCheerClip);
    }

    public void PlayAwayGoalReaction()
    {
        if (crowdSource == null) return;
        crowdSource.volume = awayGoalReactionVolume;
        if (awayGoalReactionClip != null) crowdSource.PlayOneShot(awayGoalReactionClip);
    }

    public void PlayCrowd(float volumeOverride = -1f)
    {
        if (crowdClips == null || crowdClips.Length == 0) return;
        Play(crowdClips[Random.Range(0, crowdClips.Length)], volumeOverride >= 0f ? volumeOverride : crowdVolume);
    }

    // ── Music ─────────────────────────────────────────────────────────────────

    public void PlayMenuMusic()
    {
        if (musicSource == null || menuMusicClip == null) return;
        musicSource.clip   = menuMusicClip;
        musicSource.volume = menuMusicVolume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }

    // ── Menu ──────────────────────────────────────────────────────────────────

    public void PlayButtonClick() => Play(buttonClickClip, buttonClickVolume);

    // ── Helper ────────────────────────────────────────────────────────────────

    void Play(AudioClip clip, float volume = 1f)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }
}
