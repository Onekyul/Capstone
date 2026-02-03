using UnityEngine;

public class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance { get; private set; }

    [Header("Prefab")]
    [SerializeField] private GameObject damagePopupPrefab; // 3단계에서 만든 프리팹 연결

    void Awake()
    {
        // 싱글톤 설정 (어디서든 접근 가능하게)
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    public void CreatePopup(Vector3 position, float damageAmount, bool isCritical = false)
    {
        if (damagePopupPrefab == null) return;

        // 몬스터 위치에서 생성
        GameObject popup = Instantiate(damagePopupPrefab, position, Quaternion.identity);

        // 텍스트 설정 함수 호출
        DamagePopupController damageComponent = popup.GetComponent<DamagePopupController>();
        if (damageComponent != null)
        {
            damageComponent.Setup(damageAmount, isCritical);
        }
    }
}