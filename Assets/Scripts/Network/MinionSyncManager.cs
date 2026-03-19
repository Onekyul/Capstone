using Fusion;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 잡몹 위치/방향/HP 동기화 매니저.
/// 서버: BossStageManager.ActiveMinions 배열에서 위치/플립/HP를 읽어 [Networked]에 저장.
/// 클라이언트: [Networked] 배열을 읽어 로컬 시각 오브젝트를 업데이트.
/// </summary>
public class MinionSyncManager : NetworkBehaviour
{
    public const int MaxMinions = 50;

    [Networked, Capacity(MaxMinions)]
    public NetworkArray<Vector2> MinionPositions => default;

    [Networked, Capacity(MaxMinions)]
    public NetworkArray<NetworkBool> MinionActive => default;

    [Networked, Capacity(MaxMinions)]
    public NetworkArray<NetworkBool> MinionFlipped => default;

    [Networked, Capacity(MaxMinions)]
    public NetworkArray<float> MinionHpRatio => default;

    [SerializeField] private GameObject minionVisualPrefab;

    private GameObject[] _visuals;
    private SpriteRenderer[] _visualRenderers;
    private Slider[] _hpSliders;

    public override void Spawned()
    {
#if !UNITY_SERVER
        if (!HasStateAuthority)
        {
            if (minionVisualPrefab == null)
            {
                Debug.LogError("[MinionSync] minionVisualPrefab이 할당되지 않았습니다. Inspector에서 설정해주세요.");
                return;
            }

            _visuals = new GameObject[MaxMinions];
            _visualRenderers = new SpriteRenderer[MaxMinions];
            _hpSliders = new Slider[MaxMinions];

            for (int i = 0; i < MaxMinions; i++)
            {
                _visuals[i] = Instantiate(minionVisualPrefab);
                _visuals[i].SetActive(false);

                var mc = _visuals[i].GetComponent<MonsterController>();
                if (mc != null) mc.enabled = false;

                var col = _visuals[i].GetComponent<Collider2D>();
                if (col != null) col.enabled = false;

                _visualRenderers[i] = _visuals[i].GetComponentInChildren<SpriteRenderer>();
                _hpSliders[i] = _visuals[i].GetComponentInChildren<Slider>();
            }
            Debug.Log("[MinionSync] 클라이언트 시각 오브젝트 초기화 완료");
        }
#endif
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        if (BossStageManager.instance == null) return;

        var minions = BossStageManager.instance.ActiveMinions;

        // 활성 잡몹을 앞 슬롯부터 순서대로 채움
        int slot = 0;
        for (int i = 0; i < minions.Count; i++)
        {
            var m = minions[i];
            if (m == null || !m.gameObject.activeSelf) continue;
            if (slot >= MaxMinions) break;

            MinionPositions.Set(slot, m.transform.position);
            MinionActive.Set(slot, true);

            // 스프라이트 플립 방향 동기화
            var sr = m.GetComponentInChildren<SpriteRenderer>();
            MinionFlipped.Set(slot, sr != null && sr.flipX);

            // HP 비율 동기화
            float maxHp = m.GetMaxHP();
            MinionHpRatio.Set(slot, maxHp > 0f ? m.GetCurrentHP() / maxHp : 1f);

            slot++;
        }

        // 나머지 슬롯 비활성화
        for (int i = slot; i < MaxMinions; i++)
            MinionActive.Set(i, false);
    }

    public override void Render()
    {
#if !UNITY_SERVER
        if (HasStateAuthority || _visuals == null) return;

        for (int i = 0; i < MaxMinions; i++)
        {
            bool active = MinionActive[i];
            _visuals[i].SetActive(active);
            if (active)
            {
                _visuals[i].transform.position = (Vector3)(Vector2)MinionPositions[i];
                if (_visualRenderers[i] != null)
                    _visualRenderers[i].flipX = MinionFlipped[i];
                if (_hpSliders[i] != null)
                    _hpSliders[i].value = MinionHpRatio[i];
            }
        }
#endif
    }
}
