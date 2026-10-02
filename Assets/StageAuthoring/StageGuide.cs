using UnityEngine;

public class StageGuide : MonoBehaviour
{
    public StageLayout layout;
    public Transform player;
    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;

    private void OnGUI()
    {
        if (layout == null || player == null) return;
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
        }
        foreach (var area in layout.areas)
        {
            if (player.position.x < area.fromX || player.position.x >= area.toX) continue;
            float width = Mathf.Min(650f, Screen.width - 24f);
            GUI.Box(new Rect(12, 12, width, 106), GUIContent.none);
            GUI.Label(new Rect(24, 18, width - 24, 28), area.title, titleStyle);
            GUI.Label(new Rect(24, 49, width - 24, 60), area.instruction, bodyStyle);
            break;
        }
    }
}
