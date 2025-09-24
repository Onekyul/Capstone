using System;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager instance;
    
    public Vector2 playerMoveInput { get; private set; }

    public event Action OnInteractPressed;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    // Update is called once per frame

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Update()
    {
        playerMoveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        if (Input.GetKeyDown(KeyCode.E))
        {
            OnInteractPressed?.Invoke();
        }
        
    }
}
