using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Pathfinding;
using StardewValley.Buildings;
using Microsoft.Xna.Framework;
using xTile.Tiles;
using xTile.Layers;
using StardewValley.GameData.Buildings;

namespace ExpandNPCPaths
{
    public class ModEntry : Mod
    {
        public static IMonitor ModMonitor = null!;

        public override void Entry(IModHelper helper)
        {
            ModMonitor = Monitor;

            var harmony = new Harmony(ModManifest.UniqueID);

            // NPCBarrier属性を削除
            harmony.Patch(
                original: AccessTools.Method(typeof(GameLocation), "loadObjects"),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(RemoveNPCBarrier))
            );

            // FarmとBackwoodsを経路探索の除外リストから削除
            harmony.Patch(
                original: AccessTools.Method(typeof(WarpPathfindingCache), "PopulateCache"),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(RemoveFromIgnoreLocationNames))
            );

            // Farmの通行制限を解除
            harmony.Patch(
                original: AccessTools.DeclaredMethod(typeof(Farm), "ShouldExcludeFromNpcPathfinding"),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(IncludeInNpcPathfinding))
            );

            // Backwoodsの通行制限を解除
            harmony.Patch(
                original: AccessTools.DeclaredMethod(typeof(GameLocation), "ShouldExcludeFromNpcPathfinding"),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(IncludeInNpcPathfinding))
            );
        }

        private static void RemoveNPCBarrier(GameLocation __instance)
        {
            if (__instance.NameOrUniqueName != "Farm")
            {
                return;
            }
        
            // マップのすべてのタイルを走査
            foreach (Layer layer in __instance.map.Layers)
            {
                for (int x = 0; x < layer.LayerWidth; x++)
                {
                    for (int y = 0; y < layer.LayerHeight; y++)
                    {
                        Tile tile = layer.Tiles[x, y];
                        if (tile != null && tile.Properties.ContainsKey("NPCBarrier"))
                        {
                            // NPCBarrier属性を削除
                            tile.Properties.Remove("NPCBarrier");
                            // ModMonitor.Log($"[DEBUG] Removed NPCBarrier from ({x}, {y}) in {__instance.NameOrUniqueName}", LogLevel.Debug);
                        }
                    }
                }
            }
            // farmの建物が定義されたらドアActionをセットできるよう、OnValueAddedフックを予約
            RegistBuildingsAddHook(__instance);
        }

        private static void RegistBuildingsAddHook(GameLocation location)
        {
            if (location.NameOrUniqueName != "Farm")
            {
                return;
            }
            // 建物が追加されたときにドア定義を追加し、updateDoors() を呼び出す
            location.buildings.OnValueAdded += (Building b) =>
            {
                // ModMonitor.Log($"[DEBUG] Building added: {b.buildingType.Value}. Adding door and updating doors.", LogLevel.Debug);
                AddDoorAction(location, b);
                // buildings に変更があったら updateDoors を実行
                location.updateDoors();
            };
        }

        private static void AddDoorAction(GameLocation location, Building building)
        {
            string buildingName = building.GetIndoors()?.NameOrUniqueName ?? building.nonInstancedIndoorsName.Value;
            if (building == null || string.IsNullOrEmpty(buildingName))
            {
                return;
            }

            // 屋外側のドア座標
            Point outdoorPoint = building.getPointForHumanDoor();
            // 建物内のワープ先座標
            Point indoorPoint = new Point(building.GetIndoors().warps[0].X, building.GetIndoors().warps[0].Y - 1);

            // タイルにActionを設定 (GameLocation.updateDoors()で使用)
            SetWarpActionToTile(location, location.map.GetLayer("Buildings"), outdoorPoint, indoorPoint, buildingName);

            // BuildingDataにActionを設定 (GameLocation.getWarpPointTarget()で使用)
            SetWarpActionToBuildingData(building, outdoorPoint, indoorPoint, buildingName);

            ModMonitor.Log($"[DEBUG] Set Action: {location.NameOrUniqueName} {outdoorPoint} -> {buildingName} {indoorPoint}", LogLevel.Debug);
        }

        private static void RemoveFromIgnoreLocationNames()
        {
            // FarmとBackwoodsを経路探索の除外リストから削除
            WarpPathfindingCache.IgnoreLocationNames.Remove("Farm");
            WarpPathfindingCache.IgnoreLocationNames.Remove("Backwoods");
            // ModMonitor.Log($"[DEBUG] RemoveFromIgnoreLocationNames", LogLevel.Debug);
        }

        private static void IncludeInNpcPathfinding(GameLocation __instance, ref bool __result)
        {
            // FarmとBackwoodsの通行制限を解除
            if (__instance.NameOrUniqueName == "Farm" || __instance.NameOrUniqueName == "Backwoods")
            {
                __result = false;
                // ModMonitor.Log($"[DEBUG] IncludeInNpcPathfinding in {__instance.NameOrUniqueName}", LogLevel.Debug);
            }
        }

        // helper method
        private static void SetWarpActionToTile(GameLocation location, Layer layer, Point outdoorPoint, Point indoorPoint, string buildingName)
        {
            if (layer == null)
            {
                return;
            }

            if (location.map.TileSheets.Count == 0)
            {
                ModMonitor.Log($"[WARN] No TileSheets found in map for layer {layer.Id}, cannot set tile at ({outdoorPoint.X}, {outdoorPoint.Y})", LogLevel.Warn);
                return;
            }

            if (layer.Tiles[outdoorPoint.X, outdoorPoint.Y] == null)
            {
                layer.Tiles[outdoorPoint.X, outdoorPoint.Y] = new StaticTile(layer, location.map.TileSheets[0], BlendMode.Alpha, 0);
            }

            if (layer.Tiles[outdoorPoint.X, outdoorPoint.Y] != null)
            {
                if (layer.Tiles[outdoorPoint.X, outdoorPoint.Y].Properties == null)
                {
                    ModMonitor.Log($"[WARN] Tile at ({outdoorPoint.X}, {outdoorPoint.Y}) in {layer.Id} has null Properties. Trying to add Action...", LogLevel.Warn);
                }

                string property = "Action";
                string value = $"Warp {outdoorPoint.X} {outdoorPoint.Y} {buildingName} {indoorPoint.X} {indoorPoint.Y}";

                // `Properties` に "Action" キーが存在しない場合、新しく追加
                if (!layer.Tiles[outdoorPoint.X, outdoorPoint.Y].Properties.ContainsKey(property))
                {
                    layer.Tiles[outdoorPoint.X, outdoorPoint.Y].Properties.Add(property, value);
                    // ModMonitor.Log($"[DEBUG] Added new Action property: {location.NameOrUniqueName} (layer: {layer.Id}) (value: {value})", LogLevel.Debug);
                }
                else
                {
                    layer.Tiles[outdoorPoint.X, outdoorPoint.Y].Properties[property] = value;
                    // ModMonitor.Log($"[DEBUG] Updated existing Action property: {location.NameOrUniqueName} (layer: {layer.Id}) (value: {value})", LogLevel.Debug);
                }
            }
        }

        private static void SetWarpActionToBuildingData(Building building, Point outdoorPoint, Point indoorPoint, string buildingName)
        {
            // Farmの建物マップをNPCの経路探索用に追加
            BuildingData data = building.GetData();
            if (data == null)
            {
                ModMonitor.Log($"[WARN] BuildingData not found for {building.buildingType.Value}", LogLevel.Warn);
                return;
            }

            // `TileProperties` に "Warp" の定義を追加
            if (data.TileProperties == null)
            {
                data.TileProperties = new List<BuildingTileProperty>();
            }

            data.TileProperties.Add(new BuildingTileProperty
            {
                Layer = "Buildings",
                TileArea = new Rectangle(outdoorPoint.X - building.tileX.Value, outdoorPoint.Y - building.tileY.Value, 1, 1),
                Name = "Action",
                Value = $"Warp {indoorPoint.X} {indoorPoint.Y} {buildingName}"
            });
            // ModMonitor.Log($"[DEBUG] Added Warp Action to BuildingData: {outdoorPoint} -> {buildingName} {indoorPoint}", LogLevel.Debug);
        }

    }
}