using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(OctopusHealth))]
public class OctopusAI : MonoBehaviour
{
    // ── Detection ─────────────────────────────────────────────────────────
    [Header("Detection")]
    [SerializeField] float detectionRadius = 12f;
    [SerializeField] LayerMask playerLayer;

    // ── Movement ──────────────────────────────────────────────────────────
    [Header("Movement")]
    [SerializeField] float wanderSpeed = 1.5f;

    // ── Wander ────────────────────────────────────────────────────────────
    [Header("Wander")]
    [SerializeField] float wanderRadius    = 8f;
    [SerializeField] float minWaitTime     = 2f;
    [SerializeField] float maxWaitTime     = 5f;
    [SerializeField] float arrivedDistance = 0.4f;
    [SerializeField] BoxCollider wanderZone;

    // ── Behaviour Mode ────────────────────────────────────────────────────
    public enum BehaviourMode { Wander, Patrol }
    [Header("Behaviour Mode")]
    [Tooltip("Wander: roam randomly near spawn.  Patrol: cycle through preset waypoints.")]
    [SerializeField] BehaviourMode behaviourMode = BehaviourMode.Wander;

    // ── Patrol ────────────────────────────────────────────────────────────
    [Header("Patrol")]
    [Tooltip("Assign world-space Transform waypoints in order. Only used in Patrol mode.")]
    [SerializeField] Transform[] patrolPoints;
    [Tooltip("Seconds the enemy pauses at each waypoint before moving on.")]
    [SerializeField] float patrolWaitTime = 1.5f;

    // ── Shooting ──────────────────────────────────────────────────────────
    [Header("Shooting")]
    [SerializeField] GameObject projectilePrefab; // Assign your snowball-like prefab
    [SerializeField] Transform  firePoint;        // Empty child transform at the "mouth"
    [SerializeField] float      shootCooldown = 2.5f;
    [SerializeField] float      shootRange    = 10f; // Max range at which it will shoot

    // ── Stagger ───────────────────────────────────────────────────────────
    [Header("Stagger")]
    [SerializeField] float staggerDuration = 0.8f;

    // ── Sounds ────────────────────────────────────────────────────────────
    [Header("Sounds")]
    public AudioSource audioSource;
    public AudioClip   alertSound;
    public AudioClip   shootSound;

    // ── State machine ─────────────────────────────────────────────────────
    enum State { Idle, Wander, Patrol, Alert, Shoot, Stagger }
    State currentState = State.Idle;

    // ── Internal refs ─────────────────────────────────────────────────────
    NavMeshAgent agent;
    Animator     animator;
    Transform    player;
    Vector3      wanderOrigin;
    float        wanderWaitTimer;
    float        shootTimer = 0f;
    float        staggerTimer = 0f;

    // Patrol bookkeeping
    int   patrolIndex     = 0;
    float patrolWaitTimer = 0f;
    bool  waitingAtPoint  = false;

    public bool playerVisible { get; private set; }

    // ─────────────────────────────────────────────────────────────────────

    void Awake()
    {
        agent    = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        wanderOrigin    = transform.position;
        wanderWaitTimer = Random.Range(minWaitTime, maxWaitTime);

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogWarning("[OctopusAI] No GameObject tagged 'Player' found.");

         // Kick off patrol immediately if that mode is selected
        if (behaviourMode == BehaviourMode.Patrol && patrolPoints != null && patrolPoints.Length > 0)
            EnterPatrol();
        

        GameStateManager.OnStateChanged += OnGameStateChanged;
    }

    void OnDestroy()
    {
        GameStateManager.OnStateChanged -= OnGameStateChanged;
    }

    // For if the enemy is in a certain state, freeze the enemy
    void OnGameStateChanged(GameState newState)
    {
        bool freeze = newState == GameState.ReceivingItem || newState == GameState.Dialogue || newState == GameState.Dead;
        agent.isStopped = freeze;
        if (animator != null) animator.speed = freeze ? 0f : 1f;
    }

    // ── Detection — forward half-sphere ──────────────────────────────────
    public bool CanSeePlayer()
    {
        if (player == null) return false;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > detectionRadius) return false;

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        return Vector3.Dot(transform.forward, dirToPlayer) > 0f;
    }

    bool PlayerInShootRange()
    {
        if (player == null) return false;
        return Vector3.Distance(transform.position, player.position) <= shootRange;
    }

    // ─────────────────────────────────────────────────────────────────────

    void Update()
    {
        if (player == null) return;

        if (GameStateManager.Instance != null && 
            (GameStateManager.Instance.CurrentState == GameState.Dead ||
            GameStateManager.Instance.CurrentState == GameState.ReceivingItem))
            return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);
        playerVisible = CanSeePlayer();

        // ── State transitions ──────────────────────────────────────────
        switch (currentState)
        {
            case State.Idle:
                if (playerVisible && PlayerInWanderZone()) { EnterAlert(); break; }
                wanderWaitTimer -= Time.deltaTime;
                if (wanderWaitTimer <= 0f){
                    if (behaviourMode == BehaviourMode.Wander)
                        EnterWander();
                    else
                        EnterPatrol();
                }
                break;

            case State.Wander:
                if (playerVisible && PlayerInWanderZone()) { EnterAlert(); break; }
                if (!agent.pathPending && agent.remainingDistance <= arrivedDistance)
                    EnterIdle();
                break;

            case State.Patrol:
                if (playerVisible && PlayerInWanderZone()) { EnterAlert(); break; }
                UpdatePatrol();
                break;

            case State.Alert:
                // Lost sight
                if (!playerVisible && distToPlayer > detectionRadius || (wanderZone != null && !wanderZone.bounds.Contains(player.position)))
                {
                    // if (behaviourMode == BehaviourMode.Patrol)
                    //     EnterPatrol();
                    // else
                        EnterIdle();
                    break;
                }
                if (PlayerInShootRange())
                    EnterShoot();
                break;

            case State.Shoot:
                // Player left range — go back to alert (will close no distance, just face them)
                if (!PlayerInShootRange() || (!playerVisible && distToPlayer > detectionRadius) || (wanderZone != null && !wanderZone.bounds.Contains(player.position)))
                {
                    EnterAlert();
                    break;
                }
                break;

            case State.Stagger:
                staggerTimer -= Time.deltaTime;
                if (staggerTimer <= 0f)
                {
                    if (playerVisible || distToPlayer <= detectionRadius)
                        EnterAlert();
                    else if (behaviourMode == BehaviourMode.Patrol)
                        EnterPatrol();
                    else
                        EnterIdle();
                }
                break;
        }

        // ── Per-state behaviour ────────────────────────────────────────
        switch (currentState)
        {
            case State.Alert:
                // Slowly face the player while stalking — no movement
                FaceTarget(player.position);
                break;
            
            case State.Patrol:
                // Movement handled inside UpdatePatrol()
                break;

            case State.Shoot:
                agent.SetDestination(transform.position); // Stay put
                FaceTarget(player.position);
                HandleShoot();
                break;
        }

        // ── Animation ─────────────────────────────────────────────────
        if (animator != null)
            animator.SetFloat("Speed", agent.velocity.magnitude);
    }

    // ── State enter helpers ───────────────────────────────────────────────

    void EnterIdle()
    {
        currentState    = State.Idle;
        agent.speed     = 0f;
        agent.ResetPath();
        wanderWaitTimer = Random.Range(minWaitTime, maxWaitTime);
    }

    void EnterWander()
    {
        Vector3 destination;
        if (TryGetWanderPoint(out destination))
        {
            currentState = State.Wander;
            agent.speed  = wanderSpeed;
            agent.SetDestination(destination);
        }
        else
        {
            wanderWaitTimer = Random.Range(minWaitTime, maxWaitTime);
        }
    }

    void EnterPatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            // Nothing to patrol — fall back to idle
            EnterIdle();
            return;
        }

        currentState   = State.Patrol;
        agent.speed    = wanderSpeed;
        waitingAtPoint = false;
        agent.SetDestination(patrolPoints[patrolIndex].position);
    }

    /// <summary>
    /// Called every frame while in the Patrol state.
    /// Waits at the current waypoint, then advances to the next in a loop.
    /// </summary>
    void UpdatePatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        if (waitingAtPoint)
        {
            patrolWaitTimer -= Time.deltaTime;
            if (patrolWaitTimer <= 0f)
            {
                waitingAtPoint = false;
                patrolIndex    = (patrolIndex + 1) % patrolPoints.Length;
                agent.SetDestination(patrolPoints[patrolIndex].position);
            }
        }
        else if (!agent.pathPending && agent.remainingDistance <= arrivedDistance)
        {
            // Arrived — start waiting
            waitingAtPoint  = true;
            patrolWaitTimer = patrolWaitTime;
        }
    }

    public void EnterAlert()
    {
        currentState = State.Alert;
        agent.speed  = 0f;
        agent.ResetPath();

        if (audioSource != null && alertSound != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(alertSound);
        }
    }

    void EnterShoot()
    {
        currentState = State.Shoot;
        agent.speed  = 0f;
        shootTimer   = 0f; // Shoot immediately on entering shoot state
    }

    public void EnterStagger()
    {
        currentState = State.Stagger;
        staggerTimer = staggerDuration;
        agent.speed  = 0f;
        agent.ResetPath();

        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }
    }

    // ── Shooting ──────────────────────────────────────────────────────────

    void HandleShoot()
    {
        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0f)
        {
            shootTimer = shootCooldown;
            PerformShoot();
        }
    }

    void PerformShoot()
    {
        if (animator != null)
            animator.SetTrigger("Shoot");

        
    }

    public void ShootProjectile()
    {
        if (audioSource != null && shootSound != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(shootSound);
        }

        // Spawn projectile — use firePoint if assigned, otherwise self
        Transform spawnPoint = firePoint != null ? firePoint : transform;

        // Aim at player (flat, no vertical arc)
        Vector3 dirToPlayer = (player.position - spawnPoint.position).normalized;
        dirToPlayer.y = 0f;
        if (dirToPlayer == Vector3.zero) dirToPlayer = transform.forward;

        Quaternion spawnRot = Quaternion.LookRotation(dirToPlayer.normalized);

        GameObject proj = Instantiate(projectilePrefab, spawnPoint.position, spawnRot);

        // Tell the projectile who fired it so it doesn't hit the octopus
        Snowball snowball = proj.GetComponent<Snowball>();
        if (snowball != null)
            snowball.owner = gameObject;
    }

    // ── Wander helpers ────────────────────────────────────────────────────

    bool TryGetWanderPoint(out Vector3 result)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPoint;

            if (wanderZone != null)
            {
                // Pick a random point inside the box's world-space bounds
                Bounds b = wanderZone.bounds;
                randomPoint = new Vector3(
                    Random.Range(b.min.x, b.max.x),
                    wanderOrigin.y,
                    Random.Range(b.min.z, b.max.z)
                );
            }
            else
            {
                randomPoint = wanderOrigin + Random.insideUnitSphere * wanderRadius;
                randomPoint.y = wanderOrigin.y;
            }

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 2f, NavMesh.AllAreas))
            {
                // Double-check the snapped NavMesh point is still inside the zone
                if (wanderZone != null && !wanderZone.bounds.Contains(hit.position))
                    continue;

                result = hit.position;
                return true;
            }
        }
        result = transform.position;
        return false;
    }

    bool PlayerInWanderZone()
    {
        if (wanderZone == null) return true; // No zone = no restriction
        return wanderZone.bounds.Contains(player.position);
    }

    // ── Public hooks called by EnemyHealth ────────────────────────────────

    public void HitAnimation()
    {
        if (animator != null)
            animator.SetTrigger("Hit");
    }

    public void Die()
    {
        if (animator != null)
            animator.SetTrigger("Die");
    }

    // ── Utility ───────────────────────────────────────────────────────────

    void FaceTarget(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        dir.y = 0f;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                Time.deltaTime * 8f
            );
    }

    // ── Gizmos ────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        // Detection half-sphere (yellow)
        Gizmos.color = Color.yellow;
        DrawHalfCircleGizmo(transform.position, transform.forward, detectionRadius);

        // Shoot range (cyan)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, shootRange);

        if(behaviourMode == BehaviourMode.Wander)
        {
            // Wander boundary (white)
            Gizmos.color = Color.white;
            Vector3 origin = Application.isPlaying ? wanderOrigin : transform.position;
            Gizmos.DrawWireSphere(origin, wanderRadius);
        }
        else if (behaviourMode == BehaviourMode.Patrol && patrolPoints != null)
        {
            // Patrol waypoints + connecting lines (green)
            Gizmos.color = Color.green;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] == null) continue;
                Gizmos.DrawSphere(patrolPoints[i].position, 0.3f);
                int next = (i + 1) % patrolPoints.Length;
                if (patrolPoints[next] != null)
                    Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[next].position);
            }
        }
        
    }

    void DrawHalfCircleGizmo(Vector3 center, Vector3 forward, float radius)
    {
        forward.y = 0f;
        if (forward == Vector3.zero) forward = Vector3.forward;
        forward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Gizmos.DrawLine(center, center + (-right) * radius);
        Gizmos.DrawLine(center, center +   right  * radius);

        const int segments = 24;
        Vector3 prev = center + (-right) * radius;
        for (int i = 1; i <= segments; i++)
        {
            float   angle = Mathf.Lerp(-90f, 90f, i / (float)segments);
            Vector3 dir   = Quaternion.AngleAxis(angle, Vector3.up) * forward;
            Vector3 point = center + dir * radius;
            Gizmos.DrawLine(prev, point);
            prev = point;
        }
    }
}
