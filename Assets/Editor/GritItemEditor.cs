using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GridItem))]
public class GritItemEditor : Editor
{
    #region Base item attributes
    SerializedProperty itemtype;
    SerializedProperty itemName;
    SerializedProperty itemSprite;
    SerializedProperty itemPrefab;
    #endregion

    #region example group 1
    bool exampleGroup1;
    SerializedProperty exampleAttribute1;
    #endregion

    #region example group 2
    bool exampleGroup2;
    SerializedProperty exampleAttribute2;
    SerializedProperty exampleAttribute3;
    #endregion

    private void OnEnable()
    {
        itemtype = serializedObject.FindProperty("itemtype");
        itemName = serializedObject.FindProperty("itemName");
        itemSprite = serializedObject.FindProperty("itemSprite");
        itemPrefab = serializedObject.FindProperty("itemPrefab");

        exampleAttribute1 = serializedObject.FindProperty("exampleAttribute1");
        exampleAttribute2 = serializedObject.FindProperty("exampleAttribute2");
        exampleAttribute3 = serializedObject.FindProperty("exampleAttribute3");
    }

    public override void OnInspectorGUI()
    {
        GridItem _gridItem = (GridItem)target;

        serializedObject.Update();

        EditorGUILayout.PropertyField(itemtype);
        EditorGUILayout.PropertyField(itemName);
        EditorGUILayout.PropertyField(itemSprite);
        EditorGUILayout.PropertyField(itemPrefab);

        exampleGroup1 = EditorGUILayout.BeginFoldoutHeaderGroup(exampleGroup1, "Example Group 1");
        if (exampleGroup1)
        {
            EditorGUILayout.PropertyField(exampleAttribute1);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        if (_gridItem.itemtype == GridItem.ItemType.ExampleGroup2)
        {
            exampleGroup2 = EditorGUILayout.BeginFoldoutHeaderGroup(exampleGroup2, "Example Group 2");
            if (exampleGroup2)
            {
                EditorGUILayout.PropertyField(exampleAttribute2);
                if (exampleAttribute2.boolValue)
                {
                    EditorGUILayout.PropertyField(exampleAttribute3);
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
