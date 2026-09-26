using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Dialog{
    /// Manages dynamic floating hover tooltip presentation and screen-to-canvas positioning.
    [DisallowMultipleComponent]
    public class DialogueTooltipUI : MonoBehaviour{
        private GameObject      _root;
        private RectTransform   _rect;
        private TextMeshProUGUI _text;
        private Canvas          _canvas;

        /// Hides tooltip on disable.
        private void OnDisable() => Hide();

        /// Displays hover tooltip near cursor with the given text.
        public void Show(Vector2 screenPosition, string text){
            if (string.IsNullOrEmpty(text)) return;
            EnsureHierarchy();

            _text.text = text;
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();

            if (_canvas && _canvas.renderMode != RenderMode.ScreenSpaceOverlay){
                Camera cam = _canvas.worldCamera ? _canvas.worldCamera : Camera.main;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas.transform as RectTransform, screenPosition, cam, out Vector2 localPos))
                    _rect.anchoredPosition = localPos + new Vector2(15f, 20f);
            }
            else _rect.position = screenPosition + new Vector2(15f, 20f);
        }

        /// Hides active tooltip.
        public void Hide() => _root?.SetActive(false);

        /// Constructs tooltip GameObject structure if not already built.
        private void EnsureHierarchy(){
            if (_root) return;

            _canvas = GetComponentInParent<Canvas>() ?? GetComponentInChildren<Canvas>() ?? FindFirstObjectByType<Canvas>();
            Transform parent = _canvas ? _canvas.transform : transform;

            _root = new GameObject("DialogueChoiceTooltip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            _root.transform.SetParent(parent, false);
            _rect = _root.GetComponent<RectTransform>();
            _rect.pivot = new Vector2(0f, 0f);

            CanvasGroup group = _root.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            Image bg = _root.GetComponent<Image>();
            bg.color = DialogueColorTheme.TooltipBackgroundColor;
            bg.raycastTarget = false;

            Outline outline = _root.AddComponent<Outline>();
            outline.effectColor = DialogueColorTheme.TooltipBorderColor;
            outline.effectDistance = new Vector2(1, -1);

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(_root.transform, false);

            _text = textObj.GetComponent<TextMeshProUGUI>();
            _text.fontSize = 17;
            _text.alignment = TextAlignmentOptions.Center;
            _text.color = new Color(0.96f, 0.84f, 0.43f, 1f);
            _text.raycastTarget = false;

            HorizontalLayoutGroup layout = _root.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 6, 6);
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            ContentSizeFitter fitter = _root.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _root.SetActive(false);
        }
    }
}
