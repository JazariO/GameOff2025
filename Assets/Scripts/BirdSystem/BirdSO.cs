using UnityEngine;

[CreateAssetMenu(fileName = "BirdSO", menuName = "Bird SO")]
public class BirdSO : ScriptableObject
{
    public BirdType birdType;
    public float moveSpeed = 5f;
    public float accelation = 2f;
}

public enum BirdType
{
    Magpie,
    Kookaburra,
    Lyrebird,
    EasternRosella,
    CommonMyna,
    RedWattleBird,
    SulfurCockatoo,
    Ibis,
    CrestedPigeon,
    MaxCount
}
