using System;
using UnityEngine;

public class NPCController : MonoBehaviour
{
    public GameObject interactionPrompt;

    protected bool isPlayerInRange = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }
    
    protected virtual void OnEnable()
    {
        if (InputManager.instance != null)
        {
            InputManager.instance.OnInteractPressed += HandleInteraction;
        }
    }
    
    protected virtual void OnDisable()
    {
        if (InputManager.instance != null)
        {
            InputManager.instance.OnInteractPressed -= HandleInteraction;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
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
        if (other.tag == "Player")
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
            isPlayerInRange=false;
        }
    }

    // Update is called once per frame
    void Update()
    {
     
    }

    private void HandleInteraction()
    {
        if (isPlayerInRange)
        {
            Interact();
        }
    }

    public virtual void Interact()
    {
        Debug.Log("Interact");
    }
}
