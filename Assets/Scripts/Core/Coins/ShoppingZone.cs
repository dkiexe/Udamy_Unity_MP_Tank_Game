using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ShoppingZone : NetworkBehaviour
{
    [SerializeField] private ShopItem ShopItem;

    private GameObject ShopUI;
    private HorizontalLayoutGroup ShopItemsHolder;

    private ShopItem[] ShopItems;

    private void Start()
    {
        ShopUI = GameHUD.Instance.ShopUI;
        ShopItemsHolder = GameHUD.Instance.ShopItemsHolder;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsClient)
        {
            if (IsOwner)
            {
                if (collision.attachedRigidbody.TryGetComponent(out TankPlayer player))
                {
                    ToggleShopUI();
                    CreateShopItems();
                }
            }
        }
    }

    private void CreateShopItems()
    {
        GameObject shopItemGO = Instantiate(ShopItem.gameObject, ShopItemsHolder.transform);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (IsClient && IsOwner)
        {
            if (collision.attachedRigidbody.TryGetComponent(out TankPlayer player))
            {
                ToggleShopUI(); // {_(!)_} UI DRAG ISSUE HERE!
            }
        }
    }

    public void ToggleShopUI()
    {
        ShopUI.SetActive(!ShopUI.activeSelf);
    }
}
