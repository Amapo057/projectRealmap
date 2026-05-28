using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("따라갈 대상")]
    public Transform target;

    [Header("카메라 거리")]
    public float distance = 16f;

    [Header("카메라 높이")]
    public float height = 10f;

    [Header("카메라 좌우 각도")]
    public float yaw = 0f;

    [Header("마우스 회전 속도")]
    public float mouseSensitivity = 3f;

    [Header("부드럽게 따라가는 정도")]
    public float smoothSpeed = 8f;

    [Header("바라볼 높이")]
    public float lookHeight = 1.5f;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        // 마우스 오른쪽 버튼을 누른 상태에서만 카메라 회전
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            yaw += mouseX * mouseSensitivity;
        }

        // Player의 회전은 사용하지 않습니다.
        // 카메라는 저장된 yaw 값과 target 위치만 사용합니다.
        Quaternion cameraRotation = Quaternion.Euler(0f, yaw, 0f);

        Vector3 offset = cameraRotation * new Vector3(0f, height, -distance);
        Vector3 desiredPosition = target.position + offset;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );

        Vector3 lookTarget = target.position + Vector3.up * lookHeight;
        transform.LookAt(lookTarget);
    }
}