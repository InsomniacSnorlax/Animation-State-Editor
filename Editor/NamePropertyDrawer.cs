using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Snorlax.AnimationHash
{
    [CustomPropertyDrawer(typeof(NameAttribute))]
    public class NamePropertyDrawer : PropertyDrawer
    {
        NameAttribute nameAttribute;
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);

            nameAttribute = GUIUtilities.GetAttribute<NameAttribute>(property);

            HashKeys hashKey = GetHashKey(property, nameAttribute.AttributeName);

            if (hashKey == null)
            {
                EditorGUI.HelpBox(rect, "No hashkey found", MessageType.Warning);
                return;
            }

            DrawProperties(rect, property, label, hashKey.Keys.ToList(), hashKey);

            EditorGUI.EndProperty();
        }

        private void DrawProperties(Rect rect, SerializedProperty property, GUIContent label, List<Key> keys, HashKeys hashKeys)
        {
            string name = property.stringValue;
            int index = 0;

            if (keys.Count < 1)
            {
                EditorGUI.HelpBox(rect, "No parameters found", MessageType.Warning);
                return;
            }


            for (int i = 0; i < keys.Count; i++)
            {
                if (name.Equals(keys[i].Name, System.StringComparison.Ordinal))
                {
                    index = i;
                    break;
                }
            }

            int newIndex = EditorGUI.Popup(rect, label.text, index, ReturnNames(hashKeys.Keys.ToList()));
            string newValue = keys[newIndex].Name;

            if (!property.stringValue.Equals(newValue, System.StringComparison.Ordinal))
            {
                property.stringValue = newValue;
            }
        }

        public string[] ReturnNames(List<Key> keys)
        {
            List<string> names = new List<string>();
            keys.ForEach(e => names.Add(e.Name));
            return names.ToArray();
        }

        private static HashKeys GetHashKey(SerializedProperty property, string name)
        {
            object target = GUIUtilities.GetTargetObjectWithProperty(property);

            FieldInfo fieldInfo = GUIUtilities.GetField(target, name);
            if (fieldInfo != null && fieldInfo.FieldType == typeof(HashKeys))
            {
                return fieldInfo.GetValue(target) as HashKeys;
            }

            return null;
        }
    }
}