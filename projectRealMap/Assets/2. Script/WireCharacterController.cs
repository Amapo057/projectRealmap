using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public class WireCharacterController : MonoBehaviour
{
    [Header("이동할 전선")]
    public SplineContainer targetWire;

    [Header("이동 속도")]
    [Range(0.1f, 50f)]
    public float moveSpeed = 5f;

    private float currentSplineProgress = 0f;

    private bool isAutoMoving = false;
    private float moveDirection = 0f;

    private ElectricWireEffect electricEffect;

    void Start()
    {
        electricEffect = GetComponent<ElectricWireEffect>();

        if (targetWire == null)
        {
            Debug.LogWarning("Target Wire가 비어 있습니다.");
            return;
        }

        currentSplineProgress = 0f;
        MovePlayerToSpline();
    }

    void Update()
    {
        if (targetWire == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            isAutoMoving = true;
            moveDirection = 1f;
        }

        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            isAutoMoving = true;
            moveDirection = -1f;
        }

        if (isAutoMoving)
        {
            float wireLength = targetWire.CalculateLength();

            if (wireLength > 0.01f)
            {
                currentSplineProgress += (moveDirection * moveSpeed / wireLength) * Time.deltaTime;
                currentSplineProgress = Mathf.Clamp01(currentSplineProgress);

                if (currentSplineProgress >= 1f || currentSplineProgress <= 0f)
                {
                    isAutoMoving = false;
                }
            }
        }

        MovePlayerToSpline();

        if (electricEffect != null)
        {
            if (isAutoMoving)
            {
                electricEffect.PlayMovingElectricity(moveDirection);
            }
            else
            {
                electricEffect.PlayIdleElectricity();
            }
        }
    }

    private void MovePlayerToSpline()
    {
        float3 position;
        float3 tangent;
        float3 upVector;

        bool success = targetWire.Evaluate(currentSplineProgress, out position, out tangent, out upVector);

        if (success == false)
        {
            Debug.LogWarning("Spline 위치 계산 실패: Target Wire를 확인하세요.");
            return;
        }

        Vector3 finalPosition = (Vector3)position;

        if (float.IsNaN(finalPosition.x) || float.IsNaN(finalPosition.y) || float.IsNaN(finalPosition.z))
        {
            Debug.LogWarning("전선 위치가 이상합니다. Spline 점을 확인하세요.");
            return;
        }

        transform.position = finalPosition;

        Vector3 finalTangent = (Vector3)tangent;

        if (finalTangent.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(finalTangent);
        }
    }
}