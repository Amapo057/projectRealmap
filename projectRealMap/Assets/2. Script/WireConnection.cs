using UnityEngine;
using UnityEngine.Splines;

[System.Serializable]
public class WireConnection
{
    [Header("����� ������ ���")]
    public PoleNode targetNode;

    [Header("�� ���� ������ ��带 �մ� ���� Spline")]
    public SplineContainer wireSpline;

    [Header("Spline�� �ݴ�� ���󰡾� �ϴ���")]
    public bool reverseSpline;
}