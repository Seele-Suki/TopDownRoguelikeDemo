using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TopDownRoguelike.Menu.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class SelectionCardView : MonoBehaviour
    {
        [Header("组件引用")]
        [SerializeField] private Button button;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TMP_Text unavailableLabel;

        [Header("卡片颜色")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField]
        private Color selectedColor =
            new Color32(120, 210, 140, 255);
        [SerializeField]
        private Color unavailableColor =
            new Color32(110, 110, 110, 255);

        [SerializeField] private bool isAvailable = true;
        [SerializeField] private bool isInteractable = true;

        private bool isSelected;

        public bool IsAvailable => isAvailable;
        public bool IsInteractable => isInteractable;
        public bool IsSelected => isSelected;

        private void Awake()
        {
            CacheReferences();
            RefreshVisual();
        }

        private void Reset()
        {
            CacheReferences();
        }

        public void SetAvailable(bool available)
        {
            isAvailable = available;

            if (!isAvailable)
            {
                isSelected = false;
            }

            RefreshVisual();
        }

        public void SetInteractable(bool interactable)
        {
            isInteractable = interactable;
            RefreshVisual();
        }

        public void SetSelected(bool selected)
        {
            isSelected = isAvailable && selected;
            RefreshVisual();
        }

        public void AddClickListener(UnityAction callback)
        {
            CacheReferences();
            button.onClick.AddListener(callback);
        }

        private void CacheReferences()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (backgroundImage == null && button != null)
            {
                backgroundImage = button.targetGraphic as Image;
            }
        }

        private void RefreshVisual()
        {
            CacheReferences();

            if (button != null)
            {
                button.interactable =
                    isAvailable && isInteractable;
            }

            if (unavailableLabel != null)
            {
                unavailableLabel.gameObject.SetActive(!isAvailable);
            }

            if (backgroundImage == null)
            {
                return;
            }

            if (!isAvailable)
            {
                backgroundImage.color = unavailableColor;
            }
            else if (isSelected)
            {
                backgroundImage.color = selectedColor;
            }
            else
            {
                backgroundImage.color = normalColor;
            }
        }
    }
}