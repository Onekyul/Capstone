using UnityEngine;
using System.Collections.Generic;

public class MockInjection : MonoBehaviour
{
    void Start()
    {
        if (DataManager.instance == null) return;

        // 데이터 초기화 및 생성
        if (DataManager.instance.currentPlayer == null)
            DataManager.instance.currentPlayer = new PlayerData();

        if (DataManager.instance.currentPlayer.Inventory == null)
        {
            DataManager.instance.currentPlayer.Inventory = new List<InventorySlot>();
        }
        else
        {
            DataManager.instance.ClearALlData();
            DataManager.instance.currentPlayer.Inventory.Clear(); // 기존 비우기
            
        }

        Debug.Log("[Test] 모크 데이터 주입 시작...");

        // ====================================================
        // [중요] ID 체크 필요!
        // ent_lightning은 '인챈트' ID일 확률이 높음. '재료' ID를 넣어야 함.
        // 예: "mat_lightning", "LightningStone" 등 데이터 파일 확인 필수
        // ====================================================
        DataManager.instance.AddInventory("mat_wood", 50);// ID 확인 요망!
        DataManager.instance.AddInventory("mat_icestone", 1); 
        DataManager.instance.AddInventory("mat_firestone", 20);
        
        

        // ----------------------------------------------------
        // ★ 핵심 해결: 인벤토리 UI 강제 새로고침
        // ----------------------------------------------------
        
        // 1. WizardUI 갱신
        if (WizardUI.instance != null)
            WizardUI.instance.UpdateUI();

        // 2. InventoryUI 갱신 (추가된 코드)
        // 씬에 있는 InventoryUI를 찾아서 갱신 함수를 실행
        InventoryUI inventoryUI = FindAnyObjectByType<InventoryUI>();
        if (inventoryUI != null)
        {
            inventoryUI.UpdateAllInventory();
            Debug.Log("[Test] 인벤토리 UI 갱신 완료");
        }
        else
        {
            Debug.LogWarning("[Test] InventoryUI를 찾을 수 없습니다. 씬에 활성화되어 있는지 확인하세요.");
        }
    }
}