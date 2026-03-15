using Fusion;
using UnityEngine;

/// <summary>
/// 보스 던전 경험치 보석 동기화 매니저.
/// 서버: 잡몹 사망 시 보석 위치를 [Networked] 배열에 등록.
/// 클라이언트: 배열을 읽어 보석 비주얼 오브젝트를 표시.
/// 보석 획득: 서버의 DungeonPlayerStats.FixedUpdateNetwork()에서 거리 체크 후 처리.
/// </summary>
public class JewelSyncManager : NetworkBehaviour
{
    public static JewelSyncManager Instance { get; private set; }

    public const int MaxJewels = 100;
    public const float PickupRadius = 0.6f;
    public const float JewelExpAmount = 1f;

    [Networked, Capacity(MaxJewels)]
    public NetworkArray<Vector2> JewelPositions => default;

    [Networked, Capacity(MaxJewels)]
    public NetworkArray<NetworkBool> JewelActive => default;

    [SerializeField] private GameObject jewelVisualPrefab;

    private GameObject[] _visuals;

    public override void Spawned()
    {
        Instance = this;

#if !UNITY_SERVER
        if (!HasStateAuthority)
        {
            if (jewelVisualPrefab == null)
            {
                Debug.LogError("[JewelSync] jewelVisualPrefab이 할당되지 않았습니다. Inspector에서 설정해주세요.");
                return;
            }

            _visuals = new GameObject[MaxJewels];
            for (int i = 0; i < MaxJewels; i++)
            {
                _visuals[i] = Instantiate(jewelVisualPrefab);
                _visuals[i].SetActive(false);

                // 클라이언트 비주얼은 물리/로직 비활성화
                var col = _visuals[i].GetComponent<Collider2D>();
                if (col != null) col.enabled = false;
            }
            Debug.Log("[JewelSync] 클라이언트 보석 비주얼 초기화 완료");
        }
#endif
    }

    /// <summary>
    /// 서버 전용: 잡몹 사망 시 보석 등록.
    /// </summary>
    public void AddJewel(Vector2 position)
    {
        if (!HasStateAuthority) return;

        for (int i = 0; i < MaxJewels; i++)
        {
            if (JewelActive[i]) continue;

            JewelPositions.Set(i, position);
            JewelActive.Set(i, true);
            return;
        }

        Debug.LogWarning("[JewelSync] 보석 슬롯 부족 — 최대치 초과");
    }

    /// <summary>
    /// 서버 전용: 플레이어가 보석 획득 시 슬롯 비활성화.
    /// </summary>
    public void PickupJewel(int slot)
    {
        if (!HasStateAuthority) return;
        JewelActive.Set(slot, false);
    }

    public override void Render()
    {
#if !UNITY_SERVER
        if (HasStateAuthority || _visuals == null) return;

        for (int i = 0; i < MaxJewels; i++)
        {
            bool active = JewelActive[i];
            _visuals[i].SetActive(active);
            if (active)
                _visuals[i].transform.position = (Vector3)(Vector2)JewelPositions[i];
        }
#endif
    }
}
