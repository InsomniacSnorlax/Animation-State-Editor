using UnityEngine;
using Snorlax.AnimationHash;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class TestAnimatorNames : MonoBehaviour
{
    [SerializeField] HashKeys hashKeys;
    [Name("hashKeys")]
    public string AnimationName;

    [SerializeField] public string Testing;
}

#if UNITY_EDITOR
[CustomEditor(typeof(TestAnimatorNames))]
public class TestAnimationNameEditor : Editor
{
    TestAnimatorNames board;

    private void OnEnable()
    {
        board = (TestAnimatorNames)target;
    }

    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();

        board.Testing = EditorGUILayout.TextField(board.Testing);

        if (GUILayout.Button("Debug"))
        {
            Debug.Log(board.Testing);
        }
    }
}
#endif