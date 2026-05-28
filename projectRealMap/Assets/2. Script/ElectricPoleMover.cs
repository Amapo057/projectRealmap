using UnityEngine;

public class ElectricPoleMover : MonoBehaviour
{
    [Header("현재 서 있는 전봇대 노드")]
    [SerializeField] private PoleNode currentNode;

    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 8f;

    [Tooltip("입력 방향과 후보 노드 방향이 이 값 이상 비슷해야 이동합니다.")]
    [Range(0f, 1f)]
    [SerializeField] private float directionThreshold = 0.5f;

    [Header("입력 기준")]
    [Tooltip("비워두면 월드 기준 WASD로 이동합니다. 카메라를 넣으면 카메라 기준 WASD가 됩니다.")]
    [SerializeField] private Transform cameraTransform;

    private PoleNode targetNode;
    private bool isMoving;

    private ElectricWireEffect electricEffect;

    public PoleNode CurrentNode => currentNode;
    public bool IsMoving => isMoving;

    private void Start()
    {
        electricEffect = GetComponent<ElectricWireEffect>();

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
            MoveToTarget();

            if (electricEffect != null)
            {
                electricEffect.PlayMovingElectricity(1f);
            }

            return;
        }

        if (electricEffect != null)
        {
            electricEffect.PlayIdleElectricity();
        }

        Vector3 inputDirection = GetInputDirection();

        if (inputDirection == Vector3.zero)
        {
            return;
        }

        PoleNode nextNode = FindBestNode(inputDirection);

        if (nextNode != null)
        {
            targetNode = nextNode;
            isMoving = true;
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

    private PoleNode FindBestNode(Vector3 inputDirection)
    {
        if (currentNode == null || currentNode.connectedNodes == null)
        {
            return null;
        }

        PoleNode bestNode = null;
        float bestScore = float.MinValue;

        foreach (PoleNode candidate in currentNode.connectedNodes)
        {
            if (candidate == null)
            {
                continue;
            }

            Vector3 directionToCandidate = candidate.Position - currentNode.Position;
            directionToCandidate.y = 0f;

            if (directionToCandidate.sqrMagnitude <= 0.001f)
            {
                continue;
            }

            float distance = directionToCandidate.magnitude;
            float dot = Vector3.Dot(inputDirection.normalized, directionToCandidate.normalized);

            if (dot < directionThreshold)
            {
                continue;
            }

            float score = dot * 10f - distance * 0.1f;

            if (score > bestScore)
            {
                bestScore = score;
                bestNode = candidate;
            }
        }

        return bestNode;
    }

    private void MoveToTarget()
    {
        if (targetNode == null)
        {
            isMoving = false;
            return;
        }

        Vector3 direction = targetNode.Position - transform.position;

        if (direction.sqrMagnitude > 0.001f)
        {
            Vector3 lookDirection = direction;
            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetNode.Position,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetNode.Position) <= 0.05f)
        {
            transform.position = targetNode.Position;
            currentNode = targetNode;
            targetNode = null;
            isMoving = false;
        }
    }

    public void SetCurrentNode(PoleNode node, bool snapToNode = true)
    {
        currentNode = node;
        targetNode = null;
        isMoving = false;

        if (snapToNode && currentNode != null)
        {
            transform.position = currentNode.Position;
        }
    }
}