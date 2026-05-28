using UnityEngine;

public class ElectricWireEffect : MonoBehaviour
{
    [Header("전기 기본 설정")]
    public int electricLineCount = 12;
    public int electricityPoints = 4;

    [Header("몸 주변 스파크")]
    public float bodySparkMinLength = 0.5f;
    public float bodySparkMaxLength = 1.1f;

    [Header("이동 꼬리 전기")]
    public float tailMinLength = 1.4f;
    public float tailMaxLength = 2.4f;

    [Header("전기 모양")]
    public float electricJitter = 0.18f;
    public float electricStartWidth = 0.08f;
    public float electricEndWidth = 0.025f;

    [Header("전기 깜빡임")]
    public float refreshTime = 0.035f;

    private LineRenderer[] electricLines;
    private float timer = 0f;

    private void Start()
    {
        CreateElectricLines();
    }

    private void Update()
    {
        timer += Time.deltaTime;
    }

    private void CreateElectricLines()
    {
        LineRenderer originalLine = GetComponent<LineRenderer>();

        if (originalLine == null)
        {
            Debug.LogWarning("ElectricWireEffect: Line Renderer가 없습니다. Sphere에 Line Renderer를 추가하세요.");
            return;
        }

        electricLineCount = Mathf.Max(1, electricLineCount);
        electricityPoints = Mathf.Max(2, electricityPoints);

        electricLines = new LineRenderer[electricLineCount];

        electricLines[0] = originalLine;

        for (int i = 1; i < electricLineCount; i++)
        {
            GameObject lineObject = new GameObject("Electric Spark " + i);
            lineObject.transform.SetParent(transform);

            LineRenderer newLine = lineObject.AddComponent<LineRenderer>();
            newLine.sharedMaterial = originalLine.sharedMaterial;

            electricLines[i] = newLine;
        }

        for (int i = 0; i < electricLines.Length; i++)
        {
            LineRenderer line = electricLines[i];

            line.useWorldSpace = true;
            line.positionCount = electricityPoints;

            line.startWidth = electricStartWidth;
            line.endWidth = electricEndWidth;

            line.numCapVertices = 2;
            line.numCornerVertices = 2;

            line.startColor = new Color(1f, 0.95f, 0f, 1f);
            line.endColor = new Color(1f, 0.45f, 0f, 0.75f);

            line.enabled = false;
        }
    }

    public void PlayMovingElectricity(float moveInput)
    {
        if (electricLines == null) return;

        if (timer < refreshTime) return;
        timer = 0f;

        Vector3 tailDirection;

        if (moveInput > 0)
        {
            tailDirection = -transform.forward;
        }
        else
        {
            tailDirection = transform.forward;
        }

        Vector3 centerPos = transform.position + Vector3.up * 0.35f;

        int tailLineCount = Mathf.CeilToInt(electricLines.Length * 0.55f);

        for (int lineIndex = 0; lineIndex < electricLines.Length; lineIndex++)
        {
            LineRenderer line = electricLines[lineIndex];

            if (Random.value < 0.08f)
            {
                line.enabled = false;
                continue;
            }

            line.enabled = true;
            line.positionCount = electricityPoints;

            bool isTailLine = lineIndex < tailLineCount;

            Vector3 startPos;
            Vector3 endPos;

            if (isTailLine)
            {
                Vector3 smallOffset =
                    transform.right * Random.Range(-0.16f, 0.16f) +
                    transform.up * Random.Range(-0.08f, 0.14f);

                startPos =
                    centerPos +
                    tailDirection * 0.15f +
                    smallOffset;

                float tailLength = Random.Range(tailMinLength, tailMaxLength);

                endPos =
                    centerPos +
                    tailDirection * tailLength +
                    smallOffset * Random.Range(0.5f, 1.4f);
            }
            else
            {
                Vector3 sideDirection = Random.onUnitSphere;

                startPos =
                    centerPos +
                    sideDirection * Random.Range(0.2f, 0.4f);

                endPos =
                    centerPos +
                    sideDirection * Random.Range(bodySparkMinLength, bodySparkMaxLength) +
                    tailDirection * Random.Range(0.1f, 0.3f);
            }

            DrawOneElectricLine(line, startPos, endPos, true);
        }
    }

    public void PlayIdleElectricity()
    {
        if (electricLines == null) return;

        if (timer < refreshTime) return;
        timer = 0f;

        Vector3 centerPos = transform.position + Vector3.up * 0.35f;

        for (int lineIndex = 0; lineIndex < electricLines.Length; lineIndex++)
        {
            LineRenderer line = electricLines[lineIndex];

            if (Random.value < 0.08f)
            {
                line.enabled = false;
                continue;
            }

            line.enabled = true;
            line.positionCount = electricityPoints;

            Vector3 sparkDirection = Random.onUnitSphere;

            Vector3 startPos =
                centerPos +
                sparkDirection * Random.Range(0.25f, 0.45f);

            Vector3 endPos =
                centerPos +
                sparkDirection * Random.Range(bodySparkMinLength, bodySparkMaxLength);

            DrawOneElectricLine(line, startPos, endPos, false);
        }
    }

    public void StopElectricity()
    {
        if (electricLines == null) return;

        for (int i = 0; i < electricLines.Length; i++)
        {
            electricLines[i].enabled = false;
        }
    }

    private void DrawOneElectricLine(LineRenderer line, Vector3 startPos, Vector3 endPos, bool isMoving)
    {
        float flicker = Random.Range(0.75f, 1.25f);

        if (isMoving)
        {
            line.startWidth = electricStartWidth * flicker;
            line.endWidth = electricEndWidth * flicker;
        }
        else
        {
            line.startWidth = electricStartWidth * 1.2f * flicker;
            line.endWidth = electricEndWidth * 1.1f * flicker;
        }

        for (int i = 0; i < electricityPoints; i++)
        {
            float t = (float)i / (electricityPoints - 1);

            Vector3 point = Vector3.Lerp(startPos, endPos, t);

            if (i > 0 && i < electricityPoints - 1)
            {
                point += Random.onUnitSphere * electricJitter;
            }

            line.SetPosition(i, point);
        }
    }
}