using UnityEngine;

public class GameStart : MonoBehaviour
{
    void Start()
    {
        
        DataManager.instance.InitializeNetwork(() => 
        {
            
            Debug.Log(" 로딩 완료");
            
            // 예: 씬 이동
            // UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
            
            // 데이터 잘 들어왔나 확인
            var myData = DataManager.instance.currentPlayer;
            Debug.Log($"[확인] 닉네임: {DataManager.instance.MyUserId}번 유저");
            Debug.Log($"[확인] 가진 무기 수: {myData.ownedWeapons.Count}개");
        });
    }
}
