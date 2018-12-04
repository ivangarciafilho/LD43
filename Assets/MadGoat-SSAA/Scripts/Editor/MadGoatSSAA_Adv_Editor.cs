using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace MadGoat_SSAA
{
    [CustomEditor(typeof(MadGoatSSAA_Adv))]
    public class MadGoatSSAA_Adv_Editor : MadGoatSSAA_Editor
    {

        public override void DrawRefreshButton()
        {
            if (GUILayout.Button("Refresh"))
            {
                (target as MadGoatSSAA_Adv).Refresh();
            }
        }
        public override void DrawTab2()
        {

            EditorGUILayout.PropertyField(screenshotPath, new GUIContent("Save path"));
            EditorGUILayout.PropertyField(useProductName);
            if (!useProductName.boolValue)
                EditorGUILayout.PropertyField(namePrefix, new GUIContent("File Name Prefix"));
            // the screenshot module
            EditorGUILayout.PropertyField(ImageFormat, new GUIContent("Output Image Format"));

            EditorGUI.indentLevel++;
            if (ImageFormat.enumValueIndex == 0)
                EditorGUILayout.PropertyField(JPGQuality, new GUIContent("JPG Quality"));
            if (ImageFormat.enumValueIndex == 2)
                EditorGUILayout.PropertyField(EXR32, new GUIContent("32-bit EXR"));

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
            screenshotTab = GUILayout.Toolbar(screenshotTab, new string[] { "Frame", "360 Panorama" });
            if (screenshotTab == 0)
            {
                EditorGUILayout.Separator();
                EditorGUILayout.PropertyField(screenshotSettings.FindPropertyRelative("outputResolution"), new GUIContent("Screenshot Resolution"));
                EditorGUILayout.PropertyField(screenshotSettings.FindPropertyRelative("screenshotMultiplier"), new GUIContent("Render Resolution Multiplier"));
                accent_style.fontSize = 12;
                GUILayout.Label("*Render Resolution: " + (target as MadGoatSSAA).screenshotSettings.outputResolution * (target as MadGoatSSAA).screenshotSettings.screenshotMultiplier, accent_style);

                EditorGUILayout.Separator();
                EditorGUILayout.PropertyField(screenshotSettings.FindPropertyRelative("useFilter"), new GUIContent("Use Filter"));
                if (screenshotSettings.FindPropertyRelative("useFilter").boolValue)
                {
                    screenshotSettings.FindPropertyRelative("sharpness").floatValue = EditorGUILayout.Slider("   Sharpness", screenshotSettings.FindPropertyRelative("sharpness").floatValue, 0, 1);
                }
                if (GUILayout.Button("Take Screenshot"))
                {
                   
                    MadGoatSSAA tg = (target as MadGoatSSAA);
                    if (screenshotSettings.FindPropertyRelative("useFilter").boolValue)
                        tg.TakeScreenshot(
                            tg.screenshotPath,
                            tg.screenshotSettings.outputResolution,
                            tg.screenshotSettings.screenshotMultiplier,
                            tg.screenshotSettings.sharpness
                            );
                    else
                        tg.TakeScreenshot(
                            tg.screenshotPath,
                            tg.screenshotSettings.outputResolution,
                            tg.screenshotSettings.screenshotMultiplier
                            );
                }
            }
            else
            {
                //EditorGUILayout.PropertyField(panoramaSettings, true);

                panoramaRes = (EditorPanoramaRes)EditorGUILayout.EnumPopup(new GUIContent("Panorama Face Resolution"), panoramaRes);

                panoramaSettings.FindPropertyRelative("panoramaSize").intValue = (int)panoramaRes;

                panoramaSettings.FindPropertyRelative("panoramaMultiplier").intValue = EditorGUILayout.IntSlider(new GUIContent("Resolution Multiplier"), panoramaSettings.FindPropertyRelative("panoramaMultiplier").intValue, 1, panoramaRes == EditorPanoramaRes.Square4096 ? 2 : 4);

                accent_style.fontSize = 12;

                GUILayout.Label("*Render Resolution: " + panoramaSettings.FindPropertyRelative("panoramaSize").intValue * panoramaSettings.FindPropertyRelative("panoramaMultiplier").intValue + " x " + panoramaSettings.FindPropertyRelative("panoramaSize").intValue * panoramaSettings.FindPropertyRelative("panoramaMultiplier").intValue + " x 6 faces", accent_style);

                EditorGUILayout.PropertyField(panoramaSettings.FindPropertyRelative("useFilter"));
                if (panoramaSettings.FindPropertyRelative("useFilter").boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(panoramaSettings.FindPropertyRelative("sharpness"));
                    EditorGUI.indentLevel--;
                }
                if (GUILayout.Button("Take Screenshot" ))
                {

                    MadGoatSSAA tg = (target as MadGoatSSAA);
                    if (panoramaSettings.FindPropertyRelative("useFilter").boolValue)
                        tg.TakePanorama(
                            tg.screenshotPath,
                            tg.panoramaSettings.panoramaSize,
                            tg.panoramaSettings.panoramaMultiplier,
                            tg.panoramaSettings.sharpness
                            );
                    else
                        tg.TakePanorama(
                           tg.screenshotPath,
                           tg.panoramaSettings.panoramaSize,
                           tg.panoramaSettings.panoramaMultiplier
                           );

                }
            }
        }
    }
}
