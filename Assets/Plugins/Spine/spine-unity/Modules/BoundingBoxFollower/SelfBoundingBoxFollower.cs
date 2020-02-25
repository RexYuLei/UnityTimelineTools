using UnityEngine;
using System.Collections;
using Spine.Unity;
using Spine;
using System.Collections.Generic;

public class SelfBoundingBoxFollower : MonoBehaviour
{

    #region Inspector
    public SkeletonRenderer skeletonRenderer;
    //[SpineSlot(dataField: "skeletonRenderer", containsBoundingBoxes: true)]
    public string slotName;
    public bool isTrigger;
    #endregion

    Slot slot;
    PolygonCollider2D currentCollider;


    public Slot Slot { get { return slot; } }
    public PolygonCollider2D CurrentCollider { get { return currentCollider; } }
    public bool IsTrigger { get { return isTrigger; } }


    public void HandleRebuild(SkeletonRenderer renderer)
    {
        if (string.IsNullOrEmpty(slotName))
            return;

        ClearColliders();

        var skeleton = skeletonRenderer.skeleton;
        slot = skeleton.FindSlot(slotName);
        int slotIndex = skeleton.FindSlotIndex(slotName);

        if (this.gameObject.activeInHierarchy)
        {
            foreach (var skin in skeleton.Data.Skins)
            {
                var attachmentNames = new List<string>();
                skin.FindNamesForSlot(slotIndex, attachmentNames);

                foreach (var attachmentName in attachmentNames)
                {
                    var attachment = skin.GetAttachment(slotIndex, attachmentName);
                    var boundingBoxAttachment = attachment as BoundingBoxAttachment;

#if UNITY_EDITOR
                    if (attachment != null && boundingBoxAttachment == null)
                        Debug.Log("BoundingBoxFollower tried to follow a slot that contains non-boundingbox attachments: " + slotName);
#endif

                    if (boundingBoxAttachment != null)
                    {
                        var bbCollider = SkeletonUtility.AddBoundingBoxAsComponent(boundingBoxAttachment, slot, gameObject, true);
                        bbCollider.enabled = true;
                        bbCollider.hideFlags = HideFlags.NotEditable;
                        bbCollider.isTrigger = IsTrigger;
                        currentCollider = bbCollider;
                    }
                }
            }
        }
    }

    void ClearColliders()
    {
        var colliders = GetComponents<PolygonCollider2D>();
        if (colliders.Length == 0) return;

#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            foreach (var c in colliders)
            {
                if (c != null)
                    Destroy(c);
            }
        }
        else
        {
            foreach (var c in colliders)
                DestroyImmediate(c);
        }
#else
			foreach (var c in colliders)
				if (c != null)
					Destroy(c);
#endif

    }

    void LateUpdate()
    {
        CheckCollider();
    }

    private void CheckCollider()
    {
        var skeleton = skeletonRenderer.skeleton;
        slot = skeleton.FindSlot(slotName);
        int slotIndex = skeleton.FindSlotIndex(slotName);

        if (!this.gameObject.activeInHierarchy) return;

        var attachmentNames = new List<string>();
        var skin = skeleton.Data.Skins.Items[0];
        skin.FindNamesForSlot(slotIndex, attachmentNames);

        foreach (var attachmentName in attachmentNames)
        {
            var attachment = skin.GetAttachment(slotIndex, attachmentName);
            var boundingBoxAttachment = attachment as BoundingBoxAttachment;

            if (boundingBoxAttachment != null)
            {
                var bbCollider = GetComponent<PolygonCollider2D>();
                float[] floats = boundingBoxAttachment.Vertices;
                int floatCount = floats.Length;
                int vertCount = floatCount / 2;

                var worldVerts = new float[boundingBoxAttachment.Vertices.Length];
                boundingBoxAttachment.ComputeWorldVertices(slot, worldVerts);

                Vector2[] verts = new Vector2[vertCount];
                int v = 0;
                for (int i = 0; i < floatCount; i += 2, v++)
                {
                    verts[v].x = worldVerts[i];
                    verts[v].y = worldVerts[i + 1];
                }

                bbCollider.points = verts;
                bbCollider.isTrigger = IsTrigger;
            }
        }
    }
}

