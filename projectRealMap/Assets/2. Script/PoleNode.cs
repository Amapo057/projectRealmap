using System.Collections.Generic;
using UnityEngine;

public class PoleNode : MonoBehaviour
{
    [Header("이 전봇대에서 이동 가능한 연결 정보")]
    public List<WireConnection> connections = new List<WireConnection>();

    [Header("도착 위치")]
    [Tooltip("비워두면 이 오브젝트의 위치를 도착 위치로 사용합니다.")]
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