using UnityEngine;

public class BoatControlSimple : MonoBehaviour
{
    public float maxThrust = 500f;
    public float turnTorque = 300f;

    public KeyCode forwardKey = KeyCode.W;
    public KeyCode backwardKey = KeyCode.S;
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        float thrust = 0f;
        if (Input.GetKey(forwardKey)) thrust = 1f;
        if (Input.GetKey(backwardKey)) thrust = -1f;

        float turn = 0f;
        if (Input.GetKey(leftKey)) turn = -1f;
        if (Input.GetKey(rightKey)) turn = 1f;

        rb.AddForce(-transform.right * thrust * maxThrust);
        rb.AddTorque(Vector3.up * turn * turnTorque);
    }
}