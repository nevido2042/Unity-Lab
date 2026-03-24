using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 정적 배칭(Static Batching) 효율 검증을 위해 큐브 그리드를 생성하는 스크립트
/// </summary>
public class StaticBatchingTest : MonoBehaviour
{
    [Header("그리드 설정")]
    [Tooltip("그리드의 가로, 세로, 높이 개수")]
    public Vector3Int gridDimensions = new Vector3Int(10, 1, 10);
    [Tooltip("큐브 사이의 간격")]
    public float spacing = 1.1f;
    [Tooltip("생성할 큐브 프리팹 (비어있으면 기본 큐브 생성)")]
    public GameObject cubePrefab;

    [Header("생성 설정")]
    [Tooltip("생성된 오브젝트를 Batching Static으로 설정할지 여부")]
    public bool setStatic = true;

    /// <summary>
    /// 인스펙터 컨텍스트 메뉴에서 "Generate Grid"를 클릭하여 실행 가능
    /// </summary>
    [ContextMenu("Generate Grid")]
    public void GenerateGrid()
    {
        // 기존에 생성된 자식 오브젝트들을 모두 제거
        ClearGrid();

        for (int x = 0; x < gridDimensions.x; x++)
        {
            for (int y = 0; y < gridDimensions.y; y++)
            {
                for (int z = 0; z < gridDimensions.z; z++)
                {
                    GameObject cube;
                    if (cubePrefab != null)
                    {
                        // 프리팹이 설정되어 있으면 복제
                        cube = Instantiate(cubePrefab, transform);
                    }
                    else
                    {
                        // 프리팹이 없으면 기본 Cube 생성
                        cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        cube.transform.SetParent(transform);
                    }

                    // 위치 설정
                    cube.transform.localPosition = new Vector3(x * spacing, y * spacing, z * spacing);
                    cube.name = $"Cube_{x}_{y}_{z}";

                    if (setStatic)
                    {
#if UNITY_EDITOR
                        // 에디터에서 Batching Static 플래그 설정
                        GameObjectUtility.SetStaticEditorFlags(cube, StaticEditorFlags.BatchingStatic);
#else
                        // 런타임에서 IsStatic 설정 (정적 배칭에는 런타임 설정이 바로 적용되지 않을 수 있음)
                        cube.isStatic = true;
#endif
                    }
                }
            }
        }
        
        Debug.Log($"{gridDimensions.x * gridDimensions.y * gridDimensions.z}개의 큐브를 생성했습니다.");
    }

    /// <summary>
    /// 생성된 모든 큐브를 제거합니다.
    /// </summary>
    [ContextMenu("Clear Grid")]
    public void ClearGrid()
    {
        // 에디터와 재생 모드 모두 안전하게 삭제하기 위해 뒤에서부터 순회
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }
}
