using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class RayTracingMaster : MonoBehaviour
{
    //public parameter
    public ComputeShader RayTracingShader;
    public Texture SkyboxTexture;

    private static bool _meshObjectNeedRebuild = false;
    private static List<RayTracingObject> _rayTracingObjects = new List<RayTracingObject>();
 
    //static function
    public static void RegisterObject(RayTracingObject rayTracingObject)
    {
        _rayTracingObjects.Add(rayTracingObject);
        _meshObjectNeedRebuild = true;
    }

    public static void UnregisterObject(RayTracingObject rayTracingObject)
    {
        _rayTracingObjects.Remove(rayTracingObject);
        _meshObjectNeedRebuild = true;
    }

    public GameObject plane;
    public new Light light;

    //sphere parameter
    public Vector2 SphereRadius = new Vector2(3.0f, 8.0f);
    public uint SphereMax = 100;
    public float SpherePlacementRadius = 100.0f;
    private ComputeBuffer _SphereBuffer;
    struct Sphere
    {
        public Vector3 position;
        public float radius;
        public Vector3 albedo;
        public Vector3 specular;
    };

    //rendering parameter
    [Range(0,8)]
    public int numberOfBounces = 1;
    public bool withAntiAliassing = false;

    //texturing and viewport
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

        //light direction
        Vector3 l = light.transform.forward;
        RayTracingShader.SetVector("_DirectionalLight", new Vector4(l.x, l.y, l.z, light.intensity));

        //array of Spheres
        RayTracingShader.SetBuffer(0, "_Spheres", _SphereBuffer);

        //camera matrix
        RayTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        RayTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
    }

    private void SetUpScene()
    {
        List<Sphere> spheres = new List<Sphere>();

        //Add number of sphere equals to the public
        //SphereMax value
        for(int i = 0; i < SphereMax; i++)
        {
            Sphere sphere = new Sphere();
            sphere.radius = SphereRadius.x + Random.value * (SphereRadius.y - SphereRadius.x);
            Vector2 randomPos = Random.insideUnitCircle * SpherePlacementRadius;
            sphere.position = new Vector3(randomPos.x, sphere.radius, randomPos.y);

            foreach(Sphere other in spheres)
            {
                float minDist = sphere.radius + other.radius;
                if (Vector3.SqrMagnitude(sphere.position - other.position) < minDist * minDist)
                    goto SkipSphere;
            }

            Color color = Random.ColorHSV();
            bool metal = Random.value < 0.5f;

            sphere.albedo = metal ? Vector3.zero: new Vector3(color.r, color.g, color.b);
            sphere.specular = metal ? new Vector3(color.r, color.g, color.b) : Vector3.one * 0.03f;

            spheres.Add(sphere);

            SkipSphere:
            continue;
        }

        //explaing this black magic
        // the stride is 40 because the struc Sphere
        // take 40 bytes in the memory (vec3 are 3 float)
        // so we adresse 40 bytes * spheres.Count
        _SphereBuffer = new ComputeBuffer(spheres.Count, 40);
        _SphereBuffer.SetData(spheres);
    }

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        _currentSample = 0;
        SetUpScene();
    }

    private void OnDisable()
    {
        if (_SphereBuffer != null)
            _SphereBuffer.Release();
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
