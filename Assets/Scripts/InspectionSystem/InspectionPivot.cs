#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class InspectionPivot : MonoBehaviour
{
#if UNITY_EDITOR
    private Camera playerCamera;
    private bool initPlayerCam;

    // Cache the frustum corners
    private Vector3[] cachedNearCorners = new Vector3[4];
    private Vector3[] cachedFarCorners = new Vector3[4];

    // Transformed corners
    private Vector3[] transformedNearCorners = new Vector3[4];
    private Vector3[] transformedFarCorners = new Vector3[4];

    private void OnDrawGizmosSelected()
    {
        if(!initPlayerCam)
        {
            if(FindPlayerCamera())
            {
                CacheFrustumCorners(playerCamera);
                initPlayerCam = true;  // Mark initialization complete
            }
        }

        // Draw the frustum using cached points if available
        if(initPlayerCam)
        {
            // Transform the cached corners to the InspectionPivot's position and rotation
            TransformFrustumCorners();

            // Perform raycasts and adjust the far plane
            AdjustFrustumWithRaycasts();

            // Draw the adjusted frustum
            Gizmos.color = Color.yellow;
            DrawAdjustedFrustum();
        }
    }

    private bool FindPlayerCamera()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if(player == null)
        {
            Debug.LogWarning("Player GameObject with tag 'Player' not found.");
            return false;
        }

        playerCamera = player.GetComponentInChildren<Camera>();

        if(playerCamera == null)
        {
            Debug.LogWarning("Player GameObject or its children are missing a Camera component.");
            return false;
        }

        return true;
    }

    private void CacheFrustumCorners(Camera camera)
    {
        // Get the near and far clip planes
        float nearClip = camera.nearClipPlane;
        float farClip = camera.farClipPlane;

        // Calculate frustum corners in camera's local space
        camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), nearClip, Camera.MonoOrStereoscopicEye.Mono, cachedNearCorners);
        camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), farClip, Camera.MonoOrStereoscopicEye.Mono, cachedFarCorners);

        // Transform corners to world space considering the full hierarchy
        for(int i = 0; i < 4; i++)
        {
            cachedNearCorners[i] = camera.transform.TransformPoint(cachedNearCorners[i]);
            cachedFarCorners[i] = camera.transform.TransformPoint(cachedFarCorners[i]);
        }

        // Reset frustum rotation and position to align along positive Z-axis
        // by transforming into the local space of the playerCamera's parent
        Transform playerCameraParent = playerCamera.transform.parent;
        if(playerCameraParent != null)
        {
            Matrix4x4 parentWorldToLocal = playerCameraParent.worldToLocalMatrix;
            for(int i = 0; i < 4; i++)
            {
                cachedNearCorners[i] = parentWorldToLocal.MultiplyPoint3x4(cachedNearCorners[i]);
                cachedFarCorners[i] = parentWorldToLocal.MultiplyPoint3x4(cachedFarCorners[i]);
            }
        }
        else
        {
            // If no parent, transform to world origin
            for(int i = 0; i < 4; i++)
            {
                cachedNearCorners[i] -= playerCamera.transform.position;
                cachedFarCorners[i] -= playerCamera.transform.position;
            }
        }
    }

    private void TransformFrustumCorners()
    {
        // Get the world-space position and rotation of the InspectionPivot
        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;

        // Build a transformation matrix for the InspectionPivot
        Matrix4x4 inspectionMatrix = Matrix4x4.TRS(position, rotation, Vector3.one);

        // Transform the frustum corners relative to the InspectionPivot
        for(int i = 0; i < 4; i++)
        {
            transformedNearCorners[i] = inspectionMatrix.MultiplyPoint3x4(cachedNearCorners[i]);
            transformedFarCorners[i] = inspectionMatrix.MultiplyPoint3x4(cachedFarCorners[i]);
        }
    }

    private void AdjustFrustumWithRaycasts()
    {
        float closestDistance = float.MaxValue;

        // Perform raycasts from near corners to far corners
        for(int i = 0; i < 4; i++)
        {
            Vector3 origin = transformedNearCorners[i];
            Vector3 direction = transformedFarCorners[i] - transformedNearCorners[i];
            float maxDistance = direction.magnitude;
            direction.Normalize();

            if(Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance))
            {
                if(hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                }
            }
            else
            {
                // If no hit, use the max distance
                if(maxDistance < closestDistance)
                {
                    closestDistance = maxDistance;
                }
            }
        }

        // Perform a fifth raycast along the InspectionPivot's forward direction
        Vector3 pivotOrigin = transform.position;
        Vector3 pivotDirection = transform.forward;

        if(Physics.Raycast(pivotOrigin, pivotDirection, out RaycastHit pivotHit))
        {
            if(pivotHit.distance < closestDistance)
            {
                closestDistance = pivotHit.distance;
            }
        }
        else
        {
            // If no hit, use a maximum distance (optional)
            float maxPivotDistance = 100f; // Adjust as needed
            if(maxPivotDistance < closestDistance)
            {
                closestDistance = maxPivotDistance;
            }
        }

        // Adjust the far plane based on the closest distance
        for(int i = 0; i < 4; i++)
        {
            Vector3 direction = transformedFarCorners[i] - transformedNearCorners[i];
            direction.Normalize();

            // Set the new far corner based on the closest distance
            transformedFarCorners[i] = transformedNearCorners[i] + direction * closestDistance;
        }
    }

    private void DrawAdjustedFrustum()
    {
        // Draw lines between the transformed near plane corners
        Gizmos.DrawLine(transformedNearCorners[0], transformedNearCorners[1]);
        Gizmos.DrawLine(transformedNearCorners[1], transformedNearCorners[2]);
        Gizmos.DrawLine(transformedNearCorners[2], transformedNearCorners[3]);
        Gizmos.DrawLine(transformedNearCorners[3], transformedNearCorners[0]);

        // Draw lines between the adjusted far plane corners
        Gizmos.DrawLine(transformedFarCorners[0], transformedFarCorners[1]);
        Gizmos.DrawLine(transformedFarCorners[1], transformedFarCorners[2]);
        Gizmos.DrawLine(transformedFarCorners[2], transformedFarCorners[3]);
        Gizmos.DrawLine(transformedFarCorners[3], transformedFarCorners[0]);

        // Connect the near and far planes
        for(int i = 0; i < 4; i++)
        {
            Gizmos.DrawLine(transformedNearCorners[i], transformedFarCorners[i]);
        }
    }
#endif
}