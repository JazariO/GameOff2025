using UnityEngine;

[CreateAssetMenu(fileName = "PlayerInputDataSO", menuName = "Player Input Data SO")]
public class PlayerInputDataSO : ScriptableObject
{
    public Vector3 input_move;
    public Vector2 input_look;
    public bool input_interact;
    public bool input_change_view;
}
