using UnityEngine;

public class BuildingLightController : MonoBehaviour
{
    [Header("밝기 설정")]
    public Color normalColor = Color.gray;
    public Color poweredColor = Color.yellow;

    [Header("밝아질 건물 Renderer")]
    public Renderer buildingRenderer;

    void Start()
    {
        if (buildingRenderer == null)
        {
            buildingRenderer = GetComponent<Renderer>();
        }

        SetNormalBrightness();
    }

    public void SetNormalBrightness()
    {
        if (buildingRenderer == null) return;

        buildingRenderer.material.color = normalColor;
    }

    public void SetPoweredBrightness()
    {
        if (buildingRenderer == null) return;

        buildingRenderer.material.color = poweredColor;
    }
}