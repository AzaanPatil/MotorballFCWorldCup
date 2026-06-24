using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GoalSequence : MonoBehaviour
{
    public static GoalSequence Instance { get; private set; }

    [Header("Banner")]
    public GameObject goalBanner;
    public float bannerDuration = 2.5f;

    [Header("Effects")]
    public float shakeDuration  = 0.4f;
    public float shakeMagnitude = 0.35f;

    [Header("Flash")]
    public Color flashColor    = new Color(1f, 0.8f, 0f);
    public float flashDuration = 0.5f;

    // Auto-resolved in Awake
    private TextMeshProUGUI bannerText;
    private ParticleSystem  confettiParticles;
    private GameAudio       gameAudio;
    private Scorebug        scorebug;

    void Awake()
    {
        Instance = this;

        if (goalBanner != null)
        {
            goalBanner.SetActive(false);
            bannerText = goalBanner.GetComponentInChildren<TextMeshProUGUI>();
        }

        confettiParticles = GetComponentInChildren<ParticleSystem>();
        gameAudio         = FindAnyObjectByType<GameAudio>();
        scorebug          = FindAnyObjectByType<Scorebug>();
    }

    public void Play(bool teamAScored, string scoringTeamName)
    {
        StopAllCoroutines();
        StartCoroutine(Sequence(teamAScored, scoringTeamName));
    }

    IEnumerator Sequence(bool teamAScored, string scoringTeamName)
    {
        if (goalBanner != null)
        {
            if (bannerText != null) bannerText.text = $"GOAL!\n{scoringTeamName}";
            goalBanner.SetActive(true);
        }

        if (ScreenShake.Instance != null)
            ScreenShake.Instance.Shake(shakeDuration, shakeMagnitude);

        if (confettiParticles != null)
            confettiParticles.Play();

        if (scorebug != null)
        {
            Image flash = teamAScored ? scorebug.teamAScoreFlash : scorebug.teamBScoreFlash;
            if (flash != null) StartCoroutine(Flash(flash));
        }

        if (gameAudio != null)
        {
            if (teamAScored) gameAudio.PlayGoal();
            if (teamAScored) gameAudio.PlayGoalCheer();
            else             gameAudio.PlayAwayGoalReaction();
        }

        yield return new WaitForSeconds(bannerDuration);

        if (goalBanner != null) goalBanner.SetActive(false);
        if (confettiParticles != null) confettiParticles.Stop();
    }

    IEnumerator Flash(Image img)
    {
        Color original = img.color;
        img.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        img.color = original;
    }
}
