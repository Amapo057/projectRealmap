using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Rendering;
using Unity.VisualScripting;
using Newtonsoft.Json;
using System.Data;
using UnityEngine.UIElements;
using Unity.Android.Gradle.Manifest;

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
        public int buildingType;
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
        public List<GameObject> acPrefab;

        [Header("창문 배치 규칙 (Window Placement)")]
    
        [Tooltip("창문과 창문 사이의 가로 간격(미터)입니다. 좁을수록 촘촘해집니다.")]
        public float windowSpacing = 3.0f;

        [Tooltip("창문이 생성될 확률입니다 (0.0 ~ 1.0). 1이면 꽉 채우고, 0.7이면 가끔 빈 벽이 생겨 자연스럽습니다.")]
        [Range(0f, 1f)]
        public float windowSpawnChance = 1f;

        [Tooltip("몇 층부터 창문을 달기 시작할지 정합니다. (예: 1로 설정하면 0층(1층)은 건너뛰고 2층부터 달림)")]
        public int startingWindowFloor = 0;
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
    private float floorHeight = 3.3f;   // 한 층의 높이
    private Vector2[] RowUV = new Vector2[7];
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
            RowUV[i] = new Vector2(vMin / TOTAL_TEX_HEIGHT, vMax / TOTAL_TEX_HEIGHT);
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
        int generationCount = Mathf.Min(50, buildingList.Count);        

        for (int i = 0; i < generationCount; i++)
        {
            int randomStyle = Random.Range(0, 2); 
            CreateSingleBuilding(buildingList[i], i, randomStyle);
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
                int vIndexBase = vertices.Count;
                float nextY = currentY + style.baseHeight;

                // 디딤돌 높이에 맞춰 꼭짓점 생성
                // 하단 좌우
                vertices.Add(p1 + Vector3.up * currentY);
                vertices.Add(p2 + Vector3.up * currentY);
                // 디딤돌 높이를 저장한 nextY를 활용해 상화 좌우 꼭지점 생성
                vertices.Add(p1 + Vector3.up * nextY);
                vertices.Add(p2 + Vector3.up * nextY);

                // 벽에 uv 적용
                Vector2 baseUV = RowUV[style.baseRowIndex];
                uvs.Add(new Vector2(0, baseUV.x));
                uvs.Add(new Vector2(tilingX, baseUV.x));
                uvs.Add(new Vector2(0, baseUV.y));
                uvs.Add(new Vector2(tilingX, baseUV.y));

                // 시계방향으로 점을 연결해 삼각형으로 면 만들기
                // 유니티는 꼭지점에서 면이 시계방향으로 회전해야 앞면이라고 인식
                // 전치리한 데이터가 반시계로 들어와서 반시계로 만들어 실질적으로 시계방향으로 면 생성
                wallTriangles.Add(vIndexBase);     wallTriangles.Add(vIndexBase + 1); wallTriangles.Add(vIndexBase + 2); 
                wallTriangles.Add(vIndexBase + 2); wallTriangles.Add(vIndexBase + 1); wallTriangles.Add(vIndexBase + 3);

                // 현재 Y 높이 기록
                currentY = nextY;
            }

            // 메인 벽 생성
            for (int floor = 0; floor < buildingData.floors; floor++)
            {
                // vertices는 계속 증가하니 새로 변수 생성
                int vIndexWall = vertices.Count;
                // 층 경계선 만큼의 높이를 빼고 벽 최대 높이 설정
                float wallTopY = currentY + (floorHeight - style.trimHeight);

                // 하단 좌우
                vertices.Add(p1 + Vector3.up * currentY);
                vertices.Add(p2 + Vector3.up * currentY);
                // 상단 좌우
                vertices.Add(p1 + Vector3.up * wallTopY);
                vertices.Add(p2 + Vector3.up * wallTopY);

                // 벽에 uv 적용
                Vector2 wallUV = RowUV[style.wallRowIndex];
                uvs.Add(new Vector2(0, wallUV.x));
                uvs.Add(new Vector2(tilingX, wallUV.x));
                uvs.Add(new Vector2(0, wallUV.y));
                uvs.Add(new Vector2(tilingX, wallUV.y));

                wallTriangles.Add(vIndexWall);     wallTriangles.Add(vIndexWall + 1); wallTriangles.Add(vIndexWall + 2); 
                wallTriangles.Add(vIndexWall + 2); wallTriangles.Add(vIndexWall + 1); wallTriangles.Add(vIndexWall + 3);
                currentY = wallTopY;
            }
            // 층간 띠 있다면 생성
            if (style.trimHeight > 0f)
            {
                int vIndexTrim = vertices.Count;
                float trimTopY = currentY + style.trimHeight;

                vertices.Add(p1 + Vector3.up * currentY); // 띠 하단 좌
                vertices.Add(p2 + Vector3.up * currentY); // 띠 하단 우
                vertices.Add(p1 + Vector3.up * trimTopY); // 띠 상단 좌
                vertices.Add(p2 + Vector3.up * trimTopY); // 띠 상단 우

                // 층간 띠 UV 매핑
                Vector2 trimUV = RowUV[style.trimRowIndex];
                uvs.Add(new Vector2(0, trimUV.x));
                uvs.Add(new Vector2(tilingX, trimUV.x));
                uvs.Add(new Vector2(0, trimUV.y));
                uvs.Add(new Vector2(tilingX, trimUV.y));

                wallTriangles.Add(vIndexTrim);     wallTriangles.Add(vIndexTrim + 1); wallTriangles.Add(vIndexTrim + 2); 
                wallTriangles.Add(vIndexTrim + 2); wallTriangles.Add(vIndexTrim + 1); wallTriangles.Add(vIndexTrim + 3);

                currentY = trimTopY; // 현재 고도를 띠 꼭대기(즉, 이 층의 완전한 천장)로 이동
            }

            // 2. 규칙 기반 소품(프랍) 배치 로직
            // 규칙 A: 처음으로 만드는 벽(index 0)의 중앙에 '문' 배치
            // if (i == 0)
            // {
            //     Vector3 doorPos = (p1 + p2) / 2f; // 1층 바닥 중앙
            //     GameObject chosenDoor = doorPrefabs[Random.Range(0, doorPrefabs.Length)];
            //     Instantiate(chosenDoor, doorPos, wallRotation, buildingObj.transform);
            // }

            // // 층별 창문 및 실외기 루프
            // for (int f = 0; f < data.floors; f++)
            // {
            //     float currentFloorCenterY = (f * floorHeight) + (floorHeight / 2f);
            //     Vector3 wallCenterAtFloor = (p1 + p2) / 2f + Vector3.up * currentFloorCenterY;

            //     // 규칙 B: 모든 벽 중간에 한 층에 하나씩 랜덤 창문 달기
            //     GameObject chosenWindow = windowPrefabs[Random.Range(0, windowPrefabs.Length)];
            //     GameObject windowInstance = Instantiate(chosenWindow, wallCenterAtFloor, wallRotation, buildingObj.transform);

            //     // 규칙 C: 문 기준 왼쪽 벽(마지막 벽면 index로 가정)의 창문 아래에 실외기 달기
            //     // 시계방향 데이터 구조에서 첫 번째 벽(정면 문)의 직전 벽(cornerCount - 1)이 외관상 왼쪽 벽이 됩니다.
            //     if (i == cornerCount - 1 && acPrefab != null)
            //     {
            //         // 창문 기준 약간 아래(예: 0.8미터 하단)에 실외기 배치
            //         Vector3 acPos = wallCenterAtFloor + Vector3.down * 0.8f;
            //         Instantiate(acPrefab, acPos, wallRotation, buildingObj.transform);
            //     }
            // }
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
        // 메시가 2개라 설정
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
        }
        meshFilter.mesh = mesh;
    }
}
// */