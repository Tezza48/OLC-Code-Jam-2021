using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Materials
{
    public static readonly Color32 AIR = new Color32(0, 0, 0, 0);
    public static readonly Color32 SOLID_RED = new Color32(0xff, 0x00, 0x00, 0xff);
    public static readonly Color32 SAND = new Color32(0xd9, 0xc7, 0x8b, 0xff);
}

public class PixelSimulationCanvas : MonoBehaviour
{
    public Material canvasMaterial;

    public Vector2Int canvasSize = new Vector2Int(1280, 720);

    private Texture2D canvasTexture;

    // Start is called before the first frame update
    void Start()
    {
        canvasTexture = new Texture2D(canvasSize.x, canvasSize.y, TextureFormat.RGBA32, false, true);
        canvasTexture.filterMode = FilterMode.Point;
        canvasMaterial.mainTexture = canvasTexture;
        ClearCanvas();

        InvokeRepeating("UpdateCanvas", 0, 0.1f);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            var mousePos = GetMousePos();

            canvasTexture.SetPixel((int)mousePos.x, (int)mousePos.y, Materials.SAND);

            canvasTexture.Apply();
        }

        //UpdateCanvas();
    }

    void UpdateCanvas()
    {
        var texels = canvasTexture.GetPixels32();
        bool isDirty = false;

        for (int y = 0; y < canvasSize.y; y++)
        {
            for (int x = 0; x < canvasSize.x; x++)
            {
                //canvasTexture.SetPixel(x, y, new Color32(RandomByte(), RandomByte(), RandomByte(), 0xff));

                var bottomLeft = (y - 1) * canvasSize.x + (x - 1);
                var bottom = (y - 1) * canvasSize.x + x;
                var bottomRight = (y - 1) * canvasSize.x + (x + 1);

                var left = (y) * canvasSize.x + (x - 1);
                var center = (y) * canvasSize.x + x;
                var right = (y) * canvasSize.x + (x + 1);

                var topLeft = (y + 1) * canvasSize.x + (x - 1);
                var top = (y + 1) * canvasSize.x + x;
                var topRight = (y + 1) * canvasSize.x + (x + 1);

                // TODO WT: Remove this early return and do actual edge detection
                if (x == 0 || x == canvasSize.x - 1 || y == 0 || y == canvasSize.y - 1)
                {
                    continue;
                }

                var currentMaterial = texels[y * canvasSize.x + x];

                if (currentMaterial.Equals(Materials.SAND))
                {
                    if (texels[bottom].Equals(Materials.AIR))
                    {
                        texels[bottom] = Materials.SAND;
                        texels[center] = Materials.AIR;
                        isDirty = true;
                    }
                }
            }
        }

        if (isDirty)
        {
            Debug.Log("Update");
            canvasTexture.SetPixels32(texels);
            canvasTexture.Apply();
        }
    }

    void ClearCanvas()
    {
        canvasTexture.SetPixels32(Enumerable.Repeat(Materials.AIR, canvasSize.x * canvasSize.y).ToArray());
    }

    byte RandomByte()
    {
        return (byte)(Random.value * byte.MaxValue);
    }

    Vector2 GetMousePos()
    {
        return (Vector2)Input.mousePosition / new Vector2(Screen.width, Screen.height) * (Vector2)canvasSize;
    }
}
