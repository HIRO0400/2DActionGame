using UnityEngine;
using UnityEngine.UI;

/// <summary>Player Prefab owns its HUD, so scene changes and regeneration recreate it.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Player))]
public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private Sprite cloneIcon;
    private Player player;
    private GameObject canvasObject;
    private Text capacityText;
    private Font font;
    private int displayedCapacity = -1;
    public string CapacityLabel => capacityText == null ? "" : capacityText.text;

    private void Awake()
    {
        player = GetComponent<Player>();
        font = UIFont.Font;
        canvasObject = new GameObject("PlayerHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        // No raycaster: these labels must never intercept gameplay mouse input.
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        var counter = Panel("CloneCapacity", new Vector2(0, 1), new Vector2(16, -16), new Vector2(176, 64));
        var icon = new GameObject("CloneIcon", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(counter, false);
        Place(icon.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(12, -10), new Vector2(44, 44));
        var image = icon.GetComponent<Image>();
        image.sprite = cloneIcon;
        image.preserveAspect = true;
        image.raycastTarget = false;
        capacityText = Label(counter, "Capacity", "", new Vector2(68, -8), new Vector2(100, 48), 32);

        var controls = Panel("Controls", Vector2.zero, new Vector2(16, 16), new Vector2(350, 136));
        Label(controls, "Heading", "操作方法", new Vector2(14, -8), new Vector2(320, 26), 20);
        Row(controls, "Move", "A / D・← / →", "移動", 38);
        Row(controls, "Jump", "Space", "ジャンプ", 68);
        Row(controls, "Clone", "左クリック", "クローン作成", 98);
        Refresh();
    }

    private RectTransform Panel(string name, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(canvasObject.transform, false);
        var rect = obj.GetComponent<RectTransform>();
        Place(rect, anchor, position, size);
        var image = obj.GetComponent<Image>();
        image.color = new Color(0.06f, 0.10f, 0.16f, 0.82f);
        image.raycastTarget = false;
        return rect;
    }

    private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private Text Label(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        Place(obj.GetComponent<RectTransform>(), new Vector2(0, 1), position, size);
        var text = obj.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.text = value;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.raycastTarget = false;
        return text;
    }

    private void Row(Transform parent, string name, string key, string description, float y)
    {
        Label(parent, name + "Key", key, new Vector2(14, -y), new Vector2(174, 28), 18);
        Label(parent, name + "Description", description, new Vector2(194, -y), new Vector2(142, 28), 18);
    }

    private void LateUpdate() => Refresh();
    private void Refresh()
    {
        if (displayedCapacity == player.CloneCapacity) return;
        displayedCapacity = player.CloneCapacity;
        capacityText.text = "×" + displayedCapacity;
    }

    private void OnDestroy()
    {
        if (canvasObject != null) Destroy(canvasObject);
    }
}
