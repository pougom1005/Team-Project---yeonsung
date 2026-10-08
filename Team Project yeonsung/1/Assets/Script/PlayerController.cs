using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 5f;

    [Header("점프 설정")]
    public KeyCode jumpKey = KeyCode.UpArrow;   // 점프 키 (위 방향키)
    public float jumpForce = 7f;

    [Header("대쉬 설정")]
    public KeyCode dashKey = KeyCode.Space; // 대쉬 키
    public float dashDistance = 3f;         // 대쉬 거리 (클수록 멀리 감)
    public float dashDuration = 0.15f;      // 대쉬 지속 시간(초) (짧을수록 빠름)
    public float dashCooldown = 0.8f;       // 쿨타임(초), 대쉬를 쓴 시점부터 계산
    public bool dashIgnoreGravity = true;   // 체크하면 공중 대쉬 중 떨어지지 않고 수평으로 감

    [Header("공격 설정 (바라보는 방향 앞쪽)")]
    public KeyCode attackKey = KeyCode.X;   // 공격 키
    public int attackDamage = 1;            // 대미지
    public float attackRange = 1f;          // 앞쪽 사거리 (가로 길이)
    public float attackHeight = 1f;         // 공격 범위 세로 길이
    public float attackOffsetX = 0.5f;      // 플레이어 몸에서 공격 범위가 시작되는 거리
    public float attackOffsetY = 0f;        // 공격 범위 위아래 위치 보정
    public float attackHitDelay = 0.1f;     // 공격 시작 후 대미지가 들어가는 시점(초)
    public float attackDuration = 0.4f;     // 공격 전체 시간(초), 끝나야 재공격 가능
    public LayerMask enemyLayer;            // 적이 속한 레이어

    [Header("특수공격(낙하공격) 설정")]
    public KeyCode specialKey = KeyCode.C;  // 특수공격 키
    public float specialCooldown = 5f;      // 쿨타임(초), 사용 시점부터 계산
    public int specialDamage = 3;           // 특수공격 대미지
    public float specialHoverTime = 0.25f;  // 낙하 전에 공중에서 멈춰 있는 시간(초)
    public float specialFallSpeed = 25f;    // 낙하 속도 (클수록 빠름)
    public float specialHitWidth = 0.3f;    // 히트박스 가로 (좁을수록 정중앙만 맞음)
    public float specialHitHeight = 0.6f;   // 히트박스 세로
    public float specialHitOffsetY = 0f;    // 히트박스 위아래 위치 보정 (+ 위로, - 아래로)

    [Header("화면 표시 (확인용)")]
    public bool showCooldownUI = true;       // 화면 왼쪽 아래에 쿨타임 표시
    public bool showAttackBox = true;        // 공격 중 박스 표시 여부
    public bool showAttackBoxAlways = false; // 공격 안 할 때도 박스 표시 여부

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;
    private float moveInput;
    private bool isGrounded;
    private bool isAttacking;
    private int facingDir = 1;              // 1 = 오른쪽, -1 = 왼쪽

    // 대쉬 상태
    private bool isDashing;                 // 대쉬 중에만 true
    private bool airDashUsed;               // 공중 대쉬를 이미 썼는지 (땅에 닿으면 false로 초기화)
    private float nextDashTime;             // 다음에 대쉬를 쓸 수 있는 시각

    // 특수공격 상태
    private bool isSpecialAttacking;        // 특수공격 전체 동안 true (멈춤 + 낙하)
    private bool isDiving;                  // 낙하 중에만 true
    private bool diveLanded;                // 낙하 중 착지했는지
    private bool isInvincible;              // 무적 상태
    private float nextSpecialTime;          // 다음에 특수공격을 쓸 수 있는 시각
    private HashSet<Collider2D> specialHitSet = new HashSet<Collider2D>();

    // 나중에 적의 공격 코드에서 "if (player.IsInvincible) return;" 처럼 사용합니다.
    public bool IsInvincible
    {
        get { return isInvincible; }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    void Update()
    {
        // 대쉬, 특수공격, 일반공격 중에는 이동/점프/다른 행동 입력을 막습니다.
        if (isDashing || isSpecialAttacking || isAttacking)
        {
            moveInput = 0f;
            return;
        }

        // 좌우 이동 입력
        moveInput = Input.GetAxis("Horizontal");

        // 점프 (위 방향키), 땅에 닿아있을 때만
        if (Input.GetKeyDown(jumpKey) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // 대쉬
        if (Input.GetKeyDown(dashKey))
        {
            TryStartDash();
        }

        // 공격
        if (Input.GetKeyDown(attackKey))
        {
            StartCoroutine(AttackRoutine());
        }

        // 특수공격 (낙하공격)
        if (Input.GetKeyDown(specialKey))
        {
            TryStartSpecialAttack();
        }
    }

    void FixedUpdate()
    {
        // 대쉬나 특수공격 중에는 각 코루틴이 속도를 직접 조절합니다.
        if (isDashing || isSpecialAttacking)
        {
            return;
        }

        // 공격 중에는 좌우로 멈춥니다 (낙하 속도는 유지)
        if (isAttacking)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        // 이동 방향에 따라 바라보는 방향 + 스프라이트 반전
        if (moveInput > 0f)
        {
            facingDir = 1;
            spriteRenderer.flipX = false;
        }
        else if (moveInput < 0f)
        {
            facingDir = -1;
            spriteRenderer.flipX = true;
        }

        // TODO: 이미지/애니메이션 작업 끝나면 여기에 animator.SetFloat("Speed", Mathf.Abs(moveInput)); 추가 예정
    }

    // ---------------- 대쉬 ----------------

    void TryStartDash()
    {
        if (Time.time < nextDashTime)
        {
            Debug.Log("대쉬 쿨타임 중! 남은 시간: " + (nextDashTime - Time.time).ToString("F1") + "초");
            return;
        }

        // 공중에서는 1회만 가능, 땅에 닿을 때까지 다시 못 씁니다.
        if (!isGrounded && airDashUsed)
        {
            Debug.Log("공중 대쉬는 땅에 닿을 때까지 다시 쓸 수 없어요.");
            return;
        }

        StartCoroutine(DashRoutine());
    }

    IEnumerator DashRoutine()
    {
        isDashing = true;
        nextDashTime = Time.time + dashCooldown;

        // 공중에서 쓴 대쉬만 공중 횟수를 소모합니다. (땅에서 쓴 대쉬는 소모하지 않음)
        if (!isGrounded)
        {
            airDashUsed = true;
        }

        Debug.Log("대쉬!");

        float originalGravity = rb.gravityScale;
        CollisionDetectionMode2D originalMode = rb.collisionDetectionMode;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // 빠른 속도에서 벽을 뚫지 않게

        if (dashIgnoreGravity)
        {
            rb.gravityScale = 0f;
        }

        // TODO: 애니메이션 작업 후 여기에 animator.SetTrigger("Dash"); 추가 예정

        float dashSpeed = dashDistance / Mathf.Max(0.01f, dashDuration);
        float endTime = Time.time + dashDuration;

        while (Time.time < endTime)
        {
            float vy = dashIgnoreGravity ? 0f : rb.linearVelocity.y;
            rb.linearVelocity = new Vector2(facingDir * dashSpeed, vy);
            yield return new WaitForFixedUpdate();
        }

        // 원래 상태로 복구
        rb.gravityScale = originalGravity;
        rb.collisionDetectionMode = originalMode;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        isDashing = false;
    }

    // ---------------- 일반 공격 (앞쪽) ----------------

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        Debug.Log("공격 시작!");

        // TODO: 애니메이션 작업 후 여기에 animator.SetTrigger("Attack"); 추가 예정

        yield return new WaitForSeconds(attackHitDelay);
        DealDamage();

        yield return new WaitForSeconds(Mathf.Max(0f, attackDuration - attackHitDelay));
        isAttacking = false;
        Debug.Log("공격 끝! 다시 공격 가능");
    }

    // 바라보는 방향 앞쪽 박스 안의 적에게 대미지를 줍니다.
    void DealDamage()
    {
        Vector2 center = GetAttackCenter(facingDir);
        Vector2 size = new Vector2(attackRange, attackHeight);

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f, enemyLayer);
        Debug.Log("공격 범위 안에 감지된 적 수: " + hits.Length);

        foreach (Collider2D hit in hits)
        {
            // 적 스크립트에 TakeDamage(int) 함수가 있으면 호출됩니다.
            hit.SendMessageUpwards("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
        }
    }

    Vector2 GetAttackCenter(int dir)
    {
        return new Vector2(
            transform.position.x + dir * (attackOffsetX + attackRange * 0.5f),
            transform.position.y + attackOffsetY
        );
    }

    // ---------------- 특수공격 (낙하공격) ----------------

    void TryStartSpecialAttack()
    {
        if (isGrounded)
        {
            Debug.Log("특수공격은 공중에서만 사용할 수 있어요.");
            return;
        }

        if (Time.time < nextSpecialTime)
        {
            Debug.Log("특수공격 쿨타임 중! 남은 시간: " + (nextSpecialTime - Time.time).ToString("F1") + "초");
            return;
        }

        StartCoroutine(SpecialAttackRoutine());
    }

    IEnumerator SpecialAttackRoutine()
    {
        isSpecialAttacking = true;
        isInvincible = true;
        diveLanded = false;
        specialHitSet.Clear();
        nextSpecialTime = Time.time + specialCooldown;
        Debug.Log("특수공격 시작! (무적)");

        float originalGravity = rb.gravityScale;
        CollisionDetectionMode2D originalMode = rb.collisionDetectionMode;

        // 1) 공중에서 잠깐 멈춤
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        // TODO: 애니메이션 작업 후 여기에 animator.SetTrigger("Special"); 추가 예정

        yield return new WaitForSeconds(specialHoverTime);

        // 2) 최대 속도로 수직 낙하 (착지할 때까지)
        isDiving = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // 빠른 속도에서 바닥을 뚫지 않게
        Debug.Log("낙하 시작!");

        while (!diveLanded && !isGrounded)
        {
            rb.linearVelocity = new Vector2(0f, -specialFallSpeed);
            SpecialHitCheck();
            yield return new WaitForFixedUpdate();
        }

        // 착지 순간에도 한 번 더 판정
        SpecialHitCheck();

        // 3) 원래 상태로 복구
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = originalGravity;
        rb.collisionDetectionMode = originalMode;
        isDiving = false;
        isSpecialAttacking = false;
        isInvincible = false;
        Debug.Log("착지! 특수공격 종료");
    }

    // 낙하 중 캐릭터 아래 중앙의 히트박스로 적을 찾습니다. (낙하 한 번에 적 하나당 1회만 대미지)
    void SpecialHitCheck()
    {
        Vector2 center = GetSpecialHitCenter();
        Vector2 size = new Vector2(specialHitWidth, specialHitHeight);

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f, enemyLayer);
        foreach (Collider2D hit in hits)
        {
            if (specialHitSet.Add(hit))
            {
                Debug.Log("특수공격 적중: " + hit.name);
                hit.SendMessageUpwards("TakeDamage", specialDamage, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    // 히트박스 위치: 캐릭터 콜라이더의 발밑 중앙
    Vector2 GetSpecialHitCenter()
    {
        Collider2D c = (col != null) ? col : GetComponent<Collider2D>();
        float bottomY = (c != null) ? c.bounds.min.y : transform.position.y;
        float centerX = (c != null) ? c.bounds.center.x : transform.position.x;

        return new Vector2(centerX, bottomY - specialHitHeight * 0.5f + specialHitOffsetY);
    }

    // ---------------- 화면 표시 ----------------

    // 공격 범위를 화면(Gizmos)에 그려줍니다. (Game 화면에서는 Gizmos 버튼을 켜야 보입니다.)
    void OnDrawGizmos()
    {
        // 특수공격 히트박스: 낙하 중에는 하늘색 박스, 평소에는 (옵션을 켰을 때만) 주황 테두리
        bool diving = Application.isPlaying && isDiving && showAttackBox;
        if (diving || showAttackBoxAlways)
        {
            Gizmos.color = diving ? Color.cyan : new Color(1f, 0.5f, 0f);
            Vector3 specialSize = new Vector3(specialHitWidth, specialHitHeight, 0f);
            Vector3 specialCenter = GetSpecialHitCenter();

            if (diving)
            {
                Gizmos.DrawCube(specialCenter, specialSize);
            }
            Gizmos.DrawWireCube(specialCenter, specialSize);
        }

        // 일반 공격 박스: 공격 중에는 노란 박스, 평소에는 (옵션을 켰을 때만) 빨간 테두리
        Vector3 size = new Vector3(attackRange, attackHeight, 0f);
        bool attackingNow = Application.isPlaying && isAttacking && showAttackBox;

        if (!attackingNow && !showAttackBoxAlways)
        {
            return;
        }

        Gizmos.color = attackingNow ? Color.yellow : Color.red;

        int dir = Application.isPlaying ? facingDir : 1;
        if (attackingNow)
        {
            Gizmos.DrawCube(GetAttackCenter(dir), size);
        }
        Gizmos.DrawWireCube(GetAttackCenter(dir), size);
    }

    // 화면 왼쪽 아래에 대쉬/특수공격 쿨타임을 표시합니다. (글자가 깨지지 않게 영어로 표시)
    void OnGUI()
    {
        if (!showCooldownUI)
        {
            return;
        }

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 20;
        style.normal.textColor = Color.white;

        // 대쉬 표시
        float dashRemain = nextDashTime - Time.time;
        string dashText;

        if (isDashing)
        {
            dashText = "Dash : ACTIVE";
        }
        else if (dashRemain > 0f)
        {
            dashText = "Dash CD : " + dashRemain.ToString("F1") + "s";
        }
        else if (!isGrounded && airDashUsed)
        {
            dashText = "Dash : USED (land to reset)";
        }
        else
        {
            dashText = "Dash : READY";
        }

        GUI.Label(new Rect(10f, Screen.height - 70f, 400f, 30f), dashText, style);

        // 특수공격 표시
        float remain = nextSpecialTime - Time.time;
        string text;

        if (isSpecialAttacking)
        {
            text = "Special : ACTIVE";
        }
        else if (remain > 0f)
        {
            text = "Special CD : " + remain.ToString("F1") + "s";
        }
        else
        {
            text = "Special : READY";
        }

        GUI.Label(new Rect(10f, Screen.height - 40f, 400f, 30f), text, style);
    }

    // ---------------- 충돌 처리 ----------------

    // 땅(Ground 태그)에 닿으면 isGrounded를 true로, 공중 대쉬 제한도 풉니다.
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            airDashUsed = false;
        }

        // 특수공격 낙하 중에 무언가의 윗면에 닿으면 착지로 처리합니다.
        if (isDiving)
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y > 0.5f)
                {
                    diveLanded = true;
                    break;
                }
            }
        }
    }

    // 땅에서 떨어지면 isGrounded를 false로
    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}