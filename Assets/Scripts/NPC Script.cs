using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class NPCController : MonoBehaviour
{
    public enum State { Idle, Patrol, MoveTowardsPlayer }
    public State currentState = State.Idle;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private LineRenderer lineRenderer;
    private AudioSource audioSource;

    [Header("State")]
    public float decisionInterval = 5f;
    private float timer;
    private int decisionCount = 0;

    [Header("Movement")]
    public Transform[] patrolPoints;
    private int patrolIndex = 0;
    public float speed = 2f;

    [Header("Jump Settings")]
    public float jumpForce = 7f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    private bool isGrounded;

    [Header("Player Detection")]
    public Transform player;
    public float detectionRange = 5f;
    public LayerMask playerLayer;

    [Header("UI")]
    public TMP_Text stateIdleText;
    public TMP_Text statePatrolText;
    public TMP_Text stateChaseText;
    public TMP_Text countdownText;

    [Header("Sound Effects")]
    public AudioClip collisionSFX;
    public AudioClip idleToPatrolSFX;
    public AudioClip patrolToIdleSFX;
    public AudioClip chaseSFX;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        timer = decisionInterval;
        currentState = State.Idle;
        lineRenderer = GetComponent<LineRenderer>();
        audioSource = GetComponent<AudioSource>();
        UpdateUI();
    }

    void Update()
    {
        timer -= Time.deltaTime;
        countdownText.text = "Countdown: " + Mathf.Ceil(timer).ToString();

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (currentState != State.MoveTowardsPlayer)
        {
            DetectPlayer();

            if (timer <= 0f)
            {
                decisionCount++;

                if (decisionCount >= 3)
                {
                    SwitchStateForced();
                }
                else
                {
                    RandomStateSwitch();
                }

                timer = decisionInterval;
                UpdateUI();
            }
        }

        UpdateLineOfSight();

        switch (currentState)
        {
            case State.Idle:
                break;

            case State.Patrol:
                Patrol();
                break;

            case State.MoveTowardsPlayer:
                ChasePlayer();
                break;
        }
    }


    void Patrol()
    {
        if (patrolPoints.Length == 0) return;

        Transform target = patrolPoints[patrolIndex];
        Vector2 direction = (target.position - transform.position).normalized;

        int ignoreNPCLayer = ~(1 << LayerMask.NameToLayer("NPC"));
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, 1.5f, ignoreNPCLayer);

        if (hit.collider != null)
        {
            if (hit.collider.CompareTag("Jump") && isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                return;
            }
        }

        transform.position = Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

        if (Vector2.Distance(transform.position, target.position) < 0.1f)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;

            if (patrolIndex == 0)
                timer = 0;
        }

        spriteRenderer.flipX = target.position.x < transform.position.x;
    }

    void ChasePlayer()
    {
        Vector2 direction = spriteRenderer.flipX ? Vector2.left : Vector2.right;

        Vector2 boxSize = new Vector2(0.5f, 0.5f);
        Vector2 boxOrigin = (Vector2)transform.position + direction * 0.5f;

        int ignoreNPCLayer = ~(1 << LayerMask.NameToLayer("NPC"));
        RaycastHit2D hit = Physics2D.BoxCast(boxOrigin, boxSize, 0f, direction, 0.1f, ignoreNPCLayer);

        if (hit.collider != null && hit.collider.CompareTag("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            return;
        }

        transform.position = Vector2.MoveTowards(transform.position, player.position, speed * Time.deltaTime);
        spriteRenderer.flipX = player.position.x < transform.position.x;
    }

    void DetectPlayer()
    {
        Vector2 toPlayer = (player.position - transform.position).normalized;

        Vector2 facingDir = spriteRenderer.flipX ? Vector2.left : Vector2.right;

        float dot = Vector2.Dot(toPlayer, facingDir);

        if (dot > 0.5f) 
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, toPlayer, detectionRange, playerLayer);

            if (hit.collider != null && hit.collider.CompareTag("Player"))
            {
                currentState = State.MoveTowardsPlayer;
                UpdateUI();
                audioSource.PlayOneShot(chaseSFX); 
            }
            else
            {
                lineRenderer.startColor = Color.yellow;
                lineRenderer.endColor = Color.yellow;
            }
        }
        else
        {
            lineRenderer.startColor = Color.yellow;
            lineRenderer.endColor = Color.yellow;
        }
    }

    void RandomStateSwitch()
    {
        if (currentState == State.Idle)
        {
            if (Random.value > 0.5f)
            {
                currentState = State.Patrol;
                audioSource.PlayOneShot(idleToPatrolSFX);
            }
        }
        else if (currentState == State.Patrol)
        {
            if (Random.value > 0.5f)
            {
                currentState = State.Idle;
                audioSource.PlayOneShot(patrolToIdleSFX);
            }
        }
    }
    void SwitchStateForced()
    {
        if (currentState == State.Idle)
        {
            currentState = State.Patrol;
            audioSource.PlayOneShot(idleToPatrolSFX);
        }
        else if (currentState == State.Patrol)
        {
            currentState = State.Idle;
            audioSource.PlayOneShot(patrolToIdleSFX);
        }

        decisionCount = 0;
    }

    void UpdateUI()
    {
        stateIdleText.color = Color.white;
        statePatrolText.color = Color.white;
        stateChaseText.color = Color.white;

        switch (currentState)
        {
            case State.Idle:
                stateIdleText.color = Color.yellow;
                stateIdleText.text = "Idle (ACTIVE)\n- If player enters line of sight: change to MoveTowardsPlayer\n- Every few seconds: might switch to Patrol";
                statePatrolText.text = "Patrol\n- If player enters line of sight: change to MoveTowardsPlayer\n- After reaching end: might switch to Idle";
                stateChaseText.text = "MoveTowardsPlayer\n- Always moves toward player";
                break;

            case State.Patrol:
                statePatrolText.color = Color.yellow;
                stateIdleText.text = "Idle\n- If player enters line of sight: change to MoveTowardsPlayer\n- Every few seconds: might switch to Patrol";
                statePatrolText.text = "Patrol (ACTIVE)\n- If player enters line of sight: change to MoveTowardsPlayer\n- After reaching end: might switch to Idle";
                stateChaseText.text = "MoveTowardsPlayer\n- Always moves toward player";
                break;

            case State.MoveTowardsPlayer:
                stateChaseText.color = Color.yellow;
                stateIdleText.text = "Idle\n- If player enters line of sight: change to MoveTowardsPlayer\n- Every few seconds: might switch to Patrol";
                statePatrolText.text = "Patrol\n- If player enters line of sight: change to MoveTowardsPlayer\n- After reaching end: might switch to Idle";
                stateChaseText.text = "MoveTowardsPlayer (ACTIVE)\n- Always moves toward player";
                break;
        }
    }

    void UpdateLineOfSight()
    {
        Vector2 direction = spriteRenderer.flipX ? Vector2.left : Vector2.right;

        Vector3 start = transform.position;
        Vector3 end = start + (Vector3)(direction * detectionRange);

        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            audioSource.PlayOneShot(collisionSFX);

            if (currentState == State.MoveTowardsPlayer)
            {
                StartCoroutine(LoadSceneAfterDelay("Defeat Scene"));
            }
            else
            {
                StartCoroutine(LoadSceneAfterDelay("Victory Scene"));
            }
        }
    }

    IEnumerator LoadSceneAfterDelay(string sceneName)
    {
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene(sceneName);
    }
}
