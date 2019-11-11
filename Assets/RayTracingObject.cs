using System.Collections;
using System;
using UnityEngine;
using UnityEditor;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[ExecuteInEditMode]
public class RayTracingObject : MonoBehaviour
{
    private void OnEnable()
    {
        RayTracingMaster.RegisterObject(this);
    }

    private void OnDisable()
    {
        RayTracingMaster.UnregisterObject(this);

    }
}
