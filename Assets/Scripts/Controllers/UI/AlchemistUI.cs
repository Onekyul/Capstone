using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 연금술사 재료 교환 UI.
///
/// 흐름:
///   인벤토리 슬롯 클릭  → SelectMaterial() → 패널 표시, 컨트롤 숨김
///   화살표 클릭         → SetMode()        → 해당 패널 컨트롤 표시, 수량 세팅
///   +/- 클릭            → AdjustCount()    → 수량 갱신
///   분해/합성하기 클릭   → ExecuteTrade()   → 교환 실행
/// </summary>
public class AlchemistUI : MonoBehaviour
{
    private enum TradeMode { None, Disassemble, Compound }

    // ─── Inspector References ────────────────────────────────────────────────

    [Header("Alchemist Reference")]
    [SerializeField] private Alchemist alchemist;

    [Header("Inventory Slots")]
    [SerializeField] private InventorySlotUI[] inventorySlots;

    [Header("Center — 선택 재료")]
    [SerializeField] private Image            preMaterialIcon;
    [SerializeField] private TextMeshProUGUI  preMaterialNameText;   // 없으면 비워도 됨
    [SerializeField] private TextMeshProUGUI  preMaterialCountText;  // 모드 선택 전 숨김

    [Header("분해 패널")]
    [SerializeField] private GameObject       disassemblePanel;          // 레시피 없으면 숨김
    [SerializeField] private Button           disassembleModeButton;     // 왼쪽 화살표
    [SerializeField] private Image            disassembleResultIcon;     // Aft_Dis_Material 아이콘
    [SerializeField] private TextMeshProUGUI  disassembleResultCountText;// Aft_Dis_Material 수량 (모드 선택 후 표시)
    [SerializeField] private TextMeshProUGUI  disassembleCountText;      // 교환 횟수 (모드 선택 후 표시)
    [SerializeField] private Button           disassemblePlusButton;     // (모드 선택 후 표시)
    [SerializeField] private Button           disassembleMinusButton;    // (모드 선택 후 표시)
    [SerializeField] private Button           disassembleTradeButton;    // 분해하기 (모드 선택 후 표시)

    [Header("합성 패널")]
    [SerializeField] private GameObject       compoundPanel;             // 레시피 없으면 숨김
    [SerializeField] private Button           compoundModeButton;        // 오른쪽 화살표
    [SerializeField] private Image            compoundResultIcon;        // Aft_Com_Material 아이콘
    [SerializeField] private TextMeshProUGUI  compoundResultCountText;   // Aft_Com_Material 수량 (모드 선택 후 표시)
    [SerializeField] private TextMeshProUGUI  compoundCountText;         // 교환 횟수 (모드 선택 후 표시)
    [SerializeField] private Button           compoundPlusButton;        // (모드 선택 후 표시)
    [SerializeField] private Button           compoundMinusButton;       // (모드 선택 후 표시)
    [SerializeField] private Button           compoundTradeButton;       // 합성하기 (모드 선택 후 표시)

    [Header("Status")]
    [SerializeField] private TextMeshProUGUI statusText;

    // ─── Runtime State ───────────────────────────────────────────────────────

    private ItemData    _selectedItem;
    private TradeRecipe _nextRecipe;   // costItem == _selectedItem  (합성)
    private TradeRecipe _prevRecipe;   // rewardItem == _selectedItem (분해)
    private TradeMode   _activeMode = TradeMode.None;
    private int         _tradeCount = 1;

    // ─── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        disassembleModeButton.onClick.AddListener(() => SetMode(TradeMode.Disassemble));
        compoundModeButton.onClick.AddListener(()    => SetMode(TradeMode.Compound));

        disassemblePlusButton.onClick.AddListener(()  => AdjustCount(+1));
        disassembleMinusButton.onClick.AddListener(() => AdjustCount(-1));
        disassembleTradeButton.onClick.AddListener(() => ExecuteTrade());

        compoundPlusButton.onClick.AddListener(()  => AdjustCount(+1));
        compoundMinusButton.onClick.AddListener(() => AdjustCount(-1));
        compoundTradeButton.onClick.AddListener(() => ExecuteTrade());
    }

    private void OnEnable()
    {
        _selectedItem = null;
        _nextRecipe   = null;
        _prevRecipe   = null;
        _activeMode   = TradeMode.None;
        _tradeCount   = 1;
        if (statusText != null) statusText.text = string.Empty;

        RefreshInventorySlots();
        RenderAll();
    }

    // ─── Inventory Panel ────────────────────────────────────────────────────

    private void RefreshInventorySlots()
    {
        if (DataManager.instance == null) return;

        var invList = DataManager.instance.currentPlayer.Inventory;

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (i < invList.Count)
            {
                ItemData data  = DataManager.instance.GetMaterialData(invList[i].itemId);
                int      count = invList[i].count;

                if (data != null && count > 0)
                {
                    inventorySlots[i].SetMaterial(data, count);

                    Button btn = inventorySlots[i].GetComponent<Button>()
                              ?? inventorySlots[i].GetComponentInChildren<Button>(true);
                    if (btn != null)
                    {
                        var captured = data;
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => SelectMaterial(captured));
                    }
                    else
                    {
                        Debug.LogWarning($"[AlchemistUI] 슬롯 {i} ({data.itemId}) 에 Button 없음");
                    }
                }
                else
                {
                    inventorySlots[i].ClearSlot();
                }
            }
            else
            {
                inventorySlots[i].ClearSlot();
            }
        }
    }

    // ─── State Transitions ──────────────────────────────────────────────────

    private void SelectMaterial(ItemData item)
    {
        _selectedItem = item;
        _activeMode   = TradeMode.None;
        _tradeCount   = 1;

        _nextRecipe = alchemist.TradeTable.recipes.Find(r => r.costItem   != null && r.costItem.itemId   == item.itemId);
        _prevRecipe = alchemist.TradeTable.recipes.Find(r => r.rewardItem != null && r.rewardItem.itemId == item.itemId);

        if (statusText != null) statusText.text = string.Empty;

        RenderAll();
    }

    private void SetMode(TradeMode mode)
    {
        // 같은 화살표 재클릭 시 모드 해제
        _activeMode = (_activeMode == mode) ? TradeMode.None : mode;
        _tradeCount = 1;
        RenderAll();
    }

    private void AdjustCount(int delta)
    {
        int max = MaxCount();
        _tradeCount = Mathf.Clamp(_tradeCount + delta, 1, Mathf.Max(1, max));
        RenderAll();
    }

    // ─── Render ──────────────────────────────────────────────────────────────

    private void RenderAll()
    {
        RenderCenter();
        RenderDisassemblePanel();
        RenderCompoundPanel();
    }

    private void RenderCenter()
    {
        bool has = _selectedItem != null;

        if (preMaterialIcon != null)
        {
            preMaterialIcon.sprite  = has ? _selectedItem.icon : null;
            preMaterialIcon.enabled = has;
        }

        if (preMaterialNameText != null)
            preMaterialNameText.text = has ? _selectedItem.itemName : string.Empty;

        // 수량: 모드 선택 후에만 표시, 소모량 = costPerOp * tradeCount
        bool showCount = has && _activeMode != TradeMode.None;
        if (preMaterialCountText != null)
        {
            preMaterialCountText.gameObject.SetActive(showCount);
            if (showCount)
            {
                int costPerOp  = CostPerOp();
                preMaterialCountText.text = (costPerOp * _tradeCount).ToString();
            }
        }
    }

    private void RenderDisassemblePanel()
    {
        bool hasRecipe = _selectedItem != null && _prevRecipe != null;
        if (disassemblePanel != null) disassemblePanel.SetActive(hasRecipe);
        if (!hasRecipe) return;

        // 결과 아이콘 (항상 표시)
        if (disassembleResultIcon != null)
        {
            disassembleResultIcon.sprite  = _prevRecipe.costItem.icon;
            disassembleResultIcon.enabled = true;
        }

        bool active = _activeMode == TradeMode.Disassemble;
        int  max    = active ? MaxCount() : 0;

        // 결과 수량 (모드 선택 후 표시)
        if (disassembleResultCountText != null)
        {
            disassembleResultCountText.gameObject.SetActive(active);
            if (active)
                disassembleResultCountText.text = (_prevRecipe.costAmount * _tradeCount).ToString();
        }

        // 교환 횟수 텍스트
        if (disassembleCountText != null)
        {
            disassembleCountText.gameObject.SetActive(active);
            if (active) disassembleCountText.text = _tradeCount.ToString();
        }

        // 컨트롤 버튼
        SetButtonVisible(disassemblePlusButton,  active, active && _tradeCount < max);
        SetButtonVisible(disassembleMinusButton, active, active && _tradeCount > 1);
        SetButtonVisible(disassembleTradeButton, active, active && max > 0);
    }

    private void RenderCompoundPanel()
    {
        bool hasRecipe = _selectedItem != null && _nextRecipe != null;
        if (compoundPanel != null) compoundPanel.SetActive(hasRecipe);
        if (!hasRecipe) return;

        // 결과 아이콘 (항상 표시)
        if (compoundResultIcon != null)
        {
            compoundResultIcon.sprite  = _nextRecipe.rewardItem.icon;
            compoundResultIcon.enabled = true;
        }

        bool active = _activeMode == TradeMode.Compound;
        int  max    = active ? MaxCount() : 0;

        // 결과 수량 (모드 선택 후 표시)
        if (compoundResultCountText != null)
        {
            compoundResultCountText.gameObject.SetActive(active);
            if (active)
                compoundResultCountText.text = (_nextRecipe.rewardAmount * _tradeCount).ToString();
        }

        // 교환 횟수 텍스트
        if (compoundCountText != null)
        {
            compoundCountText.gameObject.SetActive(active);
            if (active) compoundCountText.text = _tradeCount.ToString();
        }

        // 컨트롤 버튼
        SetButtonVisible(compoundPlusButton,  active, active && _tradeCount < max);
        SetButtonVisible(compoundMinusButton, active, active && _tradeCount > 1);
        SetButtonVisible(compoundTradeButton, active, active && max > 0);
    }

    // ─── Trade Execution ─────────────────────────────────────────────────────

    private void ExecuteTrade()
    {
        if (DataManager.instance == null || _selectedItem == null) return;
        if (_activeMode == TradeMode.None) return;

        TradeRecipe recipe    = ActiveRecipe();
        int         costPerOp = CostPerOp();
        int         totalCost = costPerOp * _tradeCount;

        if (!DataManager.instance.HasInventory(_selectedItem.itemId, totalCost))
        {
            if (statusText != null) statusText.text = "재료가 부족합니다.";
            return;
        }

        DataManager.instance.UseInventory(_selectedItem.itemId, totalCost);

        string rewardId;
        int    rewardPerOp;
        string rewardName;

        if (_activeMode == TradeMode.Compound)
        {
            rewardId    = recipe.rewardItem.itemId;
            rewardPerOp = recipe.rewardAmount;
            rewardName  = recipe.rewardItem.itemName;
        }
        else
        {
            rewardId    = recipe.costItem.itemId;
            rewardPerOp = recipe.costAmount;
            rewardName  = recipe.costItem.itemName;
        }

        int totalReward = rewardPerOp * _tradeCount;
        DataManager.instance.AddInventory(rewardId, totalReward);

        if (statusText != null)
            statusText.text = $"{rewardName} {totalReward}개 획득!";

        Debug.Log($"[AlchemistUI] {_activeMode}: {_selectedItem.itemName} x{totalCost} → {rewardName} x{totalReward}");

        PostTradeRefresh();
    }

    private void PostTradeRefresh()
    {
        _tradeCount = 1;
        RefreshInventorySlots();

        // 재료를 다 소모했으면 선택 해제
        if (_selectedItem != null &&
            DataManager.instance.GetInventoryCount(_selectedItem.itemId) <= 0)
        {
            _selectedItem = null;
            _nextRecipe   = null;
            _prevRecipe   = null;
            _activeMode   = TradeMode.None;
        }

        RenderAll();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private TradeRecipe ActiveRecipe() => _activeMode switch
    {
        TradeMode.Compound    => _nextRecipe,
        TradeMode.Disassemble => _prevRecipe,
        _                     => null
    };

    /// <summary>현재 모드에서 1회 교환 시 소모되는 선택 재료 수량</summary>
    private int CostPerOp()
    {
        if (_activeMode == TradeMode.Compound)
            return _nextRecipe?.costAmount ?? 0;
        else
            return Mathf.Max(1, _prevRecipe?.rewardAmount ?? 0);
    }

    /// <summary>보유량 기준 최대 교환 가능 횟수</summary>
    private int MaxCount()
    {
        if (_selectedItem == null) return 0;
        int costPerOp = CostPerOp();
        int owned     = DataManager.instance.GetInventoryCount(_selectedItem.itemId);
        return costPerOp > 0 ? Mathf.FloorToInt((float)owned / costPerOp) : 0;
    }

    private void SetButtonVisible(Button btn, bool visible, bool interactable)
    {
        if (btn == null) return;
        btn.gameObject.SetActive(visible);
        btn.interactable = interactable;
    }
}
