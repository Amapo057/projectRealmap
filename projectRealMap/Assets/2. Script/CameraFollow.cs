using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("���� ���")]
    public Transform target;

    [Header("ī�޶� �Ÿ�")]
    public float distance = 16f;

    [Header("ī�޶� ����")]
    public float height = 10f;

    [Header("ī�޶� �¿� ����")]
    public float yaw = 0f;

    [Header("���콺 ȸ�� �ӵ�")]
    public float mouseSensitivity = 3f;

    [Header("�ε巴�� ���󰡴� ����")]
    public float smoothSpeed = 8f;

    [Header("�ٶ� ����")]
    public float lookHeight = 1.5f;

    void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        // ���콺 ������ ��ư�� ���� ���¿����� ī�޶� ȸ��
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            yaw += mouseX * mouseSensitivity;
        }

        // �߿�:
        // Player�� forward, rotation�� ���� ������� �ʽ��ϴ�.
        // �׷��� W/S�� ������ ī�޶� Player �ڷ� �ڵ� �̵����� �ʽ��ϴ�.
        Quaternion fixedRotation = Quaternion.Euler(0f, yaw, 0f);

        Vector3 offset = fixedRotation * new Vector3(0f, height, -distance);

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