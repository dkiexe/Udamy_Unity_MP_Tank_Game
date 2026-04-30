using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItem : MonoBehaviour
{
    [SerializeField] private Image itemImage;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI DescriptionText;

    public void Initalize(Sprite itemSprite, int cost, string description)
    {
        itemImage.sprite = itemSprite;
        costText.text = "Cost : " + cost;
        DescriptionText.text = description;
    }
}
