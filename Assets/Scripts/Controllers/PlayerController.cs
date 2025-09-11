using UnityEngine;

public class PlayerController : MonoBehaviour
{
    
    [SerializeField]
    private float speed=5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputY = Input.GetAxisRaw("Vertical");
        Vector3 moveDir = new Vector3(inputX, inputY, 0).normalized;
        transform.position += moveDir * speed * Time.deltaTime;
    }
}
