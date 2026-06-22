using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GameAudio : MonoBehaviour
{
    [Header("Gameplay SFX")]
    public AudioClip ballHitClip;
    public AudioClip vehicleCollisionClip;
    public AudioClip goalHornClip;
    public AudioClip kickoffWhistleClip;
    public AudioClip boostClip;
    public AudioClip tankFireClip;
    public AudioClip explosionClip;

    [Header("Crowd")]
    public AudioClip crowdAmbienceClip;
    public AudioClip goalCheerClip;
    public AudioClip[] crowdClips;

    [Header("Menu")]
    public AudioClip  menuMusicClip;
    public AudioClip  buttonClickClip;

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

    public void PlayBallHit()           => Play(ballHitClip);
    public void PlayVehicleCollision()  => Play(vehicleCollisionClip);
    public void PlayGoal()              => Play(goalHornClip);
    public void PlayKickoff()           => Play(kickoffWhistleClip);
    public void PlayBoost()             => Play(boostClip);
    public void PlayTankFire()          => Play(tankFireClip);
    public void PlayExplosion()         => Play(explosionClip);

    // ── Crowd ─────────────────────────────────────────────────────────────────

    public void StartCrowdAmbience()
    {
        if (crowdSource == null || crowdAmbienceClip == null) return;
        crowdSource.clip = crowdAmbienceClip;
        crowdSource.volume = 0.3f;
        crowdSource.Play();
    }

    public void PlayGoalCheer()
    {
        if (crowdSource == null || goalCheerClip == null) return;
        crowdSource.volume = 1f;
        crowdSource.PlayOneShot(goalCheerClip);
    }

    public void PlayCrowd(float volume = 1f)
    {
        if (crowdClips == null || crowdClips.Length == 0) return;
        Play(crowdClips[Random.Range(0, crowdClips.Length)], volume);
    }

    // ── Music ─────────────────────────────────────────────────────────────────

    public void PlayMenuMusic()
    {
        if (musicSource == null || menuMusicClip == null) return;
        musicSource.clip = menuMusicClip;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }

    // ── Menu ──────────────────────────────────────────────────────────────────

    public void PlayButtonClick() => Play(buttonClickClip);

    // ── Helper ────────────────────────────────────────────────────────────────

    void Play(AudioClip clip, float volume = 1f)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }
}
