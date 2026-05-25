using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

public class JsonPaser : MonoBehaviour
{
    public List<T> LoadAndParseJson<T>(string fileName) where T : class, new()
    {
        // Application.dataPath로 프로젝트의 Assets폴더 찾기
        string filePath = Path.Combine(Application.dataPath, fileName);

        // 파일 존재 검사
        if (File.Exists(filePath))
        {
            // json파일을 전부 읽어 문자열로 저장        
            string jsonText = File.ReadAllText(filePath);

            // 문자열을 구조체에 맞춰 변환
            List<T> parsedData = JsonConvert.DeserializeObject<List<T>>(jsonText);
            return parsedData;
        }
        else
        {
            Debug.LogError($"[실패] 파일을 찾을 수 없습니다! 경로를 확인해주세요: {filePath}");
            return null;
        }
    }
}