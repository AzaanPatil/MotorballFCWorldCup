using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GoalSequence : MonoBehaviour
{
    public static GoalSequence Instance { get; private set; }

    [Header("Banner")]
    public GameObject goalBanner;
    public TextMeshProUGUI bannerText;
    public float bannerDuration = 2.5f;

    [Header("Scorebug Flash")]
    public Image teamAScoreFlash;
    public Image teamBScoreFlash;
    public Color flashColor = new Color(1f, 0.8f, 0f);
    public float flashDuration = 0.5f;

    [Header("Effects")]
    public ParticleSystem confettiParticles;
    public float shakeDuration  = 0.4f;
    public float shakeMagnitude = 0.35f;

    [Header("Audio")]
    public GameAudio gameAudio;

    void Awake()
    {
        Instance = this;
        if (goalBanner != null) goalBanner.SetActive(false);
    }

    public void Play(bool teamAScored, string scoringTeamName)
    {
        StopAllCoroutines();
        StartCoroutine(Sequence(teamAScored, scoringTeamName));
    }

    IEnumerator Sequence(bool teamAScored, string scoringTeamName)
    {
        // Banner
        if (goalBanner != null)
        {
            if (bannerText != null) bannerText.text = $"GOAL!\n{scoringTeamName}";
            goalBanner.SetActive(true);
        }

        // Screen shake
        if (ScreenShake.Instance != null)
            ScreenShake.Instance.Shake(shakeDuration, shakeMagnitude);

        // Confetti
        if (confettiParticles != null)
            confettiParticles.Play();

        // Scorebug flash
        Image flashTarget = teamAScored ? teamAScoreFlash : teamBScoreFlash;
        if (flashTarget != null)
            StartCoroutine(Flash(flashTarget));

        // Audio
        if (gameAudio != null)
        {
            gameAudio.PlayGoal();
            gameAudio.PlayGoalCheer();
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
