using UnityEngine;
using TMPro; // TextMeshPro를 사용하기 위해 필수!

public class DamagePopupController : MonoBehaviour
{
    private TextMeshPro textMesh;
    private float disappearTimer;
    private Color textColor;

    // 움직임 관련 변수
    private Vector3 moveVector;
    private const float DISAPPEAR_TIMER_MAX = 1f;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    public void Setup(float damageAmount, bool isCritical)
    {
        textMesh.text = Mathf.RoundToInt(damageAmount).ToString();

        if (isCritical)
        {
            textMesh.fontSize = 6;
            textMesh.color = Color.red; // 크리티컬은 빨간색
        }
        else
        {
            textMesh.fontSize = 4;
            textMesh.color = Color.yellow; // 일반은 노란색
        }

        // 상태이상(독, 화상) 데미지 색상 구분도 가능
        // else if (isDotDamage) textMesh.color = new Color(1f, 0.5f, 0f); // 주황색

        textColor = textMesh.color;
        disappearTimer = DISAPPEAR_TIMER_MAX;

        // 위로 솟구치는 움직임 설정
        moveVector = new Vector3(0.5f, 1f) * 2f;
    }

    void Update()
    {
        // 1. 위로 이동
        transform.position += moveVector * Time.deltaTime;
        moveVector -= moveVector * 2f * Time.deltaTime; // 점점 느려지게

        // 2. 시간이 지나면 투명해지면서 사라짐
        disappearTimer -= Time.deltaTime;
        if (disappearTimer < 0)
        {
            float disappearSpeed = 3f;
            textColor.a -= disappearSpeed * Time.deltaTime;
            textMesh.color = textColor;

            if (textColor.a < 0)
            {
                Destroy(gameObject); // (나중에는 풀링으로 반환하는 게 좋습니다)
            }
        }
    }
}
