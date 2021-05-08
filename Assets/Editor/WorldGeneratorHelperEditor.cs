using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WorldGenerator))]
class WorldGeneratorHelperEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        WorldGenerator script = (WorldGenerator)target;

        //EditorGUILayout.IntField("Size", script.Size);
        //EditorGUILayout.FloatField("Room Size", script.RoomSize);
        //EditorGUILayout. ("Size", script.Size);

        if (GUILayout.Button("Regenerate")) {
            script.Clear();
            script.Generate();
        }

        if (GUILayout.Button("Generate")) {
            script.Generate();
        }

        if (GUILayout.Button("Clear")) {
            script.Clear();
        }

        if (GUILayout.Button("Open Doorways")) {
            script.SetAllDoorways(false);
        }
        if (GUILayout.Button("Close Doorways")) {
            script.SetAllDoorways(true);
        }
    }
}
