﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Experimental.UI.BoundsControlTypes;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Experimental.UI.BoundsControl
{
    /// <summary>
    /// Rotation handles for <see cref="BoundsControl"/> that are used for rotating the
    /// Gameobject BoundsControl is attached to with near or far interaction
    /// </summary>
    public class RotationHandles : HandlesBase
    {
        protected override HandlesBaseConfiguration BaseConfig => config;
        private RotationHandlesConfiguration config;

        internal RotationHandles(RotationHandlesConfiguration configuration)
        {
            Debug.Assert(configuration != null, "Can't create BoundsControlRotationHandles without valid configuration");
            config = configuration;
            config.handlesChanged.AddListener(HandlesChanged);
            config.colliderTypeChanged.AddListener(UpdateColliderType);
        }

        ~RotationHandles()
        {
            config.handlesChanged.RemoveListener(HandlesChanged);
            config.colliderTypeChanged.RemoveListener(UpdateColliderType);
        }

        private void HandlesChanged(HandlesBaseConfiguration.HandlesChangedEventType changedType)
        {
            switch (changedType)
            {
                case HandlesBaseConfiguration.HandlesChangedEventType.MATERIAL:
                    UpdateBaseMaterial();
                    break;
                case HandlesBaseConfiguration.HandlesChangedEventType.MATERIAL_GRABBED:
                    UpdateGrabbedMaterial();
                    break;
                case HandlesBaseConfiguration.HandlesChangedEventType.PREFAB:
                    RecreateVisuals();
                    break;
                case HandlesBaseConfiguration.HandlesChangedEventType.COLLIDER_SIZE:
                case HandlesBaseConfiguration.HandlesChangedEventType.COLLIDER_PADDING:
                    UpdateColliderBounds();
                    break;
                case HandlesBaseConfiguration.HandlesChangedEventType.VISIBILITY:
                    //TODO
                    break;
            }
        }

       
        void UpdateColliderBounds()
        {
            foreach (var handle in handles)
            {
                var handleBounds = VisualUtils.GetMaxBounds(GetVisual(handle).gameObject);
                UpdateColliderBounds(handle, handleBounds.size);
            }
        }

        void UpdateColliderType()
        {
            foreach (var handle in handles)
            {
                // remove old colliders
                var oldBoxCollider = handle.GetComponent<BoxCollider>();
                if (oldBoxCollider != null)
                {
                    Object.Destroy(oldBoxCollider);
                }

                var oldSphereCollider = handle.GetComponent<SphereCollider>();
                if (oldSphereCollider != null)
                {
                    Object.Destroy(oldSphereCollider);
                }

                // attach new collider
                var handleBounds = VisualUtils.GetMaxBounds(GetVisual(handle).gameObject);
                var invScale = config.HandleSize / handleBounds.size.x;
                Vector3 colliderSizeScaled = handleBounds.size * invScale;
                Vector3 colliderCenterScaled = handleBounds.center * invScale;
                if (config.RotationHandlePrefabColliderType == HandlePrefabCollider.Box)
                {
                    BoxCollider collider = handle.gameObject.AddComponent<BoxCollider>();
                    collider.size = colliderSizeScaled;
                    collider.center = colliderCenterScaled;
                    collider.size += config.ColliderPadding;
                }
                else
                {
                    SphereCollider sphere = handle.gameObject.AddComponent<SphereCollider>();
                    sphere.center = colliderCenterScaled;
                    sphere.radius = colliderSizeScaled.x * 0.5f;
                    sphere.radius += GetMaxComponent(config.ColliderPadding);
                }
            }
        }

        internal const int NumEdges = 12;

        private Vector3[] edgeCenters = new Vector3[NumEdges];
        private CardinalAxisType[] edgeAxes;

        internal int GetRotationHandleIdx(Transform handle)
        {
            for (int i = 0; i < handles.Count; ++i)
            {
                if (handle == handles[i])
                {
                    return i;
                }
            }

            return handles.Count;
        }

        internal Vector3 GetEdgeCenter(int index)
        {
            Debug.Assert(index >= 0 && index <= NumEdges, "Edge center index out of bounds");
            return edgeCenters[index];
        }

        internal CardinalAxisType GetAxisType(int index)
        {
            Debug.Assert(index >= 0 && index <= NumEdges, "Edge axes index out of bounds");
            return edgeAxes[index];
        }

        internal CardinalAxisType GetAxisType(Transform handle)
        {
            int index = GetRotationHandleIdx(handle);
            return GetAxisType(index);
        }

        private void UpdateHandles()
        {
            for (int i = 0; i < handles.Count; ++i)
            {
                handles[i].position = GetEdgeCenter(i);
            }
        }

        internal void CalculateEdgeCenters(ref Vector3[] boundsCorners)
        {
            if (boundsCorners != null && edgeCenters != null)
            {
                for (int i = 0; i < edgeCenters.Length; ++i)
                {
                    edgeCenters[i] = VisualUtils.GetLinkPosition(i, ref boundsCorners);
                }
            }

            UpdateHandles();
        }


        internal void InitEdgeAxis()
        { 
            edgeAxes = new CardinalAxisType[NumEdges];
            edgeAxes[0] = CardinalAxisType.X;
            edgeAxes[1] = CardinalAxisType.Y;
            edgeAxes[2] = CardinalAxisType.X;
            edgeAxes[3] = CardinalAxisType.Y;
            edgeAxes[4] = CardinalAxisType.X;
            edgeAxes[5] = CardinalAxisType.Y;
            edgeAxes[6] = CardinalAxisType.X;
            edgeAxes[7] = CardinalAxisType.Y;
            edgeAxes[8] = CardinalAxisType.Z;
            edgeAxes[9] = CardinalAxisType.Z;
            edgeAxes[10] = CardinalAxisType.Z;
            edgeAxes[11] = CardinalAxisType.Z;
        }

        internal void FlattenHandles(ref int[] flattenedHandles)
        {
            if (flattenedHandles != null)
            {
                for (int i = 0; i < flattenedHandles.Length; ++i)
                {
                    handles[flattenedHandles[i]].gameObject.SetActive(false);
                }
            }
        }

        internal void Create(ref Vector3[] boundsCorners, Transform parent, bool drawManipulationTether)
        {
            edgeCenters = new Vector3[12];
            CalculateEdgeCenters(ref boundsCorners);
            InitEdgeAxis();
            CreateHandles(parent, drawManipulationTether);
        }
        
        private void CreateHandles(Transform parent, bool drawManipulationTether)
        {
            for (int i = 0; i < edgeCenters.Length; ++i)
            {
                GameObject midpoint = new GameObject();
                midpoint.name = "midpoint_" + i.ToString();
                midpoint.transform.position = edgeCenters[i];
                midpoint.transform.parent = parent;

                Bounds midpointBounds = CreateVisual(i, midpoint);
                float maxDim = GetMaxComponent(midpointBounds.size);
                float invScale = config.HandleSize / maxDim;
                VisualUtils.AddComponentsToAffordance(midpoint, new Bounds(midpointBounds.center * invScale, midpointBounds.size * invScale),
                    config.RotationHandlePrefabColliderType, CursorContextInfo.CursorAction.Rotate, config.ColliderPadding, parent, drawManipulationTether);

                handles.Add(midpoint.transform);

            }
        }

        private void RecreateVisuals()
        {
            for (int i = 0; i < handles.Count; ++i)
            {
                // get parent of visual
                Transform obsoleteChild = handles[i].Find("visuals");
                if (obsoleteChild)
                {
                    // get old child and remove it
                    obsoleteChild.parent = null;
                    Object.Destroy(obsoleteChild);
                }
                else
                {
                    Debug.LogError("couldn't find rotation visual on recreating visuals");
                }

                // create new visual
                Bounds visualBounds = CreateVisual(i, handles[i].gameObject);

                // update handle collider bounds
                UpdateColliderBounds(handles[i], visualBounds.size);
            }
        }

        private float GetMaxComponent(Vector3 vec)
        {
            return Mathf.Max(Mathf.Max(vec.x, vec.y), vec.z);
        }

        private void UpdateColliderBounds(Transform handle, Vector3 visualSize)
        {
            var invScale = config.HandleSize / visualSize.x;
            Vector3 colliderSizeScaled = visualSize * invScale;
            if (config.RotationHandlePrefabColliderType == HandlePrefabCollider.Box)
            {
                BoxCollider collider = handle.gameObject.GetComponent<BoxCollider>();
                collider.size = colliderSizeScaled;
                collider.size += BaseConfig.ColliderPadding;
            }
            else
            {
                SphereCollider collider = handle.gameObject.GetComponent<SphereCollider>();
                collider.radius = colliderSizeScaled.x * 0.5f;
                collider.radius += GetMaxComponent(config.ColliderPadding);
            }
        }


        private Bounds CreateVisual(int handleIndex, GameObject parent)
        {
            GameObject midpointVisual;
            GameObject prefabType = config.HandlePrefab;
            if (prefabType != null)
            {
                midpointVisual = GameObject.Instantiate(prefabType);
            }
            else
            {
                midpointVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                GameObject.Destroy(midpointVisual.GetComponent<SphereCollider>());
            }

            // Align handle with its edge assuming that the prefab is initially aligned with the up direction 
            if (edgeAxes[handleIndex] == CardinalAxisType.X)
            {
                Quaternion realignment = Quaternion.FromToRotation(Vector3.up, Vector3.right);
                midpointVisual.transform.localRotation = realignment * midpointVisual.transform.localRotation;
            }
            else if (edgeAxes[handleIndex] == CardinalAxisType.Z)
            {
                Quaternion realignment = Quaternion.FromToRotation(Vector3.up, Vector3.forward);
                midpointVisual.transform.localRotation = realignment * midpointVisual.transform.localRotation;
            }

            Bounds midpointBounds = VisualUtils.GetMaxBounds(midpointVisual);
            float maxDim = GetMaxComponent(midpointBounds.size);
            float invScale = config.HandleSize / maxDim;

            midpointVisual.name = "visuals";
            midpointVisual.transform.parent = parent.transform;
            midpointVisual.transform.localScale = new Vector3(invScale, invScale, invScale);
            midpointVisual.transform.localPosition = Vector3.zero;

            if (config.HandleMaterial != null)
            {
                VisualUtils.ApplyMaterialToAllRenderers(midpointVisual, config.HandleMaterial);
            }

            return midpointBounds;
        }

        #region BoundsControlHandlerBase 
        internal override bool IsVisible(Transform handle)
        {
            CardinalAxisType axisType = GetAxisType(handle);
            return
                (axisType == CardinalAxisType.X && config.ShowRotationHandleForX) ||
                (axisType == CardinalAxisType.Y && config.ShowRotationHandleForY) ||
                (axisType == CardinalAxisType.Z && config.ShowRotationHandleForZ);
        }

        internal override HandleType GetHandleType()
        {
            return HandleType.Rotation;
        }

        protected override Transform GetVisual(Transform handle)
        {
            // visual is first child 
            Transform childTransform = handle.GetChild(0);
            if (childTransform != null && childTransform.name == "visuals")
            {
                return childTransform;
            }

            return null;
        }
        #endregion BoundsControlHandlerBase

        #region IProximityScaleObjectProvider 
        public override bool IsActive()
        {
            return config.ShowRotationHandleForX || config.ShowRotationHandleForY || config.ShowRotationHandleForZ;
        }

        #endregion IProximityScaleObjectProvider

    }
}
