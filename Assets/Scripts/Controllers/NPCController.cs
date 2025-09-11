using System;
using UnityEngine;

public class NPCController : MonoBehaviour
{
    public GameObject interactionPrompt;

    protected bool isPlayerRange = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
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
            isPlayerRange = true;
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
            isPlayerRange=false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isPlayerRange && Input.GetKeyDown(KeyCode.E))
        {
            Interact();
        }
    }

    public virtual void Interact()
    {
        Debug.Log("Interact");
    }
}
