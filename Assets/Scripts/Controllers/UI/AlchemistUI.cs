using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controller for the Alchemist NPC's material exchange UI.
///
/// State Machine:
///   None        — No item selected or no recipe available in active direction
///   Compound    — > button selected: costItem → rewardItem (higher tier)
///   Disassemble — < button selected: rewardItem → costItem (lower tier, recipe reversed)
///
/// Flow:
///   Inventory slot click  → SelectMaterial()  → renders center + mode buttons
///   < or > button click   → SetMode()         → renders action area
///   +/- button            → AdjustCount()     → renders count + trade button
///   Trade button          → ExecuteTrade()    → PostTradeRefresh()
/// </summary>
public class AlchemistUI : MonoBehaviour
{
    // ─── State Definition ───────────────────────────────────────────────────

    private enum TradeMode { None, Compound, Disassemble }

    // ─── Inspector References ────────────────────────────────────────────────

    [Header("Alchemist Reference")]
    [SerializeField] private Alchemist alchemist;

    [Header("Inventory Slots — Inven ~ Inven(14), size = 15")]
    [SerializeField] private InventorySlotUI[] inventorySlots;

    [Header("Center — Selected Material")]
    [SerializeField] private Image preMaterialIcon;
    [SerializeField] private TextMeshProUGUI preMaterialNameText;

    [Header("Mode Select Buttons")]
    [SerializeField] private Button disassembleModeButton;  // < (분해 모드 선택)
    [SerializeField] private Button compoundModeButton;     // > (합성 모드 선택)

    [Header("Mode Select — Result Preview Icons")]
    [SerializeField] private Image disassembleResultIcon;   // < 버튼 옆 결과 아이콘
    [SerializeField] private Image compoundResultIcon;      // > 버튼 옆 결과 아이콘

    [Header("Action Area — Shared Controls")]
    [SerializeField] private GameObject actionArea;          // +/- 와 교환 버튼을 묶는 부모
    [SerializeField] private TextMeshProUGUI tradeInfoText;  // "철광석 x10 → 강철 x1" 형태
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Button plusButton;
    [SerializeField] private Button minusButton;
    [SerializeField] private Button tradeButton;

    [Header("Status")]
    [SerializeField] private TextMeshProUGUI statusText;

    // ─── Runtime State ───────────────────────────────────────────────────────

    private ItemData    _selectedItem;
    private TradeRecipe _nextRecipe;      // costItem == _selectedItem  (합성)
    private TradeRecipe _prevRecipe;      // rewardItem == _selectedItem (분해)
    private TradeMode   _activeMode = TradeMode.None;
    private int         _tradeCount = 1;

    // ─── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        disassembleModeButton.onClick.AddListener(() => SetMode(TradeMode.Disassemble));
        compoundModeButton.onClick.AddListener(()    => SetMode(TradeMode.Compound));
        plusButton.onClick.AddListener(OnPlus);
        minusButton.onClick.AddListener(OnMinus);
        tradeButton.onClick.AddListener(ExecuteTrade);
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
        RenderCenter();
        RenderModeButtons();
        RenderActionArea();
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

                    // 슬롯에 Button 컴포넌트가 있으면 클릭 이벤트 연결
                    Button btn = inventorySlots[i].GetComponentInChildren<Button>();
                    if (btn != null)
                    {
                        var captured = data;
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => SelectMaterial(captured));
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

    /// <summary>
    /// Called when an inventory slot is clicked.
    /// Looks up available recipes in both directions and resets mode to None.
    /// </summary>
    private void SelectMaterial(ItemData item)
    {
        _selectedItem = item;
        _activeMode   = TradeMode.None;
        _tradeCount   = 1;

        _nextRecipe = alchemist.TradeTable.recipes.Find(r => r.costItem   == item);
        _prevRecipe = alchemist.TradeTable.recipes.Find(r => r.rewardItem == item);

        if (statusText != null) statusText.text = string.Empty;

        RenderCenter();
        RenderModeButtons();
        RenderActionArea();
    }

    /// <summary>
    /// Called when < or > mode button is clicked.
    /// Switches active mode and resets trade count.
    /// </summary>
    private void SetMode(TradeMode mode)
    {
        _activeMode = mode;
        _tradeCount = 1;
        RenderActionArea();
    }

    // ─── Render Methods ──────────────────────────────────────────────────────

    /// <summary>Updates the center Pre_Material slot.</summary>
    private void RenderCenter()
    {
        bool has = _selectedItem != null;
        if (preMaterialIcon != null)    { preMaterialIcon.sprite = has ? _selectedItem.icon : null; preMaterialIcon.enabled = has; }
        if (preMaterialNameText != null)  preMaterialNameText.text = has ? _selectedItem.itemName : string.Empty;
    }

    /// <summary>
    /// Updates the two mode-select buttons and their result preview icons.
    /// Disables a button if no recipe exists in that direction.
    /// </summary>
    private void RenderModeButtons()
    {
        // Disassemble (<)
        bool canDis = _selectedItem != null && _prevRecipe != null;
        disassembleModeButton.interactable = canDis;
        if (disassembleResultIcon != null)
        {
            disassembleResultIcon.sprite  = canDis ? _prevRecipe.costItem.icon : null;
            disassembleResultIcon.enabled = canDis;
        }

        // Compound (>)
        bool canCom = _selectedItem != null && _nextRecipe != null;
        compoundModeButton.interactable = canCom;
        if (compoundResultIcon != null)
        {
            compoundResultIcon.sprite  = canCom ? _nextRecipe.rewardItem.icon : null;
            compoundResultIcon.enabled = canCom;
        }
    }

    /// <summary>
    /// Updates the shared action area (info text, count, +/-, trade button).
    /// Hides the area entirely when no mode is active.
    /// </summary>
    private void RenderActionArea()
    {
        TradeRecipe active = ActiveRecipe();

        if (actionArea != null)
            actionArea.SetActive(active != null);

        if (active == null) return;

        // Trade info line
        if (tradeInfoText != null)
        {
            if (_activeMode == TradeMode.Compound)
                tradeInfoText.text = $"{active.costItem.itemName} x{active.costAmount} → {active.rewardItem.itemName} x{active.rewardAmount}";
            else
                tradeInfoText.text = $"{active.rewardItem.itemName} x{active.rewardAmount} → {active.costItem.itemName} x{active.costAmount}";
        }

        // Max count
        int costPerOp = CostPerOperation(active);
        int owned     = DataManager.instance.GetInventoryCount(_selectedItem.itemId);
        int maxCount  = costPerOp > 0 ? Mathf.FloorToInt((float)owned / costPerOp) : 0;

        _tradeCount = Mathf.Clamp(_tradeCount, 1, Mathf.Max(1, maxCount));

        if (countText != null) countText.text = _tradeCount.ToString();

        tradeButton.interactable  = maxCount > 0;
        plusButton.interactable   = _tradeCount < maxCount;
        minusButton.interactable  = _tradeCount > 1;
    }

    // ─── Count Buttons ───────────────────────────────────────────────────────

    private void OnPlus()
    {
        _tradeCount++;
        RenderActionArea();
    }

    private void OnMinus()
    {
        _tradeCount = Mathf.Max(1, _tradeCount - 1);
        RenderActionArea();
    }

    // ─── Trade Execution ─────────────────────────────────────────────────────

    private void ExecuteTrade()
    {
        if (DataManager.instance == null) return;

        TradeRecipe active = ActiveRecipe();
        if (active == null) return;

        int costPerOp  = CostPerOperation(active);
        int totalCost  = costPerOp * _tradeCount;

        if (!DataManager.instance.HasInventory(_selectedItem.itemId, totalCost))
        {
            if (statusText != null) statusText.text = "재료가 부족합니다.";
            return;
        }

        // Consume source item
        DataManager.instance.UseInventory(_selectedItem.itemId, totalCost);

        // Grant result item
        string rewardId;
        int    rewardPerOp;

        if (_activeMode == TradeMode.Compound)
        {
            rewardId    = active.rewardItem.itemId;
            rewardPerOp = active.rewardAmount;
        }
        else // Disassemble — recipe reversed
        {
            rewardId    = active.costItem.itemId;
            rewardPerOp = active.costAmount;
        }

        int totalReward = rewardPerOp * _tradeCount;
        DataManager.instance.AddInventory(rewardId, totalReward);

        string rewardName = _activeMode == TradeMode.Compound
            ? active.rewardItem.itemName
            : active.costItem.itemName;

        if (statusText != null)
            statusText.text = $"{rewardName} x{totalReward} 획득!";

        Debug.Log($"[AlchemistUI] {_activeMode}: {_selectedItem.itemName} x{totalCost} → {rewardName} x{totalReward}");

        PostTradeRefresh();
    }

    // ─── Post-Trade ──────────────────────────────────────────────────────────

    private void PostTradeRefresh()
    {
        _tradeCount = 1;

        RefreshInventorySlots();

        // Deselect if source material is fully consumed
        if (_selectedItem != null &&
            DataManager.instance.GetInventoryCount(_selectedItem.itemId) <= 0)
        {
            _selectedItem = null;
            _nextRecipe   = null;
            _prevRecipe   = null;
            _activeMode   = TradeMode.None;
            RenderCenter();
            RenderModeButtons();
        }

        RenderActionArea();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>Returns the recipe for the currently active mode, or null.</summary>
    private TradeRecipe ActiveRecipe()
    {
        return _activeMode switch
        {
            TradeMode.Compound    => _nextRecipe,
            TradeMode.Disassemble => _prevRecipe,
            _                     => null,
        };
    }

    /// <summary>
    /// How many of _selectedItem are consumed per single trade operation.
    /// Compound:    costAmount of selectedItem
    /// Disassemble: rewardAmount of selectedItem (recipe reversal)
    /// </summary>
    private int CostPerOperation(TradeRecipe recipe)
    {
        return _activeMode == TradeMode.Compound
            ? recipe.costAmount
            : Mathf.Max(1, recipe.rewardAmount);
    }
}
