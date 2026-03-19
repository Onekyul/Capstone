using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 연금술사 재료 교환 UI.
/// TradeTableData SO에서 레시피 읽고, 로컬 인벤토리 조작 후 SaveGame으로 서버 동기화.
/// (기존 강화/인챈트와 동일 패턴)
/// </summary>
public class AlchemistUI : MonoBehaviour
{
    [Header("레시피 목록")]
    [SerializeField] private Transform recipeListParent;    // ScrollView > Content
    [SerializeField] private GameObject recipeEntryPrefab;  // 교환 항목 프리팹

    [Header("연금술사 참조")]
    [SerializeField] private Alchemist alchemist;

    [Header("상태 표시")]
    [SerializeField] private TextMeshProUGUI statusText;

    private List<GameObject> _spawnedEntries = new List<GameObject>();

    void OnEnable()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        foreach (var entry in _spawnedEntries)
            Destroy(entry);
        _spawnedEntries.Clear();

        if (alchemist == null || alchemist.TradeTable == null) return;
        if (DataManager.instance == null) return;

        foreach (var recipe in alchemist.TradeTable.recipes)
        {
            GameObject go = Instantiate(recipeEntryPrefab, recipeListParent);
            _spawnedEntries.Add(go);
            SetupRecipeEntry(go, recipe);
        }

        if (statusText != null) statusText.text = "";
    }

    private void SetupRecipeEntry(GameObject go, TradeRecipe recipe)
    {
        Image[] images = go.GetComponentsInChildren<Image>();
        TMP_Text[] texts = go.GetComponentsInChildren<TMP_Text>();
        Button tradeButton = go.GetComponentInChildren<Button>();

        // 아이콘 설정 (images[0]은 배경이므로 1, 2 사용)
        if (images.Length >= 3)
        {
            if (recipe.costItem != null) images[1].sprite = recipe.costItem.icon;
            if (recipe.rewardItem != null) images[2].sprite = recipe.rewardItem.icon;
        }

        // 텍스트 설정
        if (texts.Length >= 3)
        {
            texts[0].text = recipe.recipeName;
            int owned = DataManager.instance.GetInventoryCount(recipe.costItem.itemId);
            texts[1].text = $"{owned}/{recipe.costAmount}";
            texts[2].text = $"x{recipe.rewardAmount}";
        }

        bool canTrade = recipe.costItem != null && recipe.rewardItem != null &&
                        DataManager.instance.HasInventory(recipe.costItem.itemId, recipe.costAmount);

        if (tradeButton != null)
        {
            tradeButton.interactable = canTrade;
            tradeButton.onClick.RemoveAllListeners();
            tradeButton.onClick.AddListener(() => ExecuteTrade(recipe));
        }
    }

    private void ExecuteTrade(TradeRecipe recipe)
    {
        if (DataManager.instance == null) return;
        if (!DataManager.instance.HasInventory(recipe.costItem.itemId, recipe.costAmount))
        {
            if (statusText != null) statusText.text = "재료가 부족합니다.";
            return;
        }

        // 로컬 인벤토리 조작
        DataManager.instance.UseInventory(recipe.costItem.itemId, recipe.costAmount);
        DataManager.instance.AddInventory(recipe.rewardItem.itemId, recipe.rewardAmount);

        if (statusText != null)
            statusText.text = $"{recipe.rewardItem.itemName} x{recipe.rewardAmount} 획득!";

        Debug.Log($"[연금술사] 교환 완료: {recipe.costItem.itemName} x{recipe.costAmount} → {recipe.rewardItem.itemName} x{recipe.rewardAmount}");

        // SaveGame은 UseInventory/AddInventory 내부에서 이미 호출됨
        RefreshUI();
    }
}
