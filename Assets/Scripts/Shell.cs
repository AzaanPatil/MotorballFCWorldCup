using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Shell : MonoBehaviour
{
    [Header("Explosion")]
    public GameObject explosionPrefab;
    public float      blastRadius   = 2f;
    public float      blastForce    = 15f;
    public bool       friendlyFire  = false;
    public LayerMask  hitLayers;

    [Header("Lifetime")]
    public float lifetime = 4f;

    private Rigidbody2D  rb;
    private VehicleController owner;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        Destroy(gameObject, lifetime);
    }

    public void Launch(Vector2 direction, float speed)
    {
        rb.linearVelocity = direction * speed;
    }

    public void SetOwner(VehicleController vc)
    {
        owner = vc;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Don't hit the firing tank itself
        if (owner != null && other.gameObject == owner.gameObject) return;

        Explode(transform.position);
    }

    void Explode(Vector2 pos)
    {
        if (explosionPrefab != null)
            Instantiate(explosionPrefab, pos, Quaternion.identity);

        // Apply blast force to everything in radius
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, blastRadius, hitLayers);
        foreach (var col in hits)
        {
            // Push ball
            if (col.CompareTag("Ball"))
            {
                Rigidbody2D ballRb = col.GetComponent<Rigidbody2D>();
                if (ballRb != null)
                {
                    Vector2 dir   = ((Vector2)col.transform.position - pos).normalized;
                    float   scale = 1f - (Vector2.Distance(col.transform.position, pos) / blastRadius);
                    ballRb.AddForce(dir * blastForce * scale, ForceMode2D.Impulse);
                }
            }

            // Push vehicles
            if (col.TryGetComponent<VehicleController>(out var vc))
            {
                if (!friendlyFire && owner != null && vc.team == owner.team) continue;
                Rigidbody2D vcRb = col.GetComponent<Rigidbody2D>();
                if (vcRb != null)
                {
                    Vector2 dir   = ((Vector2)col.transform.position - pos).normalized;
                    float   scale = 1f - (Vector2.Distance(col.transform.position, pos) / blastRadius);
                    vcRb.AddForce(dir * blastForce * 0.5f * scale, ForceMode2D.Impulse);
                }
            }
        }

        Destroy(gameObject);
    }
}
