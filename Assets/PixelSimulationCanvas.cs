using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum SeedEvent: byte
{
    PLANTED,
    GROW,
    DIE,
}

public static class Materials
{
    public const uint AIR = 0x00000000;
    public const uint SOLID_RED = 0xff0000ff;
    public const uint SAND = 0xff00ffff;
    public const uint SOIL = 0xff254c6e; // 6e4c25
    public const uint WATER = 0xffff0000;
    public const uint LIVE_PLANT = 0xff00ff00;
    public const uint MATURE_PLANT = 0xff7fff7f;
    public const uint DEAD_PLANT = 0xff7f7fff;
    // TODO WT: Vapour

    public enum PhysicsType
    {
        NONE,
        PARTICLE_GRAVITY,
        LIQUID_GRAVITY,
        PLANT_GRAVITY,
    }

    public static uint[] ALL = new uint[]
    {
        AIR,
        SOLID_RED,
        SAND,
        SOIL,
        WATER,
        LIVE_PLANT,
        //MATURE_PLANT, // Not allowed
        //DEAD_PLANT, // Not allowed
    };

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
            case LIVE_PLANT:
                return 50;
            case MATURE_PLANT:
                return 50;
            case DEAD_PLANT:
                return 40;
            default:
                return 0;
        }
    }

    public static string GetName(uint material)
    {
        switch (material)
        {
            case AIR:
                return "Air";
            case SOLID_RED:
                return "Solid Red";
            case SAND:
                return "Sand";
            case SOIL:
                return "Soil";
            case WATER:
                return "Water";
            case LIVE_PLANT:
                return "Plant";
            case MATURE_PLANT:
                return "Mature Plant";
            case DEAD_PLANT:
                return "Dead Plant";
            default:
                return "Unknown";
        }
    }

    public static PhysicsType getGravityType(uint material)
    {
        switch (material)
        {
            case SAND:
            case SOIL:
            case DEAD_PLANT:
                return PhysicsType.PARTICLE_GRAVITY;

            case WATER:
                return PhysicsType.LIQUID_GRAVITY;

            case LIVE_PLANT:
            case MATURE_PLANT:
                return PhysicsType.PLANT_GRAVITY;

            case AIR:
            case SOLID_RED:
            default:
                return PhysicsType.NONE;
        }
    }

    public static Color32 ToColor32(uint material)
    {
        return new Color32((byte)(material & 0xff), (byte)(material >> 8 & 0xff), (byte)(material >> 16 & 0xff), (byte)(material >> 24 & 0xff));
    }
}

public struct Plant
{
    byte age;
    byte x;
    byte y;
    byte id;
}

public class PixelSimulationCanvas : MonoBehaviour
{
    public Material canvasMaterial;

    public Vector2Int canvasSize = new Vector2Int(1280, 720);

    private Texture2D canvasTexture;

    private uint[] pixelData;
    private bool isTextureDirty = false;

    public bool isServer = true;

    //public bool seedMode = false;
    public uint currentMaterial = Materials.SAND;

    private long frame = 0;

    private float nextCanvasUpdate;
    private float canvasTickInterval = 1.0f / 25.0f;

    private Rect Gui_rect;

    // Start is called before the first frame update
    void Start()
    {
        if (isServer)
        {
            ReceiveCanvasRefreshEvent(new uint[canvasSize.x * canvasSize.y], 0);
        }

        canvasTexture = new Texture2D(canvasSize.x, canvasSize.y, TextureFormat.RGBA32, false, true);

        canvasTexture.filterMode = FilterMode.Point;
        canvasMaterial.mainTexture = canvasTexture;

        isTextureDirty = true;

        nextCanvasUpdate = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Alpha1))
        //{
        //    currentMaterial = Materials.SOLID_RED;
        //}
        //if (Input.GetKeyDown(KeyCode.Alpha2))
        //{
        //    currentMaterial = Materials.SAND;
        //}
        //if (Input.GetKeyDown(KeyCode.Alpha3))
        //{
        //    currentMaterial = Materials.SOIL;
        //}
        //if (Input.GetKeyDown(KeyCode.Alpha4))
        //{
        //    currentMaterial = Materials.WATER;
        //}
        if (!Gui_rect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
        {
            if (Input.GetMouseButtonDown(0))
            {
                var mousePos = GetCanvasMousePos();
                var mousePosInt = new Vector2Int((int)mousePos.x, (int)mousePos.y);

                //if (seedMode)
                //{
                //    sendSeedPlantedEvent(mousePosInt);
                //}

                //else
                {
                    SendClickEvent(mousePosInt, currentMaterial);
                }
            }
        }

        if (isServer && Time.time > nextCanvasUpdate)
        {
            nextCanvasUpdate = Time.time + canvasTickInterval;
            SendTickEvent(this.frame);
            Debug.Log("Tick");
            frame++;
        }
    }

    void UpdateCanvas()
    {
        Random.InitState((int)frame);

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
                        DoParticleGravity(banned, bottomLeft, bottom, bottomRight, center, currentMaterial, currentMass);

                        break;
                    case Materials.PhysicsType.LIQUID_GRAVITY:
                        var newPos = center;

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

                    case Materials.PhysicsType.PLANT_GRAVITY:
                        // TODO WT: Like ParticleGravity but only falls if there's no neighbouring materials.

                        bool hasSolidNeighbour = false;
                        //for (int cy = Mathf.Max(0, y - 1); cy < Mathf.Min(canvasSize.y - 1, y + 2); cy++)
                        //{
                        //    for (int cx = Mathf.Max(0, x - 1); cx < Mathf.Min(canvasSize.x - 1, x + 2); cx++)
                        //    {
                        //        hasSolidNeighbour = hasSolidNeighbour || 
                        //    }
                        //}

                        LoopNeibouring(new Vector2Int(x - 1, y - 1), new Vector2Int(x + 1, y - 1), (cx, cy) =>
                        {
                            if (cx == x && cy == y) return;
                            hasSolidNeighbour = hasSolidNeighbour || pixelData[cy * canvasSize.x + cx] != Materials.AIR;
                        });

                        if (!hasSolidNeighbour)
                        {
                            DoParticleGravity(banned, bottomLeft, bottom, bottomRight, center, currentMaterial, currentMass);
                        }

                        break;
                    default:
                        break;
                }

                switch (currentMaterial)
                {
                    case Materials.LIVE_PLANT:
                        var growChance = 0.01f;
                        var growChanceScaler = 0.01f;

                        var matureChance = growChance;
                        var matureChanceScaler = 2.0f;

                        if (Random.value < growChance)
                        {
                            // Try to grow a shoot upwards
                            if (pixelData[top] == Materials.AIR)
                            {
                                pixelData[top] = Materials.LIVE_PLANT;
                                isTextureDirty = true;
                                banned.Add(top);

                                growChance *= growChanceScaler;
                                matureChance *= matureChanceScaler;
                            }
                        }

                        if (Random.value < growChance)
                        {
                            if (pixelData[topLeft] == Materials.AIR)
                            {
                                pixelData[topLeft] = Materials.LIVE_PLANT;
                                isTextureDirty = true;
                                banned.Add(topLeft);

                                growChance *= growChanceScaler;
                                matureChance *= matureChanceScaler;
                            }
                        }

                        if (Random.value < growChance)
                        {
                            if (pixelData[topRight] == Materials.AIR)
                            {
                                pixelData[topRight] = Materials.LIVE_PLANT;
                                isTextureDirty = true;
                                banned.Add(topRight);

                                growChance *= growChanceScaler;
                                matureChance *= matureChanceScaler;
                            }
                        }

                        if (Random.value < matureChance)
                        {
                            pixelData[center] = Materials.MATURE_PLANT;
                            isTextureDirty = true;
                        }

                        DoPlantDeathCheck(x, y);

                        break;

                    case Materials.MATURE_PLANT:
                        DoPlantDeathCheck(x, y);

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

    private void DoParticleGravity(List<int> banned, int bottomLeft, int bottom, int bottomRight, int center, uint currentMaterial, int currentMass)
    {
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
            }
            else
            {
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
    }

    void DoPlantDeathCheck(int x, int y)
    {
        var neighbourCount = 0;
        LoopNeibouring(new Vector2Int(x - 1, y - 1), new Vector2Int(x + 1, y + 1), (cx, cy) =>
        {
            if (cx != x && cy != y) return;
            
            if (pixelData[cy * canvasSize.x + cx] != Materials.AIR) neighbourCount++;
        });

        Debug.Log(neighbourCount);

        if (neighbourCount > 4)
        {
            pixelData[y * canvasSize.x + x] = Materials.DEAD_PLANT;
            isTextureDirty = true;
        }
    }

    byte RandomByte()
    {
        return (byte)(Random.value * byte.MaxValue);
    }

    Vector2 GetCanvasMousePos()
    {
        return (Vector2)Input.mousePosition / new Vector2(Screen.width, Screen.height) * (Vector2)canvasSize;
    }

    private void OnGUI()
    {
        Gui_rect = GUILayout.Window(0, Gui_rect, GuiWindowCallback, "Settings");
    }

    private void SendClickEvent(Vector2Int position, uint material)
    {
        ReceiveClickEvent(position, material);
    }

    //private void sendSeedPlantedEvent(Vector2Int position)
    //{
    //    ReceiveSeedPlantedEvent(position);
    //}

    private void SendTickEvent(long frame)
    {
        ReceiveTickEvent(this.frame);
    }

    private void ReceiveClickEvent(Vector2Int position, uint material)
    {
        pixelData[position.y * canvasSize.x + position.x] = material;

        if (material != Materials.LIVE_PLANT && material != Materials.DEAD_PLANT)
        {
            // Top
            if (position.x > 0 && position.y > 0)
            {
                pixelData[(position.y - 1) * canvasSize.x + (position.x - 1)] = material;
            }
            if (position.y > 0)
            {
                pixelData[(position.y - 1) * canvasSize.x + position.x] = material;
            }
            if (position.x < canvasSize.x - 1 && position.y > 0)
            {
                pixelData[(position.y - 1) * canvasSize.x + (position.x + 1)] = material;
            }

            // Center
            if (position.x > 0)
            {
                pixelData[position.y * canvasSize.x + (position.x - 1)] = material;
            }

            if (position.x < canvasSize.x - 1)
            {
                pixelData[position.y * canvasSize.x + (position.x + 1)] = material;
            }

            // Bottom
            if (position.x > 0 && position.y < canvasSize.y - 1)
            {
                pixelData[(position.y + 1) * canvasSize.x + (position.x - 1)] = material;
            }
            if (position.y < canvasSize.y - 1)
            {
                pixelData[(position.y + 1) * canvasSize.x + position.x] = material;
            }
            if (position.x < canvasSize.x - 1 && position.y < canvasSize.y - 1)
            {
                pixelData[(position.y + 1) * canvasSize.x + (position.x + 1)] = material;
            }
        }

        isTextureDirty = true;
    }

    //private void ReceiveSeedPlantedEvent(Vector2Int position)
    //{

    //}

    delegate void Callback(int x, int y);
    private void LoopNeibouring(Vector2Int min, Vector2Int max, Callback callback)
    {
        for (int y = min.y; y <= max.y; y++)
        {
            for (int x = min.x; x <= max.x; x++)
            {
                callback(x, y);
            }
        }
    }

    private void ReceiveCanvasRefreshEvent(uint[] newPixels, long frame)
    {
        pixelData = newPixels;
        this.frame = frame;
        this.isTextureDirty = true;
    }

    private void ReceiveTickEvent(long frame)
    {
        this.frame = frame;
        UpdateCanvas();

        // TODO WT: If more than a few frames behind, need to send a refresh request event.
    }

    private void GuiWindowCallback(int id)
    {
        GUILayout.Label("Selected: " + Materials.GetName(currentMaterial));
        foreach (var material in Materials.ALL)
        {
            if (GUILayout.Button(Materials.GetName(material)))
            {
                currentMaterial = material;
            }
        }
    }
}
