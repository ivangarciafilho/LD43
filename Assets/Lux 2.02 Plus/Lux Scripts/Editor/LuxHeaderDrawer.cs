using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomPropertyDrawer (typeof (LuxHeader))]
public class LuxHeaderDrawer : DecoratorDrawer {

	LuxHeader luxheader { get { return ((LuxHeader) attribute); } }

	public override void OnGUI (Rect position)
	{
		position.y += 10; // Add margin top
		EditorGUI.LabelField (position, luxheader.labeltext, EditorStyles.boldLabel);
	}

	public override float GetHeight () 
	{
		return (base.GetHeight() + 16); // Add margin top and bottom
	}
}
