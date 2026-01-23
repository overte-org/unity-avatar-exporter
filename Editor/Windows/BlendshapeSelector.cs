//  BlendshapeSelector.cs
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
    // Popup window for selecting source blendshapes from SkinnedMeshRenderers
    public class BlendshapeSelector : EditorWindow
    {
        private SkinnedMeshRenderer[] skinnedMeshRenderers;
        private SerializedProperty targetProperty;
        private SerializedObject serializedObject;

        private Vector2 scrollPosition;
        private string searchText = "";
        private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();

        public static void ShowWindow(SkinnedMeshRenderer[] renderers, SerializedProperty property,
            SerializedObject serializedObj)
        {
            if (renderers == null || renderers.Length == 0)
            {
                EditorUtility.DisplayDialog("No Skinned Mesh Renderers",
                    "No SkinnedMeshRenderer components found in the avatar hierarchy.", "OK");
                return;
            }

            var window = GetWindow<BlendshapeSelector>(true, "Blendshape Selector");
            window.minSize = new Vector2(350, 400);
            window.maxSize = new Vector2(500, 600);
            window.skinnedMeshRenderers = renderers;
            window.targetProperty = property;
            window.serializedObject = serializedObj;
            window.ShowUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Select a Blendshape", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Search bar
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            var newSearch = EditorGUILayout.TextField(searchText);
            if (newSearch != searchText)
            {
                searchText = newSearch;
                // Automatically expand all foldouts when searching
                if (!string.IsNullOrEmpty(searchText))
                {
                    foreach (var renderer in skinnedMeshRenderers)
                    {
                        if (renderer && renderer.sharedMesh)
                        {
                            foldoutStates[renderer.name] = true;
                        }
                    }
                }
            }

            if (GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                searchText = "";
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Blendshape list
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            var anyBlendshapesFound = false;

            foreach (var renderer in skinnedMeshRenderers)
            {
                if (renderer == null || renderer.sharedMesh == null) continue;

                var rendererName = string.IsNullOrEmpty(renderer.name) ? "Unnamed Mesh" : renderer.name;

                // Skip renderers with no blendshapes
                if (renderer.sharedMesh.blendShapeCount == 0) continue;

                // Filter renderers by search text
                var matchingBlendshapeIndices = new List<int>();
                for (var i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
                {
                    var blendshapeName = renderer.sharedMesh.GetBlendShapeName(i);
                    if (string.IsNullOrEmpty(searchText) ||
                        blendshapeName.ToLower().Contains(searchText.ToLower()) ||
                        rendererName.ToLower().Contains(searchText.ToLower()))
                    {
                        matchingBlendshapeIndices.Add(i);
                    }
                }

                if (matchingBlendshapeIndices.Count == 0) continue;

                anyBlendshapesFound = true;

                // Initialize foldout state if not exists
                if (!foldoutStates.ContainsKey(rendererName))
                {
                    foldoutStates[rendererName] = false;
                }

                // Renderer foldout
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Renderer header with count
                EditorGUILayout.BeginHorizontal();
                foldoutStates[rendererName] = EditorGUILayout.Foldout(
                    foldoutStates[rendererName],
                    $"{rendererName} ({matchingBlendshapeIndices.Count} blendshapes)",
                    true
                );

                EditorGUILayout.EndHorizontal();

                // List blendshapes if foldout is open
                if (foldoutStates[rendererName])
                {
                    EditorGUI.indentLevel++;

                    foreach (var i in matchingBlendshapeIndices)
                    {
                        var blendshapeName = renderer.sharedMesh.GetBlendShapeName(i);

                        EditorGUILayout.BeginHorizontal();

                        // Highlight search matches
                        var style = new GUIStyle(EditorStyles.label);
                        if (!string.IsNullOrEmpty(searchText) &&
                            blendshapeName.ToLower().Contains(searchText.ToLower()))
                        {
                            style.fontStyle = FontStyle.Bold;
                        }

                        EditorGUILayout.LabelField(blendshapeName, style);

                        if (GUILayout.Button("Select", GUILayout.Width(60)))
                        {
                            SelectBlendshape(blendshapeName);
                            GUIUtility.ExitGUI(); // Close window after selection
                        }

                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }

            if (!anyBlendshapesFound)
            {
                EditorGUILayout.HelpBox(
                    string.IsNullOrEmpty(searchText)
                        ? "No blendshapes found in any SkinnedMeshRenderer."
                        : $"No blendshapes matching '{searchText}' found.",
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

        private void SelectBlendshape(string blendshapeName)
        {
            targetProperty.stringValue = blendshapeName;
            serializedObject.ApplyModifiedProperties();
            Close();
        }
    }
}