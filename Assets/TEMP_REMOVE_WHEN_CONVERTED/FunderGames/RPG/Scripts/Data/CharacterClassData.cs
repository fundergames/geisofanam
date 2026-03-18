using UnityEngine;

[CreateAssetMenu(fileName = "New Character Class", menuName = "FunderGames/Character Class")]
public class CharacterClassData : ScriptableObject
{
    [SerializeField] private string classDisplayName;
    [SerializeField] private string description;
    [SerializeField] private Sprite icon;
    
    public string ClassDisplayName => classDisplayName;
    public string Description => description;
    public Sprite Icon => icon;
}
