using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class edgeDetector : MonoBehaviour
{

    public Shader shader;
    Material mat;

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if(mat == null)
        {
            mat = new Material(shader);
        }

        Graphics.Blit(source, destination, mat);
    }

}
