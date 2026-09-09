using System.IO;
using UnityEngine;

public class SaveCanvasToFile : MonoBehaviour
{
    public Camera renderCam;
    public RenderTexture rt;


    void Start()
    {
        RenderTexture.active = rt;
        Texture2D texture2D = new(rt.width, rt.height, UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32_SFloat, UnityEngine.Experimental.Rendering.TextureCreationFlags.None);
        texture2D.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
        var data = ImageConversion.EncodeToEXR(texture2D);
        File.WriteAllBytes("C:\\Users\\jgkaplan\\Desktop\\out.exr", data);
    }
}
