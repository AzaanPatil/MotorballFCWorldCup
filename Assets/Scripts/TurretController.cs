using UnityEngine;

public class TurretController : MonoBehaviour
{
    [Header("References")]
    public Transform  turretPivot;
    public Transform  firePoint;
    public GameObject shellPrefab;
    public Transform  target;

    [Header("Firing")]
    public float shellSpeed    = 20f;
    public float fireCooldown  = 1.5f;
    public float reloadTime    = 0.3f;
    public int   maxAmmo       = 3;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   fireClip;

    [Header("Effects")]
    public ParticleSystem muzzleFlash;

    private int   currentAmmo;
    private float lastFireTime = -10f;
    private bool  isPlayerControlled;

    void Start()
    {
        currentAmmo = maxAmmo;
    }

    public void SetPlayerControlled(bool controlled)
    {
        isPlayerControlled = controlled;
    }

    public void SetTarget(Transform t)
    {
        target = t;
    }

    void Update()
    {
        if (isPlayerControlled)
            AimAtMouse();
        else
            AimAtTarget();

        if (isPlayerControlled && Input.GetKeyDown(KeyCode.F))
            TryFire();
        else if (!isPlayerControlled && target != null)
            AutoFire();
    }

    void AimAtMouse()
    {
        if (turretPivot == null) return;
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        Vector2 dir = (mouseWorld - turretPivot.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        turretPivot.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void AimAtTarget()
    {
        if (turretPivot == null || target == null) return;
        Vector2 dir = ((Vector2)target.position - (Vector2)turretPivot.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        turretPivot.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void AutoFire()
    {
        if (target == null) return;
        float dist = Vector2.Distance(turretPivot.position, target.position);
        if (dist < 8f) TryFire();
    }

    void TryFire()
    {
        if (Time.time - lastFireTime < fireCooldown) return;
        if (currentAmmo <= 0) { StartCoroutine(Reload()); return; }
        if (shellPrefab == null || firePoint == null) return;

        GameObject shell = Instantiate(shellPrefab, firePoint.position, firePoint.rotation);
        if (shell.TryGetComponent<Shell>(out var s))
            s.Launch(firePoint.up, shellSpeed);

        if (muzzleFlash != null) muzzleFlash.Play();
        if (audioSource != null && fireClip != null) audioSource.PlayOneShot(fireClip);

        currentAmmo--;
        lastFireTime = Time.time;
    }

    System.Collections.IEnumerator Reload()
    {
        yield return new WaitForSeconds(reloadTime * maxAmmo);
        currentAmmo = maxAmmo;
    }
}
