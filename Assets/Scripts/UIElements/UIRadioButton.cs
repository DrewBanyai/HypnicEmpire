using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class UIRadioButton : MonoBehaviour
{
    [SerializeField] public bool Selected;
    [SerializeField] public Sprite SelectedIcon;
    [SerializeField] public Sprite UnselectedIcon;

    private Image _image;

    public void Start()
    {
        _image = GetComponent<Image>();
        SetSelected(Selected);
    }

    public bool GetSelected()
    {
        return Selected;
    }

    public void SetSelected(bool isSelected)
    {
        Selected = isSelected;

        _image.sprite = Selected ? SelectedIcon : UnselectedIcon;
    }
}
