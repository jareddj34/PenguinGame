using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyAI : MonoBehaviour
{
    // ── Detection ─────────────────────────────────────────────────────────
    [Header("Detection")]
    [SerializeField] float detectionRadius = 10f;   // Radius at which the mouse spots the rat (player)
    [SerializeField] float attackRadius    = 1.5f;  // How close before the mouse tries to attack
    [SerializeField] LayerMask playerLayer;          // Set this to your Player layer in the Inspector

    // ── Movement ──────────────────────────────────────────────────────────
    [Header("Movement")]
    [SerializeField] float chaseSpeed  = 3.5f;
    [SerializeField] float wanderSpeed = 1.5f;

    // ── Wander ────────────────────────────────────────────────────────────
    [Header("Wander")]
    [SerializeField] float wanderRadius    = 8f;   // How far from spawn origin the mouse can roam
    [SerializeField] float minWaitTime     = 2f;   // Min seconds to pause between wanders
    [SerializeField] float maxWaitTime     = 5f;   // Max seconds to pause between wanders
    [SerializeField] float arrivedDistance = 0.4f; // How close = "reached destination"

    // ── Attack ────────────────────────────────────────────────────────────
    [Header("Attack")]
    [SerializeField] float attackDamage   = 10f;
    [SerializeField] float attackCooldown = 1.5f;   // Seconds between bites

    // ── State machine ─────────────────────────────────────────────────────
    enum State { Idle, Wander, Chase, Attack }
    State currentState = State.Idle;

    // ── Internal refs ─────────────────────────────────────────────────────
    NavMeshAgent agent;
    Transform    player;
    Vector3      wanderOrigin;      // Spawn position — wander stays near here
    float        wanderWaitTimer;   // Countdown until next wander move
    float        attackTimer = 0f;

    // ─────────────────────────────────────────────────────────────────────

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        wanderOrigin    = transform.position;
        wanderWaitTimer = Random.Range(minWaitTime, maxWaitTime);

        // Find the player by tag — make sure your player GameObject is tagged "Player"
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogWarning("[EnemyAI] No GameObject tagged 'Player' found. " +
                             "Tag your player object 'Player' in the Inspector.");
    }

    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // ── State transitions ──────────────────────────────────────────
        switch (currentState)
        {
            case State.Idle:
                if (distanceToPlayer <= detectionRadius)
                {
                    EnterChase();
                    break;
                }
                wanderWaitTimer -= Time.deltaTime;
                if (wanderWaitTimer <= 0f)
                    EnterWander();
                break;

            case State.Wander:
                if (distanceToPlayer <= detectionRadius)
                {
                    EnterChase();
                    break;
                }
                // Arrived at the wander destination — go back to idle
                if (!agent.pathPending && agent.remainingDistance <= arrivedDistance)
                    EnterIdle();
                break;

            case State.Chase:
                if (distanceToPlayer > detectionRadius)
                    EnterIdle();
                else if (distanceToPlayer <= attackRadius)
                    EnterAttack();
                break;

            case State.Attack:
                if (distanceToPlayer > attackRadius)
                    EnterChase();
                break;
        }

        // ── State behaviour ────────────────────────────────────────────
        switch (currentState)
        {
            case State.Idle:
                // Waiting — timer ticks in transitions above
                break;

            case State.Wander:
                // NavMeshAgent drives movement automatically; nothing extra needed
                break;

            case State.Chase:
                agent.SetDestination(player.position);
                break;

            case State.Attack:
                agent.SetDestination(transform.position); // Stop in place
                FaceTarget(player.position);
                HandleAttack();
                break;
        }
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
            // Couldn't find a valid NavMesh point nearby — wait and try again
            wanderWaitTimer = Random.Range(minWaitTime, maxWaitTime);
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

    /// <summary>
    /// Picks a random point within wanderRadius of the spawn origin and
    /// snaps it to the NavMesh. Returns true if a valid point was found.
    /// </summary>
    bool TryGetWanderPoint(out Vector3 result)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPoint = wanderOrigin + Random.insideUnitSphere * wanderRadius;
            randomPoint.y = wanderOrigin.y; // Keep the sample near floor level

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
        // Try to damage the player — expects a PlayerHealth component on the player
        // PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        // if (playerHealth != null)
        //     playerHealth.TakeDamage(attackDamage);

        // Hook for animations/sounds — add an Animator reference and trigger here
        // e.g. animator.SetTrigger("Bite");
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
        // Detection radius (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Attack radius (red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);

        // Wander boundary around spawn origin (cyan)
        Gizmos.color = Color.cyan;
        Vector3 origin = Application.isPlaying ? wanderOrigin : transform.position;
        Gizmos.DrawWireSphere(origin, wanderRadius);
    }
}