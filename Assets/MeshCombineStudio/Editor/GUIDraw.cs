using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MeshCombineStudio
{ 
    static public class GUIDraw
    {

        static public void DrawSpacer(float spaceBegin = 5, float height = 5, float spaceEnd = 5)
        {
            GUILayout.Space(spaceBegin - 1);
            EditorGUILayout.BeginHorizontal();
            GUI.color = new Color(0.5f, 0.5f, 0.5f, 1);
            GUILayout.Button("", GUILayout.Height(height));
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(spaceEnd);

            GUI.color = Color.white;
        }

        static public void Label(string label, int fontSize)
        {
            int fontSizeOld = EditorStyles.label.fontSize;
            EditorStyles.boldLabel.fontSize = fontSize;
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel, GUILayout.Height(fontSize + 6));
            EditorStyles.boldLabel.fontSize = fontSizeOld;
        }

        static public void LabelWidthUnderline(GUIContent guiContent, int fontSize, bool boldLabel = true)
        {
            int fontSizeOld = EditorStyles.label.fontSize;
            EditorStyles.boldLabel.fontSize = fontSize;
            EditorGUILayout.LabelField(guiContent, boldLabel ? EditorStyles.boldLabel : EditorStyles.label, GUILayout.Height(fontSize + 6));
            EditorStyles.boldLabel.fontSize = fontSizeOld;
            DrawUnderLine();
            GUILayout.Space(5);
        }

        static public void DrawUnderLine(float offsetY = 0)
        {
            Rect rect = GUILayoutUtility.GetLastRect();
            if (EditorGUIUtility.isProSkin) GUI.color = Color.grey; else GUI.color = Color.black;
            GUI.DrawTexture(new Rect(rect.x, rect.yMax + offsetY, rect.width, 1), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        static public void PropertyArray(SerializedProperty property, GUIContent arrayName, bool drawUnderLine = true, bool editArrayLength = true)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.indentLevel++;
            GUILayout.Space(0);
            Rect rect = GUILayoutUtility.GetLastRect();
            property.isExpanded = EditorGUI.Foldout(new Rect(rect.x, rect.y + 3, 25, 18), property.isExpanded, "");
            EditorGUILayout.PrefixLabel(new GUIContent(arrayName.text + " Size", arrayName.tooltip));
            
            if (property.isExpanded)
            {
                if (editArrayLength)
                {
                    EditorGUI.indentLevel -= 2;
                    property.arraySize = EditorGUILayout.IntField("", property.arraySize);
                    EditorGUI.indentLevel += 2;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel++;
                for (int i = 0; i < property.arraySize; i++)
                {
                    SerializedProperty elementProperty = property.GetArrayElementAtIndex(i);

                    EditorGUILayout.PropertyField(elementProperty);
                }
                EditorGUI.indentLevel--;
            }
            else EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }
    }
}