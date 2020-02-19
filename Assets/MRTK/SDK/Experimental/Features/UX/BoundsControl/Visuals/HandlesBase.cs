﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Experimental.UI.BoundsControlTypes;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Experimental.UI.BoundsControl
{
    /// <summary>
    /// Base class for any type of <see cref="BoundsControl"/> handle.
    /// Handles are used for manipulating the BoundsControl by near or far user interaction.
    /// </summary>
    public abstract class HandlesBase : IProximityEffectObjectProvider
    {
        protected abstract HandlesBaseConfiguration BaseConfig
        {
            get;
        }

        internal void ResetHandleVisibility(bool isVisible)
        {
            if (handles != null)
            {
                for (int i = 0; i < handles.Count; ++i)
                {
                    handles[i].gameObject.SetActive(isVisible && IsVisible(handles[i]));
                    VisualUtils.ApplyMaterialToAllRenderers(handles[i].gameObject, BaseConfig.HandleMaterial);
                }
            }
            highlightedHandle = null;
        }

        internal abstract bool IsVisible(Transform handle);
        

        internal protected List<Transform> handles = new List<Transform>();
        private Transform highlightedHandle = null;

        

        public IReadOnlyList<Transform> Handles
        {
            get { return handles; }
        }

        internal void SetHighlighted(Transform handleToHighlight)
        {
            // turn off all handles that aren't the handle we want to highlight
            if (handles != null)
            {
                for (int i = 0; i < handles.Count; ++i)
                {
                    if (handles[i] != handleToHighlight)
                    {
                        handles[i].gameObject.SetActive(false);
                    }
                    else
                    {
                        VisualUtils.ApplyMaterialToAllRenderers(handles[i].gameObject, BaseConfig.HandleGrabbedMaterial);
                        highlightedHandle = handleToHighlight;
                    }
                }
            }
        }

        internal void HandleIgnoreCollider(Collider handlesIgnoreCollider)
        {
            VisualUtils.HandleIgnoreCollider(handlesIgnoreCollider, handles);
        }

        internal void DestroyHandles()
        {
            if (handles != null)
            {
                foreach (Transform transform in handles)
                {
                    GameObject.Destroy(transform.gameObject);
                }

                handles.Clear();
            }
        }

        internal bool IsHandleType(Transform handle)
        {
            for (int i = 0; i < handles.Count; ++i)
            {
                if (handle == handles[i])
                {
                    return true;
                }
            }

            return false;
        }


        internal virtual HandleType GetHandleType()
        {
            return HandleType.None;
        }

        protected abstract Transform GetVisual(Transform handle);

        protected void UpdateBaseMaterial()
        {
            if (handles != null)
            {
                for (int i = 0; i < handles.Count; ++i)
                {
                    if (handles[i] != highlightedHandle)
                    {
                        VisualUtils.ApplyMaterialToAllRenderers(handles[i].gameObject, BaseConfig.HandleMaterial);
                    }
                }
            }
        }

        protected void UpdateGrabbedMaterial()
        {
            SetHighlighted(highlightedHandle);
        }

        //protected void UpdateColliderPadding(HandlePrefabCollider colliderType, Vector3 size)
        //{
        //    foreach (var handle in handles)
        //    {
        //        if (colliderType == HandlePrefabCollider.Box)
        //        {
        //            BoxCollider collider = handle.gameObject.GetComponent<BoxCollider>();
        //            collider.size = size;
        //            collider.size += BaseConfig.ColliderPadding;
        //        }
        //        else
        //        {
        //            SphereCollider sphere = handle.gameObject.GetComponent<SphereCollider>();
        //            sphere.radius = size.x;
        //            sphere.radius += Mathf.Max(Mathf.Max(BaseConfig.ColliderPadding.x, BaseConfig.ColliderPadding.y), BaseConfig.ColliderPadding.z);
        //        }
        //    }
        //}

        #region IProximityScaleObjectProvider 
        public abstract bool IsActive();

        public void ForEachProximityObject(Action<Transform> action)
        {
            for (int i = 0; i < handles.Count; ++i)
            {
                action(GetVisual(handles[i]));
            }
        }

        public Material GetBaseMaterial()
        {
            return BaseConfig.HandleMaterial;
        }

        public Material GetHighlightedMaterial()
        {
            return BaseConfig.HandleGrabbedMaterial;
        }

        public float GetObjectSize()
        {
            return BaseConfig.HandleSize;
        }

        #endregion IProximityScaleObjectProvider
    }
}
