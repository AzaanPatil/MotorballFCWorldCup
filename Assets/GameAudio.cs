using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GameAudio : MonoBehaviour
{
	public AudioClip goalClip;
	public AudioClip kickoffClip;
	public AudioClip[] crowdClips;

	AudioSource src;

	void Awake()
	{
		src = GetComponent<AudioSource>();
	}

	public void PlayGoal()
	{
		if (goalClip == null || src == null)
			return;

		src.PlayOneShot(goalClip);
	}

	public void PlayKickoff()
	{
		if (kickoffClip == null || src == null)
			return;

		src.PlayOneShot(kickoffClip);
	}

	public void PlayCrowd(float volume = 1f)
	{
		if (crowdClips == null || crowdClips.Length == 0 || src == null)
			return;

		var clip = crowdClips[Random.Range(0, crowdClips.Length)];
		src.PlayOneShot(clip, Mathf.Clamp01(volume));
	}
}
