#pragma warning disable 8600

using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Pathfinding;
using StardewValley.Buildings;
using Microsoft.Xna.Framework;
using Netcode;
using xTile.Tiles;
using xTile.Layers;

namespace NPCPassableInFarm
{
    public class ModEntry : Mod
    {
        public static IMonitor ModMonitor = null!;

        public override void Entry(IModHelper helper)
        {
            ModMonitor = Monitor;

            var harmony = new Harmony(ModManifest.UniqueID);

            // Farmの建物マップをNPCの経路探索用に追加
            harmony.Patch(
                original: AccessTools.Method(typeof(GameLocation), "updateDoors"),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(AddFarmDoors))
            );

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

            // Farmの建物内へのワープを追加
            harmony.Patch(
                original: AccessTools.Method(typeof(GameLocation), "getWarpPointTarget"),
				postfix: new HarmonyMethod(typeof(ModEntry), nameof(SetWarpPointTarget))
            );
        }

        private static void RegistFarmBuildingsAddHook(GameLocation location)
            {
            if (location is Farm farm)
            {
                ModMonitor.Log("[DEBUG] Farm.buildings.OnValueAdded hook registered", LogLevel.Debug);
                
                // 建物が追加されたときにドア定義を追加し、updateDoors() を呼び出す
                farm.buildings.OnValueAdded += (Building b) =>
                {
                    // ModMonitor.Log($"[DEBUG] Building added: {b.buildingType.Value}. Adding door and updating doors.", LogLevel.Debug);
                    AddDoorDefinition(farm, b);
                    farm.updateDoors(); // buildings に変更があったら updateDoors を実行
                };
            }
        }

        private static void AddDoorDefinition(Farm farm, Building building)
        {
            if (building == null || string.IsNullOrEmpty(building.buildingType.Value))
            {
                return;
            }

            string buildingName = building.GetIndoors()?.NameOrUniqueName ?? building.nonInstancedIndoorsName.Value;
            if (string.IsNullOrEmpty(buildingName))
            {
                return;
            }

            Point farmPoint = building.getPointForHumanDoor();
            Point indoorPoint = new Point(building.GetIndoors().warps[0].X, building.GetIndoors().warps[0].Y);

            if (farmPoint.X < 0 || farmPoint.Y < 0) // 無効な座標をスキップ
            {
                return;
            }

            if (!farm.doors.ContainsKey(new Point(farmPoint.X, farmPoint.Y)))
            {
                Layer buildingLayer = farm.map.GetLayer("Buildings");
                if (buildingLayer == null)
                {
                    return;
                }

                Tile tile = buildingLayer.Tiles[farmPoint.X, farmPoint.Y];

                if (tile == null && farm.map.TileSheets.Count > 0)
                {
                    tile = new StaticTile(buildingLayer, farm.map.TileSheets[0], BlendMode.Alpha, 0);
                    buildingLayer.Tiles[farmPoint.X, farmPoint.Y] = tile;
                }

                if (tile != null)
                {
                    tile.Properties["Action"] = $"Warp {farmPoint.X} {farmPoint.Y} {buildingName} {indoorPoint.X} {indoorPoint.Y}";
                    ModMonitor.Log($"[DEBUG] Added Warp: Farm ({farmPoint.X}, {farmPoint.Y}) -> {buildingName} ({indoorPoint.X}, {indoorPoint.Y})", LogLevel.Debug);
                }
            }
        }

        private static void AddFarmDoors(GameLocation __instance)
        {
            if (__instance.NameOrUniqueName != "Farm")
            {
                return;
            }



            if (Game1.IsClient)
            {
                ModMonitor.Log("[DEBUG] Skipping updateDoors() on client.", LogLevel.Debug);
                return;
            }

            // ModMonitor.Log($"[DEBUG] updateDoors() started for: {__instance.NameOrUniqueName}", LogLevel.Debug);

            __instance.doors.Clear();
            
            Layer buildingLayer = __instance.map.GetLayer("Buildings");
            if (buildingLayer == null)
            {
                ModMonitor.Log("[WARN] Buildings layer not found!", LogLevel.Warn);
                return;
            }

            for (int x = 0; x < __instance.map.Layers[0].LayerWidth; x++)
            {
                for (int y = 0; y < __instance.map.Layers[0].LayerHeight; y++)
                {
                    Tile tile = buildingLayer.Tiles[x, y];
                    if (tile == null || !tile.Properties.TryGetValue("Action", out var doorAction))
                    {
                        continue;
                    }

                    string doorString = (string)doorAction;
                    if (!doorString.Contains("Warp"))
                    {
                        continue;
                    }

                    string[] split = doorString.Split(' ');
                    string actionType = split[0];

                    switch (actionType)
                    {
                        case "WarpBoatTunnel":
                            __instance.doors.Add(new Point(x, y), new NetString("BoatTunnel"));
                            ModMonitor.Log($"[DEBUG] Added BoatTunnel door at ({x}, {y})", LogLevel.Debug);
                            continue;
                        case "WarpCommunityCenter":
                            __instance.doors.Add(new Point(x, y), new NetString("CommunityCenter"));
                            ModMonitor.Log($"[DEBUG] Added CommunityCenter door at ({x}, {y})", LogLevel.Debug);
                            continue;
                        case "Warp_Sunroom_Door":
                            __instance.doors.Add(new Point(x, y), new NetString("Sunroom"));
                            ModMonitor.Log($"[DEBUG] Added Sunroom door at ({x}, {y})", LogLevel.Debug);
                            continue;
                        case "LockedDoorWarp":
                        case "Warp":
                        case "WarpMensLocker":
                        case "WarpWomensLocker":
                            break;
                        default:
                            if (!actionType.Contains("Warp"))
                            {
                                continue;
                            }
                            ModMonitor.Log($"[WARN] {__instance.NameOrUniqueName} ({x}, {y}) has unknown warp property '{doorString}', using legacy logic.", LogLevel.Warn);
                            break;
                    }

                    if (__instance.Name != "Mountain" || x != 8 || y != 20)
                    {
                        string locationName = split.Length > 3 ? split[3] : null;
                        if (!string.IsNullOrEmpty(locationName))
                        {
                            __instance.doors.Add(new Point(x, y), new NetString(locationName));
                            ModMonitor.Log($"[DEBUG] Added generic Warp door to {locationName} at ({x}, {y})", LogLevel.Debug);
                        }
                    }
                }
            }
            // ModMonitor.Log($"[DEBUG] updateDoors() completed for: {__instance.NameOrUniqueName}", LogLevel.Debug);
            return;
        }

        private static void RemoveNPCBarrier(GameLocation __instance)
        {
            // NPCBarrier属性を削除
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
                            tile.Properties.Remove("NPCBarrier");
                            // ModMonitor.Log($"[DEBUG] Removed NPCBarrier from ({x}, {y}) in {__instance.NameOrUniqueName}", LogLevel.Debug);
                        }
                    }
                }
            }
            RegistFarmBuildingsAddHook(__instance);
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

		private static void SetWarpPointTarget(GameLocation __instance, ref Point __result, Point warpPointLocation, Character? character = null)
        {
            // Farmの建物内へのワープを追加
            if (__instance.NameOrUniqueName != "Farm")
            {
                return;
            }


            foreach (Building building in __instance.buildings)
            {
                GameLocation indoorLocation = building.GetIndoors();
                if (warpPointLocation.X == (float)(building.humanDoor.X + building.tileX.Value) && warpPointLocation.Y == (float)(building.humanDoor.Y + building.tileY.Value) && indoorLocation != null)
                {
                    ModMonitor.Log($"[DEBUG] changed WarpPointTarget: {indoorLocation.NameOrUniqueName} {__result} -> {new Point(indoorLocation.warps[0].X, indoorLocation.warps[0].Y - 1)}", LogLevel.Debug);
                    __result = new Point(indoorLocation.warps[0].X, indoorLocation.warps[0].Y - 1);
                }

            }

            // ModMonitor.Log($"[DEBUG] getWarpPointTarget() called in {__instance.NameOrUniqueName} at ({warpPointLocation.X}, {warpPointLocation.Y})", LogLevel.Debug);


            foreach (Warp wrp in __instance.warps)
            {
                if (wrp.X == warpPointLocation.X && wrp.Y == warpPointLocation.Y)
                {
                    __result = new Point(wrp.TargetX, wrp.TargetY);
                    ModMonitor.Log($"[DEBUG] Found warp target: ({wrp.TargetX}, {wrp.TargetY})", LogLevel.Debug);
                    return;
                }
            }

            foreach (KeyValuePair<Point, string> indoor in __instance.doors.Pairs)
            {
                if (!indoor.Key.Equals(warpPointLocation))
                {
                    continue;
                }

                string[] action = __instance.GetTilePropertySplitBySpaces("Action", "Buildings", warpPointLocation.X, warpPointLocation.Y);
                string propertyName = ArgUtility.Get(action, 0, "");
                ModMonitor.Log($"[DEBUG] Found door match -> Target: {indoor.Value}, Action: {propertyName}", LogLevel.Debug);

                switch (propertyName)
                {
                case "WarpCommunityCenter":
                    __result = new Point(32, 23);
                    ModMonitor.Log($"[DEBUG] Warp to CommunityCenter: (32, 23)", LogLevel.Debug);
                    return;
                case "Warp_Sunroom_Door":
                    __result = new Point(5, 13);
                    ModMonitor.Log($"[DEBUG] Warp to Sunroom: (5, 13)", LogLevel.Debug);
                    return;
                case "WarpBoatTunnel":
                    __result = new Point(17, 43);
                    ModMonitor.Log($"[DEBUG] Warp to BoatTunnel: (17, 43)", LogLevel.Debug);
                    return;
                case "LockedDoorWarp":
                case "Warp":
                case "WarpMensLocker":
                case "WarpWomensLocker":
                    break;
                default:
                    if (!propertyName.Contains("Warp"))
                    {
                        continue;
                    }
                    ModMonitor.Log($"[WARN] Unknown warp property '{string.Join(" ", action)}' at {indoor.Key}", LogLevel.Warn);
                    break;
                }

                ModMonitor.Log($"[DEBUG] 1", LogLevel.Debug);

                if (!ArgUtility.TryGetPoint(action, 1, out var tile, out var error) || !ArgUtility.TryGet(action, 3, out var locationName, out error))
                {
                    ModMonitor.Log($"[ERROR] LogTileActionError action = {action}, warpPointLocation.X = {warpPointLocation.X}, warpPointLocation.Y = {warpPointLocation.Y}, error = {error}", LogLevel.Error);
                    __instance.LogTileActionError(action, warpPointLocation.X, warpPointLocation.Y, error);
                    continue;
                }

                ModMonitor.Log($"[DEBUG] 2", LogLevel.Debug);

                if (!(locationName == "BoatTunnel"))
                {
                    if (locationName == "Trailer" && Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade"))
                    {
                        __result = new Point(13, 24);
                        ModMonitor.Log($"[DEBUG] Warp to upgraded Trailer: (13, 24)", LogLevel.Debug);
                    }
                    else
                    {
                        __result = new Point(tile.X, tile.Y);
                        ModMonitor.Log($"[DEBUG] Warp to {locationName}: ({tile.X}, {tile.Y})", LogLevel.Debug);
                    }
                }
                else
                {
                    __result = new Point(17, 43);
                    ModMonitor.Log($"[DEBUG] Warp to BoatTunnel: (17, 43)", LogLevel.Debug);
                }
            }

            ModMonitor.Log($"[DEBUG] getWarpPointTarget() result -> ({__result.X}, {__result.Y})", LogLevel.Debug);
        }

    }
}