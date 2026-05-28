using UnityEngine;
using UnityEngine.Splines;

[System.Serializable]
public class WireConnection
{
    [Header("연결된 목적지 노드")]
    public PoleNode targetNode;

    [Header("이 노드와 목적지 노드를 잇는 전선 Spline")]
    public SplineContainer wireSpline;

    [Header("Spline을 반대로 따라가야 하는지")]
    public bool reverseSpline;
}