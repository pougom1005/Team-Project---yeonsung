using UnityEngine;

public class TrainingDummy : MonoBehaviour
{
    [Header("측정 설정")]
    public float combatResetTime = 3f;      // 마지막 타격 후 이 시간(초) 동안 안 맞으면 측정 종료
    public float minMeasureTime = 1f;       // DPS 계산 시 최소 측정 시간 (한 번만 때렸을 때 숫자가 튀는 것 방지)
    public KeyCode resetKey = KeyCode.R;    // 수치 초기화 키

    [Header("화면 표시 설정")]
    public Vector2 displayPosition = new Vector2(10f, 10f);   // 화면 왼쪽 위 기준 위치
    public Vector2 displaySize = new Vector2(260f, 130f);     // 표시 상자 크기
    public int fontSize = 20;

    private int lastDamage;
    private int totalDamage;
    private int hitCount;
    private float startTime;
    private float lastHitTime;
    private bool inCombat;

    // 플레이어의 공격이 이 함수를 호출합니다.
    public void TakeDamage(int damage)
    {
        // 측정이 끝난 뒤 새로 맞으면 수치를 처음부터 다시 측정합니다.
        if (!inCombat)
        {
            ResetStats();
            startTime = Time.time;
            inCombat = true;
        }

        lastDamage = damage;
        totalDamage += damage;
        hitCount++;
        lastHitTime = Time.time;

        Debug.Log("허수아비 피격! 대미지: " + damage + " / 누적: " + totalDamage + " / DPS: " + GetDps().ToString("F2"));
    }

    void Update()
    {
        // 마지막 타격 후 일정 시간이 지나면 측정을 끝냅니다 (수치는 그대로 남아 있습니다).
        if (inCombat && Time.time - lastHitTime > combatResetTime)
        {
            inCombat = false;
        }

        if (Input.GetKeyDown(resetKey))
        {
            ResetStats();
        }
    }

    float GetDps()
    {
        if (hitCount == 0)
        {
            return 0f;
        }

        // 첫 타격부터 마지막 타격까지의 시간으로 계산합니다.
        float elapsed = Mathf.Max(lastHitTime - startTime, minMeasureTime);
        return totalDamage / elapsed;
    }

    void ResetStats()
    {
        lastDamage = 0;
        totalDamage = 0;
        hitCount = 0;
        inCombat = false;
    }

    // 화면에 수치를 표시합니다. (글자가 깨지지 않게 영어로 표시)
    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = fontSize;
        style.normal.textColor = Color.white;

        GUI.Box(new Rect(displayPosition.x, displayPosition.y, displaySize.x, displaySize.y), "");

        string text =
            "Last Hit : " + lastDamage + "\n" +
            "Total    : " + totalDamage + "\n" +
            "Hits     : " + hitCount + "\n" +
            "DPS      : " + GetDps().ToString("F2");

        GUI.Label(new Rect(displayPosition.x + 10f, displayPosition.y + 5f, displaySize.x, displaySize.y), text, style);
    }
}