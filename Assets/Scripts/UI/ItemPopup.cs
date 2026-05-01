using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ItemPopup : MonoBehaviour
{

    public GameObject popupImage;
    public Image itemImage;
    public Animator animator;

    private void Start()
    {
        GameEvents.Instance.OnItemPickedUp += Show;
    }

    private void OnDisable()
    {
        GameEvents.Instance.OnItemPickedUp -= Show;
    }

    public void Show(ItemData itemData)
    {
        itemImage.sprite = itemData.itemSprite;
        popupImage.SetActive(true);
    }

    public void Hide()
    {
        animator.SetTrigger("End");
        StartCoroutine(HideAfterAnimation());
    }
    IEnumerator HideAfterAnimation()
    {
        yield return new WaitForSeconds(1f); // Match this to your animation length
        popupImage.SetActive(false);
    }
}
