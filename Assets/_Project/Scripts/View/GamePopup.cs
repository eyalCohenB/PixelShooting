using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelShooting.View
{
    /// <summary>
    /// End-of-level popup: "Level Complete!" + Next, or "Out of Space!" + Retry.
    /// Goes on the full-screen Popup object (the dim background). The look and layout are set in
    /// code (Init), so the scene objects only need to exist and be wired: rounded card, coloured
    /// header ribbon, big title, subtitle, and a big rounded button that gently pulses.
    /// Sizes are in Canvas Scaler reference pixels (1125 x 2436).
    /// The button's listener is added once by GameController, so showing it allocates nothing.
    /// </summary>
    public class GamePopup : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;   // on this object: fades the dim + blocks taps
        [SerializeField] private RectTransform card;
        [SerializeField] private TMP_Text title;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text buttonLabel;
        [SerializeField] private float showTime = 0.35f;

        [Header("Look")]
        [SerializeField] private Vector2 cardSize = new Vector2(900f, 760f);
        [SerializeField] private Vector2 buttonSize = new Vector2(600f, 190f);
        [SerializeField] private Color dimColor = new Color(0.05f, 0.08f, 0.12f, 0.72f);
        [SerializeField] private Color cardColor = new Color32(255, 248, 235, 255);
        [SerializeField] private Color winColor = new Color32(76, 186, 84, 255);
        [SerializeField] private Color loseColor = new Color32(240, 108, 64, 255);
        [SerializeField] private Color titleColor = new Color32(255, 255, 255, 255);
        [SerializeField] private Color subtitleColor = new Color32(110, 84, 60, 255);

        private Image ribbon;     // coloured header band on the card (green / orange)
        private TMP_Text subtitle;
        private Image buttonImage;
        private RectTransform buttonRect;
        private bool initialized;

        public Button Button => button;
        public bool IsShown { get; private set; }

        /// <summary>Builds the look once (called by GameController.Awake; runs even if inactive).</summary>
        public void Init()
        {
            if (initialized) return;
            initialized = true;

            // Dim background (this object, full screen).
            var dim = GetComponent<Image>();
            if (dim != null) dim.color = dimColor;

            // Card: centred rounded panel with a soft drop shadow.
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = new Vector2(0f, 60f);
            card.sizeDelta = cardSize;
            Image cardImage = card.GetComponent<Image>();
            UiStyle.Rounded(cardImage, cardColor, 0.35f);
            UiStyle.Shadow(card.gameObject, new Vector2(0f, -14f), 0.35f);

            // Ribbon: the coloured top band, behind the title.
            var ribbonGo = new GameObject("Ribbon", typeof(RectTransform), typeof(Image));
            var ribbonRect = (RectTransform)ribbonGo.transform;
            ribbonRect.SetParent(card, false);
            ribbonRect.SetAsFirstSibling();
            ribbonRect.anchorMin = new Vector2(0f, 1f);
            ribbonRect.anchorMax = new Vector2(1f, 1f);
            ribbonRect.pivot = new Vector2(0.5f, 1f);
            ribbonRect.anchoredPosition = Vector2.zero;
            ribbonRect.sizeDelta = new Vector2(0f, 230f);
            ribbon = ribbonGo.GetComponent<Image>();
            UiStyle.Rounded(ribbon, winColor, 0.35f);
            ribbon.raycastTarget = false;

            // Title: big, bold, white, on the ribbon.
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -25f);
            titleRect.sizeDelta = new Vector2(-60f, 180f);
            UiStyle.Text(title, 96f, titleColor, true);

            // Subtitle: a copy of the title, smaller, in the middle of the card.
            subtitle = Instantiate(title, card);
            subtitle.name = "Subtitle";
            var subRect = subtitle.rectTransform;
            subRect.anchorMin = subRect.anchorMax = new Vector2(0.5f, 0.5f);
            subRect.pivot = new Vector2(0.5f, 0.5f);
            subRect.anchoredPosition = new Vector2(0f, 40f);
            subRect.sizeDelta = new Vector2(cardSize.x - 120f, 160f);
            UiStyle.Text(subtitle, 56f, subtitleColor, false);

            // Button: big rounded pill at the bottom of the card, white bold label, pressed = darker.
            buttonRect = (RectTransform)button.transform;
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = new Vector2(0f, 70f);
            buttonRect.sizeDelta = buttonSize;
            buttonImage = button.GetComponent<Image>();
            UiStyle.Rounded(buttonImage, winColor, 0.3f);
            UiStyle.Shadow(button.gameObject, new Vector2(0f, -10f), 0.3f);
            UiStyle.ButtonColors(button);
            UiStyle.Fill(buttonLabel.rectTransform);
            UiStyle.Text(buttonLabel, 84f, Color.white, true);
        }

        public void Show(bool won)
        {
            Init();
            title.SetText(won ? "Level Complete!" : "Out of Space!");
            subtitle.SetText(won ? "Every pixel cleared.\nNice shooting!" : "The holder overflowed.\nTry a different order!");
            buttonLabel.SetText(won ? "Next" : "Retry");
            Color accent = won ? winColor : loseColor;
            ribbon.color = accent;
            buttonImage.color = accent;

            IsShown = true;
            gameObject.SetActive(true);
            KillTweens();
            group.alpha = 0f;
            group.blocksRaycasts = true;
            card.localScale = Vector3.one * 0.7f;
            buttonRect.localScale = Vector3.one;

            group.DOFade(1f, showTime);
            card.DOScale(1f, showTime).SetEase(Ease.OutBack);
            // A gentle "press me" pulse once the card has landed.
            buttonRect.DOScale(1.06f, 0.6f).SetDelay(showTime).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
        }

        public void HideNow()
        {
            IsShown = false;
            KillTweens();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        private void KillTweens()
        {
            group.DOKill();
            card.DOKill();
            if (buttonRect != null) buttonRect.DOKill();
        }
    }
}
