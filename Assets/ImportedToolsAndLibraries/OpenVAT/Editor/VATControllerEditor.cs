// VATControllerEditor.cs
// Author: Luke Stilson

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VATController))]
public class VATControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var controller = (VATController)target;
        var anims = controller.Anims;
        int animCount = anims?.Count ?? 0;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("animationData"));

        EditorGUILayout.LabelField("Init Playback", EditorStyles.boldLabel);
        controller.playInitMode = (VATController.PlayInitMode)EditorGUILayout.EnumPopup("Mode", controller.playInitMode);

        if (controller.playInitMode == VATController.PlayInitMode.Single)
        {
            controller.singleAnimIndex = EditorGUILayout.IntSlider("Anim Index", controller.singleAnimIndex, 0, Mathf.Max(0, animCount - 1));
        }
        else if (controller.playInitMode == VATController.PlayInitMode.Sequence)
        {
            controller.seqStartIndex = EditorGUILayout.IntSlider("Sequence Start", controller.seqStartIndex, 0, Mathf.Max(0, animCount - 1));
            controller.seqEndIndex = EditorGUILayout.IntSlider("Sequence End", controller.seqEndIndex, 0, Mathf.Max(0, animCount - 1));
            controller.seqTransition = EditorGUILayout.FloatField("Transition Time", controller.seqTransition);
            controller.seqLoop = EditorGUILayout.Toggle("Loop Sequence", controller.seqLoop);
        }

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("Play Animation (Runtime Only)", EditorStyles.boldLabel);

        if (Application.isPlaying && anims != null && animCount > 0)
        {
            for (int i = 0; i < animCount; i++)
            {
                var anim = anims[i];
                if (GUILayout.Button($"Play '{anim.name}'"))
                {
                    controller.PlayIndex(i, 0.25f);
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Enter Play Mode to test animations.", MessageType.Info);
        }

        if (GUI.changed)
            EditorUtility.SetDirty(controller);
    }
}
