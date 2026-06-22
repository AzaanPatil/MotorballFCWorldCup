using UnityEngine;

public class ExplosionEffect : MonoBehaviour
{
    public ParticleSystem particles;
    public AudioSource    audioSource;
    public AudioClip      explosionClip;
    public float          lifetime = 2f;

    void Start()
    {
        if (particles    != null) particles.Play();
        if (audioSource  != null && explosionClip != null)
            audioSource.PlayOneShot(explosionClip);

        Destroy(gameObject, lifetime);
    }
}
