using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;

[System.Serializable]
public class BuildingData
{
    public string use;
    public int floors;
    public List<float[]> vertices;
    public List<int> triangles;
}

public class CityGenerator : MonoBehaviour
{
    [Header("JSON Data")]
    public string jsonFileName = "preprocessing.json";
    
    [Header("Pre-made Materials")]
    // 유니티 인스펙터에서 직접 마테리얼 에셋을 할당하세요.
    public Material officeMaterial;
    public Material commercialMaterial;
    public Material residentialMaterial;
    public Material industrialMaterial;
    public Material defaultMaterial;

    [Header("Optimization")]
    public int buildingsPerBatch = 50; 
    public float delaySeconds = 0.01f; 

    private GameObject cityRoot;

    void Start()
    {
        // 하이라이키 정리를 위한 부모 오브젝트
        cityRoot = new GameObject("GeneratedCity");
        
        // 코루틴 시작
        StartCoroutine(GenerateCityRoutine());
    }

    IEnumerator GenerateCityRoutine()
    {
        string path = Path.Combine(Application.dataPath, jsonFileName);
        if (!File.Exists(path)) {
            Debug.LogError("JSON 파일을 찾을 수 없습니다! 경로: " + path);
            yield break;
        }

        string jsonString = File.ReadAllText(path);
        List<BuildingData> buildings = JsonConvert.DeserializeObject<List<BuildingData>>(jsonString);

        int totalCount = buildings.Count;
        for (int i = 0; i < totalCount; i++)
        {
            CreateBuilding(buildings[i], i);

            // CPU 부하 분산을 위해 배치마다 대기
            if (i > 0 && i % buildingsPerBatch == 0)
            {
                yield return new WaitForSeconds(delaySeconds); 
            }
        }
        Debug.Log($"✅ 총 {totalCount}개의 건물이 마테리얼과 연결되어 생성되었습니다!");
    }

    void CreateBuilding(BuildingData data, int index)
    {
        GameObject obj = new GameObject($"Building_{index}_{data.use}");
        obj.transform.SetParent(cityRoot.transform);

        MeshFilter mf = obj.AddComponent<MeshFilter>();
        MeshRenderer mr = obj.AddComponent<MeshRenderer>();

        // 1. ⭐️ 미리 만들어둔 마테리얼 에셋을 데이터 종류에 맞게 연결
        mr.material = GetMaterialByUse(data.use);

        // 2. 메쉬 생성
        Mesh mesh = new Mesh();
        Vector3[] unityVertices = new Vector3[data.vertices.Count];
        for (int i = 0; i < data.vertices.Count; i++)
        {
            // 파이썬의 (x, y)를 유니티 바닥인 (x, 0, z) 평면으로 변환
            unityVertices[i] = new Vector3(data.vertices[i][0], 0, data.vertices[i][1]);
        }

        mesh.vertices = unityVertices;
        mesh.triangles = data.triangles.ToArray();
        mesh.RecalculateNormals(); // 법선 재계산 (빛을 정상적으로 받게 함)
        
        mf.mesh = mesh;
    }

    // 데이터의 'use' 문자열을 보고 어떤 마테리얼 변수를 사용할지 결정합니다.
    Material GetMaterialByUse(string use)
    {
        switch (use)
        {
            case "Office": 
                return officeMaterial != null ? officeMaterial : defaultMaterial;
            case "Commercial": 
                return commercialMaterial != null ? commercialMaterial : defaultMaterial;
            case "Residential": 
                return residentialMaterial != null ? residentialMaterial : defaultMaterial;
            case "Industrial": 
                return industrialMaterial != null ? industrialMaterial : defaultMaterial;
            default: 
                return defaultMaterial;
        }
    }
}