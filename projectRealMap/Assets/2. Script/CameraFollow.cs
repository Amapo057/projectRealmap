using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("따라갈 대상")]
    public Transform target;

    [Header("카메라 위치 조절")]
    public Vector3 offset = new Vector3(0f, 4f, -7f);

    [Header("부드럽게 따라가는 정도")]
    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 targetPosition = target.position + offset;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            smoothSpeed * Time.deltaTime
        );

        transform.LookAt(target);
    }
}