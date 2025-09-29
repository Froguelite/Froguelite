using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "NewAutoTileSet", menuName = "Froguelite/AutoTileSet")]
public class AutoTileSet : ScriptableObject
{

    // Enum representing different tile types for auto-tiling
    public enum AutoTileType
    {
        FullWater,          // All water
        FullLand,           // All land
        HalfWaterTop,       // Water on top half, land on bottom
        HalfWaterBottom,    // Water on bottom half, land on top
        HalfWaterLeft,      // Water on left half, land on right
        HalfWaterRight,     // Water on right half, land on left
        ThreeQuarterWaterBottomLeft,  // 3/4 water, land only in bottom-left corner
        ThreeQuarterWaterBottomRight, // 3/4 water, land only in bottom-right corner
        ThreeQuarterWaterTopLeft,     // 3/4 water, land only in top-left corner
        ThreeQuarterWaterTopRight,    // 3/4 water, land only in top-right corner
        ThreeQuarterLandBottomLeft,   // 3/4 land, water only in bottom-left corner
        ThreeQuarterLandBottomRight,  // 3/4 land, water only in bottom-right corner
        ThreeQuarterLandTopLeft,      // 3/4 land, water only in top-left corner
        ThreeQuarterLandTopRight      // 3/4 land, water only in top-right corner
    }

    public TileBase fullWater;
    public TileBase fullLand;
    public TileBase halfWaterTop;
    public TileBase halfWaterBottom;
    public TileBase halfWaterLeft;
    public TileBase halfWaterRight;
    public TileBase threeQuarterWaterBottomLeft;
    public TileBase threeQuarterWaterBottomRight;
    public TileBase threeQuarterWaterTopLeft;
    public TileBase threeQuarterWaterTopRight;
    public TileBase threeQuarterLandBottomLeft;
    public TileBase threeQuarterLandBottomRight;
    public TileBase threeQuarterLandTopLeft;
    public TileBase threeQuarterLandTopRight;

    // Gets the appropriate tile based on given AutoTileType
    public TileBase GetTile(AutoTileType tileType)
    {
        switch (tileType)
        {
            case AutoTileType.FullWater: return fullWater;
            case AutoTileType.FullLand: return fullLand;
            case AutoTileType.HalfWaterTop: return halfWaterTop;
            case AutoTileType.HalfWaterBottom: return halfWaterBottom;
            case AutoTileType.HalfWaterLeft: return halfWaterLeft;
            case AutoTileType.HalfWaterRight: return halfWaterRight;
            case AutoTileType.ThreeQuarterWaterBottomLeft: return threeQuarterWaterBottomLeft;
            case AutoTileType.ThreeQuarterWaterBottomRight: return threeQuarterWaterBottomRight;
            case AutoTileType.ThreeQuarterWaterTopLeft: return threeQuarterWaterTopLeft;
            case AutoTileType.ThreeQuarterWaterTopRight: return threeQuarterWaterTopRight;
            case AutoTileType.ThreeQuarterLandBottomLeft: return threeQuarterLandBottomLeft;
            case AutoTileType.ThreeQuarterLandBottomRight: return threeQuarterLandBottomRight;
            case AutoTileType.ThreeQuarterLandTopLeft: return threeQuarterLandTopLeft;
            case AutoTileType.ThreeQuarterLandTopRight: return threeQuarterLandTopRight;
            default: return fullWater;
        }
    }
}
