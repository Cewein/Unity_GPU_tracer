using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class RayTracingMaster : MonoBehaviour
{
    public ComputeShader RayTracingShader;
    public Texture SkyboxTexture;
    public GameObject plane;
    public GameObject sphere;
    public Light light;

    [Range(0,8)]
    public int numberOfBounces = 1;
    public bool withAntiAliassing = false;

    private RenderTexture _target;
    private Camera _camera;

    private uint _currentSample = 0;
    private Material _addMaterial;

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Render(destination);
    }

    private void Render(RenderTexture dest)
    {
        InitRenderTexture();

        SetShaderParameters();
        int threadGroupX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupY = Mathf.CeilToInt(Screen.width / 8.0f);

        RayTracingShader.Dispatch(0, threadGroupX, threadGroupY, 1);
        if(!withAntiAliassing)
            Graphics.Blit(_target, dest);

        if(_addMaterial == null)
            _addMaterial = new Material(Shader.Find("Hidden/addShader"));
        _addMaterial.SetFloat("_Sample", _currentSample);
        if (withAntiAliassing)
            Graphics.Blit(_target, dest, _addMaterial);
        _currentSample++;

    }

    private void InitRenderTexture()
    {
        if (_target == null || _target.width != Screen.width || _target.height != Screen.height)
        {
            _currentSample = 0;
            if (_target != null)
                _target.Release();

            _target = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _target.enableRandomWrite = true;
            _target.Create();
        }
    }

    private void SetShaderParameters()
    {
        RayTracingShader.SetTexture(0, "Result", _target);
        RayTracingShader.SetTexture(0, "_SkyboxTexture", SkyboxTexture);
        RayTracingShader.SetFloat("_GroundPlane", plane.GetComponent<Transform>().position.y);

        RayTracingShader.SetInt("nbBounces", numberOfBounces);

        if (withAntiAliassing)
            RayTracingShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));
        else
            RayTracingShader.SetVector("_PixelOffset", new Vector2(0.5f, 0.5f));

        //sphere gameObject
        Transform sp = sphere.GetComponent<Transform>();
        sp.localScale = new Vector3(sp.localScale.y, sp.localScale.y, sp.localScale.y);
        RayTracingShader.SetVector("_Sphere", new Vector4(sp.position.x, sp.position.y, sp.position.z, sp.localScale.x * 0.5f));

        //light direction
        Vector3 l = light.transform.forward;
        RayTracingShader.SetVector("_DirectionalLight", new Vector4(l.x, l.y, l.z, light.intensity));

        //camera matrix
        RayTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        RayTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
    }

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void Update()
    {
        if (transform.hasChanged || light.transform.hasChanged)
        {
            _currentSample = 0;

            transform.hasChanged = false;
            light.transform.hasChanged = false;
        }
    }


}
