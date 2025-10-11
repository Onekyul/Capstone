using System;
using UnityEngine;

public class PortalController : MonoBehaviour
{
    [SerializeField] private GameObject interactionPrompt;
    private bool isPlayerInRange = false;

    private void OnEnable()
    {
        if (InputManager.instance != null)
        {
            InputManager.instance.OnInteractPressed += HandleInteraction;
        }
    }
    private void OnDisable()
    {
        if (InputManager.instance != null)
        {
            InputManager.instance.OnInteractPressed -= HandleInteraction;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
           
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(true);
            }
            isPlayerInRange = true;
        }
        ;
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
            isPlayerInRange=false;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void HandleInteraction()
    {
        if (isPlayerInRange)
        {
            UIManager.instance.OpenDungeonSelectPanel();
        }
    }
}
