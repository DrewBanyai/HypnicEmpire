using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace HypnicEmpire
{
    public class UITooltip : MonoBehaviour
    {
        private static UITooltip Instance;

        [SerializeField] public GameObject TooltipObject;
        [SerializeField] public TextMeshProUGUI TitleText;
        [SerializeField] public TextMeshProUGUI ContentText;
        [SerializeField] public RectTransform TooltipRect;

        private Canvas _canvas;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                _canvas = GetComponentInParent<Canvas>();
                HideInternal();
            }
            else
            {
                Debug.LogWarning($"UITooltip: Destroying duplicate tooltip instance on GameObject '{gameObject.name}'. If this object was NOT intended to be a tooltip holder, please remove the UITooltip script from it.");
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (TooltipObject != null && TooltipObject.activeSelf)
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();
                
                //  Convert screen space to local space of the parent to handle scaling correctly
                RectTransform parentRect = TooltipRect.parent as RectTransform;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect, 
                    mousePosition, 
                    _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera, 
                    out Vector2 localPoint))
                {
                    //  Apply offset in local space (pixels scaled by canvas)
                    TooltipRect.localPosition = localPoint + new Vector2(8, -8);
                }
                
                //  Simple screen bounds clamping (stays in screen space for simplicity)
                Vector2 pivot = new Vector2(0, 1);
                if (mousePosition.x + TooltipRect.rect.width * _canvas.scaleFactor > Screen.width) pivot.x = 1;
                if (mousePosition.y - TooltipRect.rect.height * _canvas.scaleFactor < 0) pivot.y = 0;
                TooltipRect.pivot = pivot;
            }
        }

        public static void Show(string title, string content)
        {
            Instance?.ShowInternal(title, content);
        }

        public static void Hide()
        {
            Instance?.HideInternal();
        }

        private void ShowInternal(string title, string content)
        {
            if (TooltipObject == null) return;
            TooltipObject.SetActive(true);
            TitleText?.SetText(title);
            ContentText?.SetText(content);
        }

        private void HideInternal()
        {
            TooltipObject?.SetActive(false);
        }
    }
}
