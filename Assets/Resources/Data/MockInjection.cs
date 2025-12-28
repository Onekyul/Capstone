using UnityEngine;
using System.Collections.Generic;
public class MockInjection : MonoBehaviour
{
    void Start()
    {
        // 1. DataManager가 없으면 중단
        if (DataManager.instance == null) return;

        // 2. currentPlayer가 null이면(LoadGame을 안 했으니) 새로 생성
        if (DataManager.instance.currentPlayer == null)
        {
            DataManager.instance.currentPlayer = new PlayerData();
            DataManager.instance.currentPlayer.Inventory = new List<InventorySlot>();
        }

        Debug.Log("[Test] 모크 데이터 주입 시작...");
        
        if (DataManager.instance.currentPlayer.Inventory != null)
        {
            DataManager.instance.currentPlayer.Inventory.Clear(); // 리스트 내용물 싹 비움
        }
        else
        {
            // 만약 리스트 자체가 null이면 새로 만듦
            DataManager.instance.currentPlayer.Inventory = new List<InventorySlot>();
        }
        // 3. 원하는 아이템 강제 주입
        // (DataManager의 AddInventory 함수를 쓰면 저장까지 되므로, 테스트면 리스트에 직접 넣는 게 나을 수도 있음)
        
        // 재료 넣기
        DataManager.instance.AddInventory("mat_firestone", 50);      // 화염의 돌
        DataManager.instance.AddInventory("ent_lightning", 50); // 번개의 돌
        DataManager.instance.AddInventory("mat_wood", 100);         // 나무

        // 4. (중요) 데이터가 들어갔으니 UI를 강제로 새로고침
        if (WizardUI.instance != null)
        {
            // 현재 보고 있는 탭의 내용을 다시 그리기
            WizardUI.instance.UpdateUI();
        }
    }
}
