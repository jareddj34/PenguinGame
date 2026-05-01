using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Items/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite itemSprite;
    public string yarnNodeName;
}
