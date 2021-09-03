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

public enum MaterialType
{
    AIR,
    SOLID,
    SAND,
    WATER,
    LIVE_PLANT,
    MATURE_PLANT,
}

public static class Materials
{
    //public const uint AIR = 0x00000000;
    //public const uint SOLID_RED = 0xff0000ff;
    //public const uint SAND = 0xff00ffff;
    //public const uint SOIL = 0xff254c6e; // 6e4c25
    //public const uint WATER = 0xffff0000;
    //public const uint LIVE_PLANT = 0xff00ff00;
    //public const uint MATURE_PLANT = 0xff7fff7f;
    //public const uint DEAD_PLANT = 0xff7f7fff;
    // TODO WT: Vapour

    public enum PhysicsType
    {
        NONE,
        PARTICLE_GRAVITY,
        LIQUID_GRAVITY,
        PLANT_GRAVITY,
    }

    public static MaterialType[] ALL = new MaterialType[]
    {
        MaterialType.AIR,
        MaterialType.SOLID,
        MaterialType.SAND,
        MaterialType.WATER,
        MaterialType.LIVE_PLANT,
        //MATURE_PLANT, // Not allowed
        //DEAD_PLANT, // Not allowed
    };

    //public static int GetMass(uint material)
    //{
    //    switch (material)
    //    {
    //        case AIR:
    //            return 20;
    //        case SOLID_RED:
    //            return 50;
    //        case SAND:
    //            return 50;
    //        case SOIL:
    //            return 50;
    //        case WATER:
    //            return 30;
    //        case LIVE_PLANT:
    //            return 50;
    //        case MATURE_PLANT:
    //            return 50;
    //        case DEAD_PLANT:
    //            return 40;
    //        default:
    //            return 0;
    //    }
    //}

    //public static string GetName(MaterialType material)
    //{
    //    switch (material)
    //    {
    //        case AIR:
    //            return "Air";
    //        case SOLID_RED:
    //            return "Solid Red";
    //        case SAND:
    //            return "Sand";
    //        case SOIL:
    //            return "Soil";
    //        case WATER:
    //            return "Water";
    //        case LIVE_PLANT:
    //            return "Plant";
    //        case MATURE_PLANT:
    //            return "Mature Plant";
    //        case DEAD_PLANT:
    //            return "Dead Plant";
    //        default:
    //            return "Unknown";
    //    }
    //}

    //public static PhysicsType getGravityType(uint material)
    //{
    //    switch (material)
    //    {
    //        case SAND:
    //        case SOIL:
    //        case DEAD_PLANT:
    //            return PhysicsType.PARTICLE_GRAVITY;

    //        case WATER:
    //            return PhysicsType.LIQUID_GRAVITY;

    //        case LIVE_PLANT:
    //        case MATURE_PLANT:
    //            return PhysicsType.PLANT_GRAVITY;

    //        case AIR:
    //        case SOLID_RED:
    //        default:
    //            return PhysicsType.NONE;
    //    }
    //}

    public static Color32 ToColor32(uint material)
    {
        return new Color32((byte)(material & 0xff), (byte)(material >> 8 & 0xff), (byte)(material >> 16 & 0xff), (byte)(material >> 24 & 0xff));
    }
}

public class PixelSimulationCanvas : MonoBehaviour
{
    public Material canvasMaterial;

    public Vector2Int canvasSize = new Vector2Int(1280, 720);

    public ComputeShader cs;

    private RenderTexture stateTexture;
    private RenderTexture canvasTexture;

    private uint[] pixelData;

    public bool isServer = true;

    //public bool seedMode = false;
    public MaterialType currentMaterial = MaterialType.SAND;

    private long frame = 0;

    private float nextCanvasUpdate;
    public float canvasTickRate = 15.0f;
    private float canvasTickInterval;

    private Rect Gui_rect;

    private void OnValidate()
    {
        canvasTickInterval = 1.0f / canvasTickRate;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (isServer)
        {
            ReceiveCanvasRefreshEvent(new uint[canvasSize.x * canvasSize.y], 0);
        }

        stateTexture = new RenderTexture(canvasSize.x, canvasSize.y, 1, RenderTextureFormat.RInt);
        stateTexture.enableRandomWrite = true;
        cs.SetTexture(0, "State", stateTexture);
        cs.SetTexture(1, "State", stateTexture);
        cs.SetTexture(2, "State", stateTexture);

        canvasTexture = new RenderTexture(canvasSize.x, canvasSize.y, 1, RenderTextureFormat.ARGBFloat, 0);
        canvasMaterial.mainTexture = canvasTexture;
        canvasTexture.filterMode = FilterMode.Point;
        canvasTexture.enableRandomWrite = true;
        cs.SetTexture(0, "Result", canvasTexture);
        cs.SetTexture(1, "Result", canvasTexture);

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
            if (Input.GetMouseButton(0))
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
            //var fence = Graphics.CreateGraphicsFence(UnityEngine.Rendering.GraphicsFenceType.CPUSynchronisation, UnityEngine.Rendering.SynchronisationStageFlags.ComputeProcessing);
            //cs.Dispatch(0, canvasTexture.width, canvasTexture.height, 1);
            ////while (!fence.passed) { }
            //cs.Dispatch(1, canvasTexture.width, canvasTexture.height, 1);
            StartCoroutine(RunComputeSection());
            nextCanvasUpdate = Time.time + canvasTickInterval;
            //SendTickEvent(frame);
            //frame++;
        }
    }

    IEnumerator RunComputeSection()
    {
        cs.Dispatch(0, canvasTexture.width, canvasTexture.height, 1);
        var fence = Graphics.CreateGraphicsFence(UnityEngine.Rendering.GraphicsFenceType.CPUSynchronisation, UnityEngine.Rendering.SynchronisationStageFlags.ComputeProcessing);

        while (!fence.passed) {
            Debug.Log("Fence has not passed yet");
            yield return new WaitForEndOfFrame();
        }

        Debug.Log("Dispatching the second kernel");

        cs.Dispatch(1, canvasTexture.width, canvasTexture.height, 1);
    }

    //void UpdateCanvas()
    //{
    //    Random.InitState((int)frame);

    //    List<int> banned = new List<int>();
    //    for (int y = 0; y < canvasSize.y; y++)
    //    {
    //        for (int x = 0; x < canvasSize.x; x++)
    //        {
    //            var thisIndex = GetIndex(x, y);
    //            if (banned.Contains(thisIndex)) continue;

    //            banned.Add(thisIndex);

    //            var thisMaterial = pixelData[GetIndex(x, y)];
    //            var thisMass = Materials.GetMass(thisMaterial);

    //            switch (Materials.getGravityType(thisMaterial))
    //            {
    //                case Materials.PhysicsType.PARTICLE_GRAVITY:
    //                    DoParticleGravity(x, y, thisMaterial, thisMass, banned);

    //                    break;
    //                case Materials.PhysicsType.LIQUID_GRAVITY:
    //                    DoWaterGravity(x, y, thisMaterial, thisMass, banned);
    //                    break;

    //                case Materials.PhysicsType.PLANT_GRAVITY:
    //                    bool hasSolidNeighbour = false;

    //                    LoopNeibouring(new Vector2Int(x - 1, y - 1), new Vector2Int(x + 1, y - 1), (cx, cy) =>
    //                    {
    //                        var index = GetIndex(cx, cy);

    //                        if ((cx == x && cy == y) || !IsInBounds(cx, cy)) return;

    //                        var thisPixel = pixelData[index];

    //                        hasSolidNeighbour = hasSolidNeighbour || (thisPixel != Materials.AIR && thisPixel != Materials.WATER);
    //                    });

    //                    if (!hasSolidNeighbour)
    //                    {
    //                        DoParticleGravity(x, y, thisMaterial, thisMass, banned);
    //                    }

    //                    break;
    //                default:
    //                    break;
    //            }

    //            switch (thisMaterial)
    //            {
    //                case Materials.LIVE_PLANT:
    //                    var growChance = 0.01f;
    //                    var growChanceScaler = 0.01f;

    //                    var matureChance = growChance;
    //                    var matureChanceScaler = 2.0f;

    //                    var top = GetIndex(x, y + 1);
    //                    var topLeft = GetIndex(x - 1, y + 1);
    //                    var topRight = GetIndex(x + 1, y + 1);

    //                    if (Random.value < growChance)
    //                    {
    //                        // Try to grow a shoot upwards
    //                        if (IsInBounds(x, y + 1) && pixelData[top] == Materials.AIR)
    //                        {
    //                            pixelData[top] = Materials.LIVE_PLANT;
    //                            banned.Add(top);

    //                            growChance *= growChanceScaler;
    //                            matureChance *= matureChanceScaler;
    //                        }
    //                    }

    //                    if (Random.value < growChance)
    //                    {
    //                        if (IsInBounds(x - 1, y + 1) && pixelData[topLeft] == Materials.AIR)
    //                        {
    //                            pixelData[topLeft] = Materials.LIVE_PLANT;
    //                            banned.Add(topLeft);

    //                            growChance *= growChanceScaler;
    //                            matureChance *= matureChanceScaler;
    //                        }
    //                    }

    //                    if (Random.value < growChance)
    //                    {
    //                        if (IsInBounds(x + 1, y + 1) && pixelData[topRight] == Materials.AIR)
    //                        {
    //                            pixelData[topRight] = Materials.LIVE_PLANT;
    //                            banned.Add(topRight);

    //                            growChance *= growChanceScaler;
    //                            matureChance *= matureChanceScaler;
    //                        }
    //                    }

    //                    if (Random.value < matureChance)
    //                    {
    //                        pixelData[thisIndex] = Materials.MATURE_PLANT;
    //                    }

    //                    DoPlantDeathCheck(x, y);

    //                    break;

    //                case Materials.MATURE_PLANT:
    //                    DoPlantDeathCheck(x, y);

    //                    break;
    //                default:
    //                    break;
    //            }
    //        }
    //    }

    //    //if (isTextureDirty)
    //    //{
    //    //    isTextureDirty = false;

    //    //    canvasTexture.SetPixelData(pixelData, 0);
    //    //    canvasTexture.Apply();
    //    //}
    //}

    //private void DoParticleGravity(int x, int y, uint material, int mass, List<int> banned)
    //{
    //    var thisIndex = GetIndex(x, y);
    //    var swapIndex = thisIndex;

    //    var bottom = GetIndex(x, y - 1);
    //    var bottomLeft = GetIndex(x - 1, y - 1);
    //    var bottomRight = GetIndex(x + 1, y - 1);

    //    if (IsInBounds(x, y - 1) && mass > Materials.GetMass(pixelData[bottom]))
    //    {
    //        swapIndex = bottom;
    //    }
    //    else
    //    {
    //        if (Random.value > 0.5)
    //        {
    //            if (IsInBounds(x - 1, y - 1) && mass > Materials.GetMass(pixelData[bottomLeft]))
    //            {
    //                swapIndex = bottomLeft;
    //            }
    //            else if (IsInBounds(x + 1, y - 1) && mass > Materials.GetMass(pixelData[bottomRight]))
    //            {
    //                swapIndex = bottomRight;
    //            }
    //        }
    //        else
    //        {
    //            if (IsInBounds(x + 1, y - 1) && mass > Materials.GetMass(pixelData[bottomRight]))
    //            {
    //                swapIndex = bottomRight;
    //            }
    //            else if (IsInBounds(x - 1, y - 1) && mass > Materials.GetMass(pixelData[bottomLeft]))
    //            {
    //                swapIndex = bottomLeft;
    //            }
    //        }
    //    }

    //    if (swapIndex != thisIndex)
    //    {
    //        pixelData[thisIndex] = pixelData[swapIndex];
    //        pixelData[swapIndex] = material;
    //        banned.Add(swapIndex);
    //    }
    //}


    //private void DoWaterGravity(int x, int y, uint material, int mass, List<int> banned)
    //{
    //    var thisIndex = GetIndex(x, y);
    //    var swapIndex = thisIndex;

    //    var bottom = GetIndex(x, y - 1);
    //    var bottomLeft = GetIndex(x - 1, y - 1);
    //    var bottomRight = GetIndex(x + 1, y - 1);
    //    var left = GetIndex(x - 1, y);
    //    var right = GetIndex(x + 1, y);

    //    if (IsInBounds(x, y - 1) && mass > Materials.GetMass(pixelData[bottom]))
    //    {
    //        swapIndex = bottom;
    //    }
    //    else
    //    {
    //        if (Random.value > 0.5)
    //        {
    //            if (IsInBounds(x - 1, y - 1) && mass > Materials.GetMass(pixelData[bottomLeft]))
    //            {
    //                swapIndex = bottomLeft;
    //            }
    //            else if (IsInBounds(x + 1, y - 1) && mass > Materials.GetMass(pixelData[bottomRight]))
    //            {
    //                swapIndex = bottomRight;
    //            }
    //            else if (IsInBounds(x - 1, y) && mass > Materials.GetMass(pixelData[left]))
    //            {
    //                swapIndex = left;
    //            }
    //            else if (IsInBounds(x + 1, y) && mass > Materials.GetMass(pixelData[right]))
    //            {
    //                swapIndex = right;
    //            }
    //        }
    //        else
    //        {
    //            if (IsInBounds(x + 1, y - 1) && mass > Materials.GetMass(pixelData[bottomRight]))
    //            {
    //                swapIndex = bottomRight;
    //            }
    //            else if (IsInBounds(x - 1, y - 1) && mass > Materials.GetMass(pixelData[bottomLeft]))
    //            {
    //                swapIndex = bottomLeft;
    //            }
    //            else if (IsInBounds(x + 1, y) && mass > Materials.GetMass(pixelData[right]))
    //            {
    //                swapIndex = right;
    //            }
    //            else if (IsInBounds(x - 1, y) && mass > Materials.GetMass(pixelData[left]))
    //            {
    //                swapIndex = left;
    //            }
    //        }
    //    }

    //    if (swapIndex != thisIndex)
    //    {
    //        pixelData[thisIndex] = pixelData[swapIndex];
    //        pixelData[swapIndex] = material;
    //        banned.Add(swapIndex);
    //    }
    //}

    //void DoPlantDeathCheck(int x, int y)
    //{
    //    var neighbourCount = 0;
    //    LoopNeibouring(new Vector2Int(x - 1, y - 1), new Vector2Int(x + 1, y + 1), (cx, cy) =>
    //    {
    //        var index = GetIndex(cx, cy);

    //        if ((cx == x && cy == y) || !IsInBounds(cx, cy)) return;

    //        if (pixelData[index] != Materials.AIR) neighbourCount++;
    //    });

    //    if (neighbourCount > 4)
    //    {
    //        pixelData[y * canvasSize.x + x] = Materials.SOIL;
    //    }
    //}

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

    private void SendClickEvent(Vector2Int position, MaterialType material)
    {
        ReceiveClickEvent(position, material);
    }

    //private void sendSeedPlantedEvent(Vector2Int position)
    //{
    //    ReceiveSeedPlantedEvent(position);
    //}

    //private void SendTickEvent(long frame)
    //{
    //    ReceiveTickEvent(this.frame);
    //}

    private int GetIndex(int x, int y)
    {
        return y * canvasSize.x + x;
    }

    private bool IsInBounds(int x, int y)
    {
        return (x >= 0 && x < canvasSize.x && y >= 0 && y < canvasSize.y);
    }

    private void ReceiveClickEvent(Vector2Int position, MaterialType material)
    {
        // TODO WT: Figure out good way to draw a pixel

        cs.SetInts("writeCellMaterial", (int)currentMaterial);
        cs.SetInts("writeCellOffset", new int[] { position.x, position.y });
        // Write this material to the state at the given position.
        // Needs to allow for a radius too but need to then calculate the start offset and size.
        cs.Dispatch(2, 1, 1, 1);

        //pixelData[position.y * canvasSize.x + position.x] = material;

        //if (material != Materials.LIVE_PLANT && material != Materials.DEAD_PLANT)
        //{
        //    // Top
        //    if (position.x > 0 && position.y > 0)
        //    {
        //        pixelData[(position.y - 1) * canvasSize.x + (position.x - 1)] = material;
        //    }
        //    if (position.y > 0)
        //    {
        //        pixelData[(position.y - 1) * canvasSize.x + position.x] = material;
        //    }
        //    if (position.x < canvasSize.x - 1 && position.y > 0)
        //    {
        //        pixelData[(position.y - 1) * canvasSize.x + (position.x + 1)] = material;
        //    }

        //    // Center
        //    if (position.x > 0)
        //    {
        //        pixelData[position.y * canvasSize.x + (position.x - 1)] = material;
        //    }

        //    if (position.x < canvasSize.x - 1)
        //    {
        //        pixelData[position.y * canvasSize.x + (position.x + 1)] = material;
        //    }

        //    // Bottom
        //    if (position.x > 0 && position.y < canvasSize.y - 1)
        //    {
        //        pixelData[(position.y + 1) * canvasSize.x + (position.x - 1)] = material;
        //    }
        //    if (position.y < canvasSize.y - 1)
        //    {
        //        pixelData[(position.y + 1) * canvasSize.x + position.x] = material;
        //    }
        //    if (position.x < canvasSize.x - 1 && position.y < canvasSize.y - 1)
        //    {
        //        pixelData[(position.y + 1) * canvasSize.x + (position.x + 1)] = material;
        //    }
        //}

        //isTextureDirty = true;
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
    }

    //private void ReceiveTickEvent(long frame)
    //{
    //    this.frame = frame;
    //    UpdateCanvas();

    //    // TODO WT: If more than a few frames behind, need to send a refresh request event.
    //}

    private void GuiWindowCallback(int id)
    {
        GUILayout.Label("Selected: " + currentMaterial.ToString());
        foreach (var material in Materials.ALL)
        {
            if (GUILayout.Button(material.ToString()))
            {
                currentMaterial = material;
            }
        }
    }
}
