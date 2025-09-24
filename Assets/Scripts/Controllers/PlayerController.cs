using UnityEngine;

public class PlayerController : MonoBehaviour
{
    
    [SerializeField]
    private float speed=5f;
    private Rigidbody2D rb;
    private Vector2 movementInput;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
            movementInput = InputManager.instance.playerMoveInput;
        
    }

    void FixedUpdate()
    {
        rb.linearVelocity = movementInput * speed;
    }
}
