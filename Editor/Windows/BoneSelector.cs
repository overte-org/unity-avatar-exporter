//  BoneSelector.cs
//
//  Created by Edgar on 03-01-2026
//  Copyright 2026 Overte e.V.
//
//  Distributed under the Apache License, Version 2.0.
//  See the accompanying file LICENSE or http://www.apache.org/licenses/LICENSE-2.0.html

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Overte.Exporter.Avatar.Editor.Windows
{
    public class BoneSelector : EditorWindow
    {
        private SkinnedMeshRenderer[] _skinnedMeshRenderers;
        private SerializedProperty _targetProperty;
        private SerializedObject _serializedObject;

        private Vector2 _scrollPosition;
        private string _searchText = "";
        private Dictionary<string, bool> _foldoutStates = new Dictionary<string, bool>();

        public static void ShowWindow(SkinnedMeshRenderer[] renderers, SerializedProperty property,
            SerializedObject serializedObj)
        {
            if (renderers == null || renderers.Length == 0)
            {
                EditorUtility.DisplayDialog("No Skinned Mesh Renderers",
                    "No SkinnedMeshRenderer components found in the avatar hierarchy.", "OK");
                return;
            }

            var window = GetWindow<BoneSelector>(true, "Bone Selector");
            window.minSize = new Vector2(350, 400);
            window.maxSize = new Vector2(500, 600);
            window._skinnedMeshRenderers = renderers;
            window._targetProperty = property;
            window._serializedObject = serializedObj;
            window.ShowUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Select a bone", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Search bar
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            var newSearch = EditorGUILayout.TextField(_searchText);
            if (newSearch != _searchText)
            {
                _searchText = newSearch;
                // Automatically expand all foldouts when searching
                if (!string.IsNullOrEmpty(_searchText))
                {
                    foreach (var renderer in _skinnedMeshRenderers)
                    {
                        if (renderer && renderer.sharedMesh)
                        {
                            _foldoutStates[renderer.name] = true;
                        }
                    }
                }
            }

            if (GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                _searchText = "";
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Flowbone list
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            var anyFlowbonesFound = false;

            foreach (var renderer in _skinnedMeshRenderers)
            {
                if (renderer == null || renderer.sharedMesh == null) continue;

                var rendererName = string.IsNullOrEmpty(renderer.name) ? "Unnamed Mesh" : renderer.name;

                // Skip renderers with no Flowbones
                if (renderer.bones.Length == 0) continue;

                // Filter renderers by search text
                var matchingFlowboneIndices = new List<int>();
                for (var i = 0; i < renderer.bones.Length; i++)
                {
                    var flowboneName = renderer.bones[i].name;
                    if (string.IsNullOrEmpty(_searchText) ||
                        flowboneName.ToLower().Contains(_searchText.ToLower()) ||
                        rendererName.ToLower().Contains(_searchText.ToLower()))
                    {
                        matchingFlowboneIndices.Add(i);
                    }
                }

                if (matchingFlowboneIndices.Count == 0) continue;

                anyFlowbonesFound = true;

                // Initialize foldout state if not exists
                if (!_foldoutStates.ContainsKey(rendererName))
                {
                    _foldoutStates[rendererName] = false;
                }

                // Renderer foldout
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Renderer header with count
                EditorGUILayout.BeginHorizontal();
                _foldoutStates[rendererName] = EditorGUILayout.Foldout(
                    _foldoutStates[rendererName],
                    $"{rendererName} ({matchingFlowboneIndices.Count} bones)",
                    true
                );

                EditorGUILayout.EndHorizontal();

                // List Flowbones if foldout is open
                if (_foldoutStates[rendererName])
                {
                    EditorGUI.indentLevel++;

                    foreach (var i in matchingFlowboneIndices)
                    {
                        var flowboneName = renderer.bones[i].name;

                        EditorGUILayout.BeginHorizontal();

                        // Highlight search matches
                        var style = new GUIStyle(EditorStyles.label);
                        if (!string.IsNullOrEmpty(_searchText) &&
                            flowboneName.ToLower().Contains(_searchText.ToLower()))
                        {
                            style.fontStyle = FontStyle.Bold;
                        }

                        EditorGUILayout.LabelField(flowboneName, style);

                        if (GUILayout.Button("Select", GUILayout.Width(60)))
                        {
                            SelectFlowbone(flowboneName);
                            GUIUtility.ExitGUI(); // Close window after selection
                        }

                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }

            if (!anyFlowbonesFound)
            {
                EditorGUILayout.HelpBox(
                    string.IsNullOrEmpty(_searchText)
                        ? "No bones found in any SkinnedMeshRenderer."
                        : $"No bones matching '{_searchText}' found.",
                    MessageType.Info
                );
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            // Cancel button at bottom
            if (GUILayout.Button("Cancel", GUILayout.Height(30)))
            {
                Close();
            }
        }

        private void SelectFlowbone(string flowboneName)
        {
            _targetProperty.stringValue = flowboneName;
            _serializedObject.ApplyModifiedProperties();
            Close();
        }
    }
}