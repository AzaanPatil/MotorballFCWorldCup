using UnityEngine;

public class TurretController : MonoBehaviour
{
    [Header("References")]
    public Transform   turretPivot;
    public Transform   firePoint;
    public GameObject  shellPrefab;

    [Header("Firing")]
    public float shellSpeed   = 20f;
    public float fireCooldown = 5f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   fireClip;

    [Header("Effects")]
    public ParticleSystem muzzleFlash;

    private Transform target;
    private bool      isPlayerControlled;
    private float     lastFireTime = -10f;

    public void SetPlayerControlled(bool controlled) => isPlayerControlled = controlled;
    public void SetTarget(Transform t)               => target = t;

    void Start()
    {
        // Kill any Play-on-Awake particle system on the fire point (leftover muzzle flash placeholder)
        if (firePoint != null)
            foreach (var ps in firePoint.GetComponents<ParticleSystem>())
                if (ps != muzzleFlash) { ps.Stop(); ps.Clear(); ps.gameObject.SetActive(false); }
    }

    void Update()
    {
        if (isPlayerControlled)
        {
            AimAtMouse();
            if (Input.GetKeyDown(KeyCode.F)) TryFire();
        }
        else
        {
            AimAtTarget();
            if (target != null && Time.time - lastFireTime >= fireCooldown)
                TryFire();
        }
    }

    void AimAtMouse()
    {
        if (turretPivot == null) return;
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        Vector2 dir = (mouseWorld - turretPivot.position).normalized;
        turretPivot.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f);
    }

    void AimAtTarget()
    {
        if (turretPivot == null || target == null) return;
        Vector2 dir = ((Vector2)target.position - (Vector2)turretPivot.position).normalized;
        turretPivot.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f);
    }

    void TryFire()
    {
        if (shellPrefab == null || firePoint == null) return;

        GameObject shell = Instantiate(shellPrefab, firePoint.position, firePoint.rotation);
        if (shell.TryGetComponent<Shell>(out var s))
        {
            s.SetOwner(GetComponentInParent<VehicleController>());
            s.Launch(-firePoint.up, shellSpeed);
        }

        if (muzzleFlash != null) muzzleFlash.Play();
        if (audioSource  != null && fireClip != null) audioSource.PlayOneShot(fireClip);

        lastFireTime = Time.time;
    }
}
