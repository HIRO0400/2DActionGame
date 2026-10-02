using System;
using UnityEditor;
using UnityEngine;

public static class StageBackgroundConfiguration
{
    public static float Configure(GameObject background, Transform camera)
    {
        var panels = background.GetComponentsInChildren<ParallaxBackground>();
        Array.Sort(panels, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
        var sky = Array.Find(background.GetComponentsInChildren<SpriteRenderer>(), s => s.name == "Far");
        if (sky == null || panels.Length < 2) throw new InvalidOperationException("背景のFar画像または背景パネルが不足しています。");
        float width = sky.bounds.size.x;
        Sprite skySprite = sky.sprite;
        Material skyMaterial = sky.sharedMaterial;
        int skyLayer = sky.sortingLayerID;
        int skyOrder = sky.sortingOrder;
        Color skyColor = sky.color;
        Vector3 skyScale = sky.transform.localScale;
        float skyDepth = sky.transform.localPosition.z;
        float center = panels[panels.Length / 2].transform.position.x;
        foreach (var panel in panels)
        {
            var image = Array.Find(panel.GetComponentsInChildren<SpriteRenderer>(), s => s.name == "Far");
            // RectTransform's anchored position is recomputed on load. These are
            // world sprites, so remove UI layout/mask components with their object.
            if (image != null && image.transform is RectTransform)
            {
                UnityEngine.Object.DestroyImmediate(image.gameObject);
                image = null;
            }
            if (image == null)
            {
                var obj = new GameObject("Far", typeof(SpriteRenderer));
                obj.transform.SetParent(panel.transform, false);
                image = obj.GetComponent<SpriteRenderer>();
                image.sprite = skySprite;
                image.sharedMaterial = skyMaterial;
                image.sortingLayerID = skyLayer;
                image.sortingOrder = skyOrder;
                image.color = skyColor;
                image.transform.localScale = skyScale;
            }
            image.transform.localPosition = new Vector3(0, 0, skyDepth);
            PrefabUtility.RecordPrefabInstancePropertyModifications(image.transform);
            var settings = new SerializedObject(panel);
            settings.FindProperty("cameraTransform").objectReferenceValue = camera;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
        for (int i = 0; i < panels.Length; i++)
        {
            var position = panels[i].transform.position;
            position.x = center + (i - panels.Length / 2) * width;
            panels[i].transform.position = position;
            PrefabUtility.RecordPrefabInstancePropertyModifications(panels[i].transform);
        }
        var loop = new SerializedObject(background.GetComponent<BackgroundLooper>());
        loop.FindProperty("cameraTransform").objectReferenceValue = camera;
        loop.FindProperty("backgroundWidth").floatValue = width;
        var array = loop.FindProperty("backgrounds");
        array.arraySize = panels.Length;
        for (int i = 0; i < panels.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = panels[i].transform;
        loop.ApplyModifiedPropertiesWithoutUndo();
        return width;
    }
}
