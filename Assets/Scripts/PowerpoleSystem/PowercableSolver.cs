using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PowercableSolver : MonoBehaviour
{
    [SerializeField] Transform startpointTF;
    [SerializeField] Transform endpointTF;
    [SerializeField] SkinnedMeshRenderer skinnedMeshRenderer;

    public void Solve()
    {
        // Store endpoint's world position
        Vector3 endpointWorldPos = endpointTF.position;

        // Set rotation
        startpointTF.LookAt(endpointTF);

        // Calculate distance and scale to fit
        float scalar = Vector3.Distance(startpointTF.position, endpointTF.position);
        startpointTF.localScale = new Vector3(1,1, scalar);

        // Restore endpoint's world position
        endpointTF.position = endpointWorldPos;

        Bounds bounds = skinnedMeshRenderer.localBounds;
        bounds.center = new Vector3(0, 0, 0.5f);
        bounds.extents = new Vector3(0.04f, 0.04f, 0.5f);
        skinnedMeshRenderer.localBounds = bounds;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(PowercableSolver))]
[CanEditMultipleObjects]
public class PowercableSolverEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if(GUILayout.Button("Solve"))
        {
            foreach(Object obj in targets)
            {
                PowercableSolver solver = obj as PowercableSolver;
                if(solver != null)
                {
                    solver.Solve();
                }
            }
        }
    }
}

#endif
