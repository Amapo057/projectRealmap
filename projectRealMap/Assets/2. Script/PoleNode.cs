using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

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

    public void ClearConnections()
    {
        if (connections == null)
        {
            connections = new List<WireConnection>();
        }

        connections.Clear();
    }

    public void AddConnection(PoleNode targetNode, SplineContainer wireSpline, bool reverseSpline = false)
    {
        if (targetNode == null)
        {
            return;
        }

        if (wireSpline == null)
        {
            return;
        }

        if (connections == null)
        {
            connections = new List<WireConnection>();
        }

        for (int i = connections.Count - 1; i >= 0; i--)
        {
            WireConnection connection = connections[i];

            if (connection == null)
            {
                connections.RemoveAt(i);
                continue;
            }

            if (connection.targetNode == targetNode)
            {
                connections.RemoveAt(i);
            }
        }

        WireConnection newConnection = new WireConnection();
        newConnection.targetNode = targetNode;
        newConnection.wireSpline = wireSpline;
        newConnection.reverseSpline = reverseSpline;

        connections.Add(newConnection);
    }

    private void OnDrawGizmos()
    {
        Vector3 start = Position;

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

            Gizmos.DrawLine(start, connection.targetNode.Position);
        }
    }
}