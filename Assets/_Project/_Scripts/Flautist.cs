using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flautist : MonoBehaviour
{
    public Rigidbody rb;
	public float speed;
	
    void Start()
    {
        
    }

    void FixedUpdate()
    {
		float dt = Time.deltaTime;
		
		Transform camTransform = Camera.main.transform.parent;
		
        float h = Input.GetAxis("Horizontal");
		float v = Input.GetAxis("Vertical");
		

		Vector3 velocity = (v * camTransform.forward + h * camTransform.right);
		velocity.y = rb.velocity.y;

		rb.velocity = velocity * speed * dt;
    }
}
