using Fusion;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보스 던전 재단 + 보스 페이즈 동기화 매니저.
/// 서버: BossStageManager/BossMonsterController 상태를 [Networked]에 저장.
/// 클라이언트: [Networked] 값을 읽어 재단 비주얼과 보스 연출을 업데이트.
/// </summary>
public class AltarSyncManager : NetworkBehaviour
{
    public static AltarSyncManager Instance { get; private set; }

    // ── 재단 ──
    [Networked, OnChangedRender(nameof(OnAltarActiveChanged))]
    public NetworkBool AltarActive { get; set; }

    [Networked]
    public Vector2 AltarPosition { get; set; }

    [Networked]
    public float AltarHpRatio { get; set; }

    // ── 보스 HP ──
    [Networked]
    public float BossHpRatio { get; set; }

    // ── 보스 페이즈 (0=Normal, 1=Rage, 2=Groggy) ──
    [Networked, OnChangedRender(nameof(OnBossPhaseChanged))]
    public int BossPhase { get; set; }

    [Header("재단 비주얼")]
    [SerializeField] private GameObject altarVisualPrefab;

    [Header("보스 레퍼런스 (씬에 있는 보스)")]
    [SerializeField] private BossMonsterController boss;

    private GameObject _altarVisual;
    private Slider _altarHpSlider;

    public override void Spawned()
    {
        Instance = this;

#if !UNITY_SERVER
        if (!HasStateAuthority && altarVisualPrefab != null)
        {
            // 프리팹을 비활성 상태로 Instantiate → 한 프레임 노출 방지
            altarVisualPrefab.SetActive(false);
            _altarVisual = Instantiate(altarVisualPrefab);
            altarVisualPrefab.SetActive(true);

            // AltarController 로직 비활성화 (비주얼만 사용)
            var ac = _altarVisual.GetComponent<AltarController>();
            if (ac != null) ac.enabled = false;

            var col = _altarVisual.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            _altarHpSlider = _altarVisual.GetComponentInChildren<Slider>();
        }
#endif
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        if (BossStageManager.instance == null) return;

        // ── 재단 동기화 ──
        var altar = BossStageManager.instance.ActiveAltar;
        bool altarExists = altar != null && altar.gameObject.activeSelf;

        AltarActive = altarExists;
        if (altarExists)
        {
            AltarPosition = altar.transform.position;
            float max = altar.GetMaxHP();
            AltarHpRatio = max > 0f ? altar.GetCurrentHP() / max : 1f;
        }

        // ── 보스 페이즈 동기화 ──
        if (boss != null)
        {
            int phase = (int)boss.CurrentPhase; // 0=Normal, 1=Rage, 2=Groggy
            BossPhase = phase;
        }
    }

    public override void Render()
    {
#if !UNITY_SERVER
        if (HasStateAuthority || _altarVisual == null) return;

        _altarVisual.SetActive(AltarActive);
        if (AltarActive)
        {
            _altarVisual.transform.position = (Vector3)(Vector2)AltarPosition;
            if (_altarHpSlider != null)
                _altarHpSlider.value = AltarHpRatio;
        }
#endif
    }

    // ── 클라이언트 콜백 ──

    private void OnAltarActiveChanged()
    {
#if !UNITY_SERVER
        if (_altarVisual != null)
            _altarVisual.SetActive(AltarActive);
#endif
    }

    private void OnBossPhaseChanged()
    {
#if !UNITY_SERVER
        if (boss == null) return;

        var phase = (BossMonsterController.BossPhase)BossPhase;
        switch (phase)
        {
            case BossMonsterController.BossPhase.Normal:
                boss.ClientSetNormal();
                break;
            case BossMonsterController.BossPhase.Rage:
                boss.ClientSetRage();
                break;
            case BossMonsterController.BossPhase.Groggy:
                boss.ClientSetGroggy();
                break;
        }
#endif
    }
}
