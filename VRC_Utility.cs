#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace SRTTbacon.VRCScript
{
    //汎用的なコードを記述するソースファイル
    public static class DragAndDropAreaUtility
    {
        //ドラッグされたGameObjectまたはアセットファイルを取得
        public static T GetObject<T>(string areaTitle = "Drag & Drop", float widthMin = 0, float height = 50) where T : Object
        {
            var objectReferences = GetObjects(areaTitle, widthMin, height);
            return objectReferences?.FirstOrDefault(o => o is T) as T;
        }
        public static bool GetObjects<T>(List<T> targetList, string areaTitle = "Drag & Drop", float widthMin = 0, float height = 50) where T : Object
        {
            var objectReferences = GetObjects(areaTitle, widthMin, height);
            if (objectReferences == null)
                return false;
            var targetObjectReferences = objectReferences.OfType<T>().ToList();
            if (targetObjectReferences.Count == 0)
                return false;
            targetList.AddRange(targetObjectReferences);
            return true;
        }
        public static Object[] GetObjects(string areaTitle = "Drag & Drop", float widthMin = 0, float height = 50)
        {
            var dropArea = GUILayoutUtility.GetRect(widthMin, height, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, areaTitle);
            if (!dropArea.Contains(Event.current.mousePosition))
                return null;
            var eventType = Event.current.type;
            if (eventType != EventType.DragUpdated && eventType != EventType.DragPerform)
                return null;
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (eventType != EventType.DragPerform)
                return null;
            DragAndDrop.AcceptDrag();
            Event.current.Use();
            return DragAndDrop.objectReferences;
        }
    }
    public static class StringExt
    {
        //文字列に特定の文字(大文字小文字を無視する)が存在するかを調べる
        public static bool Contains(this string self, string value, System.StringComparison comparisonType)
        {
            return self.IndexOf(value, comparisonType) != -1;
        }
    }
    public class VRC_Utility
    {
        //エディタウィンドウ内にリストを表示(soは基本的にthisを使う。Nameは変数名。nameof(変数名)で変数名を取得可能)
        public static void GUI_List(ScriptableObject so, string Name)
        {
            ScriptableObject scriptableObj = so;
            SerializedObject serialObj = new SerializedObject(scriptableObj);
            SerializedProperty serialProp = serialObj.FindProperty(Name);
            EditorGUILayout.PropertyField(serialProp, true);
            serialObj.ApplyModifiedProperties();
        }
        //親オブジェクトから子オブジェクトにかけての構造を文字列化
        public static string Get_Structure(Transform Child_Object, Transform Parent_Object, string Now_Struct = "NULL")
        {
            if (Now_Struct == "NULL")
                Now_Struct = Child_Object.name;
            if (Child_Object.parent == null || Child_Object.parent == Parent_Object)
                return Now_Struct;
            return Get_Structure(Child_Object.parent, Parent_Object, Child_Object.parent.name + "/" + Now_Struct);
        }
        //オブジェクトの親オブジェクトを取得(VRCAvatarDescriptorが存在するオブジェクトが親だと仮定)
        public static GameObject Get_Avatar_Object(Transform Child_Object)
        {
            if (Child_Object.gameObject.TryGetComponent(out VRCAvatarDescriptor _))
                return Child_Object.gameObject;
            else if (Child_Object.parent == null)
                return null;
            return Get_Avatar_Object(Child_Object.parent);
        }
    }
}
#endif