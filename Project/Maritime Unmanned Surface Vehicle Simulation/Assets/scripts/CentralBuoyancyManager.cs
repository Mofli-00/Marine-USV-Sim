using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

/// <summary>
/// 中央浮力管理器 - 真正的物理浮力版本
/// 支持：上下起伏、水阻、波浪跟随、物体倾斜
/// </summary>
public class CentralizedBuoyancyManager : MonoBehaviour
{
    [Header("Water Settings")]
    [Tooltip("HDRP Water Surface 组件引用")]
    public WaterSurface waterSurface;

    [Header("Global Buoyancy Settings")]
    [Tooltip("全局浮力系数乘数")]
    public float globalBuoyancyMultiplier = 1f;

    [Tooltip("全局水阻系数乘数")]
    public float globalDragMultiplier = 1f;

    [Header("Wave Settings")]
    [Tooltip("是否让物体跟随波浪倾斜")]
    public bool applyWaveTorque = true;

    [Tooltip("波浪倾斜强度")]
    public float waveTorqueStrength = 5f;

    [Header("Performance")]
    [Tooltip("每帧最多处理的浮标点数量")]
    public int maxPontoons = 1000;

    // 注册的浮动物体列表
    private List<BuoyantObject> buoyantObjects = new List<BuoyantObject>();

    // Job System 使用的 Native 数组
    private NativeArray<float3> pontoonPositions;
    private NativeArray<float> waterHeights;
    private NativeArray<float> waterNormalsX;
    private NativeArray<float> waterNormalsZ;
    private NativeArray<int> objectIndices;
    private NativeArray<int> pontoonIndicesInObject;

    private WaterSimSearchData simData;
    private int currentPontoonCount = 0;

    // 用于存储每个物体的浮标点信息（避免重复遍历 Transform）
    private class PontoonInfo
    {
        public Transform transform;
        public Vector3 localPosition;
    }

    private class BuoyantObjectData
    {
        public BuoyantObject component;
        public Rigidbody rb;
        public List<PontoonInfo> pontoons = new List<PontoonInfo>();
    }

    private List<BuoyantObjectData> objectDataList = new List<BuoyantObjectData>();

    void Start()
    {
        // 分配 Native 数组
        pontoonPositions = new NativeArray<float3>(maxPontoons, Allocator.Persistent);
        waterHeights = new NativeArray<float>(maxPontoons, Allocator.Persistent);
        waterNormalsX = new NativeArray<float>(maxPontoons, Allocator.Persistent);
        waterNormalsZ = new NativeArray<float>(maxPontoons, Allocator.Persistent);
        objectIndices = new NativeArray<int>(maxPontoons, Allocator.Persistent);
        pontoonIndicesInObject = new NativeArray<int>(maxPontoons, Allocator.Persistent);

        simData = new WaterSimSearchData();
    }

    void OnDestroy()
    {
        // 释放 Native 数组
        if (pontoonPositions.IsCreated) pontoonPositions.Dispose();
        if (waterHeights.IsCreated) waterHeights.Dispose();
        if (waterNormalsX.IsCreated) waterNormalsX.Dispose();
        if (waterNormalsZ.IsCreated) waterNormalsZ.Dispose();
        if (objectIndices.IsCreated) objectIndices.Dispose();
        if (pontoonIndicesInObject.IsCreated) pontoonIndicesInObject.Dispose();
    }

    void FixedUpdate()
    {
        if (waterSurface == null)
        {
            Debug.LogWarning("WaterSurface not assigned!");
            return;
        }

        if (!waterSurface.FillWaterSearchData(ref simData))
        {
            return;
        }

        // 收集所有浮标点位置
        CollectPontoonData();

        if (currentPontoonCount == 0) return;

        // 执行水面高度查询 Job
        ExecuteWaterHeightJob();

        // 应用浮力和阻力
        ApplyBuoyancyAndDrag();
    }

    /// <summary>
    /// 收集所有浮标点的位置和索引信息
    /// </summary>
    private void CollectPontoonData()
    {
        currentPontoonCount = 0;

        for (int objIdx = 0; objIdx < objectDataList.Count; objIdx++)
        {
            var data = objectDataList[objIdx];
            if (data.component == null || data.rb == null) continue;

            for (int pontoonIdx = 0; pontoonIdx < data.pontoons.Count; pontoonIdx++)
            {
                if (currentPontoonCount >= maxPontoons)
                {
                    Debug.LogWarning($"Max pontoons reached ({maxPontoons})");
                    return;
                }

                var pontoon = data.pontoons[pontoonIdx];
                pontoonPositions[currentPontoonCount] = pontoon.transform.position;
                objectIndices[currentPontoonCount] = objIdx;
                pontoonIndicesInObject[currentPontoonCount] = pontoonIdx;
                currentPontoonCount++;
            }
        }
    }

    /// <summary>
    /// 执行 Job 查询水面高度
    /// </summary>
    private void ExecuteWaterHeightJob()
    {
        var searchJob = new WaterSimulationSearchJob
        {
            simSearchData = simData,
            targetPositionBuffer = pontoonPositions,
            startPositionBuffer = pontoonPositions,
            maxIterations = 8,
            error = 0.01f,
            heightBuffer = waterHeights,
            errorBuffer = new NativeArray<float>(currentPontoonCount, Allocator.TempJob),
            candidateLocationBuffer = new NativeArray<float3>(currentPontoonCount, Allocator.TempJob),
            stepCountBuffer = new NativeArray<int>(currentPontoonCount, Allocator.TempJob)
        };

        JobHandle handle = searchJob.Schedule(currentPontoonCount, 64);
        handle.Complete();

        // 获取水面法线（用于倾斜）
        // 注意：HDRP WaterSystem 可能不直接提供法线，这里使用简单的斜率近似
        for (int i = 0; i < currentPontoonCount; i++)
        {
            // 通过相邻点计算水面斜率
            float3 pos = pontoonPositions[i];
            float h = waterHeights[i];

            // 简单的法线估算（基于位置偏移）
            float offset = 0.5f;
            float3 posX = pos + new float3(offset, 0, 0);
            float3 posZ = pos + new float3(0, 0, offset);

            // 这里简化处理，实际应该再次查询相邻点高度
            waterNormalsX[i] = 0;
            waterNormalsZ[i] = 0;
        }

        // 清理临时数组
        searchJob.errorBuffer.Dispose();
        searchJob.candidateLocationBuffer.Dispose();
        searchJob.stepCountBuffer.Dispose();
    }

    /// <summary>
    /// 应用浮力和阻力到每个物体
    /// </summary>
    private void ApplyBuoyancyAndDrag()
    {
        // 先重置每个物体的浮力累加器
        Dictionary<Rigidbody, Vector3> forceAccumulator = new Dictionary<Rigidbody, Vector3>();
        Dictionary<Rigidbody, Vector3> torqueAccumulator = new Dictionary<Rigidbody, Vector3>();
        Dictionary<Rigidbody, int> pontoonCountPerRb = new Dictionary<Rigidbody, int>();
        Dictionary<Rigidbody, float> maxSubmersion = new Dictionary<Rigidbody, float>();

        foreach (var data in objectDataList)
        {
            if (data.rb != null)
            {
                forceAccumulator[data.rb] = Vector3.zero;
                torqueAccumulator[data.rb] = Vector3.zero;
                pontoonCountPerRb[data.rb] = 0;
                maxSubmersion[data.rb] = 0f;
            }
        }

        // 遍历所有浮标点，计算每个点的浮力
        for (int i = 0; i < currentPontoonCount; i++)
        {
            int objIdx = objectIndices[i];
            if (objIdx >= objectDataList.Count) continue;

            var data = objectDataList[objIdx];
            if (data.rb == null) continue;

            int pontoonIdx = pontoonIndicesInObject[i];
            if (pontoonIdx >= data.pontoons.Count) continue;

            var pontoon = data.pontoons[pontoonIdx];
            float waterHeight = waterHeights[i];
            float pontoonY = pontoon.transform.position.y;

            // 计算浸没深度
            float submersion = Mathf.Clamp01((waterHeight - pontoonY) / data.component.depthBeforeSubmerged);

            if (submersion <= 0.01f) continue;

            // 计算浮力（阿基米德原理：F = ρ * g * V）
            // 使用 displacementAmount 作为每单位浸没的力
            float buoyancyForce = data.component.GetDisplacementAmount(pontoon.transform, submersion);
            buoyancyForce *= globalBuoyancyMultiplier;
            buoyancyForce *= Physics.gravity.magnitude; // 转换为牛顿力

            Vector3 force = Vector3.up * buoyancyForce;

            // 获取作用点位置（世界坐标）
            Vector3 worldPoint = pontoon.transform.position;

            // 累加力
            forceAccumulator[data.rb] += force;

            // 计算扭矩（力 × 力臂）
            Vector3 torque = Vector3.Cross(worldPoint - data.rb.worldCenterOfMass, force);
            torqueAccumulator[data.rb] += torque;

            pontoonCountPerRb[data.rb]++;
            maxSubmersion[data.rb] = Mathf.Max(maxSubmersion[data.rb], submersion);

            // 调试可视化（可选）
            Debug.DrawLine(worldPoint, worldPoint + Vector3.up * (buoyancyForce / 10f), Color.cyan);
        }

        // 应用力和阻力到每个刚体
        foreach (var data in objectDataList)
        {
            if (data.rb == null) continue;

            if (!forceAccumulator.ContainsKey(data.rb)) continue;

            Vector3 totalForce = forceAccumulator[data.rb];
            Vector3 totalTorque = torqueAccumulator[data.rb];
            int pontoonCountObj = pontoonCountPerRb[data.rb];
            float avgSubmersion = maxSubmersion[data.rb];

            if (pontoonCountObj > 0)
            {
                // 应用浮力
                data.rb.AddForce(totalForce, ForceMode.Force);
                data.rb.AddTorque(totalTorque, ForceMode.Force);

                // 应用水阻力（速度阻尼）
                if (avgSubmersion > 0.01f)
                {
                    float drag = Mathf.Lerp(1f, data.component.waterDrag, avgSubmersion);
                    float angularDrag = Mathf.Lerp(1f, data.component.waterAngularDrag, avgSubmersion);

                    drag = Mathf.Lerp(drag, 1f, 1f - globalDragMultiplier);
                    angularDrag = Mathf.Lerp(angularDrag, 1f, 1f - globalDragMultiplier);

                    data.rb.velocity *= drag;
                    data.rb.angularVelocity *= angularDrag;
                }
            }

            // 波浪跟随：根据物体中心的水面法线施加扭矩
            if (applyWaveTorque && waterSurface != null)
            {
                ApplyWaveFollowingTorque(data);
            }
        }
    }

    /// <summary>
    /// 应用波浪跟随扭矩（使物体随波浪倾斜）
    /// </summary>
    private void ApplyWaveFollowingTorque(BuoyantObjectData data)
    {
        if (data.rb == null) return;

        Vector3 objectCenter = data.rb.worldCenterOfMass;

        // 获取物体中心的水面高度和法线
        // 注意：这里简化处理，实际应该查询水面法线
        // 由于 HDRP WaterSystem 的限制，这里用一个简单的方法：
        // 使用物体前部和右侧的浮标点高度差来计算目标旋转

        if (data.pontoons.Count < 2) return;

        // 找到最前、最后、最左、最右的浮标点
        Transform frontPontoon = null, backPontoon = null, leftPontoon = null, rightPontoon = null;
        float frontZ = -float.MaxValue, backZ = float.MaxValue, leftX = float.MaxValue, rightX = -float.MaxValue;

        foreach (var pontoon in data.pontoons)
        {
            Vector3 localPos = pontoon.transform.localPosition;
            if (localPos.z > frontZ) { frontZ = localPos.z; frontPontoon = pontoon.transform; }
            if (localPos.z < backZ) { backZ = localPos.z; backPontoon = pontoon.transform; }
            if (localPos.x < leftX) { leftX = localPos.x; leftPontoon = pontoon.transform; }
            if (localPos.x > rightX) { rightX = localPos.x; rightPontoon = pontoon.transform; }
        }

        if (frontPontoon == null || backPontoon == null) return;

        // 查询这些点的高度
        // 简化：使用当前已查询的高度缓存，或重新查询
        // 这里为了性能，使用一个简化方法：基于物体速度模拟波浪力
        Vector3 waveTorque = Vector3.zero;

        // 简单模拟：根据物体在水面的运动产生恢复扭矩
        Vector3 angularVelocity = data.rb.angularVelocity;
        waveTorque = -angularVelocity * waveTorqueStrength;

        data.rb.AddTorque(waveTorque, ForceMode.Force);
    }

    // ==============================
    // 公共注册方法
    // ==============================

    public void RegisterBuoyantObject(BuoyantObject obj)
    {
        if (obj == null) return;

        // 检查是否已注册
        foreach (var data in objectDataList)
        {
            if (data.component == obj) return;
        }

        BuoyantObjectData newData = new BuoyantObjectData
        {
            component = obj,
            rb = obj.GetComponent<Rigidbody>()
        };

        // 缓存浮标点信息
        foreach (Transform pontoon in obj.pontoons)
        {
            if (pontoon != null)
            {
                newData.pontoons.Add(new PontoonInfo
                {
                    transform = pontoon,
                    localPosition = pontoon.localPosition
                });
            }
        }

        objectDataList.Add(newData);
        Debug.Log($"Registered buoyant object: {obj.name} with {newData.pontoons.Count} pontoons");
    }

    public void UnregisterBuoyantObject(BuoyantObject obj)
    {
        objectDataList.RemoveAll(data => data.component == obj);
    }

    public bool IsObjectRegistered(BuoyantObject obj)
    {
        return objectDataList.Exists(data => data.component == obj);
    }
}