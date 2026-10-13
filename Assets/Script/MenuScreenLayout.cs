using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Shared by the scene authoring tool and runtime status updates.
public static class MenuScreenLayout
{
    static readonly Color Ink = new Color(.055f, .09f, .15f);
    static readonly Color Card = new Color(.13f, .23f, .33f);
    public static Canvas Setup()
    {
        var canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = .5f;
        Camera.main.backgroundColor = Ink;
        return canvas;
    }
    public static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity;
        rect.anchoredPosition = position; rect.sizeDelta = size;
    }
    public static TMP_Text Text(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize)
    {
        var child = parent.Find(name);
        var text = child != null ? child.GetComponent<TMP_Text>() : new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        text.transform.SetParent(parent, false);
        Place(text.rectTransform, position, size);
        text.font = UIFont.TMPFont; text.text = value; text.fontSize = fontSize;
        text.enableAutoSizing = false; text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
        return text;
    }
    public static Button Button(Transform parent, string name, string value, Vector2 position, Vector2 size)
    {
        var child = parent.Find(name);
        var button = child != null ? child.GetComponent<Button>() : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
        button.transform.SetParent(parent, false); Style(button);
        Place(button.GetComponent<RectTransform>(), position, size);
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label == null) label = Text(button.transform, "Label", value, Vector2.zero, size, 24);
        label.text = value; label.font = UIFont.TMPFont; label.color = Color.white;
        label.fontSize = 24; label.enableAutoSizing = false; label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        label.margin = new Vector4(12, 8, 12, 8);
        return button;
    }
    public static void Style(Button button)
    {
        var image = button.GetComponent<Image>(); image.sprite = null; image.color = Card;
        var colors = button.colors;
        colors.normalColor = Color.white; colors.highlightedColor = new Color(.65f, .9f, 1);
        colors.pressedColor = new Color(.45f, .7f, .85f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(.5f, .55f, .6f); button.colors = colors;
    }
    public static void Title()
    {
        var canvas = Setup();
        var logo = canvas.transform.Find("Rogo"); if (logo != null) logo.gameObject.SetActive(false);
        Text(canvas.transform, "TitleHeading", "2D ACTION GAME", new Vector2(0, 205), new Vector2(800, 80), 48);
        Text(canvas.transform, "TitleSubtitle", "クローンを足場に、次のステージへ", new Vector2(0, 143), new Vector2(800, 44), 23);
        Button(canvas.transform, "ContinueButton", "続きから", new Vector2(0, 30), new Vector2(440, 64));
        Button(canvas.transform, "StartButton", "ステージを選ぶ", new Vector2(0, -50), new Vector2(440, 64));
        Button(canvas.transform, "HowToPlayButton", "遊び方", new Vector2(-116, -130), new Vector2(208, 56));
        Button(canvas.transform, "QuitButton", "終了", new Vector2(116, -130), new Vector2(208, 56));
    }
    public static void Selection(Button[] buttons, string[] scenes)
    {
        var canvas = Setup();
        Text(canvas.transform, "NameText", "ステージ選択", new Vector2(0, 235), new Vector2(800, 70), 36);
        Text(canvas.transform, "SelectionSubtitle", "ステージをクリアすると、次のステージが解放されます", new Vector2(0, 181), new Vector2(1000, 40), 20);
        int count = Mathf.Min(buttons.Length, scenes.Length), visible = 0;
        for (int i = 0; i < count; i++) if (buttons[i] != null && StageManager.CanLoadScene(scenes[i])) visible++;
        int item = 0;
        for (int i = 0; i < count; i++)
        {
            if (buttons[i] == null) continue;
            bool available = StageManager.CanLoadScene(scenes[i]); buttons[i].gameObject.SetActive(available);
            if (!available) continue;
            int row = item / 3, columns = Mathf.Min(3, visible - row * 3);
            var position = new Vector2((item % 3 - (columns - 1) * .5f) * 308, 65 - row * 152);
            buttons[i].transform.SetParent(canvas.transform, false);
            Style(buttons[i]); Place(buttons[i].GetComponent<RectTransform>(), position, new Vector2(280, 128));
            var label = buttons[i].GetComponentInChildren<TMP_Text>(true);
            if (label != null) { label.color = Color.white; label.raycastTarget = false; }
            item++;
        }
        var back = Button(canvas.transform, "Back", "タイトルへ戻る", Vector2.zero, new Vector2(280, 56)).GetComponent<RectTransform>();
        back.anchorMin = back.anchorMax = back.pivot = new Vector2(1, 0);
        back.anchoredPosition = new Vector2(-24, 80);
    }
}
