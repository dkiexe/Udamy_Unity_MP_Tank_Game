using TMPro;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "ShopUpgrade", menuName = "Scriptable Objects/ShopUpgrade")]
public class UpgradeData : ScriptableObject
{
    [SerializeField] private Image itemImage;
    [SerializeField] private int costText;
    [SerializeField] private string DescriptionText;
}
