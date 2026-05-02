using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyAI : MonoBehaviour
{
    // ── Detection ─────────────────────────────────────────────────────────
    [Header("Detection")]
    [SerializeField] float detectionRadius = 10f;   // Radius of the forward half-sphere
    [SerializeField] float attackRadius    = 1.5f;  // How close before attacking
    [SerializeField] LayerMask playerLayer;          // Set to your Player layer in the Inspector

    // ── Movement ──────────────────────────────────────────────────────────
    [Header("Movement")]
    [SerializeField] float chaseSpeed  = 3.5f;
    [SerializeField] float wanderSpeed = 1.5f;

    // ── Behaviour Mode ────────────────────────────────────────────────────
    public enum BehaviourMode { Wander, Patrol }
    [Header("Behaviour Mode")]
    [Tooltip("Wander: roam randomly near spawn.  Patrol: cycle through preset waypoints.")]
    [SerializeField] BehaviourMode behaviourMode = BehaviourMode.Wander;

    // ── Wander ────────────────────────────────────────────────────────────
    [Header("Wander")]
    [SerializeField] float wanderRadius    = 8f;   // Max distance from spawn to roam
    [SerializeField] float minWaitTime     = 2f;
    [SerializeField] float maxWaitTime     = 5f;
    [SerializeField] float arrivedDistance = 0.4f; // "Close enough" threshold

    // ── Patrol ────────────────────────────────────────────────────────────
    [Header("Patrol")]
    [Tooltip("Assign world-space Transform waypoints in order. Only used in Patrol mode.")]
    [SerializeField] Transform[] patrolPoints;
    [Tooltip("Seconds the enemy pauses at each waypoint before moving on.")]
    [SerializeField] float patrolWaitTime = 1.5f;

    // ── Attack ────────────────────────────────────────────────────────────
    [Header("Attack")]
    [SerializeField] float attackDamage   = 10f;
    [SerializeField] float attackCooldown = 1.5f;
    public GameObject hitbox; // Assign a child GameObject with a trigger collider for the attack hitbox

    // ── State machine ─────────────────────────────────────────────────────
    enum State { Idle, Wander, Patrol, Chase, Attack }
    State currentState = State.Idle;

    // ── Internal refs ─────────────────────────────────────────────────────
    NavMeshAgent agent;
    Animator     animator;
    Transform    player;
    Vector3      wanderOrigin;
    float        wanderWaitTimer;
    float        attackTimer = 0f;

    // Patrol bookkeeping
    int   patrolIndex     = 0;
    float patrolWaitTimer = 0f;
    bool  waitingAtPoint  = false;

    // ─────────────────────────────────────────────────────────────────────

    void Awake()
    {
        agent    = GetComponent<NavMeshAgent>();
        // Animator may live on a child object (the model), so search children too
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
            Debug.LogWarning("[EnemyAI] No GameObject tagged 'Player' found. " +
                             "Tag your player object 'Player' in the Inspector.");

        // Kick off patrol immediately if that mode is selected
        if (behaviourMode == BehaviourMode.Patrol && patrolPoints != null && patrolPoints.Length > 0)
            EnterPatrol();
    }

    // ── Detection — forward half-sphere ───────────────────────────────────
    /// <summary>
    /// Returns true only when the player is within <see cref="detectionRadius"/>
    /// AND is located in the forward hemisphere (dot product > 0),
    /// preventing the enemy from "seeing" targets directly behind it.
    /// </summary>
    bool CanSeePlayer()
    {
        if (player == null) return false;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > detectionRadius) return false;

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        // Dot > 0  →  angle to player < 90°  →  player is in the forward half
        return Vector3.Dot(transform.forward, dirToPlayer) > 0f;
    }

    // ─────────────────────────────────────────────────────────────────────

    void Update()
    {
        if (player == null) return;

        float distToPlayer  = Vector3.Distance(transform.position, player.position);
        bool  playerVisible = CanSeePlayer();

        // ── State transitions ──────────────────────────────────────────
        switch (currentState)
        {
            case State.Idle:
                if (playerVisible) { EnterChase(); break; }
                wanderWaitTimer -= Time.deltaTime;
                if (wanderWaitTimer <= 0f)
                {
                    if (behaviourMode == BehaviourMode.Wander)
                        EnterWander();
                    else
                        EnterPatrol();
                }
                break;

            case State.Wander:
                if (playerVisible) { EnterChase(); break; }
                if (!agent.pathPending && agent.remainingDistance <= arrivedDistance)
                    EnterIdle();
                break;

            case State.Patrol:
                if (playerVisible) { EnterChase(); break; }
                UpdatePatrol();
                break;

            case State.Chase:
                // Lost sight — return to idle/patrol
                if (!playerVisible && distToPlayer > detectionRadius)
                {
                    if (behaviourMode == BehaviourMode.Patrol)
                        EnterPatrol();
                    else
                        EnterIdle();
                    break;
                }
                if (distToPlayer <= attackRadius) EnterAttack();
                break;

            case State.Attack:
                if (distToPlayer > attackRadius) {
                    EnterChase();
                    animator.SetBool("IsAttacking", false);
                }
                break;
        }

        // ── State behaviour ────────────────────────────────────────────
        switch (currentState)
        {
            case State.Idle:
                // Waiting — timer ticks in transitions above
                break;

            case State.Wander:
                // NavMeshAgent drives movement automatically
                break;

            case State.Patrol:
                // Movement handled inside UpdatePatrol()
                break;

            case State.Chase:
                agent.SetDestination(player.position);
                break;

            case State.Attack:
                agent.SetDestination(transform.position); // Stop in place
                FaceTarget(player.position);
                HandleAttack();
                animator.SetBool("IsAttacking", true);
                break;
        }

        // ── Animation ──────────────────────────────────────────────────
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
            // Couldn't find a valid NavMesh point — wait and retry
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

    void EnterChase()
    {
        currentState = State.Chase;
        agent.speed  = chaseSpeed;
    }

    void EnterAttack()
    {
        currentState = State.Attack;
        agent.speed  = 0f;
        attackTimer  = 0f; // Attack immediately on entering attack state
    }

    // ── Wander helpers ────────────────────────────────────────────────────

    bool TryGetWanderPoint(out Vector3 result)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPoint = wanderOrigin + Random.insideUnitSphere * wanderRadius;
            randomPoint.y = wanderOrigin.y;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 2f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }
        result = transform.position;
        return false;
    }

    // ── Attack logic ──────────────────────────────────────────────────────

    void HandleAttack()
    {
        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            attackTimer = attackCooldown;
            PerformAttack();
        }
    }

    void PerformAttack()
    {
        // PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        // if (playerHealth != null)
        //     playerHealth.TakeDamage(attackDamage);

        if (animator != null)
            animator.SetTrigger("Attack");
    }

    public void ActivateHitbox() 
    {
        hitbox.SetActive(true);
    }

    public void DeactivateHitbox() 
    {
        hitbox.SetActive(false);
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

    // ── Gizmos — visualise radii in the Scene view ────────────────────────

    void OnDrawGizmosSelected()
    {
        // Detection half-sphere (yellow) — forward-facing arc
        Gizmos.color = Color.yellow;
        DrawHalfCircleGizmo(transform.position, transform.forward, detectionRadius);

        // Attack radius (red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);

        if (behaviourMode == BehaviourMode.Wander)
        {
            // Wander boundary around spawn origin (cyan)
            Gizmos.color = Color.cyan;
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

    /// <summary>
    /// Draws a 180° arc (forward hemisphere) in the XZ plane as a Scene-view gizmo.
    /// </summary>
    void DrawHalfCircleGizmo(Vector3 center, Vector3 forward, float radius)
    {
        forward.y = 0f;
        if (forward == Vector3.zero) forward = Vector3.forward;
        forward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, forward);

        // Left and right boundary lines (the flat edges of the half-circle)
        Gizmos.DrawLine(center, center + (-right) * radius);
        Gizmos.DrawLine(center, center +   right  * radius);

        // Arc from -90° to +90° relative to forward
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
