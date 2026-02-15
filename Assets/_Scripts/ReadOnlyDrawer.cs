using UnityEditor;
using UnityEngine;

// Questo script dice a Unity come disegnare la variabile nell'Inspector
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // 1. Disattiva l'interfaccia (rende tutto grigio e non cliccabile)
        GUI.enabled = false;

        // 2. Disegna la variabile normalmente (ma ora apparirà disattivata)
        EditorGUI.PropertyField(position, property, label);

        // 3. Riattiva l'interfaccia per le variabili successive
        GUI.enabled = true;
    }
}
