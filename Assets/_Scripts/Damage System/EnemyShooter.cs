using UnityEngine;

public class EnemyShooter : Shooter
{
    [Header("Detection properties")]
    [SerializeField] private float detectionRadius = 50f;
    [SerializeField] private float scanFrequency = 0.25f;
    [SerializeField] private float spotReactionTime = 0.15f;

    [Header("Engage / Aim")]
    [SerializeField] private float engageDistance = 50f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float weaponPitchSpeed = 10f;
    [SerializeField] private float minWeaponPitch = -45f;
    [SerializeField] private float maxWeaponPitch = 60f;

    [Header("Prediction")]
    [Tooltip("Quanto velocemente la stima della velocità si adatta (0..1). Più alto = più reattivo.")]
    [Range(0f, 1f)]
    [SerializeField] private float velocityLerp = 0.4f;

    [Tooltip("Limite massimo del tempo di anticipo (in secondi). Evita lead assurdi a distanze grandi.")]
    [SerializeField] private float maxLeadTime = 1.25f;

    [Tooltip("Fallback altezza aim se non trova un collider sul player.")]
    [SerializeField] private float aimHeightFallback = 1.2f;

    [Header("Weapon properties")]
    [SerializeField] private float bulletSpeed = 20f;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform weaponHolderTransform;

    private Transform playerTr;
    private Health playerHealth;
    private Health myHealth;
    private Transform weaponHolder;
    private Collider playerCollider;

    private bool playerSpotted;
    private float spotTimer;
    private float scanTimer;

    // Velocity estimate (works even without Rigidbody)
    private bool hasPlayerPos;
    private Vector3 lastPlayerPos;
    private Vector3 estimatedPlayerVel;

    protected override void Awake()
    {
        base.Awake();

        myHealth = GetComponent<Health>();
        weaponHolder = weaponHolderTransform;

        if (weaponHolder == null)
            Debug.LogError("[EnemyShooter] WeaponHolderTransform non assegnato nell'inspector.");

        if (muzzle == null)
            Debug.LogError("[EnemyShooter] Muzzle non assegnata (campo 'muzzle' in Shooter).");

        scanTimer = 0f; // scan immediato
    }

    private void Update()
    {
        ScanForPlayer();

        if (!playerSpotted || playerTr == null)
            return;

        // Aggiorna subito la stima velocità anche durante la reaction (così non "parte in ritardo")
        UpdatePlayerVelocityEstimate();

        if (spotTimer < spotReactionTime)
        {
            spotTimer += Time.deltaTime;
            return;
        }

        RotateTowardsPredictedAimPoint();

        if (bulletsLeft > 0)
            TryShoot();
        else if (!reloading)
            Reload();
    }

    private void ScanForPlayer()
    {
        scanTimer -= Time.deltaTime;
        if (scanTimer > 0f) return;
        scanTimer = Mathf.Max(0.01f, scanFrequency);

        bool previouslySpotted = playerSpotted;

        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius, ~0, QueryTriggerInteraction.Ignore);

        Health foundHealth = null;
        Collider foundCollider = null;

        for (int i = 0; i < colliders.Length; i++)
        {
            // Importante: spesso Health sta sul parent (non sul collider colpito)
            Health h = colliders[i].GetComponentInParent<Health>();
            if (h != null && h.Team == Team.Player)
            {
                foundHealth = h;
                foundCollider = colliders[i];
                break;
            }
        }

        if (foundHealth != null)
        {
            playerSpotted = true;
            playerHealth = foundHealth;
            playerTr = foundHealth.transform;

            // per aimpoint: preferisci un collider del player (anche child)
            playerCollider = foundCollider != null ? foundCollider : playerTr.GetComponentInChildren<Collider>();

            if (!previouslySpotted)
            {
                spotTimer = 0f;
                ResetVelocityEstimate();
            }
        }
        else
        {
            // perso il player
            if (previouslySpotted)
            {
                spotTimer = 0f;
                ResetVelocityEstimate();
            }

            playerSpotted = false;
            playerHealth = null;
            playerTr = null;
            playerCollider = null;
        }
    }

    private void ResetVelocityEstimate()
    {
        hasPlayerPos = false;
        estimatedPlayerVel = Vector3.zero;
        lastPlayerPos = Vector3.zero;
    }

    private void UpdatePlayerVelocityEstimate()
    {
        if (playerTr == null) return;

        Vector3 pos = playerTr.position;
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);

        if (!hasPlayerPos)
        {
            hasPlayerPos = true;
            lastPlayerPos = pos;
            estimatedPlayerVel = Vector3.zero;
            return;
        }

        Vector3 rawVel = (pos - lastPlayerPos) / dt;
        estimatedPlayerVel = Vector3.Lerp(estimatedPlayerVel, rawVel, velocityLerp);
        lastPlayerPos = pos;
    }

    private Vector3 GetAimPoint()
    {
        if (playerTr == null)
            return transform.position;

        // Mira al centro del collider (busto), evita "sopra la testa"
        if (playerCollider != null)
            return playerCollider.bounds.center;

        Collider c = playerTr.GetComponentInChildren<Collider>();
        if (c != null)
        {
            playerCollider = c;
            return c.bounds.center;
        }

        // fallback
        return playerTr.position + Vector3.up * aimHeightFallback;
    }

    private Vector3 GetPredictedAimPoint()
    {
        Vector3 origin = (muzzle != null) ? muzzle.position : transform.position;
        Vector3 targetPos = GetAimPoint();

        float s = Mathf.Max(bulletSpeed, 0.001f);
        Vector3 v = estimatedPlayerVel;
        Vector3 r = targetPos - origin;

        // Solve |r + v t| = s t  -> quadratic:
        // (v·v - s^2)t^2 + 2(r·v)t + (r·r) = 0
        float a = Vector3.Dot(v, v) - s * s;
        float b = 2f * Vector3.Dot(r, v);
        float c = Vector3.Dot(r, r);

        float t = 0f;

        if (Mathf.Abs(a) < 0.0001f)
        {
            // quasi lineare
            if (Mathf.Abs(b) > 0.0001f)
                t = -c / b;
            else
                t = 0f;
        }
        else
        {
            float disc = b * b - 4f * a * c;
            if (disc < 0f)
            {
                // nessuna soluzione reale -> fallback tempo di volo semplice
                t = r.magnitude / s;
            }
            else
            {
                float sqrt = Mathf.Sqrt(disc);
                float t1 = (-b - sqrt) / (2f * a);
                float t2 = (-b + sqrt) / (2f * a);

                // scegli il più piccolo positivo
                if (t1 > 0f && t2 > 0f) t = Mathf.Min(t1, t2);
                else if (t1 > 0f) t = t1;
                else if (t2 > 0f) t = t2;
                else t = 0f;
            }
        }

        t = Mathf.Clamp(t, 0f, maxLeadTime);
        return targetPos + v * t;
    }

    private void RotateTowardsPredictedAimPoint()
    {
        Vector3 predicted = GetPredictedAimPoint();

        // Yaw: ruota il corpo solo su Y verso il punto predetto
        Vector3 toPred = predicted - transform.position;
        Vector3 dirYOnly = new Vector3(toPred.x, 0f, toPred.z);

        if (dirYOnly.sqrMagnitude > 0.0001f)
        {
            Quaternion targetYaw = Quaternion.LookRotation(dirYOnly.normalized);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetYaw, rotationSpeed * Time.deltaTime);
        }

        // Pitch: arma su X (local) usando la muzzle come origine (molto più preciso)
        if (weaponHolder != null && muzzle != null)
        {
            Vector3 dir = (predicted - muzzle.position).normalized;

            float horizontal = new Vector3(dir.x, 0f, dir.z).magnitude;
            float targetPitch = -Mathf.Atan2(dir.y, Mathf.Max(horizontal, 0.0001f)) * Mathf.Rad2Deg;
            targetPitch = Mathf.Clamp(targetPitch, minWeaponPitch, maxWeaponPitch);

            float currentPitch = weaponHolder.localEulerAngles.x;
            if (currentPitch > 180f) currentPitch -= 360f;

            float lerpedPitch = Mathf.Lerp(currentPitch, targetPitch, weaponPitchSpeed * Time.deltaTime);
            weaponHolder.localRotation = Quaternion.Euler(lerpedPitch, 0f, 0f);
        }
    }

    protected override void Shoot()
    {
        if (!playerSpotted || playerTr == null || muzzle == null || bulletPrefab == null)
            return;

        Vector3 aimPoint = GetPredictedAimPoint();

        if (!CheckLineOfSight(aimPoint))
            return;

        bulletsLeft--;

        Vector3 spawnPos = muzzle.position;
        Vector3 dir = (aimPoint - spawnPos).normalized;
        Quaternion rot = Quaternion.LookRotation(dir);

        GameObject bulletObj = Instantiate(bulletPrefab, spawnPos, rot);
        Bullet bullet = bulletObj.GetComponent<Bullet>();

        if (bullet != null)
        {
            Collider myCollider = GetComponent<Collider>();
            bullet.Initialize(bulletSpeed, myHealth.Team, bulletDamage, myCollider);
        }
    }

    private bool CheckLineOfSight(Vector3 aimPoint)
    {
        Vector3 origin = (muzzle != null) ? muzzle.position : transform.position;
        float dist = Vector3.Distance(origin, aimPoint);

        if (dist > engageDistance)
            return false;

        Vector3 dir = (aimPoint - origin).normalized;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, ~0, QueryTriggerInteraction.Ignore))
        {
            Health h = hit.collider.GetComponentInParent<Health>();
            return (h != null && h.Team == Team.Player);
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, engageDistance);
    }
}