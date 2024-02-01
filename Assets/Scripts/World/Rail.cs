using System.Collections;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class Rail : MonoBehaviour
{
    public float Length;
    public Transform[] nodes;

    public Vector3 LinearPosition(int seg, float ratio) {
        Vector3 p1 = nodes[seg].position;
        Vector3 p2 = nodes[seg +1].position;
        return Vector3.Lerp(p1,p2, ratio);
    }
    public Quaternion Orientation(int seg, float ratio) {
        if (seg == nodes.Length - 1) {
            return Quaternion.Lerp(nodes[seg-1].rotation, nodes[seg].rotation, ratio);
        }
        if (seg == -1) {
            return Quaternion.Lerp(nodes[seg+2].rotation, nodes[seg+1].rotation, ratio);  
        }
        return Quaternion.Lerp(nodes[seg].rotation, nodes[seg+1].rotation, ratio);
    }

    private void Start() {
        Length = Vector3.Distance(nodes[1].position, nodes[^1].position);
    }


    private void OnDrawGizmos() {
            for (int i = 0; i < nodes.Length - 1; i++) {
                Handles.DrawDottedLine(nodes[i].position, nodes[i+1].position, 3.0f);
            }
    }
}
