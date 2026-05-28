using System.Collections.Generic;
using UnityEngine;

public class PoleNode : MonoBehaviour
{
    [Header("�� �����뿡�� �̵� ������ ���� ����")]
    public List<WireConnection> connections = new List<WireConnection>();

    [Header("���� ��ġ")]
    [Tooltip("����θ� �� ������Ʈ�� ��ġ�� ���� ��ġ�� ����մϴ�.")]
    public Transform movePoint;

    public Vector3 Position
    {
        get
        {
            if (movePoint != null)
            {
                return movePoint.position;
            }

            return transform.position;
        }
    }

    private void Reset()
    {
        movePoint = transform;
    }

    private void OnDrawGizmos()
    {
        Vector3 start;

        if (movePoint != null)
        {
            start = movePoint.position;
        }
        else
        {
            start = transform.position;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(start, 0.25f);

        if (connections == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;

        foreach (WireConnection connection in connections)
        {
            if (connection == null)
            {
                continue;
            }

            if (connection.targetNode == null)
            {
                continue;
            }

            Vector3 end = connection.targetNode.Position;

            Gizmos.DrawLine(start, end);
        }
    }
}