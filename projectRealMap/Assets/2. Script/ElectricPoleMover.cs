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
    [SerializeField] private float directionThreshold = 0.8f;

    [Header("입력 기준")]
    [Tooltip("비워두면 월드 기준 WASD로 이동합니다. 카메라를 넣으면 카메라 기준 WASD가 됩니다.")]
    [SerializeField] private Transform cameraTransform;

    private WireConnection currentConnection;
    private bool isMoving;

    private float splineProgress;
    private float splineMoveDirection;

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
    }

    private void StartSplineMove(WireConnection connection)
    {
        currentConnection = connection;
        isMoving = true;

        Vector3 splineStart = currentConnection.wireSpline.EvaluatePosition(0f);
        Vector3 splineEnd = currentConnection.wireSpline.EvaluatePosition(1f);

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
    }

    private Vector3 GetInputDirection()
    {
        Vector2 input = Vector2.zero;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            input.y = 1f;
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            input.y = -1f;
        }
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            input.x = -1f;
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            input.x = 1f;
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
        if (currentNode == null || currentNode.connections == null)
        {
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

            Vector3 directionToTarget = connection.targetNode.Position - currentNode.Position;
            directionToTarget.y = 0f;

            if (directionToTarget.sqrMagnitude <= 0.001f)
            {
                continue;
            }

            float distance = directionToTarget.magnitude;
            float dot = Vector3.Dot(inputDirection.normalized, directionToTarget.normalized);

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
        }

        return bestConnection;
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
            isMoving = false;
            return;
        }

        float moveAmount = (moveSpeed / wireLength) * Time.deltaTime;

        splineProgress += splineMoveDirection * moveAmount;
        splineProgress = Mathf.Clamp01(splineProgress);

        Vector3 splinePosition = currentConnection.wireSpline.EvaluatePosition(splineProgress);
        transform.position = splinePosition;

        if (splineProgress <= 0f || splineProgress >= 1f)
        {
            FinishMove();
        }
    }

    private void FinishMove()
    {
        if (currentConnection != null && currentConnection.targetNode != null)
        {
            currentNode = currentConnection.targetNode;
            transform.position = currentNode.Position;
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