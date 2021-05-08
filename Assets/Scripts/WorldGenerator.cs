using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEditor;

public class WorldGenerator : MonoBehaviour
{
    [SerializeField] public GameManager GameManager;
    [SerializeField] public int Size = 10;
    [SerializeField] public float RoomSize = 14;
    [SerializeField] public Transform _roomHolder;
    [SerializeField] public GameObject _roomPrefab;


    public void Generate() {
        for(int y = -Size; y <= Size; y++) {
            for (int x = -Size; x <= Size; x++) {
                Vector3 position = new Vector3(x * RoomSize, 0, y * RoomSize);
                RoomController room = Instantiate(_roomPrefab, position, Quaternion.identity, _roomHolder).GetComponent<RoomController>();
                room.GridPos = new Vector2Int(x, y);
            }
        }
    }

    public void Clear() {
        Transform[] rooms = _roomHolder.GetComponentsInChildren<Transform>();

        for (int i = 1; i < rooms.Length; i++) {
            if (rooms[i] != null) DestroyImmediate(rooms[i].gameObject);
        }

        //foreach (Transform child in _roomHolder) {
        //    DestroyImmediate(child.gameObject);
        //}
    }


    public void SetAllDoorways(bool open) {
        RoomController[] rooms = _roomHolder.GetComponentsInChildren<RoomController>();

        for (int i = 0; i < rooms.Length; i++) {
            if (rooms[i] != null && rooms[i].GridPos != Vector2Int.zero) 
                rooms[i].SetAllDoorways(open, GameManager.InvisibleDoorwayMaterial, GameManager.BaseDoorwayMaterial);
        }
    }

    
}

//[CustomEditor(typeof(WorldGenerator))]
//class WorldGeneratorHelperEditor : Editor {
//    public override void OnInspectorGUI() {
//        DrawDefaultInspector();

//        WorldGenerator script = (WorldGenerator)target;

//        //EditorGUILayout.IntField("Size", script.Size);
//        //EditorGUILayout.FloatField("Room Size", script.RoomSize);
//        //EditorGUILayout. ("Size", script.Size);

//        if (GUILayout.Button("Regenerate")) {
//            script.Clear();
//            script.Generate();
//        }

//        if (GUILayout.Button("Generate")) {
//            script.Generate();
//        }

//        if (GUILayout.Button("Clear")) {
//            script.Clear();
//        }

//        if (GUILayout.Button("Open Doorways")) {
//            script.SetAllDoorways(false);
//        }
//        if (GUILayout.Button("Close Doorways")) {
//            script.SetAllDoorways(true);
//        }
//    }
//}
