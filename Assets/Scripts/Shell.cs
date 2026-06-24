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
        transform.localScale = Vector2.one * 0.25f;
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = 5;
            // Force the correct material regardless of what the prefab serialized
            var shader = Shader.Find("Sprites/Default");
            if (shader != null) sr.material = new Material(shader);
        }
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
        bool isBall = other.CompareTag("Ball");

        // GetComponentInParent catches vehicles whose collider is on a child object
        VehicleController hitVc = other.GetComponentInParent<VehicleController>();
        bool isEnemy = hitVc != null
                       && hitVc.gameObject != (owner != null ? owner.gameObject : null)
                       && (friendlyFire || owner == null || hitVc.team != owner.team);

        if (!isBall && !isEnemy) return;

        // Stun the directly-hit vehicle immediately before the blast radius sweep
        if (isEnemy) hitVc.ApplyStun(1.0f);

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
            // Push ball — high multiplier so a direct hit sends it across the field
            if (col.CompareTag("Ball"))
            {
                Rigidbody2D ballRb = col.GetComponent<Rigidbody2D>();
                if (ballRb != null)
                {
                    Vector2 dir   = ((Vector2)col.transform.position - pos).normalized;
                    float   scale = 1f - (Vector2.Distance(col.transform.position, pos) / blastRadius);
                    ballRb.linearVelocity = Vector2.zero;
                    ballRb.AddForce(dir * blastForce * 5f * scale, ForceMode2D.Impulse);
                }
            }

            // Stun and knock back vehicles
            VehicleController blastVc = col.GetComponentInParent<VehicleController>();
            if (blastVc != null)
            {
                if (!friendlyFire && owner != null && blastVc.team == owner.team) continue;
                blastVc.ApplyStun(1.0f);
                Rigidbody2D vcRb = blastVc.GetComponent<Rigidbody2D>();
                if (vcRb != null)
                {
                    Vector2 dir   = ((Vector2)col.transform.position - pos).normalized;
                    float   scale = 1f - (Vector2.Distance(col.transform.position, pos) / blastRadius);
                    vcRb.linearVelocity = Vector2.zero;
                    vcRb.AddForce(dir * blastForce * 3f * scale, ForceMode2D.Impulse);
                }
            }
        }

        Destroy(gameObject);
    }
}
