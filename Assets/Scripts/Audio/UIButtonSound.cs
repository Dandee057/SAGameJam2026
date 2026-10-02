using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;


public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    [SerializeField] private string clickSound = "UIClick";
    [SerializeField] private string hoverSound = "UIHover";

    private Selectable selectable;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();

        if (TryGetComponent(out Button button))
            button.onClick.AddListener(PlayClick);
        else if (TryGetComponent(out Toggle toggle))
            toggle.onValueChanged.AddListener(_ => PlayClick());
    }

    public void OnPointerEnter(PointerEventData eventData) => PlayHover();
    public void OnSelect(BaseEventData eventData)
    {
        if (Mouse.current != null && Mouse.current.leftButton.isPressed) return;
        if (Time.timeSinceLevelLoad < 0.2f) return;
        PlayHover();
    }

    private void PlayClick()
    {
        if (!string.IsNullOrEmpty(clickSound) && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(clickSound);
    }

    private void PlayHover()
    {
        if (selectable != null && !selectable.interactable) return;
        if (!string.IsNullOrEmpty(hoverSound) && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(hoverSound);
    }
}
