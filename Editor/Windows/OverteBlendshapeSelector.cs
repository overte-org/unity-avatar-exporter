//  OverteBlendshapeSelector.cs
//
//  Created by Edgar on 03-01-2026
//  Copyright 2026 Overte e.V.
//
//  Distributed under the Apache License, Version 2.0.
//  See the accompanying file LICENSE or http://www.apache.org/licenses/LICENSE-2.0.html

using System.Collections.Generic;
using System.Linq;
using Overte.Exporter.Avatar;
using UnityEditor;
using UnityEngine;

namespace Overte.Exporter.Avatar.Editor.Windows
{
    // Popup window for selecting Overte blendshape enum values
    public class OverteBlendshapeSelector : EditorWindow
    {
        private SerializedProperty _targetProperty;
        private SerializedObject _serializedObject;

        private Vector2 _scrollPosition;
        private string _searchText = "";
        private string[] _enumNames;

        public static void ShowWindow(SerializedProperty property, SerializedObject serializedObj)
        {
            var window = GetWindow<OverteBlendshapeSelector>(true, "Overte Blendshape Selector");
            window.minSize = new Vector2(350, 400);
            window.maxSize = new Vector2(500, 600);
            window._targetProperty = property;
            window._serializedObject = serializedObj;
            window._enumNames = System.Enum.GetNames(typeof(Constants.Blendshapes));
            window.ShowUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Select Overte Blendshape", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Search bar
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            var newSearch = EditorGUILayout.TextField(_searchText);
            if (newSearch != _searchText)
            {
                _searchText = newSearch;
            }

            if (GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                _searchText = "";
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Group blendshapes by category
            var groupedBlendshapes = new Dictionary<string, List<string>>();

            // Define groups based on enum naming patterns
            groupedBlendshapes["Basic"] = new List<string> { "EyeBlink_L", "EyeBlink_R", "JawOpen" };
            groupedBlendshapes["Eye"] = new List<string>();
            groupedBlendshapes["Brows"] = new List<string>();
            groupedBlendshapes["Jaw"] = new List<string>();
            groupedBlendshapes["Mouth"] = new List<string>();
            groupedBlendshapes["Lips"] = new List<string>();
            groupedBlendshapes["Cheek"] = new List<string>();
            groupedBlendshapes["Nose"] = new List<string>();
            groupedBlendshapes["Tongue"] = new List<string>();
            groupedBlendshapes["User"] = new List<string>();
            groupedBlendshapes["Other"] = new List<string>();

            // Populate groups
            foreach (var enumName in _enumNames)
            {
                var added = false;
                foreach (var group in groupedBlendshapes.Keys.ToArray())
                {
                    if (group != "Other" && enumName.Contains(group))
                    {
                        groupedBlendshapes[group].Add(enumName);
                        added = true;
                        break;
                    }
                }

                // Add to User group for UserBlendshape items
                if (!added && enumName.StartsWith("UserBlendshape"))
                {
                    groupedBlendshapes["User"].Add(enumName);
                    added = true;
                }

                // Add to Other if no match was found
                if (!added)
                {
                    groupedBlendshapes["Other"].Add(enumName);
                }
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            var anyFound = false;

            // Display grouped blendshapes
            foreach (var group in groupedBlendshapes)
            {
                // Filter by search text
                var matchingItems = group.Value.Where(item =>
                    string.IsNullOrEmpty(_searchText) ||
                    item.ToLower().Contains(_searchText.ToLower())).ToList();

                if (matchingItems.Count == 0)
                    continue;

                anyFound = true;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"{group.Key} Blendshapes ({matchingItems.Count})", EditorStyles.boldLabel);

                foreach (var item in matchingItems)
                {
                    EditorGUILayout.BeginHorizontal();

                    // Highlight search matches
                    var style = new GUIStyle(EditorStyles.label);
                    if (!string.IsNullOrEmpty(_searchText) &&
                        item.ToLower().Contains(_searchText.ToLower()))
                    {
                        style.fontStyle = FontStyle.Bold;
                    }

                    EditorGUILayout.LabelField(item, style);

                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        SelectBlendshape(item);
                        GUIUtility.ExitGUI(); // Close window after selection
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }

            if (!anyFound)
            {
                EditorGUILayout.HelpBox(
                    $"No Overte blendshapes matching '{_searchText}' found.",
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
            _targetProperty.stringValue = blendshapeName;
            _serializedObject.ApplyModifiedProperties();
            Close();
        }
    }
}