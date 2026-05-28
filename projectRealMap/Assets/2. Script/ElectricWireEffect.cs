using UnityEngine;

public class ElectricWireEffect : MonoBehaviour
{
    [Header("���� ���� ��ġ")]
    [Tooltip("����θ� Player �߽��� �������� ���Ⱑ ���ɴϴ�. Sphere�� ������ Sphere ��ġ �������� ���Ⱑ ���ɴϴ�.")]
    public Transform effectCenter;

    [Header("���� �⺻ ����")]
    public int electricLineCount = 12;
    public int electricityPoints = 4;

    [Header("�� �ֺ� ����ũ")]
    public float bodySparkMinLength = 0.35f;
    public float bodySparkMaxLength = 0.85f;

    [Header("�̵� ���� ����")]
    public float tailMinLength = 0.6f;
    public float tailMaxLength = 1.2f;

    [Header("���� �β�")]
    public float electricStartWidth = 0.08f;
    public float electricEndWidth = 0.025f;

    [Header("���� ������ �ӵ�")]
    [Tooltip("���� �������� �̵� �� ���� ����� �� ������ �ٲ�ϴ�.")]
    public float movingFlickerRate = 55f;

    [Tooltip("������ ���� �� ����ũ�� �ٲ�� �ӵ��Դϴ�.")]
    public float idleFlickerRate = 35f;

    [Header("���� ���� ����")]
    public float lightningJitter = 0.32f;
    public float tailScatterAmount = 0.18f;
    public float bodyScatterAmount = 0.22f;

    [Header("���� �ٴ� ����")]
    public float tailStartDistance = 0.03f;

    [Header("�̵� ���� ����")]
    [Tooltip("�� ������ ���� �����̸� �̵� �� ����� �Ǵ��մϴ�.")]
    public float movementDetectThreshold = 0.00001f;

    private LineRenderer[] electricLines;
    private float[] lineSeeds;

    private Vector3 lastCenterWorldPosition;
    private Vector3 lastMoveDirectionLocal = Vector3.forward;

    private int lastMovingTick = -1;
    private int lastIdleTick = -1;

    private void Start()
    {
        CreateElectricLines();
        lastCenterWorldPosition = GetCenterWorldPosition();
    }

    private void LateUpdate()
    {
        if (electricLines == null)
        {
            return;
        }

        Vector3 currentCenterWorldPosition = GetCenterWorldPosition();
        Vector3 worldMoveDelta = currentCenterWorldPosition - lastCenterWorldPosition;

        if (worldMoveDelta.sqrMagnitude > movementDetectThreshold)
        {
            DrawMovingElectricityByMovement(worldMoveDelta.normalized);
        }
        else
        {
            DrawIdleElectricityAlways();
        }

        lastCenterWorldPosition = currentCenterWorldPosition;
    }

    private void CreateElectricLines()
    {
        LineRenderer originalLine = GetComponent<LineRenderer>();

        if (originalLine == null)
        {
            Debug.LogWarning("ElectricWireEffect: Player�� Line Renderer�� �����ϴ�.");
            return;
        }

        electricLineCount = Mathf.Max(1, electricLineCount);
        electricityPoints = Mathf.Max(3, electricityPoints);

        electricLines = new LineRenderer[electricLineCount];
        lineSeeds = new float[electricLineCount];

        electricLines[0] = originalLine;

        for (int i = 1; i < electricLineCount; i++)
        {
            GameObject lineObject = new GameObject("Electric Spark " + i);
            lineObject.transform.SetParent(transform);
            lineObject.transform.localPosition = Vector3.zero;
            lineObject.transform.localRotation = Quaternion.identity;
            lineObject.transform.localScale = Vector3.one;

            LineRenderer newLine = lineObject.AddComponent<LineRenderer>();
            newLine.sharedMaterial = originalLine.sharedMaterial;

            electricLines[i] = newLine;
        }

        for (int i = 0; i < electricLines.Length; i++)
        {
            LineRenderer line = electricLines[i];

            line.useWorldSpace = false;
            line.positionCount = electricityPoints;

            line.startWidth = electricStartWidth;
            line.endWidth = electricEndWidth;

            line.numCapVertices = 2;
            line.numCornerVertices = 1;

            line.startColor = new Color(1f, 0.95f, 0f, 1f);
            line.endColor = new Color(1f, 0.45f, 0f, 0.75f);

            line.enabled = false;

            lineSeeds[i] = Random.Range(0f, 10000f);
        }
    }

    private Vector3 GetCenterWorldPosition()
    {
        if (effectCenter != null)
        {
            return effectCenter.position;
        }

        return transform.position;
    }

    private Vector3 GetCenterLocalPosition()
    {
        if (effectCenter != null)
        {
            return transform.InverseTransformPoint(effectCenter.position);
        }

        return Vector3.zero;
    }

    private void DrawMovingElectricityByMovement(Vector3 worldMoveDirection)
    {
        Vector3 localMoveDirection = transform.InverseTransformDirection(worldMoveDirection);

        if (localMoveDirection.sqrMagnitude > 0.001f)
        {
            lastMoveDirectionLocal = localMoveDirection.normalized;
        }

        int currentTick = Mathf.FloorToInt(Time.time * movingFlickerRate);

        // �̵� �߿��� ��ư �Է°� ������� �׻� ���� ��ġ �������� ���⸦ �ٽ� �׸��ϴ�.
        DrawMovingElectricity(currentTick);
    }

    private void DrawMovingElectricity(int tick)
    {
        Vector3 tailDirection = -lastMoveDirectionLocal.normalized;

        Vector3 centerLocal = GetCenterLocalPosition() + Vector3.up * 0.35f;

        Vector3 sideAxis = Vector3.Cross(tailDirection, Vector3.up);

        if (sideAxis.sqrMagnitude <= 0.001f)
        {
            sideAxis = Vector3.right;
        }

        sideAxis.Normalize();

        Vector3 upAxis = Vector3.Cross(sideAxis, tailDirection).normalized;

        int tailLineCount = Mathf.CeilToInt(electricLines.Length * 0.6f);

        for (int lineIndex = 0; lineIndex < electricLines.Length; lineIndex++)
        {
            LineRenderer line = electricLines[lineIndex];

            line.enabled = true;
            line.useWorldSpace = false;
            line.positionCount = electricityPoints;

            bool isTailLine = lineIndex < tailLineCount;

            float seed = lineSeeds[lineIndex];

            Vector3 startPos;
            Vector3 endPos;

            if (isTailLine)
            {
                float sideRandom = SharpRandom(seed, tick, 1);
                float upRandom = SharpRandom(seed, tick, 2);
                float lengthRandom = Mathf.Abs(SharpRandom(seed, tick, 3));

                Vector3 startOffset =
                    sideAxis * sideRandom * 0.08f +
                    upAxis * upRandom * 0.06f;

                Vector3 endScatter =
                    sideAxis * SharpRandom(seed, tick, 4) * tailScatterAmount +
                    upAxis * SharpRandom(seed, tick, 5) * tailScatterAmount;

                float tailLength = Mathf.Lerp(tailMinLength, tailMaxLength, lengthRandom);

                startPos =
                    centerLocal +
                    tailDirection * tailStartDistance +
                    startOffset;

                endPos =
                    centerLocal +
                    tailDirection * tailLength +
                    endScatter;
            }
            else
            {
                float sideRandom = SharpRandom(seed, tick, 6);
                float upRandom = SharpRandom(seed, tick, 7);
                float lengthRandom = Mathf.Abs(SharpRandom(seed, tick, 8));

                Vector3 bodyDirection =
                    sideAxis * sideRandom +
                    upAxis * upRandom;

                if (bodyDirection.sqrMagnitude <= 0.001f)
                {
                    bodyDirection = upAxis;
                }

                bodyDirection.Normalize();

                Vector3 bodyScatter =
                    sideAxis * SharpRandom(seed, tick, 9) * bodyScatterAmount +
                    upAxis * SharpRandom(seed, tick, 10) * bodyScatterAmount;

                float bodyLength = Mathf.Lerp(bodySparkMinLength, bodySparkMaxLength, lengthRandom);

                startPos =
                    centerLocal +
                    bodyDirection * 0.12f;

                endPos =
                    centerLocal +
                    bodyDirection * bodyLength +
                    tailDirection * 0.12f +
                    bodyScatter;
            }

            DrawLightningLine(line, startPos, endPos, sideAxis, upAxis, seed, tick, true);
        }

        lastMovingTick = tick;
    }

    private void DrawIdleElectricityAlways()
    {
        int currentTick = Mathf.FloorToInt(Time.time * idleFlickerRate);

        // ���� Tick�̸� ����� ���������� ��ġ�� ��� ���� ��ġ �������� �پ� �ֽ��ϴ�.
        if (currentTick == lastIdleTick)
        {
            return;
        }

        lastIdleTick = currentTick;

        Vector3 centerLocal = GetCenterLocalPosition() + Vector3.up * 0.35f;

        for (int lineIndex = 0; lineIndex < electricLines.Length; lineIndex++)
        {
            LineRenderer line = electricLines[lineIndex];

            if (Random.value < 0.08f)
            {
                line.enabled = false;
                continue;
            }

            line.enabled = true;
            line.useWorldSpace = false;
            line.positionCount = electricityPoints;

            float seed = lineSeeds[lineIndex];

            Vector3 sparkDirection = new Vector3(
                SharpRandom(seed, currentTick, 20),
                SharpRandom(seed, currentTick, 21),
                SharpRandom(seed, currentTick, 22)
            );

            if (sparkDirection.sqrMagnitude <= 0.001f)
            {
                sparkDirection = Vector3.up;
            }

            sparkDirection.Normalize();

            Vector3 startPos =
                centerLocal +
                sparkDirection * 0.18f;

            Vector3 endPos =
                centerLocal +
                sparkDirection * Mathf.Lerp(
                    bodySparkMinLength,
                    bodySparkMaxLength,
                    Mathf.Abs(SharpRandom(seed, currentTick, 23))
                );

            Vector3 sideAxis = Vector3.Cross(sparkDirection, Vector3.up);

            if (sideAxis.sqrMagnitude <= 0.001f)
            {
                sideAxis = Vector3.right;
            }

            sideAxis.Normalize();

            Vector3 upAxis = Vector3.Cross(sideAxis, sparkDirection).normalized;

            DrawLightningLine(
                line,
                startPos,
                endPos,
                sideAxis,
                upAxis,
                seed,
                currentTick,
                false
            );
        }
    }

    private void DrawLightningLine(
        LineRenderer line,
        Vector3 startPos,
        Vector3 endPos,
        Vector3 sideAxis,
        Vector3 upAxis,
        float seed,
        int tick,
        bool isMoving
    )
    {
        float widthRandom = Mathf.Abs(SharpRandom(seed, tick, 30));
        float flicker = Mathf.Lerp(0.75f, 1.35f, widthRandom);

        if (isMoving)
        {
            line.startWidth = electricStartWidth * flicker;
            line.endWidth = electricEndWidth * flicker;
        }
        else
        {
            line.startWidth = electricStartWidth * 1.1f * flicker;
            line.endWidth = electricEndWidth * 1.0f * flicker;
        }

        for (int i = 0; i < electricityPoints; i++)
        {
            float t = (float)i / (electricityPoints - 1);

            Vector3 point = Vector3.Lerp(startPos, endPos, t);

            if (i > 0 && i < electricityPoints - 1)
            {
                float sideRandom = SharpRandom(seed, tick, 100 + i * 2);
                float upRandom = SharpRandom(seed, tick, 101 + i * 2);

                Vector3 sharpJitter =
                    sideAxis * sideRandom * lightningJitter +
                    upAxis * upRandom * lightningJitter;

                point += sharpJitter;
            }

            line.SetPosition(i, point);
        }
    }

    private float SharpRandom(float seed, int tick, int salt)
    {
        float value = Mathf.Sin(seed * 12.9898f + tick * 78.233f + salt * 37.719f) * 43758.5453f;
        return (value - Mathf.Floor(value)) * 2f - 1f;
    }

    public void StopElectricity()
    {
        if (electricLines == null)
        {
            return;
        }

        for (int i = 0; i < electricLines.Length; i++)
        {
            electricLines[i].enabled = false;
        }
    }

    // ���� �ڵ� ȣȯ��
    public void PlayMovingElectricity(float moveInput)
    {
        if (moveInput > 0f)
        {
            DrawMovingElectricityByMovement(transform.forward);
        }
        else
        {
            DrawMovingElectricityByMovement(-transform.forward);
        }
    }

    // ���� �ڵ� ȣȯ��
    public void PlayMovingElectricity(Vector3 moveDirection)
    {
        DrawMovingElectricityByMovement(moveDirection);
    }

    // ���� �ڵ� ȣȯ��
    public void PlayIdleElectricity()
    {
        DrawIdleElectricityAlways();
    }
}