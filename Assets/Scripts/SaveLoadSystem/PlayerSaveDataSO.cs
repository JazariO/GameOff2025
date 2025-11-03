using UnityEngine;
//using Proselyte.Persistence;

[CreateAssetMenu(fileName = "PlayerSaveDataSO", menuName = "Save-Load System/Player Save Data SO")]
public class PlayerSaveDataSO : ScriptableObject
{
    //public PersistentID persistentId;
    public Vector3 position;
    public Vector3 cameraPosition;
    public Quaternion cameraRotation;
    public bool isCrouched;
    public bool wishStand;
    //public bool isMantling;
    //public Vector3 mantleTargetPos;
    //public bool mantleWasCrouched;
}
