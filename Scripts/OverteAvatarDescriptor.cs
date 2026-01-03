//  OverteAvatarDescriptor.cs
//
//  Created by Edgar on 03-01-2026
//  Copyright 2026 Overte e.V.
//
//  Distributed under the Apache License, Version 2.0.
//  See the accompanying file LICENSE or http://www.apache.org/licenses/LICENSE-2.0.html

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Overte.Exporter.Avatar
{
    public class OverteAvatarDescriptor : MonoBehaviour
    {
        [SerializeField] public string AvatarName;

        [SerializeField] public bool OptimizeBlendShapes = true;

        // [SerializeField] private string m_exportPath;

        [SerializeField] public List<OvBlendshape> RemapedBlendShapeList = new();

        [Serializable]
        public class OvBlendshape
        {
            public string from;
            public string to;
            public float multiplier;
        }

        [SerializeField] public List<FlowBone> FlowBoneList = new();

        [Serializable]
        public class FlowBone
        {
            public string boneName;
            public string ID;
        }

        [SerializeField] public string FlowConfiguration = "";
    }
}