using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 5f;

    [Header("점프 설정")]
    public float jumpForce = 7f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private float moveInput;
    private bool isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // 좌우 이동 입력
        moveInput = Input.GetAxis("Horizontal");

        // 점프 입력 (위 화살표), 땅에 닿아있을 때만 점프
        if (Input.GetKeyDown(KeyCode.Z) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    void FixedUpdate()
    {
        // 실제 물리 이동은 FixedUpdate에서 처리합니다.
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        // 이동 방향에 따라 스프라이트 좌우 반전
        if (moveInput > 0f)
        {
            spriteRenderer.flipX = false;
        }
        else if (moveInput < 0f)
        {
            spriteRenderer.flipX = true;
        }

        // TODO: 이미지/애니메이션 작업 끝나면 여기에 animator.SetFloat("Speed", Mathf.Abs(moveInput)); 추가 예정
    }

    // 땅(Ground 태그)에 닿으면 isGrounded를 true로
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
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