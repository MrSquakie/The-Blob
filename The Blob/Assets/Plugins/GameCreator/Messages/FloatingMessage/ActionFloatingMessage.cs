﻿namespace GameCreator.Messages
{
	using System.Collections;
	using System.Collections.Generic;
    using UnityEngine;
	using UnityEngine.Events;
    using GameCreator.Core;
    using GameCreator.Localization;

    #if UNITY_EDITOR
    using UnityEditor;
    #endif

    [AddComponentMenu("")]
	public class ActionFloatingMessage : IAction
	{
        public AudioClip audioClip;

        [LocStringNoPostProcess]
        public LocString message = new LocString();

        public Color color = Color.white;
        public float time = 2.0f;

        public TargetGameObject target = new TargetGameObject(TargetGameObject.Target.GameObject);
        public Vector3 offset = new Vector3(0, 2, 0);

        private bool forceStop = false;

        // EXECUTABLE: ----------------------------------------------------------------------------

        public override IEnumerator Execute(GameObject target, IAction[] actions, int index)
        {
            Transform targetTransform = this.target.GetTransform(target);
            if (targetTransform != null)
            {
                if (this.audioClip != null)
                {
                    AudioManager.Instance.PlaySound2D(this.audioClip);
                }

                FloatingMessageManager.Show(
                    this.message.GetText(), this.color,
                    targetTransform, this.offset, this.time
                );

                float waitTime = Time.time + this.time;
                WaitUntil waitUntil = new WaitUntil(() => Time.time > waitTime || this.forceStop);
                yield return waitUntil;

                if (this.audioClip != null)
                {
                    AudioManager.Instance.StopSound2D(this.audioClip);
                }
            }

            yield return 0;
        }

        public override void Stop()
        {
            this.forceStop = true;
        }

        #if UNITY_EDITOR
        public static new string NAME = "Messages/Floating Message";
        private const string NODE_TITLE = "Show message: {0}";

        // PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spAudioClip;
        private SerializedProperty spMessage;
        private SerializedProperty spColor;
        private SerializedProperty spTime;
        private SerializedProperty spTarget;
        private SerializedProperty spOffset;

        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
        {
            return string.Format(
                NODE_TITLE,
                (this.message.content.Length > 23
                    ? this.message.content.Substring(0, 20) + "..."
                    : this.message.content
                )
            );
        }

        protected override void OnEnableEditorChild()
        {
            this.spAudioClip = this.serializedObject.FindProperty("audioClip");
            this.spMessage = this.serializedObject.FindProperty("message");
            this.spColor = this.serializedObject.FindProperty("color");
            this.spTime = this.serializedObject.FindProperty("time");

            this.spTarget = this.serializedObject.FindProperty("target");
            this.spOffset = this.serializedObject.FindProperty("offset");
        }

        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();

            EditorGUILayout.PropertyField(this.spMessage);
            EditorGUILayout.PropertyField(this.spAudioClip);
            EditorGUILayout.PropertyField(this.spColor);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(this.spTime);

            if (this.spAudioClip.objectReferenceValue != null)
            {
                AudioClip clip = (AudioClip)this.spAudioClip.objectReferenceValue;
                if (!Mathf.Approximately(clip.length, this.spTime.floatValue))
                {
                    Rect btnRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.miniButton);
                    btnRect = new Rect(
                        btnRect.x + EditorGUIUtility.labelWidth,
                        btnRect.y,
                        btnRect.width - EditorGUIUtility.labelWidth,
                        btnRect.height
                    );

                    if (GUI.Button(btnRect, "Use Audio Length", EditorStyles.miniButton))
                    {
                        this.spTime.floatValue = clip.length;
                    }
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(this.spTarget);
            EditorGUILayout.PropertyField(this.spOffset);

            this.serializedObject.ApplyModifiedProperties();
        }
        #endif
    }
}
