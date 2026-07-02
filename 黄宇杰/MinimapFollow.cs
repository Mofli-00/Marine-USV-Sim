using UnityEngine;

public class MinimapFollow : MonoBehaviour
{
    private Transform targetBoat;
    private float currentHeight;

    void Start()
    {
        // 自动读取你在 Inspector 面板里拉高后的安全高度（比如 200）
        currentHeight = transform.position.y;
        FindBoat();
    }

    void LateUpdate()
    {
        if (targetBoat != null)
        {
            // 精准追踪 Player 1，同时保持你在面板里设置的高空俯瞰视角
            transform.position = new Vector3(targetBoat.position.x, currentHeight, targetBoat.position.z);
        }
        else if (Camera.main != null)
        {
            // 终极备用雷达：万一连 Player 1 都没找到，跟着主相机飞
            transform.position = new Vector3(Camera.main.transform.position.x, currentHeight, Camera.main.transform.position.z);
            if (Time.frameCount % 60 == 0) FindBoat();
        }
    }

    void FindBoat()
    {
        GameObject boat = GameObject.FindWithTag("Player");
        if (boat != null)
        {
            targetBoat = boat.transform;
            Debug.Log($"[实时小地图] 成功锁定目标: {boat.name}");
        }
    }
}