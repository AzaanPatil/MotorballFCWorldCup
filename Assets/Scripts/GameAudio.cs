using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GameAudio : MonoBehaviour
{
	// Audio clips for major game events.
	public AudioClip goalClip;
	public AudioClip kickoffClip;
	public AudioClip[] crowdClips;

	AudioSource src;

	void Awake()
	{
		// Cache the audio source used for SFX playback.
		src = GetComponent<AudioSource>();
	}

	public void PlayGoal()
	{
		if (goalClip == null || src == null)
			return;

		// Play the goal sound once.
		src.PlayOneShot(goalClip);
	}

	public void PlayKickoff()
	{
		if (kickoffClip == null || src == null)
			return;

		// Play the kickoff sound once.
		src.PlayOneShot(kickoffClip);
	}

	public void PlayCrowd(float volume = 1f)
	{
		if (crowdClips == null || crowdClips.Length == 0 || src == null)
			return;

		// Choose a random crowd clip for variety.
		var clip = crowdClips[Random.Range(0, crowdClips.Length)];
		src.PlayOneShot(clip, Mathf.Clamp01(volume));
	}
}
