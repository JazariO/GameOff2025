using UnityEngine;

[CreateAssetMenu(fileName = "BirdManagerStatsSOs", menuName = "Bird Manager Stats SOs")]
public class BirdManagerStatsSOs : ScriptableObject
{
    public BirdBrain[] birds = new BirdBrain[32];
}
