using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;

public class GUIutilities : MonoBehaviour {

 

    public static void sliderMinMaxRange(string label, ref float min, ref float max, float sliderMin, float sliderMax)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(50));
        min = EditorGUILayout.FloatField("", min, GUILayout.Width(50));
        EditorGUILayout.MinMaxSlider(ref min, ref max, sliderMin, sliderMax);
        max = EditorGUILayout.FloatField("", max, GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();

    }


    public static bool SimpleFoldOut(string text, bool foldOut, bool bold = true, bool leftAligment = false, bool endSpace = true)
    {
        GUIStyle myToggleStyle = new GUIStyle(EditorStyles.label);
        Color myStyleColor = Color.black;
        myToggleStyle.fontStyle = FontStyle.Bold;
        myToggleStyle.fontSize = 11;
        //myFoldoutStyle.fontStyle = FontStyle.Bold;
        //myFoldoutStyle.normal.textColor = myStyleColor;
        //myFoldoutStyle.onNormal.textColor = myStyleColor;
        //myFoldoutStyle.hover.textColor = myStyleColor;
        //myFoldoutStyle.onHover.textColor = myStyleColor;
        //myFoldoutStyle.focused.textColor = myStyleColor;
        //myFoldoutStyle.onFocused.textColor = myStyleColor;
        //myFoldoutStyle.active.textColor = myStyleColor;
        //myFoldoutStyle.onActive.textColor = myStyleColor;

        if (foldOut)
        {
            text = "\u25BC " + text;
        }
        else
        {
            text = "\u25BA " + text;
        }

        if (!GUILayout.Toggle(true, text, myToggleStyle))
        {
            foldOut = !foldOut;
        }

        if (!foldOut && endSpace) GUILayout.Space(5f);

        return foldOut;
    }

    public static void simpleSlider(string label, ref float value, float min, float max, int widthLabel)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(100));
        EditorGUILayout.FloatField("", min, GUILayout.Width(50));
        value= EditorGUILayout.Slider(value, min, max);
        EditorGUILayout.FloatField("", max, GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();

    }
    public static void SliderMinMax(string label, ref float min, ref float max, ref float value, int widthLabel)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(widthLabel));
        min = EditorGUILayout.FloatField("", min, GUILayout.Width(50));
        value= EditorGUILayout.Slider(value, min, max);
        max= EditorGUILayout.FloatField("", max, GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();
    }

    public static void SliderMinMax(GUIContent content, ref float min, ref float max, ref float value, int widthLabel)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(content, GUILayout.Width(widthLabel));
        min = EditorGUILayout.FloatField("", min, GUILayout.Width(50));
        value = EditorGUILayout.Slider(value, min, max);
        max = EditorGUILayout.FloatField("", max, GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();
    }

    static public bool Button(string label, Color color, int width, int height = 0, bool leftAligment = false, Texture2D icon = null)
    {

        GUI.backgroundColor = color;
        GUIStyle buttonStyle = new GUIStyle("Button");

        if (leftAligment)
            buttonStyle.alignment = TextAnchor.MiddleLeft;

        GUIContent guiContent = new GUIContent(label, icon);
        if (height == 0)
        {
            if (GUILayout.Button(guiContent, buttonStyle, GUILayout.Width(width)))
            {
                GUI.backgroundColor = Color.white;
                return true;
            }
        }
        else
        {
            if (GUILayout.Button(guiContent, buttonStyle, GUILayout.Width(width), GUILayout.Height(height)))
            {
                GUI.backgroundColor = Color.white;
                return true;
            }
        }
        GUI.backgroundColor = Color.white;

        return false;
    }

    public static Component CopyComponent(Component original, GameObject destination)
    {
      
        System.Type type = original.GetType();
        Rigidbody copy = (Rigidbody)destination.AddComponent(type);

        EditorUtility.CopySerialized(original, copy);
        return copy; 
    }

    public static void WriteTitle(string text)
    {
        GUIStyle labelStyle = new GUIStyle("label");
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.fontSize = 11;
        EditorGUILayout.LabelField(text, labelStyle);
    }
}
