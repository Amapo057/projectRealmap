using System.Collections.Generic;
using UnityEngine;

public class BuildingController : MonoBehaviour
{
    private List<Material> materialsList = new List<Material>();

    private readonly string emissionController = "EmissionIntensity";

    public void RegisterRenderer(MeshRenderer renderer)
    {
        // 렌더러 있는지 검사
        if (renderer == null) return;
        // 임시로 마테리얼 잡음
        foreach (var mat in renderer.materials)
        {
            // 마테리얼에 에미션 변경 변수가 있다면 리스트에 마테리얼 저장
            if (mat.HasProperty(emissionController))
            {
                materialsList.Add(mat);
            }
        }
    }

    public void SetEmission(float intensity)
    {
        foreach(var mat in materialsList)
        {
            // 마테리얼의 emmision값을 설정한 값으로 변경
            mat.SetFloat(emissionController, intensity);

        }
    }
    
}
