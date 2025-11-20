using UnityEngine;

[CreateAssetMenu(fileName = "PlayerInputDataSO", menuName = "Player Input Data SO")]
public class PlayerInputDataSO : ScriptableObject
{
    public Vector2 input_look;
    public bool input_interact;
    public bool input_view_change;
}
