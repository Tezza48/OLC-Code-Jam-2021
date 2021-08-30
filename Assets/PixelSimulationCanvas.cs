using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Materials
{
    public const uint AIR = 0x00000000;
    public const uint SOLID_RED = 0xff0000ff;
    public const uint SAND = 0xff00ffff;
    public const uint SOIL = 0xff254c6e; // 6e4c25
    public const uint WATER = 0xffff0000;
    // TODO WT: Vapour

    public enum PhysicsType
    {
        NONE,
        PARTICLE_GRAVITY,
        LIQUID_GRAVITY,
    }

    public static int GetMass(uint material)
    {
        switch (material)
        {
            case AIR:
                return 20;
            case SOLID_RED:
                return 50;
            case SAND:
                return 50;
            case SOIL:
                return 50;
            case WATER:
                return 30;
            default:
                return 0;
        }
    }

    public static PhysicsType getGravityType(uint material)
    {
        switch (material)
        {
            case SAND:
            case SOIL:
                return PhysicsType.PARTICLE_GRAVITY;

            case WATER:
                return PhysicsType.LIQUID_GRAVITY;

            case AIR:
            case SOLID_RED:
            default:
                return PhysicsType.NONE;
        }
    }
}

public class PixelSimulationCanvas : MonoBehaviour
{
    public Material canvasMaterial;

    public Vector2Int canvasSize = new Vector2Int(1280, 720);

    private Texture2D canvasTexture;

    private uint[] pixelData;
    private bool isTextureDirty = false;

    public uint currentMaterial = Materials.SAND;

    private long frame = 0;

    // Start is called before the first frame update
    void Start()
    {
        pixelData = new uint[canvasSize.x * canvasSize.y];

        canvasTexture = new Texture2D(canvasSize.x, canvasSize.y, TextureFormat.RGBA32, false, true);

        canvasTexture.filterMode = FilterMode.Point;
        canvasMaterial.mainTexture = canvasTexture;
        ClearCanvas();

        InvokeRepeating("UpdateCanvas", 0, 1.0f / 5f);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            currentMaterial = Materials.SOLID_RED;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            currentMaterial = Materials.SAND;
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            currentMaterial = Materials.SOIL;
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            currentMaterial = Materials.WATER;
        }

        if (Input.GetMouseButton(0))
        {
            var mousePos = GetMousePos();

            pixelData[(int)mousePos.y * canvasSize.x + (int)mousePos.x] = currentMaterial;
            isTextureDirty = true;
        }
    }

    void UpdateCanvas()
    {
        frame++;

        List<int> banned = new List<int>();
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

                if (banned.Contains(center)) continue;

                banned.Add(center);

                var currentMaterial = pixelData[y * canvasSize.x + x];
                var currentMass = Materials.GetMass(currentMaterial);

                switch (Materials.getGravityType(currentMaterial))
                {
                    case Materials.PhysicsType.PARTICLE_GRAVITY:
                        var newPos = center;

                        if (currentMass > Materials.GetMass(pixelData[bottom]))
                        {
                            newPos = bottom;
                        }
                        else
                        {
                            if (Random.value > 0.5)
                            {
                                if (currentMass > Materials.GetMass(pixelData[bottomLeft]))
                                {
                                    newPos = bottomLeft;
                                }
                                else if (currentMass > Materials.GetMass(pixelData[bottomRight]))
                                {
                                    newPos = bottomRight;
                                }
                            } else { 
                                if (currentMass > Materials.GetMass(pixelData[bottomRight]))
                                {
                                    newPos = bottomRight;
                                }
                                else if (currentMass > Materials.GetMass(pixelData[bottomLeft]))
                                {
                                    newPos = bottomLeft;
                                }
                            }
                        }

                        if (newPos != center)
                        {
                            pixelData[center] = pixelData[newPos];
                            pixelData[newPos] = currentMaterial;
                            banned.Add(newPos);

                            isTextureDirty = true;
                        }

                        break;
                    case Materials.PhysicsType.LIQUID_GRAVITY:
                        newPos = center;

                        if (pixelData[bottom] == Materials.AIR)
                        {
                            newPos = bottom;
                        }
                        else
                        {
                            if (Random.value > 0.5)
                            {
                                if (pixelData[bottomLeft] == Materials.AIR)
                                {
                                    newPos = bottomLeft;
                                }
                                else if (pixelData[bottomRight] == Materials.AIR)
                                {
                                    newPos = bottomRight;
                                }
                                else if (pixelData[left] == Materials.AIR)
                                {
                                    newPos = left;
                                }
                                else if (pixelData[right] == Materials.AIR)
                                {
                                    newPos = right;
                                }
                            } else
                            {
                                if (pixelData[bottomRight] == Materials.AIR)
                                {
                                    newPos = bottomRight;
                                }
                                else if (pixelData[bottomLeft] == Materials.AIR)
                                {
                                    newPos = bottomLeft;
                                }
                                else if (pixelData[right] == Materials.AIR)
                                {
                                    newPos = right;
                                }
                                else if (pixelData[left] == Materials.AIR)
                                {
                                    newPos = left;
                                }
                            }
                        }

                        if (newPos != center)
                        {
                            pixelData[center] = pixelData[newPos];
                            pixelData[newPos] = currentMaterial;
                            banned.Add(newPos);

                            isTextureDirty = true;
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        if (isTextureDirty)
        {
            isTextureDirty = false;

            canvasTexture.SetPixelData(pixelData, 0);
            canvasTexture.Apply();
        }
    }

    void ClearCanvas()
    {
        pixelData = Enumerable.Repeat(Materials.AIR, pixelData.Length).ToArray();
        isTextureDirty = true;
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
