//  OverteBlendshapeSelector.cs
//
//  Created by Edgar on 03-01-2026
//  Copyright 2026 Overte e.V.
//
//  Distributed under the Apache License, Version 2.0.
//  See the accompanying file LICENSE or http://www.apache.org/licenses/LICENSE-2.0.html

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Overte.Exporter.Avatar.Editor.Windows;

namespace Overte.Exporter.Avatar.Editor
{
    [CustomEditor(typeof(OverteAvatarDescriptor))]
    public class OverteAvatarDescriptorEditor : UnityEditor.Editor
    {
        private SerializedProperty _avatarNameProperty;
        private SerializedProperty _remapedBlendShapeListProperty;
        private SerializedProperty _optimizeBlendShapesProperty;
        private SerializedProperty _flowBoneListProperty;
        private SerializedProperty _flowConfigurationProperty;

        private bool _showBlendshapeList = true;
        private bool _showFlowConfig;
        private Vector2 _scrollPosition;
        private SkinnedMeshRenderer[] _skinnedMeshRenderers;
        private AvatarExporter _exporter;
        private Dictionary<Constants.AvatarRule, string> _warnings = new();
        private bool _showFlowBoneList = true;


        private void OnEnable()
        {
            _exporter = new AvatarExporter();
            _avatarNameProperty = serializedObject.FindProperty("AvatarName");
            _remapedBlendShapeListProperty = serializedObject.FindProperty("RemapedBlendShapeList");
            _optimizeBlendShapesProperty = serializedObject.FindProperty("OptimizeBlendShapes");
            _flowBoneListProperty = serializedObject.FindProperty("FlowBoneList");
            _flowConfigurationProperty = serializedObject.FindProperty("FlowConfiguration");

            // if (string.IsNullOrEmpty(avatarNameProperty.stringValue))
            // {
            //     var av = (OverteAvatarDescriptor)target;
            //     avatarNameProperty.stringValue = av.gameObject.name;
            // }

            // Cache skinned mesh renderers
            RefreshSkinnedMeshRenderers();
            CheckForErrors();
        }

        private void CheckForErrors()
        {
            var av = (OverteAvatarDescriptor)target;
            _warnings = _exporter.CheckForErrors(av.gameObject);
        }

        private void RefreshSkinnedMeshRenderers()
        {
            var descriptor = (OverteAvatarDescriptor)target;
            _skinnedMeshRenderers = descriptor.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_avatarNameProperty);

            EditorGUILayout.PropertyField(_optimizeBlendShapesProperty);

            // Refresh button for skinned mesh renderers
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Skinned Mesh Renderers", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh", GUILayout.Width(80)))
            {
                RefreshSkinnedMeshRenderers();
            }

            EditorGUILayout.EndHorizontal();

            if (_skinnedMeshRenderers.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "No SkinnedMeshRenderer components found in children. Add meshes to your avatar to map blendshapes.",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField($"Found {_skinnedMeshRenderers.Length} SkinnedMeshRenderer(s)",
                    EditorStyles.miniLabel);
            }

            // Custom GUI for the RemapedBlendShapeList
            DrawBlendShapeList();

            DrawFlowBoneList();

            EditorGUILayout.Separator();

            foreach (var warning in _warnings)
            {
                EditorGUILayout.HelpBox(warning.Value, MessageType.Warning);
            }

            if (_avatarNameProperty.stringValue == "")
            {
                EditorGUILayout.HelpBox("Avatar name not set!", MessageType.Error);
                GUI.enabled = false;
            }

            _showFlowConfig = EditorGUILayout.Foldout(_showFlowConfig, "Flow configuration");
            if (_showFlowConfig)
                _flowConfigurationProperty.stringValue =
                    EditorGUILayout.TextArea(_flowConfigurationProperty.stringValue);

            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Export avatar"))
            {
                ExportAvatar();
            }

            GUI.enabled = true;
        }

        private void ExportAvatar()
        {
            var path = EditorUtility.SaveFilePanel("Select .fst", "", _avatarNameProperty.stringValue, "fst");
            if (path == "")
                return;

            var av = (OverteAvatarDescriptor)target;
            Debug.Log(path);
            _exporter.ExportAvatar(_avatarNameProperty.stringValue, path, av.gameObject);
        }


        #region BlendShapeList

        private void DrawBlendShapeList()
        {
            EditorGUILayout.Space();

            // Header with foldout and buttons
            EditorGUILayout.BeginHorizontal();
            _showBlendshapeList = EditorGUILayout.Foldout(_showBlendshapeList, "Blend Shape Remapping", true,
                EditorStyles.foldoutHeader);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Add Mapping", GUILayout.Width(100)))
            {
                AddNewBlendshapeMapping();
            }

            EditorGUILayout.EndHorizontal();

            if (!_showBlendshapeList)
                return;

            // Scrollable area for blend shape mappings
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition,
                GUILayout.Height(Mathf.Min(_remapedBlendShapeListProperty.arraySize * 120f, 300f)));

            for (var i = 0; i < _remapedBlendShapeListProperty.arraySize; i++)
            {
                DrawBlendshapeElement(i);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawBlendshapeElement(int index)
        {
            var blendshapeElement = _remapedBlendShapeListProperty.GetArrayElementAtIndex(index);
            var fromProperty = blendshapeElement.FindPropertyRelative("from");
            var toProperty = blendshapeElement.FindPropertyRelative("to");
            var multiplierProperty = blendshapeElement.FindPropertyRelative("multiplier");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Heading with delete button
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Mapping {index + 1}", EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("×", GUILayout.Width(20), GUILayout.Height(20)))
            {
                RemoveBlendshapeMapping(index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.EndHorizontal();

            // From field with blendshape selector
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(fromProperty, new GUIContent("From Blendshape"));

            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                BlendshapeSelector.ShowWindow(_skinnedMeshRenderers, fromProperty, serializedObject);
            }

            EditorGUILayout.EndHorizontal();

            // To field with enum selector
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(toProperty, new GUIContent("To Overte Shape"));

            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                OverteBlendshapeSelector.ShowWindow(toProperty, serializedObject);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Slider(multiplierProperty, 0f, 2f, new GUIContent("Multiplier"));

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        private void AddNewBlendshapeMapping()
        {
            var index = _remapedBlendShapeListProperty.arraySize;
            _remapedBlendShapeListProperty.arraySize++;

            var newElement = _remapedBlendShapeListProperty.GetArrayElementAtIndex(index);
            newElement.FindPropertyRelative("from").stringValue = "";
            newElement.FindPropertyRelative("to").stringValue = "";
            newElement.FindPropertyRelative("multiplier").floatValue = 1.0f;

            serializedObject.ApplyModifiedProperties();
        }

        private void RemoveBlendshapeMapping(int index)
        {
            _remapedBlendShapeListProperty.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region FlowBoneList

        private void DrawFlowBoneList()
        {
            EditorGUILayout.Space();

            // Header with foldout and buttons
            EditorGUILayout.BeginHorizontal();
            _showFlowBoneList = EditorGUILayout.Foldout(_showFlowBoneList, "Flowbones", true,
                EditorStyles.foldoutHeader);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Add Flowbone", GUILayout.Width(100)))
            {
                AddNewFlowBoneMapping();
            }

            EditorGUILayout.EndHorizontal();

            if (!_showFlowBoneList)
                return;

            // Scrollable area for blend shape mappings
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition,
                GUILayout.Height(Mathf.Min(_flowBoneListProperty.arraySize * 120f, 300f)));

            for (var i = 0; i < _flowBoneListProperty.arraySize; i++)
            {
                DrawFlowBoneElement(i);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawFlowBoneElement(int index)
        {
            var flowBoneElement = _flowBoneListProperty.GetArrayElementAtIndex(index);
            var boneNameProperty = flowBoneElement.FindPropertyRelative("boneName");
            var idProperty = flowBoneElement.FindPropertyRelative("ID");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Heading with delete button
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Mapping {index + 1}", EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("×", GUILayout.Width(20), GUILayout.Height(20)))
            {
                RemoveFlowBoneMapping(index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.EndHorizontal();

            // From field with FlowBone selector
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(boneNameProperty, new GUIContent("Bone"));
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                BoneSelector.ShowWindow(_skinnedMeshRenderers, boneNameProperty, serializedObject);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(idProperty, new GUIContent("Flowbone Name"));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        private void AddNewFlowBoneMapping()
        {
            var index = _flowBoneListProperty.arraySize;
            _flowBoneListProperty.arraySize++;

            var newElement = _flowBoneListProperty.GetArrayElementAtIndex(index);
            newElement.FindPropertyRelative("boneName").stringValue = "";
            newElement.FindPropertyRelative("ID").stringValue = "";

            serializedObject.ApplyModifiedProperties();
        }

        private void RemoveFlowBoneMapping(int index)
        {
            _flowBoneListProperty.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }
}