using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// GPU Instancing 효율을 검증하기 위한 스크립트.
/// StaticBatchingTest와 설정을 맞추어 비교하기 쉽도록 구성되었습니다.
/// </summary>
public class GPUInstancingTest : MonoBehaviour
{
    [Header("렌더링 설정")]
    [Tooltip("그릴 메시 (예: 기본 Cube 메시)")]
    public Mesh mesh;
    [Tooltip("GPU Instancing이 활성화된 머티리얼")]
    public Material material;

    [Header("그리드 설정")]
    [Tooltip("그리드의 가로, 세로, 높이 개수")]
    public Vector3Int gridDimensions = new Vector3Int(10, 1, 10);
    [Tooltip("인스턴스 간격")]
    public float spacing = 1.1f;

    // DrawMeshInstanced는 한 번에 최대 1023개까지만 그릴 수 있습니다.
    private const int MAX_INSTANCES_PER_BATCH = 1023;
    private List<Matrix4x4[]> batches = new List<Matrix4x4[]>();

    void Start()
    {
        // 머티리얼 인스턴싱 강제 활성화
        if (material != null) material.enableInstancing = true;

        InitializeBatches();
    }

    /// <summary>
    /// 지정된 그리드 크기만큼 위치 행렬을 계산하여 배치(Batch) 단위로 나눕니다.
    /// </summary>
    [ContextMenu("Initialize Batches")]
    public void InitializeBatches()
    {
        batches.Clear();
        
        int totalCount = gridDimensions.x * gridDimensions.y * gridDimensions.z;
        if (totalCount <= 0) return;

        int fullBatches = totalCount / MAX_INSTANCES_PER_BATCH;
        int remaining = totalCount % MAX_INSTANCES_PER_BATCH;

        for (int i = 0; i < fullBatches; i++) batches.Add(new Matrix4x4[MAX_INSTANCES_PER_BATCH]);
        if (remaining > 0) batches.Add(new Matrix4x4[remaining]);

        int count = 0;
        for (int x = 0; x < gridDimensions.x; x++)
        {
            for (int y = 0; y < gridDimensions.y; y++)
            {
                for (int z = 0; z < gridDimensions.z; z++)
                {
                    int batchIndex = count / MAX_INSTANCES_PER_BATCH;
                    int innerIndex = count % MAX_INSTANCES_PER_BATCH;

                    Vector3 position = new Vector3(x * spacing, y * spacing, z * spacing);
                    batches[batchIndex][innerIndex] = Matrix4x4.TRS(
                        transform.position + position, 
                        Quaternion.identity, 
                        Vector3.one
                    );
                    count++;
                }
            }
        }

        Debug.Log($"총 {totalCount}개의 인스턴스를 {batches.Count}개의 배치로 준비했습니다.");
    }

    void Update()
    {
        if (mesh == null || material == null) return;

        // 그림자 설정 및 기타 옵션을 맞춥니다.
        foreach (var batch in batches)
        {
            Graphics.DrawMeshInstanced(
                mesh, 
                0, 
                material, 
                batch, 
                batch.Length, 
                null, 
                UnityEngine.Rendering.ShadowCastingMode.Off, // 그림자 끔
                false // 그림자 받지 않음
            );
        }
    }
}
