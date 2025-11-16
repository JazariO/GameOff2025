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
        // Set rotation
        startpointTF.LookAt(endpointTF);

        // Calculate distance and scale to fit
        float scalar = Vector3.Distance(startpointTF.position, endpointTF.position);
        startpointTF.localScale = new Vector3(1,1, scalar);

        Bounds bounds = skinnedMeshRenderer.localBounds;
        bounds.center = new Vector3(0, 0, 0.5f);
        bounds.extents = new Vector3(0.04f, 0.04f, 0.5f);
        skinnedMeshRenderer.localBounds = bounds;
    }
}

[CustomEditor(typeof(PowercableSolver))]
public class PowercableSolverEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        PowercableSolver powercableSolver = (PowercableSolver)target;

        if(GUILayout.Button("Solve"))
        {
            powercableSolver.Solve();
        }
    }
}