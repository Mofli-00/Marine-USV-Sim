using UnityEngine;

public class BuoyantObject : MonoBehaviour
{
    [Header("Buoyancy Points")]
    public Transform[] pontoons;

    [Header("Buoyancy Parameters (per pontoon)")]
    public float depthBeforeSubmerged = 1f;
    public float displacementAmount = 3f;

    [Header("Water Drag")]
    public float waterDrag = 0.99f;
    public float waterAngularDrag = 0.5f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("BuoyantObject must have a Rigidbody component.");
            return;
        }

        // 如果没有指定浮标点，使用自身位置
        if (pontoons == null || pontoons.Length == 0)
        {
            pontoons = new Transform[1] { transform };
        }

        // 注册到中央管理器
        CentralizedBuoyancyManager manager = FindObjectOfType<CentralizedBuoyancyManager>();
        if (manager != null)
        {
            manager.RegisterBuoyantObject(this);
        }
        else
        {
            Debug.LogError("No CentralizedBuoyancyManager found in scene!");
        }
    }

    void OnDestroy()
    {
        CentralizedBuoyancyManager manager = FindObjectOfType<CentralizedBuoyancyManager>();
        if (manager != null)
        {
            manager.UnregisterBuoyantObject(this);
        }
    }

    // 获取单个浮标点的浮力参数（可被子类重写以实现不同区域不同浮力）
    public virtual float GetDisplacementAmount(Transform pontoon, float submersion)
    {
        return displacementAmount * submersion;
    }
}