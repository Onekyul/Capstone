using System;
using UnityEngine;
using Fusion; // Fusion 네임스페이스 추가

public class NPCController : MonoBehaviour
{
    public GameObject interactionPrompt;
    protected bool isPlayerInRange = false;

    protected virtual void Start()
    {
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }
    
    protected virtual void OnEnable()
    {
        // InputManager가 싱글톤이므로 여기서 이벤트를 구독하는 방식은 유지하되,
        // 나중에 InputManager에서 ChatManager.IsChatting 체크가 잘 되는지 확인해야 합니다.
        if (InputManager.instance != null)
            InputManager.instance.OnInteractPressed += HandleInteraction;
    }
    
    protected virtual void OnDisable()
    {
        if (InputManager.instance != null)
            InputManager.instance.OnInteractPressed -= HandleInteraction;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // ✨ [핵심 수정] 충돌한 플레이어가 '내' 캐릭터인지 확인합니다.
            // NetworkObject를 찾아서 나에게 입력 권한(InputAuthority)이 있는지 체크합니다.
            NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
            
            if (netObj != null && netObj.HasInputAuthority)
            {
                if (interactionPrompt != null) interactionPrompt.SetActive(true);
                isPlayerInRange = true;
                Debug.Log($"[NPC] 로컬 플레이어 감지됨: {other.name}");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 나갈 때도 내 캐릭터였는지 확인 (아니면 무시)
            NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
            
            if (netObj != null && netObj.HasInputAuthority)
            {
                if (interactionPrompt != null) interactionPrompt.SetActive(false);
                isPlayerInRange = false;
                
                if (UIManager.instance != null)
                    UIManager.instance.CloseDialoguePanel();
            }
        }
    }

    private void HandleInteraction()
    {
        // 내가 범위 안에 있을 때만 상호작용 실행
        if (isPlayerInRange)
        {
            Interact();
        }
    }

    public virtual void Interact()
    {
        Debug.Log("Interact 실행됨");
    }
}