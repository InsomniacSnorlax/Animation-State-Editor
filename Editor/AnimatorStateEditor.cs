using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using UnityEditor.Callbacks;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections;
using UnityEditor.Animations;

namespace Snorlax.AnimationHash
{
    [Serializable]
    public class AnimatorStateEditor : EditorWindow
    {
        #region Variables
        // Editor Values
        private string SearchString = "";
        private Vector2 scrollView = Vector2.zero;
        private int previousSelected = -1;
        private int currentSelected = -1;
        private int selectedHashKeys = -1;

        private const float height = 20f;
        private const float width = 100f;

        private static HashKeys currentHashkeys = null;
        private List<HashKeys> hashkeys = new List<HashKeys>();
        private List<AnimationClip> foundAnimations = new List<AnimationClip>();

        // Textures
        private Texture2D iconPlusMore;
        private Texture2D iconDelete;
        private GUIStyle toolbar;
        // Editor Events
        private Event currentEvent;
        private bool isEditName = false;
        private bool checkToggle;
        #endregion

        #region Default Methods
        [MenuItem("Snorlax's Tools/AnimationStateEditor")]
        public static void ShowWindow()
        {
            GetWindow<AnimatorStateEditor>("Animation State Editor");
        }

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            if (Selection.activeObject is HashKeys)
            {
                currentHashkeys = (HashKeys)Selection.activeObject;
                ShowWindow();
                return true;
            }
            return false;
        }

        private void OnEnable()
        {
            this.minSize = new Vector2(200, 300);

            FindAllHashKeys(currentHashkeys);
            iconPlusMore = EditorGUIUtility.FindTexture("d_Toolbar Plus More");
            iconDelete = EditorGUIUtility.FindTexture("d_Toolbar Minus");
        }

        private void OnGUI()
        {
            if (toolbar == null) toolbar = GUI.skin.FindStyle("toolbarbutton");

            currentEvent = Event.current;

            GUIUtilities.HorizontalWrapper(1, PopupList);

            InputHandler();

            GUIUtilities.VerticalWrapper(1, Main);

            if(currentHashkeys != null) EditorUtility.SetDirty(currentHashkeys);
        }

        #endregion

        #region Editor Window Methods

        public void PopupList()
        {
            if (hashkeys.Count <= 0)
            {
                GUILayout.Label("No state name scriptable objects found");
            }
            else
            {
                int newIndex = EditorGUILayout.Popup(selectedHashKeys, GetNames());

                if (selectedHashKeys != newIndex)
                {
                    currentHashkeys = hashkeys[newIndex];
                    Reset();
                }
            }

            if (GUILayout.Button(iconDelete, toolbar, GUILayout.Width(30)))
            {
                if (hashkeys.Count <= 0) return;
                string assetPath = AssetDatabase.GetAssetPath(new SerializedObject(hashkeys[selectedHashKeys]).targetObject);
                AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                FindAllHashKeys();
            }

            if (GUILayout.Button(iconPlusMore, toolbar, GUILayout.Width(30)))
            {
                PopupWindow.Show(new Rect(currentEvent.mousePosition.x, currentEvent.mousePosition.y, 20, 20), new SavePopupMenu() { action = FindAllHashKeys}); ;
            }
        }

        private void Main()
        {
            if (currentHashkeys == null) return;

            GUIUtilities.HorizontalWrapper(0, SearchBarAndMore);

            GUIUtilities.ScrollViewWrapper(ref scrollView, ListView);

        }

        private void ListView()
        {
            EditorGUI.BeginChangeCheck();
            currentHashkeys.animatorController = (RuntimeAnimatorController)EditorGUILayout.ObjectField("Animator Controller", currentHashkeys.animatorController, typeof(RuntimeAnimatorController), false);
            if(EditorGUI.EndChangeCheck())
            {
                Reset();
            }

            Color defaultColor = GUI.backgroundColor;

            for (int i = 0; i < currentHashkeys.Keys.Length; i++)
            {
                string hashName = currentHashkeys.Keys[i].Name;

                if (!GUIUtilities.StringContains(hashName, SearchString) && !String.IsNullOrEmpty(SearchString))
                    continue;

                bool missing = !foundAnimations.Contains(currentHashkeys.Keys[i].Clip);

                if (checkToggle)
                {
                    if (!missing)
                    {
                        GUI.backgroundColor = Color.green;
                    }
                    else
                    {
                        GUI.backgroundColor = Color.red;
                    }
                }
                else
                {
                    GUI.backgroundColor = currentSelected == i ? Color.blue : defaultColor;
                }

                Rect rect = EditorGUILayout.BeginHorizontal("Box", GUILayout.Height(height));
                {
                    GUI.backgroundColor = defaultColor;
                    if (isEditName == true && currentSelected == i && !missing)
                    {
                        UnityEditor.Animations.AnimatorController controller = currentHashkeys.animatorController as UnityEditor.Animations.AnimatorController;
                        controller.layers[currentHashkeys.Keys[i].LayerIndex].stateMachine.states[currentHashkeys.Keys[i].StateIndex].state.name = EditorGUILayout.TextField(hashName, GUILayout.Width(150));
                        currentHashkeys.Keys[i].Name = controller.layers[currentHashkeys.Keys[i].LayerIndex].stateMachine.states[currentHashkeys.Keys[i].StateIndex].state.name;
                    }
                    else
                    {
                        EditorGUILayout.LabelField(hashName, GUILayout.Width(150));
                    }

                    var returnNothing = (AnimationClip)EditorGUILayout.ObjectField(currentHashkeys.Keys[i].Clip, typeof(AnimationClip), false);

                    if (rect.Contains(currentEvent.mousePosition) && currentEvent.button == 0)
                    {
                        currentSelected = i;
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorUtility.SetDirty(currentHashkeys);
            }
        }

        private void InputHandler()
        {
            if (currentEvent.keyCode == KeyCode.Return)
            {
                isEditName = false;
                this.Repaint();
            }

            if (previousSelected != currentSelected)
            {
                isEditName = false;
                previousSelected = currentSelected;
                this.Repaint();
            }

            if (currentEvent.clickCount == 2)
            {
                isEditName = true;
            }
        }

        #endregion

        #region Utilities 
        public void SearchBarAndMore()
        {
            SearchString = GUILayout.TextField(SearchString, GUI.skin.FindStyle("ToolbarSeachTextField"));

            if (GUILayout.Button("Add States", toolbar, GUILayout.Width(70)))
            {
                if (currentHashkeys.animatorController == null) return;
                AddStates();
            }

            if (GUILayout.Button("Check", toolbar, GUILayout.Width(45)))
            {
                if (currentHashkeys.animatorController == null) return;
                checkToggle = !checkToggle;
                foundAnimations = currentHashkeys.animatorController.animationClips.ToList<AnimationClip>();
            }

            if (GUILayout.Button("Delete Missing", toolbar, GUILayout.Width(90)))
            {
                if (currentHashkeys.animatorController == null) return;
                DeleteMissing();
            }
        }

        private void DeleteMissing()
        {
            if (currentHashkeys == null) return;

            List<Key> newKeys = new List<Key>();

            currentHashkeys.Keys.ToList().ForEach(e => { if (foundAnimations.Contains(e.Clip)) newKeys.Add(e); });

            currentHashkeys.Keys = newKeys.ToArray();

            newKeys.Clear();
        }

        private void Reset()
        {
            if (currentHashkeys == null)
                return;

            selectedHashKeys = hashkeys.IndexOf(currentHashkeys);

            foundAnimations = currentHashkeys.animatorController.animationClips.ToList<AnimationClip>();

            checkToggle = false;
            isEditName = false;
            this.Repaint();
        }

        private void AddStates()
        {
            if (currentHashkeys == null) return;

            List<string> Contained = new List<string>();
            currentHashkeys.Keys.ToList().ForEach(e => Contained.Add(e.Name));
            

            UnityEditor.Animations.AnimatorController controller = currentHashkeys.animatorController as UnityEditor.Animations.AnimatorController;

            for(int layerIndex = 0; layerIndex < controller.layers.Length; layerIndex++)//(UnityEditor.Animations.AnimatorControllerLayer layer in controller.layers)
            {
                UnityEditor.Animations.AnimatorControllerLayer layer = controller.layers[layerIndex];

                for (int stateIndex = 0; stateIndex < layer.stateMachine.states.Length; stateIndex++)//foreach (ChildAnimatorState childState in layer.stateMachine.states)
                {
                    ChildAnimatorState childState = layer.stateMachine.states[stateIndex];
                    if (!Contained.Contains(childState.state.name))
                    {
                        
                        Key key = new Key() { Name = childState.state.name, Clip = childState.state.motion as AnimationClip, LayerIndex = layerIndex, StateIndex = stateIndex };
                        Debug.Log(key.Name);
                        currentHashkeys.Keys = currentHashkeys.Keys.AddItemToArray( key);
                    }
                }
            }
        }

        public string[] GetNames()
        {
            List<string> names = new List<string>();

            foreach (HashKeys board in hashkeys)
            {
                names.Add(board.name);
            }

            return names.ToArray();
        }

        private void FindAllHashKeys(HashKeys current = null)
        {
            hashkeys = GUIUtilities.GetAllInstances<HashKeys>();
            if (hashkeys.Count <= 0) return;
            currentHashkeys = current != null ? current : hashkeys[hashkeys.Count - 1];
            selectedHashKeys = hashkeys.IndexOf(currentHashkeys);
            Reset();
        }

        #endregion
    }

    public class SavePopupMenu : PopupWindowContent
    {
        public Action<HashKeys> action;

        private DefaultAsset targetFolder = null;
        public readonly string HashKeyPaths = "Assets/Animation Hash Keys/ScriptableObjects";
        public readonly string HashKeysKeyName = "HashKeySaves";
        private RuntimeAnimatorController runtimeAnimator;
        string InputName = "";

        public override Vector2 GetWindowSize()
        {
            return new Vector2(300, 115);
        }

        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.BeginVertical("Box");

            HandleSaveFolderEditorPref(HashKeysKeyName, HashKeyPaths, "HashKey");
            runtimeAnimator = (RuntimeAnimatorController)EditorGUILayout.ObjectField("Animator Controller", runtimeAnimator, typeof(RuntimeAnimatorController), false);
            GUIUtilities.HorizontalWrapper(1, CreateButton);

            EditorGUILayout.EndVertical();
        }

        private void CreateButton()
        {
            GUILayout.Label("HashKey Name", GUILayout.Width(120f));
            InputName = GUILayout.TextField(InputName);
            if (GUILayout.Button("Create", "toolbarbutton", GUILayout.Width(60f)))
            {
                var so = ScriptableObject.CreateInstance<HashKeys>();
                so.animatorController = runtimeAnimator;

                SaveScriptableObject(HashKeysKeyName, HashKeyPaths, so, InputName);
                action(null);
                editorWindow.Close();
            }
        }


        #region Save Methods
        public static void SaveScriptableObject(string keyName, string defaultPath, ScriptableObject saveObject, string name)
        {
            if (name == null)
            {
                Debug.Log("Empty Name");
                return;
            }

            string path = defaultPath;
            if (PlayerPrefs.HasKey(keyName)) path = PlayerPrefs.GetString(keyName);
            else PlayerPrefs.SetString(keyName, defaultPath);
            path += "/";
            if (!System.IO.Directory.Exists(path))
            {
                EditorUtility.DisplayDialog("The desired save folder doesn't exist",
                    "Make sure you select a valid folder", "Ok");
                return;
            }

            path += name;
            string fullPath = path + ".asset";
            if (System.IO.File.Exists(fullPath))
            {
                SaveScriptableObjectWithOtherName(path, saveObject);
            }
            else DoSaving(fullPath, saveObject);
        }

        private static void SaveScriptableObjectWithOtherName(string path, ScriptableObject saveObject, int i = 1)
        {
            int number = i;
            string newPath = path + "_" + number.ToString();
            string fullPath = newPath + ".asset";
            if (File.Exists(fullPath))
            {
                number++;
                SaveScriptableObjectWithOtherName(path, saveObject, number);
            }
            else
            {
                DoSaving(fullPath, saveObject);
            }
        }

        private static void DoSaving(string fileName, ScriptableObject saveObject)
        {
            AssetDatabase.CreateAsset(saveObject, fileName);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void HandleSaveFolderEditorPref(string keyName, string defaultPath, string logsFeatureName)
        {
            if (!PlayerPrefs.HasKey(keyName))
                PlayerPrefs.SetString(keyName, defaultPath);

            targetFolder = (DefaultAsset)AssetDatabase.LoadAssetAtPath(PlayerPrefs.GetString(keyName), typeof(DefaultAsset));

            if (targetFolder == null)
            {
                PlayerPrefs.SetString(keyName, defaultPath);
                targetFolder = (DefaultAsset)AssetDatabase.LoadAssetAtPath(PlayerPrefs.GetString(keyName), typeof(DefaultAsset));

                if (targetFolder == null)
                {
                    targetFolder = (DefaultAsset)AssetDatabase.LoadAssetAtPath("Assets/", typeof(DefaultAsset));
                    if (targetFolder == null)
                        Debug.LogWarning("The desired save folder doesn't exist. " + PlayerPrefs.GetString(keyName) +
                                                                     "\n Make sure to set a valid folder");
                    else
                        PlayerPrefs.SetString("Assets/", defaultPath);
                }
            }

            targetFolder = (DefaultAsset)EditorGUILayout.ObjectField("New " + logsFeatureName + " Folder", targetFolder, typeof(DefaultAsset), false);

            if (targetFolder != null && IsAssetAFolder(targetFolder))
            {
                string path = AssetDatabase.GetAssetPath(targetFolder); //EditorUtility.OpenFilePanel("Open Folder", "", "");
                PlayerPrefs.SetString(keyName, path);
                EditorGUILayout.HelpBox("Valid folder! ", MessageType.Info, true);
            }
            else EditorGUILayout.HelpBox("Select the new " + logsFeatureName + " Folder", MessageType.Warning, true);
        }

        private static bool IsAssetAFolder(UnityEngine.Object obj)
        {
            string path = "";

            if (obj == null) return false;

            path = AssetDatabase.GetAssetPath(obj.GetInstanceID());

            if (path.Length > 0)
            {
                if (Directory.Exists(path)) return true;
                else return false;
            }

            return false;
        }
        #endregion
    }

    public static class GUIUtilities
    {
        // From Yaell
        #region Wrappers
        public static void HorizontalWrapper(int indent, Action<Action> action, Action actions)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(indent);
            action(actions);
            GUILayout.EndHorizontal();
        }

        public static void HorizontalWrapper(int indent, Action action)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(indent);
            action();
            GUILayout.EndHorizontal();
        }

        public static void VerticalWrapper(int indent, Action action)
        {
            GUILayout.BeginVertical();
            GUILayout.Space(indent);
            action();
            GUILayout.EndVertical();
        }

        public static void VerticalWrapper(int indent, Action action, float minSize)
        {
            GUILayout.BeginVertical(GUILayout.MinWidth(minSize));
            GUILayout.Space(indent);
            action();
            GUILayout.EndVertical();
        }

        public static void AreaWrapper(Rect area, Action action)
        {
            GUILayout.BeginArea(area);
            action();
            GUILayout.EndArea();
        }

        public static void ScrollViewWrapper(ref Vector2 scrollPos, Action action)
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            action();
            GUILayout.EndScrollView();
        }
        #endregion

        public static bool StringContains(string source, string toCheck, StringComparison comp = StringComparison.OrdinalIgnoreCase)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }

        public static List<T> GetAllInstances<T>() where T : ScriptableObject
        {
            return AssetDatabase.FindAssets($"t: {typeof(T).Name}").ToList()
                        .Select(AssetDatabase.GUIDToAssetPath)
                        .Select(AssetDatabase.LoadAssetAtPath<T>)
                        .ToList();
        }

        public static T[] AddItemToArray<T>(this T[] original, T itemToAdd) {
            T[] finalArray = new T[original.Length + 1];
            for (int i = 0; i <original.Length; i++ ) {
                finalArray[i] = original[i];
            }
            finalArray[finalArray.Length - 1] = itemToAdd;
            return finalArray;
        }

        // From NaughtyAttributes
        #region Get Object 
        public static FieldInfo GetField(object target, string fieldName)
        {
            return GetAllFields(target, f => f.Name.Equals(fieldName, StringComparison.Ordinal)).FirstOrDefault();
        }

        public static object GetTargetObjectWithProperty(SerializedProperty property)
        {
            string path = property.propertyPath.Replace(".Array.data[", "[");
            object obj = property.serializedObject.targetObject;
            string[] elements = path.Split('.');

            for (int i = 0; i < elements.Length - 1; i++)
            {
                string element = elements[i];
                if (element.Contains("["))
                {
                    string elementName = element.Substring(0, element.IndexOf("["));
                    int index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetValue_Imp(obj, elementName, index);
                }
                else
                {
                    obj = GetValue_Imp(obj, element);
                }
            }

            return obj;
        }

        public static IEnumerable<FieldInfo> GetAllFields(object target, Func<FieldInfo, bool> predicate)
        {
            if (target == null)
            {
                Debug.LogError("The target object is null. Check for missing scripts.");
                yield break;
            }

            List<Type> types = GetSelfAndBaseTypes(target);

            for (int i = types.Count - 1; i >= 0; i--)
            {
                IEnumerable<FieldInfo> fieldInfos = types[i]
                    .GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Where(predicate);

                foreach (var fieldInfo in fieldInfos)
                {
                    yield return fieldInfo;
                }
            }
        }

        private static object GetValue_Imp(object source, string name)
        {
            if (source == null)
            {
                return null;
            }

            Type type = source.GetType();

            while (type != null)
            {
                FieldInfo field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (field != null)
                {
                    return field.GetValue(source);
                }

                PropertyInfo property = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (property != null)
                {
                    return property.GetValue(source, null);
                }

                type = type.BaseType;
            }

            return null;
        }

        private static object GetValue_Imp(object source, string name, int index)
        {
            IEnumerable enumerable = GetValue_Imp(source, name) as IEnumerable;
            if (enumerable == null)
            {
                return null;
            }

            IEnumerator enumerator = enumerable.GetEnumerator();
            for (int i = 0; i <= index; i++)
            {
                if (!enumerator.MoveNext())
                {
                    return null;
                }
            }

            return enumerator.Current;
        }

        private static List<Type> GetSelfAndBaseTypes(object target)
        {
            List<Type> types = new List<Type>()
            {
                target.GetType()
            };

            while (types.Last().BaseType != null)
            {
                types.Add(types.Last().BaseType);
            }

            return types;
        }

        public static T GetAttribute<T>(SerializedProperty property) where T : class
        {
            T[] attributes = GetAttributes<T>(property);
            return (attributes.Length > 0) ? attributes[0] : null;
        }

        public static T[] GetAttributes<T>(SerializedProperty property) where T : class
        {
            FieldInfo fieldInfo = GetField(GetTargetObjectWithProperty(property), property.name);
            if (fieldInfo == null)
            {
                return new T[] { };
            }

            return (T[])fieldInfo.GetCustomAttributes(typeof(T), true);
        }
        #endregion 
    }
}