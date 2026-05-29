using UnityEngine;
using UnityEngine.Splines;

public class ElectricPoleMover : MonoBehaviour
{
    [Header("현재 서 있는 전봇대 노드")]
    [SerializeField] private PoleNode currentNode;

    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 8f;

    [Tooltip("입력 방향과 후보 노드 방향이 이 값 이상 비슷해야 이동합니다.")]
    [Range(0f, 1f)]
    [SerializeField] private float directionThreshold = 0.55f;

    [Header("입력 기준")]
    [Tooltip("비워두면 월드 기준 WASD로 이동합니다. 카메라를 넣으면 카메라 기준 WASD가 됩니다.")]
    [SerializeField] private Transform cameraTransform;

    [Header("방향 판정 설정")]
    [Tooltip("켜면 W/S는 앞뒤 후보만, A/D는 좌우 후보만 선택합니다.")]
    [SerializeField] private bool useStrictInputAxis = true;

    [Header("디버그")]
    [SerializeField] private bool showDebugLog = true;

    private WireConnection currentConnection;
    private bool isMoving;

    private float splineProgress;
    private float splineMoveDirection;

    private enum InputAxisType
    {
        None,
        ForwardBack,
        LeftRight
    }

    private InputAxisType currentInputAxis = InputAxisType.None;

    public PoleNode CurrentNode => currentNode;
    public bool IsMoving => isMoving;

    private void Start()
    {
        if (currentNode != null)
        {
            transform.position = currentNode.Position;
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        if (isMoving)
        {
            MoveAlongSpline();
            return;
        }

        Vector3 inputDirection = GetInputDirection();

        if (inputDirection == Vector3.zero)
        {
            return;
        }

        WireConnection nextConnection = FindBestConnection(inputDirection);

        if (nextConnection != null && nextConnection.targetNode != null && nextConnection.wireSpline != null)
        {
            StartSplineMove(nextConnection);
        }
        else
        {
            if (showDebugLog)
            {
                Debug.LogWarning("이동 가능한 연결을 찾지 못했습니다.");
            }
        }
    }

    private void StartSplineMove(WireConnection connection)
    {
        currentConnection = connection;
        isMoving = true;

        Vector3 splineStart = GetSplinePosition(currentConnection.wireSpline, 0f);
        Vector3 splineEnd = GetSplinePosition(currentConnection.wireSpline, 1f);

        float distanceToStart = Vector3.Distance(currentNode.Position, splineStart);
        float distanceToEnd = Vector3.Distance(currentNode.Position, splineEnd);

        if (distanceToStart <= distanceToEnd)
        {
            splineProgress = 0f;
            splineMoveDirection = 1f;
        }
        else
        {
            splineProgress = 1f;
            splineMoveDirection = -1f;
        }

        if (showDebugLog)
        {
            Debug.Log(
                "이동 시작: " +
                currentNode.name +
                " -> " +
                currentConnection.targetNode.name +
                " / Spline: " +
                currentConnection.wireSpline.name
            );
        }
    }

    private Vector3 GetInputDirection()
    {
        Vector2 input = Vector2.zero;
        currentInputAxis = InputAxisType.None;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            input.y = 1f;
            currentInputAxis = InputAxisType.ForwardBack;
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            input.y = -1f;
            currentInputAxis = InputAxisType.ForwardBack;
        }
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            input.x = -1f;
            currentInputAxis = InputAxisType.LeftRight;
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            input.x = 1f;
            currentInputAxis = InputAxisType.LeftRight;
        }

        if (input == Vector2.zero)
        {
            return Vector3.zero;
        }

        if (cameraTransform != null)
        {
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            return (forward * input.y + right * input.x).normalized;
        }

        return new Vector3(input.x, 0f, input.y).normalized;
    }

    private WireConnection FindBestConnection(Vector3 inputDirection)
    {
        if (currentNode == null)
        {
            Debug.LogWarning("Current Node가 비어 있습니다.");
            return null;
        }

        if (currentNode.connections == null)
        {
            Debug.LogWarning("Current Node의 Connections가 비어 있습니다.");
            return null;
        }

        WireConnection bestConnection = null;
        float bestScore = float.MinValue;

        foreach (WireConnection connection in currentNode.connections)
        {
            if (connection == null)
            {
                continue;
            }

            if (connection.targetNode == null)
            {
                continue;
            }

            if (connection.wireSpline == null)
            {
                continue;
            }

            Vector3 directionToTarget = connection.targetNode.Position - currentNode.Position;
            directionToTarget.y = 0f;

            if (directionToTarget.sqrMagnitude <= 0.001f)
            {
                continue;
            }

            Vector3 targetDirection = directionToTarget.normalized;

            if (useStrictInputAxis)
            {
                if (!IsTargetInCorrectInputAxis(targetDirection))
                {
                    if (showDebugLog)
                    {
                        Debug.Log(
                            "방향 축 불일치로 제외: " +
                            connection.targetNode.name
                        );
                    }

                    continue;
                }
            }

            float distance = directionToTarget.magnitude;
            float dot = Vector3.Dot(inputDirection.normalized, targetDirection);

            if (dot < directionThreshold)
            {
                continue;
            }

            float score = dot * 10f - distance * 0.1f;

            if (score > bestScore)
            {
                bestScore = score;
                bestConnection = connection;
            }

            if (showDebugLog)
            {
                Debug.Log(
                    "후보: " +
                    connection.targetNode.name +
                    " / Dot: " +
                    dot +
                    " / Distance: " +
                    distance
                );
            }
        }

        if (bestConnection != null && showDebugLog)
        {
            Debug.Log("선택된 이동 대상: " + bestConnection.targetNode.name);
        }

        return bestConnection;
    }

    private bool IsTargetInCorrectInputAxis(Vector3 targetDirection)
    {
        Vector3 forward;
        Vector3 right;

        if (cameraTransform != null)
        {
            forward = cameraTransform.forward;
            right = cameraTransform.right;
        }
        else
        {
            forward = Vector3.forward;
            right = Vector3.right;
        }

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        float forwardAmount = Mathf.Abs(Vector3.Dot(targetDirection, forward));
        float rightAmount = Mathf.Abs(Vector3.Dot(targetDirection, right));

        if (currentInputAxis == InputAxisType.ForwardBack)
        {
            return forwardAmount >= rightAmount;
        }

        if (currentInputAxis == InputAxisType.LeftRight)
        {
            return rightAmount > forwardAmount;
        }

        return true;
    }

    private void MoveAlongSpline()
    {
        if (currentConnection == null || currentConnection.wireSpline == null)
        {
            isMoving = false;
            return;
        }

        float wireLength = currentConnection.wireSpline.CalculateLength();

        if (wireLength <= 0.01f)
        {
            if (showDebugLog)
            {
                Debug.LogWarning("Spline 길이가 너무 짧습니다: " + currentConnection.wireSpline.name);
            }

            isMoving = false;
            return;
        }

        float moveAmount = (moveSpeed / wireLength) * Time.deltaTime;

        splineProgress += splineMoveDirection * moveAmount;
        splineProgress = Mathf.Clamp01(splineProgress);

        Vector3 splinePosition = GetSplinePosition(currentConnection.wireSpline, splineProgress);
        transform.position = splinePosition;

        if (splineProgress <= 0f || splineProgress >= 1f)
        {
            FinishMove();
        }
    }

    private Vector3 GetSplinePosition(SplineContainer splineContainer, float progress)
    {
        return splineContainer.EvaluatePosition(progress);
    }

    private void FinishMove()
    {
        if (currentConnection != null && currentConnection.targetNode != null)
        {
            currentNode = currentConnection.targetNode;
            transform.position = currentNode.Position;

            if (showDebugLog)
            {
                Debug.Log("도착 완료. 현재 노드: " + currentNode.name);
            }
        }

        currentConnection = null;
        isMoving = false;
        splineMoveDirection = 0f;
    }

    public void SetCurrentNode(PoleNode node, bool snapToNode = true)
    {
        currentNode = node;
        currentConnection = null;
        isMoving = false;
        splineProgress = 0f;
        splineMoveDirection = 0f;

        if (snapToNode && currentNode != null)
        {
            transform.position = currentNode.Position;
        }
    }
}