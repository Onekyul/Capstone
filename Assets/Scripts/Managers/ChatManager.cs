using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json; 

public class ChatManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField inputField;
    public Transform chatContent;
    public GameObject messagePrefab;
    public ScrollRect scrollRect;
    
    private string baseUrl = "http://localhost:7200/api/chat"; 
    
    private bool isPolling = false;
    public static bool IsChatting = false;
    void Start()
    {
        // 닉네임 임시 생성
        //myNickname = "Player_" + Random.Range(1000, 9999); 
        CloseChatInput(); 
        inputField.onSubmit.AddListener(SendChatMessage);
        StartCoroutine(PollingRoutine()); 
    }

    void Update()
    {
        // 엔터
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (!inputField.gameObject.activeSelf)
            {
                OpenChatInput();
            }
        }

        // ESC
        if (Input.GetKeyDown(KeyCode.Escape) && inputField.gameObject.activeSelf)
            if (Input.GetKeyDown(KeyCode.Escape) && inputField.gameObject.activeSelf)
            {
                CloseChatInput();
            }
    }
    
    void SendChatMessage(string msg)
    {
        if (!string.IsNullOrWhiteSpace(msg))
        {
            StartCoroutine(SendMessageCoroutine(msg));
        }
        
        // 메시지 보낸 후 입력창 닫기 (계속 입력하게 하려면 이 줄 주석 처리)
        CloseChatInput(); 
    }
    IEnumerator PollingRoutine()
    {
        isPolling = true;
        while (isPolling)
        {
            using (UnityWebRequest req = UnityWebRequest.Get(baseUrl + "/receive"))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    UpdateChatUI(req.downloadHandler.text);
                }
            }
            yield return new WaitForSeconds(1.0f);
        }
    }

    IEnumerator SendMessageCoroutine(string msg)
    {
        string nickname = "Guest";
        
        if (SessionManager.Instance != null && !string.IsNullOrEmpty(SessionManager.Instance.Nickname))
        {
            nickname = SessionManager.Instance.Nickname;
        }

        var data = new { Nickname = nickname, Message = msg };
        string json = JsonConvert.SerializeObject(data);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest req = new UnityWebRequest(baseUrl + "/send", "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
        }
    }

    // --- 2. UI 로직 ---
    void UpdateChatUI(string jsonArray)
    {
        var messages = JsonConvert.DeserializeObject<List<string>>(jsonArray);
        if (messages == null || messages.Count == 0) return;

        // 기존 메시지 삭제
        foreach (Transform child in chatContent) Destroy(child.gameObject);

        // 새 메시지 생성
        foreach (string msg in messages)
        {
            GameObject newMsg = Instantiate(messagePrefab, chatContent);

            // ★ [핵심] 스케일과 위치를 강제로 1, 1, 1로 맞춤 (이게 원인일 확률 99%)
            newMsg.transform.localScale = Vector3.one; 
        
            // Z축이 튀면 카메라 뒤로 숨을 수 있으니 0으로 고정
            Vector3 pos = newMsg.transform.localPosition;
            newMsg.transform.localPosition = new Vector3(pos.x, pos.y, 0);

            // 텍스트 컴포넌트 찾기
            var textComp = newMsg.GetComponent<TextMeshProUGUI>();
            if (textComp == null) textComp = newMsg.GetComponentInChildren<TextMeshProUGUI>();
        
            if (textComp != null)
            {
                textComp.text = msg;
                textComp.color = Color.white; // 혹시 글자색이 투명/검정일까봐 흰색 강제
            }
            else
            {
                Debug.LogError(" 프리팹에 TextMeshProUGUI 컴포넌트가 없습니다!");
            }
        }
        StartCoroutine(AutoScroll());
    }

    void OpenChatInput() { IsChatting = true; inputField.gameObject.SetActive(true); inputField.text = ""; inputField.ActivateInputField(); inputField.Select(); }
    void CloseChatInput() {IsChatting = false; inputField.text = ""; inputField.DeactivateInputField(); inputField.gameObject.SetActive(false); EventSystem.current.SetSelectedGameObject(null); }
    IEnumerator AutoScroll() { yield return new WaitForEndOfFrame(); scrollRect.verticalNormalizedPosition = 0f; }
}