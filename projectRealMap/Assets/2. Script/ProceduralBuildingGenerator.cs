using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Rendering;
using Unity.VisualScripting;
using Newtonsoft.Json;
using System.Data;
using UnityEngine.UIElements;
using Unity.Android.Gradle.Manifest;
using System.Collections;
using Unity.Android.Gradle;

public class ProceduralBuildingGenerator : MonoBehaviour
{
    [System.Serializable]
    // 로더에서 값을 만들어 넘겨주기 위해 public으로 선언
    public class BuildingData
    {
        public string type;
        public int floors;
        // 꼭지점 좌표는 x와 z좌표 두개가 이중 리스트로 들어있으니 float리스트 타입의 리스트로 생성
        public List<float[]> vertices;
        // 유니티 mesh가 int[]타입으로 받기에 동일하게 타입 사용
        public int[] triangles;
        
    }

    [System.Serializable]
    public class BuildingStyle
    {
        public string styleName;
        public BuildingType buildingType;
        [Header("옥상 설정")]
        public Material roofMaterial;
        public float roofUVScale = 0.3f;

        [Header("3D 공간 높이 설정")]
        public float baseHeight = 0f;
        public float trimHeight = 0f;

        [Header("트림시트 줄 번호")]
        public int baseRowIndex;
        public int wallRowIndex;
        public int trimRowIndex;

        // 💡 [추가] 이제 창문과 문 프리팹도 스타일 설계도에서 한 번에 관리합니다!
        [Header("조립용 프리팹 에셋")]
        public List<GameObject> doorPrefab;
        public List<GameObject> windowPrefab;
        public List<GameObject> otherPrefab;

        [Header("창문 배치")]
    
        [Tooltip("창문 간격")]
        public float windowSpacing = 3.0f;

        [Tooltip("창문 생성 확률")]
        [Range(0f, 1f)]
        public float windowSpawnChance = 1f;

        [Tooltip("창문 시작할 층")]
        public int startingWindowFloor = 0;

        [Header("난간 설정")]
        public float parapetHeight = 0.5f;
        public int parapetRowIndex = 2;
    }

    [SerializeField] private List<BuildingStyle> buildingStylesList = new List<BuildingStyle>();

    [Header("파일 설정")]
    public string fileName = "1. Assets/preprocessing.json";
    [SerializeField] private JsonPaser jsonPaser;

    [Header("Material & Trim Sheet Settings")]
    [SerializeField] private Material buildingMaterial;

    public enum BuildingType
    {
        Residential, //0 주택
        Apartment, //1 아파트
        Commercial, //2 편의점, 학원, 마트, 숙소 등
        Office, //3 오피스텔, 빌딩
        Public, //4  문화시설, 병원, 학교 등
        Religious, //5 종교
        Industrial, //6 공장, 창고, 축사 등
        Special_Sports, //7 체육관
        Special_Hazard, //8 주유소 등
        Special_Auto //9 자동차 관련
    }

    // 시트 한줄 높이
    private const float ROW_HEIGHT = 512f;
    // 시트 간격
    private const float PADDING = 16f;
    private const float TOTAL_TEX_HEIGHT = 4096;
    private float floorHeight = 4f;   // 한 층의 높이
    private Vector2[] RowVRange = new Vector2[7];
// }
// /*
    public void Start()
    {
        // 트림 시트 uv 계산
        for (int i = 0; i < 7; i++)
        {
            // 총 높이에서 여백과 줄 높이를 빼서 현재 높이 계산
            float vMax = TOTAL_TEX_HEIGHT - ((ROW_HEIGHT * i) + (PADDING * (i + 1)));
            // 현재 높이에서 줄 높이를 빼서 아래 좌표 계산
            float vMin = vMax - ROW_HEIGHT;

            // 좌표를 최대 높이로 나눠 UV에 사용하는 0.0~1.0 사이의 좌표로 변환
            RowVRange[i] = new Vector2(vMin / TOTAL_TEX_HEIGHT, vMax / TOTAL_TEX_HEIGHT);
        }
        // paser코드에서 함수 호출해 json 구조체로 변환
        List<BuildingData> buildingList = jsonPaser.LoadAndParseJson<BuildingData>(fileName);

        // 건물 데이터 확인 코드
        // for (int i = 0; i < 5; i++)
        // {
        //     string debugData = JsonConvert.SerializeObject(buildingList[i]);
        //     Debug.Log(debugData);
        // }

        // 건물 생성 코드 호출
        StartCoroutine(CreateBuildingCoroutine(buildingList));
    }

    private IEnumerator CreateBuildingCoroutine(List<BuildingData> buildingList)
    {
        int generationCount = Mathf.Min(5000, buildingList.Count);

        int batchSize = 50;

        for (int i = 0; i < generationCount; i++)
        {
            int styleNumber = 0;
            if (System.Enum.TryParse<BuildingType>(buildingList[i].type, true, out BuildingType buildingType))
            {
                styleNumber = (int)buildingType;
                if (styleNumber > 4){styleNumber = 4;}
            }
            CreateSingleBuilding(buildingList[i], i, styleNumber);

            if (i % batchSize == 0)
            {
                yield return null; // 다음 프레임까지 쉼
            }
        }
    }

    private void CreateSingleBuilding(BuildingData buildingData, int index, int currentStyle)
    {
        // 순서 붙여서 건물 오브젝트 생성
        GameObject buildingObj = new GameObject($"Procedural_Building_{index}");
        // 스크립트 붙어있는 오브젝트를 건물 오브젝트의 부모로 설정
        buildingObj.transform.SetParent(this.transform);

        // 생성하는 버텍스 저장
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        // 벽 삼각형 순서 저장
        List<int> wallTriangles = new List<int>();
        // 옥상 삼각형 순서 저장
        List<int> roofTriangles = new List<int>();

        // 모서리 갯수 계산
        int cornerCount = buildingData.vertices.Count;
        // 넘겨받은 인덱스로 현재 스타일 파악
        BuildingStyle style = buildingStylesList[currentStyle];

        // --- [뼈대 외벽 메쉬 생성 및 프랍 배치 스캔] ---
        for (int i = 0; i < cornerCount; i++)
        {
            // 꼭지점 값을 벡터값으로 변환
            float p1_x = buildingData.vertices[i][0];
            float p1_z = buildingData.vertices[i][1];
            Vector3 p1 = new Vector3(p1_x, 0, p1_z);

            // 마지막 꼭지점 순서에서 0번째를 지정해주기 위해 나머지 나눗셈을 해 0을 지정
            int nextIndex = (i + 1) % cornerCount;
            float p2_x = buildingData.vertices[nextIndex][0];
            float p2_z = buildingData.vertices[nextIndex][1];
            Vector3 p2 = new Vector3(p2_x, 0, p2_z); // 다음 꼭짓점 (순환)

            // 벽면의 방향(Normal) 계산하여 프랍이 바깥을 보게 만듦
            Vector3 wallDir = (p2 - p1).normalized;
            Vector3 wallNormal = new Vector3(-wallDir.z, 0, wallDir.x); // 시계방향 기준 바깥쪽 노멀
            Quaternion wallRotation = Quaternion.LookRotation(wallNormal);      

            float wallWidth = Vector3.Distance(p1, p2);
            float totalBuildingHeight = buildingData.floors * floorHeight;

            // 4k 시트 사용하니 그에 맞춰서 가로 비율을 늘려줌
            float textureScaleX = 24f;
            // 텍스쳐 크기와 실제 벽 길이를 비교해 텍스쳐 대비 사용할 비율 계산
            float tilingX = wallWidth / textureScaleX;

            float currentY = 0f;

            // 외벽 메시 생성
            
            // 만약 베이스 높이가 있다면 디딤돌 생성
            if (style.baseHeight > 0f)
            {
                float nextY = style.baseHeight;
                CreateWall(currentY, nextY, tilingX, p1, p2, RowVRange[style.baseRowIndex], ref vertices, ref uvs, ref wallTriangles);
                currentY = nextY;
            }

            // 메인 벽 생성
            for (int floor = 0; floor < buildingData.floors; floor++)
            {
                // 층 경계선 만큼의 높이를 빼고 벽 최대 높이 설정
                float nextY = currentY + (floorHeight - style.trimHeight);
                CreateWall(currentY, nextY, tilingX, p1, p2, RowVRange[style.wallRowIndex], ref vertices, ref uvs, ref wallTriangles);
                currentY = nextY;
                // 층간 띠 있다면 생성
                if (style.trimHeight > 0f)
                {
                    float trimTopY = currentY + style.trimHeight;
                    CreateWall(currentY, trimTopY, tilingX, p1, p2, RowVRange[style.trimRowIndex], ref vertices, ref uvs, ref wallTriangles);
                    currentY = trimTopY;
                }
            }
            if (style.parapetHeight > 0f)
            {
                float nextY = currentY + style.parapetHeight;
                Vector2 parapetUV = RowVRange[style.parapetRowIndex];
                CreateWall(currentY, nextY, tilingX, p1, p2, parapetUV, ref vertices, ref uvs, ref wallTriangles);
                
                // 난간 안쪽 면 만들기
                Vector3 forward = (p2 - p1).normalized;
                Vector3 inwardNormal = Vector3.Cross(forward, Vector3.up).normalized;

                // 아래쪽 꼭지점은 건물 안쪽 방향으로 밀어 넣어 바깥면과 유격 생성
                Vector3 innerP1_Bottom = p1 - inwardNormal * 0.05f;
                Vector3 innerP2_Bottom = p2 - inwardNormal * 0.05f;

                // 위쪽 꼭지점은 바깥면과 동일하게 사용
                Vector3 innerP1_Top = p1;
                Vector3 innerP2_Top = p2;

                int vIndexInner = vertices.Count;

                // 계산한 점을 이용해 점 생성
                vertices.Add(innerP1_Bottom + Vector3.up * currentY);
                vertices.Add(innerP2_Bottom + Vector3.up * currentY);
                vertices.Add(innerP1_Top + Vector3.up * nextY);
                vertices.Add(innerP2_Top + Vector3.up * nextY);

                uvs.Add(new Vector2(0, parapetUV.x));
                uvs.Add(new Vector2(tilingX, parapetUV.x));
                uvs.Add(new Vector2(0, parapetUV.y));
                uvs.Add(new Vector2(tilingX, parapetUV.y));

                // 삼각형 안쪽 면 엮기 (카메라가 옥상 내부를 봐야 하므로 반시계 정렬 저격용 순서)
                wallTriangles.Add(vIndexInner);     wallTriangles.Add(vIndexInner + 2); wallTriangles.Add(vIndexInner + 1);
                wallTriangles.Add(vIndexInner + 1); wallTriangles.Add(vIndexInner + 2); wallTriangles.Add(vIndexInner + 3);
            }
            if (style.windowSpawnChance > 0)
            {
                PlaceWindowsOnWall(buildingData, style, buildingObj, p1, p2, wallNormal, wallRotation);
            }
            
        }

        int roofVIndexStart = vertices.Count;

        float roofHeight = (style.baseHeight > 0 ? style.baseHeight : 0f) + (buildingData.floors * floorHeight);

        for (int i = 0; i < cornerCount; i++)
        {
            float x = buildingData.vertices[i][0];
            float z = buildingData.vertices[i][1];
            
            vertices.Add(new Vector3(x, roofHeight, z));

            // 텍스쳐가 무한 반복될 수 있도록 추가
            uvs.Add(new Vector2(x * style.roofUVScale, z * style.roofUVScale));
        }

        // 전처리된 데이터가 반시계 방향이라 2번과 3번을 뒤집어 정리
        for (int i = 0; i < buildingData.triangles.Length; i+=3)
        {
            int index0 = buildingData.triangles[i];
            int index1 = buildingData.triangles[i+1];
            int index2 = buildingData.triangles[i+2];

            // 이전 버텍스들에 더해 전처리된 순서를 더해 버텍스 전체에서 지정할 순서 정리
            roofTriangles.Add(roofVIndexStart + index0);
            roofTriangles.Add(roofVIndexStart + index2);
            roofTriangles.Add(roofVIndexStart + index1);
        }

        // 메시 생성
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        // 천장과 벽을 구분하기 위해 서브메쉬 2개로 분리
        mesh.subMeshCount = 2;

        // 들어있는 버텍스들을 triangles에 들어있는 순서를 이용해 삼각형으로 조립
        mesh.SetTriangles(wallTriangles.ToArray(), 0);
        mesh.SetTriangles(roofTriangles.ToArray(), 1);
        // 건물의 크기를 계산에 화면 밖에 나갔을 경우 사라질 수 있도록 준비
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        mesh.RecalculateNormals();

        // 마테리얼 처리를 위해 컴포넌트 추가
        MeshFilter meshFilter = buildingObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = buildingObj.AddComponent<MeshRenderer>();

        if (meshRenderer != null)
        {
            // 마테리얼 담기위한 리스트 생성
            Material[] buildingMaterials = new Material[2];
            buildingMaterials[0] = buildingMaterial;
            buildingMaterials[1] = style.roofMaterial;

            meshRenderer.materials = buildingMaterials;

            MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(propBlock);

            // 랜덤 색상 더해 조절
            float r = Random.Range(0.9f, 1.0f);
            float g = Random.Range(0.9f, 1.0f);
            float b = Random.Range(0.9f, 1.0f);

            propBlock.SetColor("_BuildingColor", new Color(r, g, b, 1.0f));

            meshRenderer.SetPropertyBlock(propBlock);

            // 건물에 컨트롤러 코드 추가
            BuildingController controller = buildingObj.AddComponent<BuildingController>();
            controller.RegisterRenderer(meshRenderer);

            // 만들어진 메쉬 모양 사용해 콜라이더 추가
            MeshCollider meshCollider = buildingObj.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
        }
        meshFilter.mesh = mesh;
    }

    // 현재 Y높이, 목표 Y 높이, 마테리얼 X비율, 첫번째 점, 두번째 점, 사용할 uv 범위, 꼭지점 리스트, uv리스트, 삼각형 생성 순서
    private void CreateWall(float currentY, float nextY, float tilingX, Vector3 p1, Vector3 p2, Vector2 uvRange, ref List<Vector3> vertices, ref List<Vector2> uvs, ref List<int> wallTriangles)
    {
        // 계속해서 꼭지점 갯수가 늘어나니 벽을 생성 할 때마다 업데이트
        int vIndex = vertices.Count;

        // 목표 높이에 맞춰 꼭짓점 생성
        // 하단 좌우
        vertices.Add(p1 + Vector3.up * currentY);
        vertices.Add(p2 + Vector3.up * currentY);
        vertices.Add(p1 + Vector3.up * nextY);
        vertices.Add(p2 + Vector3.up * nextY);

        // 벽에 uv 적용
        Vector2 wallUV = uvRange;
        uvs.Add(new Vector2(0, wallUV.x));
        uvs.Add(new Vector2(tilingX, wallUV.x));
        uvs.Add(new Vector2(0, wallUV.y));
        uvs.Add(new Vector2(tilingX, wallUV.y));

        // 시계방향으로 점을 연결해 삼각형으로 면 만들기
        // 유니티는 꼭지점에서 면이 시계방향으로 회전해야 앞면이라고 인식
        // 전치리한 데이터가 반시계로 들어와서 반시계로 만들어 실질적으로 시계방향으로 면 생성
        wallTriangles.Add(vIndex); wallTriangles.Add(vIndex + 1); wallTriangles.Add(vIndex + 2);
        wallTriangles.Add(vIndex + 2); wallTriangles.Add(vIndex + 1); wallTriangles.Add(vIndex + 3);
    }
    // 창문 생성 코드
    private void PlaceWindowsOnWall(
    BuildingData buildingData,
    BuildingStyle style,
    GameObject buildingObj,
    Vector3 p1,
    Vector3 p2,
    Vector3 wallNormal,
    Quaternion wallRotation
    )
    {
        if (style.windowPrefab == null || style.windowPrefab.Count == 0)
            return;

        float wallWidth = Vector3.Distance(p1, p2);

        // 너무 짧은 벽에는 창문 생략
        if (wallWidth < style.windowSpacing)
            return;

        Vector3 wallDir = (p2 - p1).normalized;

        // 벽 길이에 따라 창문 개수 계산
        int windowCount = Mathf.FloorToInt(wallWidth / style.windowSpacing);

        if (windowCount <= 0)
            return;

        // 양끝 모서리에 너무 붙지 않도록 중앙 정렬용 여백 계산
        float usedWidth = (windowCount - 1) * style.windowSpacing;
        float startOffset = (wallWidth - usedWidth) * 0.5f;

        for (int floor = style.startingWindowFloor; floor < buildingData.floors; floor++)
        {
            float y = floor * floorHeight + floorHeight * 0.55f;

            for (int w = 0; w < windowCount; w++)
            {
                if (Random.value > style.windowSpawnChance)
                    continue;

                float distanceAlongWall = startOffset + w * style.windowSpacing;

                Vector3 pos = p1 + wallDir * distanceAlongWall;
                pos += Vector3.up * y;

                // z-fighting 방지: 벽 바깥쪽으로 살짝 띄움
                pos += wallNormal * 0.05f;

                GameObject chosenWindow = style.windowPrefab[
                    Random.Range(0, style.windowPrefab.Count)
                ];

                GameObject windowInstance = Instantiate(
                    chosenWindow,
                    pos,
                    wallRotation,
                    buildingObj.transform
                );

                // 필요하면 스케일 랜덤화
                // windowInstance.transform.localScale *= Random.Range(0.9f, 1.1f);
            }
        }
    }
}
// */