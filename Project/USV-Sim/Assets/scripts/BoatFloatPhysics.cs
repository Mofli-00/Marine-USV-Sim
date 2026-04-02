using UnityEngine;

public class BoatFloatPhysics : MonoBehaviour
{
    [Header("浮力点")]
    public Transform[] floatPoints;

    [Header("浮力参数")]
    public float floatStrength = 10f;
    public float waterDrag = 0.98f;

    [Header("水面高度提供者")]
    public WaveHeightProvider waveProvider;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        foreach (Transform point in floatPoints)
        {
            float waterHeight = waveProvider.GetWaveHeight(point.position.x, point.position.z);
            float depth = waterHeight - point.position.y;
            if (depth > 0)
            {
                float force = depth * floatStrength * rb.mass;
                rb.AddForceAtPosition(Vector3.up * force, point.position);
            }
        }

        // 使用正确的属性名 velocity 和 angularVelocity
        rb.velocity *= waterDrag;
        rb.angularVelocity *= waterDrag;
    }
}