#pragma warning disable 8600
#pragma warning disable 0168
#pragma warning disable 8602
#pragma warning disable 8605
#pragma warning disable 0618
#pragma warning disable 8601
#pragma warning disable 8604
#pragma warning disable CS8622
#pragma warning disable AvoidNetField // Avoid Netcode types when possible

using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Pathfinding;
using StardewValley.Buildings;
using System.Reflection;

// for debug
using Microsoft.Xna.Framework;
using StardewValley.Network;
using Netcode;
using System.Diagnostics;
using StardewValley.GameData.Buildings;
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
				// prefix: new HarmonyMethod(typeof(ModEntry), nameof(updateDoorsPrefix)),
				postfix: new HarmonyMethod(typeof(ModEntry), nameof(updateDoorsPostfix))
			);

			// FarmとBackwoodsを経路探索のブラックリストから削除
			harmony.Patch(
				original: AccessTools.Method(typeof(WarpPathfindingCache), "PopulateCache"),
				prefix: new HarmonyMethod(typeof(ModEntry), nameof(PopulateCachePrefix))
			);

			// Farmの通行制限を解除
			harmony.Patch(
				original: AccessTools.DeclaredMethod(typeof(Farm), "ShouldExcludeFromNpcPathfinding"),
				postfix: new HarmonyMethod(typeof(ModEntry), nameof(ShouldExcludeFromNpcPathfindingPostfix))
			);

			// Backwoodsの通行制限を解除
			harmony.Patch(
				original: AccessTools.DeclaredMethod(typeof(GameLocation), "ShouldExcludeFromNpcPathfinding"),
				postfix: new HarmonyMethod(typeof(ModEntry), nameof(ShouldExcludeFromNpcPathfindingPostfix))
			);

			// Farmの建物内へのワープを追加
			harmony.Patch(
				original: AccessTools.Method(typeof(GameLocation), "getWarpPointTarget"),
				postfix: new HarmonyMethod(typeof(ModEntry), nameof(getWarpPointTargetPostfix))
			);

			// `NPCBarrier` 属性を削除
			harmony.Patch(
				original: AccessTools.Method(typeof(GameLocation), "loadObjects"),
				postfix: new HarmonyMethod(typeof(ModEntry), nameof(loadObjectsPostfix))
			);

            // ゲーム開始時に buildings.OnValueAdded をセット
            // helper.Events.GameLoop.SaveLoaded += OnDayStarted;

            // harmony.Patch(
            // 	original: typeof(GameLocation).GetMethod("UpdateWhenCurrentLocation", BindingFlags.Public | BindingFlags.Instance),
            //     postfix: new HarmonyMethod(typeof(ModEntry), nameof(UpdateWhenCurrentLocationPostfix))
            // );

            // harmony.Patch(
            //     original: AccessTools.Method(typeof(GameLocation), "loadObjects"),
            //     postfix: new HarmonyMethod(typeof(ModEntry), nameof(loadObjectsPostfix))
            // );

            // harmony.Patch(
            //     original: AccessTools.Method(typeof(Farm), "DayUpdate"),
            //     postfix: new HarmonyMethod(typeof(ModEntry), nameof(DayUpdatePostfix))
            // );

            // harmony.Patch(
            //     original: AccessTools.Method(typeof(GameLocation), "buildStructure", new Type[] { typeof(Building), typeof(Vector2), typeof(Farmer), typeof(bool) }),
            //     postfix: new HarmonyMethod(typeof(ModEntry), nameof(BuildStructurePostfix))
            // );



            // for debug
            // harmony.Patch(
            //	 original: AccessTools.DeclaredMethod(typeof(Backwoods), "ShouldExcludeFromNpcPathfinding"),
            //	 postfix: new HarmonyMethod(typeof(ModEntry), nameof(ShouldExcludeFromNpcPathfindingPostfix))
            // );

            // harmony.Patch(
            // 	original: AccessTools.Method(typeof(PathFindController), "isPositionImpassableForNPCSchedule"),
            // 	postfix: new HarmonyMethod(typeof(ModEntry), nameof(isPositionImpassableForNPCSchedulePostfix))
            // );



            // harmony.Patch(
            // 	original: AccessTools.Method(typeof(PathFindController), "update", new Type[] { typeof(GameTime) }),
            // 	postfix: new HarmonyMethod(typeof(ModEntry), nameof(updatePostfix))
            // );

            // harmony.Patch(
            //	 original: AccessTools.Method(typeof(WarpPathfindingCache), "PopulateCache"),
            //	 postfix: new HarmonyMethod(typeof(ModEntry), nameof(PopulateCachePostfix))
            // );

            // harmony.Patch(
            //	 original: AccessTools.Method(typeof(WarpPathfindingCache), "ExploreWarpPoints", new Type[] { typeof(GameLocation), typeof(List<string>), typeof(Gender?) }),
            //	 prefix: new HarmonyMethod(typeof(ModEntry), nameof(ExploreWarpPointsPrefix))
            // );

            // harmony.Patch(
            //	 original: AccessTools.Method(typeof(WarpPathfindingCache), "ExploreWarpPoints", new Type[] { typeof(string
            //	 ), typeof(List<string>), typeof(Gender?), typeof(HashSet<string>) }),
            //	 prefix: new HarmonyMethod(typeof(ModEntry), nameof(ExploreWarpPoints2Prefix))
            // );

            // ModMonitor.Log("[DEBUG] ExploreWarpPointsPatch applied!", LogLevel.Debug);

            // harmony.Patch(
            //	 original: AccessTools.DeclaredMethod(typeof(GameLocation), "ShouldExcludeFromNpcPathfinding"),
            //	 postfix: new HarmonyMethod(typeof(ModEntry), nameof(GameLocationShouldExcludeFromNpcPathfindingPostfix))
            // );

            // harmony.Patch(
            //	 original: AccessTools.Method(typeof(GameLocation), "updateDoors"),
            //	 postfix: new HarmonyMethod(typeof(ModEntry), nameof(updateDoorsPostfix))
            // );

            // harmony.Patch(
            //	 original: AccessTools.Method(typeof(GameLocation), "getWarpFromDoor"),
            //	 postfix: new HarmonyMethod(typeof(ModEntry), nameof(getWarpFromdoorPointtfix))
            // );

            // harmony.Patch(
            //	 original: AccessTools.Method(typeof(PathFindController), "handleWarps"),
            //	 prefix: new HarmonyMethod(typeof(ModEntry), nameof(handleWarpsPrefix))
            // );

            // harmony.Patch(
            // 	original: AccessTools.Method(typeof(WarpPathfindingCache), "GetLocationRoute"),
            // 	postfix: new HarmonyMethod(typeof(ModEntry), nameof(GetLocationRoutePostfix))
            // );

            // harmony.Patch(
            // 	original: AccessTools.Method(typeof(NPC), "pathfindToNextScheduleLocation", new Type[] { typeof(string), typeof(string), typeof(int), typeof(int), typeof(string), typeof(int), typeof(int), typeof(int), typeof(string), typeof(string) }),
            // 	// prefix: new HarmonyMethod(typeof(ModEntry), nameof(pathfindToNextScheduleLocationPrefix)),
            // 	postfix: new HarmonyMethod(typeof(ModEntry), nameof(pathfindToNextScheduleLocationPostfix))
            // );

            // harmony.Patch(
            // 	original: typeof(NPC).GetMethod("parseMasterScheduleImpl", BindingFlags.NonPublic | BindingFlags.Instance),
            // 	postfix: new HarmonyMethod(typeof(ModEntry), nameof(parseMasterScheduleImplPostfix))
            // );

            // harmony.Patch(
            // 	original: AccessTools.Method(typeof(PathFindController), "findPathForNPCSchedules",
            // 		new Type[] { typeof(Point), typeof(Point), typeof(GameLocation), typeof(int) }
            // 	),
            // 	prefix: new HarmonyMethod(typeof(ModEntry), nameof(findPathForNPCSchedulesPrefix)),
            // 	postfix: new HarmonyMethod(typeof(ModEntry), nameof(findPathForNPCSchedulesPostfix))
            // );

            // harmony.Patch(
            // 	 original: AccessTools.Method(typeof(GameLocation), "getWarpPointTo", new Type[] { typeof(string), typeof(Character) }),
            // 	 postfix: new HarmonyMethod(typeof(ModEntry), nameof(getWarpPointToPostfix))
            // );

            // harmony.Patch(
            // 	original: AccessTools.Method(typeof(NPC), "checkSchedule", new Type[] { typeof(int) }),
            // 	prefix: new HarmonyMethod(typeof(ModEntry), nameof(checkSchedulePrefix))
            // );




            // foreach (var method in Harmony.GetAllPatchedMethods())
            // {
            // 	if (method.Name == "findPathForNPCSchedules" || method.Name == "pathfindToNextScheduleLocation" || method.Name == "checkSchedule" || method.Name == "GetLocationRoute" || method.Name == "getWarpPointTo")
            // 	{
            // 		ModMonitor.Log($"[DEBUG] {method.Name}() is PATCHED by:", LogLevel.Debug);

            // 		var patches = Harmony.GetPatchInfo(method);
            // 		if (patches != null)
            // 		{
            // 			foreach (var patch in patches.Prefixes)
            // 			{
            // 				ModMonitor.Log($"[DEBUG] Prefix applied by {patch.owner}", LogLevel.Debug);
            // 			}
            // 			foreach (var patch in patches.Postfixes)
            // 			{
            // 				ModMonitor.Log($"[DEBUG] Postfix applied by {patch.owner}", LogLevel.Debug);
            // 			}
            // 		}
            // 		else
            // 		{
            // 			ModMonitor.Log($"[DEBUG] No patches found for {method.Name}()!", LogLevel.Debug);
            // 		}
            // 	}
            // }


            // 🔥 SMAPI のコンソールコマンドとして `findpath` を登録！
            // helper.ConsoleCommands.Add("findpath", "Find path for NPC. Usage: findpath <startX> <startY> <endX> <endY>", DebugFindPathCommand);



        }

// private static bool isUpdatingDoors = false; // ループ防止用フラグ

// private static bool updateDoorsPrefix(GameLocation __instance)
// {
//     if (__instance is Farm farm)
//     {
//         Layer buildingLayer = __instance.map.GetLayer("Buildings");

//         if (buildingLayer == null)
//         {
//             return true;
//         }

//         if (farm.buildings == null || farm.buildings.Count == 0)
//         {
//             ModMonitor.Log("[WARN] Farm buildings are not initialized yet. Waiting for buildings to be set.", LogLevel.Warn);

//             farm.buildings.OnValueAdded += (Building b) =>
//             {
//                 if (!isUpdatingDoors) // 無限ループ防止
//                 {
//                     isUpdatingDoors = true;
//                     ModMonitor.Log($"[DEBUG] Building {b.buildingType.Value} added. Updating doors.", LogLevel.Debug);
//                     updateDoorsManually(farm);
//                     isUpdatingDoors = false;
//                 }
//             };

//             return true;
//         }

//         ModMonitor.Log($"[DEBUG] updateDoors() __instance = {__instance.NameOrUniqueName}", LogLevel.Debug);

//         if (!isUpdatingDoors) // すでに処理中でない場合のみ実行
//         {
//             isUpdatingDoors = true;
//             updateDoorsManually(farm);
//             isUpdatingDoors = false;
//         }
//     }

//     return true;
// }

// // Farmに建物が追加された後にドア定義を追加
// private static void updateDoorsManually(Farm farm)
// {
//     Layer buildingLayer = farm.map.GetLayer("Buildings");

//     foreach (Building building in farm.buildings)
//     {
//         if (building == null || string.IsNullOrEmpty(building.buildingType.Value))
//         {
//             continue;
//         }

//         string buildingName = building.GetIndoors()?.NameOrUniqueName ?? building.nonInstancedIndoorsName.Value;
//         if (string.IsNullOrEmpty(buildingName))
//         {
//             continue;
//         }

//         // **修正: NetPoint を Point に変換**
//         Point doorOffset = building.humanDoor.Value;
//         int farmTileX = building.tileX.Value + doorOffset.X;
//         int farmTileY = building.tileY.Value + doorOffset.Y;

//         if (farmTileX < 0 || farmTileY < 0)
//         {
//             continue;
//         }

//         if (!farm.doors.ContainsKey(new Point(farmTileX, farmTileY)))
//         {
//             Tile tile = buildingLayer.Tiles[farmTileX, farmTileY];

//             if (tile == null)
//             {
//                 if (farm.map.TileSheets.Count > 0)
//                 {
//                     tile = new StaticTile(buildingLayer, farm.map.TileSheets[0], BlendMode.Alpha, 0);
//                     buildingLayer.Tiles[farmTileX, farmTileY] = tile;
//                 }
//             }

//             if (tile != null)
//             {
//                 tile.Properties["Action"] = $"Warp {farmTileX} {farmTileY} {buildingName} {doorOffset.X} {doorOffset.Y}";
//                 ModMonitor.Log($"[DEBUG] Added Warp: {farmTileX} {farmTileY} -> {buildingName} ({doorOffset.X}, {doorOffset.Y})", LogLevel.Debug);
//             }
//         }
//     }

//     // **ここでは updateDoors() を直接呼ばない**
// }



    // private static void OnDayStarted(object sender, StardewModdingAPI.Events.SaveLoadedEventArgs e)
    private static void OnDayStarted(GameLocation location)
		{
			if (location is Farm farm)
			{
				ModMonitor.Log("[INFO] Farm.buildings.OnValueAdded hook registered", LogLevel.Info);
				
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

			Point doorOffset = building.getPointForHumanDoor();
			// int farmTileX = building.tileX.Value + doorOffset.X;
			// int farmTileY = building.tileY.Value + doorOffset.Y;
			int farmTileX = doorOffset.X;
			int farmTileY = doorOffset.Y;

			if (farmTileX < 0 || farmTileY < 0) // 無効な座標をスキップ
			{
				return;
			}

			if (!farm.doors.ContainsKey(new Point(farmTileX, farmTileY)))
			{
				Layer buildingLayer = farm.map.GetLayer("Buildings");
				if (buildingLayer == null)
				{
					return;
				}

				Tile tile = buildingLayer.Tiles[farmTileX, farmTileY];

				if (tile == null && farm.map.TileSheets.Count > 0)
				{
					tile = new StaticTile(buildingLayer, farm.map.TileSheets[0], BlendMode.Alpha, 0);
					buildingLayer.Tiles[farmTileX, farmTileY] = tile;
				}

				if (tile != null)
				{
					tile.Properties["Action"] = $"Warp {farmTileX} {farmTileY} {buildingName} {doorOffset.X} {doorOffset.Y}";
					ModMonitor.Log($"[DEBUG] Added Warp: {farmTileX} {farmTileY} -> {buildingName} ({doorOffset.X}, {doorOffset.Y})", LogLevel.Debug);
				}
			}
		}





		private static bool updateDoors2(GameLocation __instance)
		{
		    if (__instance.NameOrUniqueName == "Farm")
			{

				Farm farm = __instance as Farm; // Farmのインスタンスを直接取得

				if (farm == null || farm.buildings.Count == 0)
				{
					ModMonitor.Log("[ERROR] Failed to retrieve Farm instance or its buildings list!", LogLevel.Error);
					return true;
				}

				Layer buildingLayer = __instance.map.GetLayer("Buildings");

				if (buildingLayer == null)
				{
					return true;
				}

				// `Buildings.json` からすべての建物情報を取得
				foreach (var buildingEntry in Game1.buildingData)
				{
					// 建物の室内名を取得
					string buildingName = null;
					if (buildingEntry.Value.IndoorMap != null)
					{
						buildingName = buildingEntry.Value.IndoorMap;
					}
					if (buildingEntry.Value.NonInstancedIndoorLocation != null)
					{
						buildingName = buildingEntry.Value.NonInstancedIndoorLocation;
					}

					if (string.IsNullOrEmpty(buildingName))
					{
						continue;
					}

					// `HumanDoor` の座標を取得
					Point doorPoint = buildingEntry.Value.HumanDoor;
					if (doorPoint.X < 0 || doorPoint.Y < 0) // 無効な座標はスキップ
					{
						continue;
					}

					// Farm 内でのドアの座標を特定（tile のX・Y）
					int farmTileX = -1;
					int farmTileY = -1;

					for (int x = 0; x < __instance.map.Layers[0].LayerWidth; x++)
					{
						for (int y = 0; y < __instance.map.Layers[0].LayerHeight; y++)
						{
							Tile tile = buildingLayer.Tiles[x, y];
							if (tile != null && tile.Properties.TryGetValue("Action", out var action) && ((string)action).Contains("Warp"))
							{
								farmTileX = x;
								farmTileY = y;
								break;
							}
						}
						if (farmTileX != -1) break;
					}

					if (farmTileX == -1 || farmTileY == -1)
					{
						ModMonitor.Log($"[WARN] Could not determine farm tile position for {buildingName}", LogLevel.Warn);
						continue;
					}

					if (!__instance.doors.ContainsKey(doorPoint))
					{
						// `updateDoors()` の処理対象に含めるためのタイル設定
						Tile tile = buildingLayer.Tiles[doorPoint.X, doorPoint.Y];

						if (tile == null)
						{
							if (__instance.map.TileSheets.Count > 0)
							{
								tile = new StaticTile(buildingLayer, __instance.map.TileSheets[0], BlendMode.Alpha, 0);
								buildingLayer.Tiles[doorPoint.X, doorPoint.Y] = tile;
							}
						}

						if (tile != null)
						{
							tile.Properties["Action"] = $"Warp {farmTileX} {farmTileY} {buildingName} {doorPoint.X} {doorPoint.Y}";
							ModMonitor.Log($"[DEBUG] updateDoors() -> Added {buildingName} door at ({doorPoint.X}, {doorPoint.Y})", LogLevel.Debug);
						}
					}
				}
			}

			return true;
		}


		private static void updateDoorsPostfix(GameLocation __instance)
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

			ModMonitor.Log($"[DEBUG] updateDoors() started for: {__instance.NameOrUniqueName}", LogLevel.Debug);

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

			ModMonitor.Log($"[DEBUG] updateDoors() completed for: {__instance.NameOrUniqueName}", LogLevel.Debug);


			return;


			if (__instance is Farm farm)
			{
				// `Buildings.json` からすべての建物情報を取得
				foreach (var buildingEntry in Game1.buildingData)
				{
					// 建物名を取得
					string buildingName = null;
					if (buildingEntry.Value.IndoorMap != null)
					{
						buildingName = buildingEntry.Value.IndoorMap;
					}
					if (buildingEntry.Value.NonInstancedIndoorLocation != null)
					{
						buildingName = buildingEntry.Value.NonInstancedIndoorLocation;
					}

					// if (Game1.getLocationFromName(buildingName) != null)
					// {
						BuildingData buildingData = buildingEntry.Value;

						// `HumanDoor` の座標を取得
						Point doorPoint = buildingData.HumanDoor;
						if (doorPoint.X >= 0 && doorPoint.Y >= 0) // 有効な座標かチェック
						{
							farm.doors[doorPoint] = buildingName; // ワープポイントとして登録
							ModMonitor.Log($"[DEBUG] updateDoors() -> Added {buildingName} door at ({doorPoint.X}, {doorPoint.Y})", LogLevel.Debug);
						}
						else
						{
							// ModMonitor.Log($"[WARN] {buildingName} has an invalid HumanDoor position in Buildings.json!", LogLevel.Warn);
						}
					// }
				}
			}
		}

        private static void loadObjectsPostfix(GameLocation __instance)
        {
			// `NPCBarrier` 属性を削除
            if (__instance.NameOrUniqueName == "Farm")
            {
                foreach (Layer layer in __instance.map.Layers)
                {
                    for (int x = 0; x < layer.LayerWidth; x++)
                    {
                        for (int y = 0; y < layer.LayerHeight; y++)
                        {
                            Tile tile = layer.Tiles[x, y];
                            if (tile != null)
                            {
                                if (tile.Properties.ContainsKey("NPCBarrier"))
                                {
                                    tile.Properties.Remove("NPCBarrier");
                                    // ModMonitor.Log($"[DEBUG] Removed NPCBarrier from ({x}, {y}) in {__instance.NameOrUniqueName}", LogLevel.Debug);
                                }
                            }
                        }
                    }
                }
    			OnDayStarted(__instance);
            }
        }

		private static bool PopulateCachePrefix()
		{
			// FarmとBackwoodsを経路探索のブラックリストから削除
			WarpPathfindingCache.IgnoreLocationNames.Remove("Farm");
			WarpPathfindingCache.IgnoreLocationNames.Remove("Backwoods");
			return true;
		}

		private static void ShouldExcludeFromNpcPathfindingPostfix(GameLocation __instance, ref bool __result)
		{
			// FarmとBackwoodsの通行制限を解除
			if (__instance.NameOrUniqueName == "Farm" || __instance.NameOrUniqueName == "Backwoods")
			{
				__result = false;
			}
		}

		private static void getWarpPointTargetPostfix(GameLocation __instance, ref Point __result, Point warpPointLocation, Character? character = null)
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


		// for debug
		private static void updatePostfix(PathFindController __instance, GameTime time)
		{
			Character? character = AccessTools.Field(typeof(PathFindController), "character").GetValue(__instance) as Character;

			if (character != null && character.Name == "Seiris")
			{
				if (__instance.pathToEndPoint != null && __instance.pathToEndPoint.Count != 0)
				{
					string pathString = string.Join(" -> ", __instance.pathToEndPoint.Select(p => $"({p.X}, {p.Y})"));
					ModMonitor.Log($"[DEBUG] current: {character.currentLocation.NameOrUniqueName} X:{character.TilePoint.X} Y:{character.TilePoint.Y} / pathToEndPoint: {pathString}", LogLevel.Debug);
				}
			}
		}

		private static void isPositionImpassableForNPCSchedulePostfix(GameLocation loc, int x, int y, ref bool __result)
		{
			if ( loc.NameOrUniqueName == "Farm" && __result == true)
			{

				ModMonitor.Log($"[DEBUG] Checking passability at {loc.NameOrUniqueName} ({x}, {y}): {__result}", LogLevel.Debug);
				__result = false;
			}
		}


		private static void PopulateCachePostfix()
		{
			try
			{
				ModMonitor.Log("[DEBUG] Finished PopulateCache()", LogLevel.Debug);

				ModMonitor.Log("[DEBUG] Adding manual path from Farm to FarmHouse", LogLevel.Debug);

				// すでに Farm -> FarmHouse の経路が登録されていないかチェック
				var routesField = AccessTools.Field(typeof(WarpPathfindingCache), "Routes");

				Dictionary<string, List<LocationWarpRoute>>? routes = routesField.GetValue(null) as Dictionary<string, List<LocationWarpRoute>>;

				if (routes != null && !routes.ContainsKey("Farm"))
				{
					routes["Farm"] = new List<LocationWarpRoute>();
				}

				// 経路がすでにある場合は追加しない
				if (routes != null && !routes["Farm"].Any(route => route.LocationNames.Contains("FarmHouse")))
				{
					routes["Farm"].Add(new LocationWarpRoute(new string[] { "Farm", "FarmHouse" }, Gender.Undefined));
					ModMonitor.Log("[DEBUG] Successfully added Farm -> FarmHouse path", LogLevel.Debug);
				}
			}
			catch (Exception ex)
			{
				ModMonitor.Log($"[ERROR] Failed to insert FarmHouse route: {ex}", LogLevel.Error);
			}



			foreach (GameLocation loc in Game1.locations)
			{
				// ModMonitor.Log($"[DEBUG] Checking location: {loc.NameOrUniqueName}", LogLevel.Debug);

				if (!WarpPathfindingCache.IgnoreLocationNames.Contains(loc.NameOrUniqueName))
				{
					if (loc.NameOrUniqueName == "Farm" || loc.NameOrUniqueName == "FarmHouse")
					{
						ModMonitor.Log($"[DEBUG] Sending {loc.NameOrUniqueName} to ExploreWarpPoints()", LogLevel.Debug);
					}

					// `ExploreWarpPoints` メソッドを取得！
					MethodInfo exploreMethod = AccessTools.Method(typeof(WarpPathfindingCache), "ExploreWarpPoints", new Type[] { typeof(GameLocation), typeof(List<string>), typeof(Gender?) });

					if (exploreMethod != null && loc != null)
					{
						// `ExploreWarpPoints()` を強制実行！！
						// ModMonitor.Log($"[DEBUG] location: {loc}", LogLevel.Debug);
						exploreMethod.Invoke(null, new object[] { loc, new List<string>(), null! });
					}
					else
					{
						ModMonitor.Log("[ERROR] Could not find ExploreWarpPoints() method!", LogLevel.Error);
					}
				}
				else
				{
					ModMonitor.Log($"[DEBUG] Skipping {loc.NameOrUniqueName} (Ignored)", LogLevel.Debug);
				}
			}



			Dictionary<string, List<LocationWarpRoute>>? routes2 = AccessTools.Field(typeof(WarpPathfindingCache), "Routes").GetValue(null) as Dictionary<string, List<LocationWarpRoute>>;

			foreach (var route in routes2 ?? new Dictionary<string, List<LocationWarpRoute>>())
			{
				string startLocation = route.Key;
				// ModMonitor.Log($"[DEBUG] route.Key: {route.Key}", LogLevel.Debug);
				foreach (var path in route.Value)
				{
					if (startLocation == "Farm" || startLocation == "FarmHouse") {
						string pathStr = string.Join(" -> ", path.LocationNames);
						ModMonitor.Log($"[DEBUG] Path: {startLocation} -> {pathStr}", LogLevel.Debug);
					}
				}
			}
		}

		private static bool ExploreWarpPointsPrefix(GameLocation location, List<string> route, Gender? genderRestriction)
		{
			 // ModMonitor.Log($"[DEBUG] Exploring warps", LogLevel.Debug);
			 if (location is Farm farm)
			 {
				 ModMonitor.Log($"[DEBUG] Exploring warps from: {location.NameOrUniqueName}", LogLevel.Debug);

				 if (route.Contains("FarmHouse"))
				 {
					 ModMonitor.Log($"[DEBUG] Exploring warps to: FarmHouse", LogLevel.Debug);
				 }

				 // Farm にある建物を探索し、ワープポイントとして追加
				 foreach (Building building in farm.buildings)
				 {
					 GameLocation indoors = building.GetIndoors();
					 if (indoors != null)
					 {
						 Point door = building.getPointForHumanDoor();
						 string indoorName = indoors.NameOrUniqueName;
						 ModMonitor.Log($"[DEBUG] Found building {indoorName} at door {door}", LogLevel.Debug);

						 MethodInfo addRouteMethod = AccessTools.Method(typeof(WarpPathfindingCache), "AddRoute", new Type[] { typeof(List<string>), typeof(Gender?) });
						 addRouteMethod.Invoke(null, new object[] { new List<string> { indoorName ?? "UnknownLocation" }, genderRestriction ?? Gender.Undefined });

						 // ExploreWarpPointsメソッドを取得
						 MethodInfo exploreMethod = AccessTools.Method(
							 typeof(WarpPathfindingCache), 
							 "ExploreWarpPoints", 
							 new Type[] { typeof(string), typeof(List<string>), typeof(Gender?), typeof(HashSet<string>) }
						 );
						 // 人間用のドアの位置を経路として追加
						 // exploreMethod.Invoke(null, new object[] { indoorName, route, genderRestriction, new HashSet<string>() });
					 }
				 }
			 }
			 return true; // 元の処理も実行

			 /*
			 // ModMonitor.Log($"[DEBUG] Exploring warps...", LogLevel.Debug);

			 if (location != null)
			 {
				 if (location.NameOrUniqueName == "Farm")
				 {
					 ModMonitor.Log($"[DEBUG] Exploring warps from: {location.NameOrUniqueName}", LogLevel.Debug);
				 }

				 if (location.ShouldExcludeFromNpcPathfinding())
				 {
					 // ModMonitor.Log($"[DEBUG] {location.NameOrUniqueName} is EXCLUDED from pathfinding!", LogLevel.Debug);
				 }
			 }
			 else
			 {
				 ModMonitor.Log($"[DEBUG] location is null!", LogLevel.Debug);
			 }
			 return true;
			 */
		}

		private static bool ExploreWarpPoints2Prefix(string locationName, List<string> route, Gender? genderRestriction, HashSet<string> seenTargets)
		{
			 GameLocation location = Game1.getLocationFromName(locationName);

			 if (location != null)
			 {
				 if (location is Farm farm)
				 {
					 // ModMonitor.Log($"[DEBUG] Exploring warps (2) from: {location.NameOrUniqueName}", LogLevel.Debug);
				 }
				
				 if (location.ShouldExcludeFromNpcPathfinding())
				 {
					 // ModMonitor.Log($"[DEBUG] {location.NameOrUniqueName} is EXCLUDED from pathfinding!", LogLevel.Debug);
				 }
			 }
			 else
			 {
				 // ModMonitor.Log($"[DEBUG] Exploring warps (2)", LogLevel.Debug);
			 }

			 return true;
		}

		private static void GameLocationShouldExcludeFromNpcPathfindingPostfix(GameLocation __instance, ref bool __result)
		{
			 // ModMonitor.Log($"[DEBUG] NameOrUniqueName: {__instance.NameOrUniqueName}", LogLevel.Debug);
			 if (__instance.NameOrUniqueName == "Farm" || __instance.NameOrUniqueName == "FarmHouse")
			 {
				 // ModMonitor.Log($"[DEBUG] ShouldExcludeFromNpcPathfinding() called for Farm: {__result}", LogLevel.Debug);
			 }
		}

		// private static void updateDoorsPostfix(GameLocation __instance)
		// {
		// 	 try
		// 	 {
		// 		 ModMonitor.Log("[DEBUG] Manually adding FarmHouse door", LogLevel.Debug);

		// 		 // `Farm` を取得
		// 		 GameLocation farm = Game1.getLocationFromName("Farm");
		// 		 if (farm == null)
		// 		 {
		// 			 ModMonitor.Log("[ERROR] Could not find Farm location!", LogLevel.Error);
		// 			 return;
		// 		 }

		// 		 // `FarmHouse` へのドアを追加
		// 		 Point farmHouseDoor = new Point(69, 16); // ★ FarmHouse の入口のタイル座標（適宜変更！）
		// 		 farm.doors[farmHouseDoor] = new String("FarmHouse");

		// 		 ModMonitor.Log($"[DEBUG] Added Farm -> FarmHouse door at {farmHouseDoor}", LogLevel.Debug);
		// 	 }
		// 	 catch (Exception ex)
		// 	 {
		// 		 ModMonitor.Log($"[ERROR] Failed to add FarmHouse door: {ex}", LogLevel.Error);
		// 	 }
		// }

		private static void getWarpFromdoorPointtfix(GameLocation __instance, Point door, ref Warp __result, Character character)
		{
			 if (__instance is Farm farm && character is NPC)
			 {
				 foreach (Building building in farm.buildings)
				 {
					 if (door == building.getPointForHumanDoor())
					 {
						 GameLocation indoors = building.GetIndoors();
						 if (indoors != null)
						 {
							 ModMonitor.Log($"[DEBUG] NPC entering {indoors.NameOrUniqueName} from {door}", LogLevel.Debug);
							 __result = new Warp(door.X, door.Y, indoors.NameOrUniqueName, indoors.warps[0].X, indoors.warps[0].Y - 1, false);
						 }
					 }
				 }
			 }
		}

		// private static bool handleWarpsPrefix(PathFindController __instance, Rectangle position)
		// {
		// 	Character? character = AccessTools.Field(typeof(PathFindController), "character").GetValue(__instance) as Character;

		// 	Warp w = __instance.location.isCollidingWithWarpOrDoor(position, character);
		// 	if (w == null)
		// 	{
		// 		return true;
		// 	}
		// 	ModMonitor.Log("[DEBUG] Entering handleWarps()", LogLevel.Debug);
		// 	return true;
		// }

		private static void GetLocationRoutePostfix(NPC __instance, ref string[] __result, string startingLocation, string endingLocation)
		{
			try
			{
				if (__instance != null && __instance.Name == "Seiris")
				{
					if (__result != null && __result.Length > 0)
					{
						string routeStr = string.Join(" -> ", __result);
						ModMonitor.Log($"[DEBUG] getLocationRoute({startingLocation}, {endingLocation}) -> {routeStr}", LogLevel.Debug);
					}
					else
					{
						ModMonitor.Log($"[ERROR] getLocationRoute({startingLocation}, {endingLocation}) returned NULL or EMPTY!", LogLevel.Error);
					}
				}
			}
			catch (Exception ex)
			{
				ModMonitor.Log($"[ERROR] Exception in GetLocationRoutePostfix: {ex}", LogLevel.Error);
			}
		}

		private static bool pathfindToNextScheduleLocationPrefix(NPC __instance, string scheduleKey, string startingLocation, int startingX, int startingY, string endingLocation, int endingX, int endingY, int finalFacingDirection, string endBehavior, string endMessage)
		{
			if (__instance.Name == "Seiris" )
			{

				ModMonitor.Log($"[DEBUG]", LogLevel.Debug);
				ModMonitor.Log($"[DEBUG] pathfindToNextScheduleLocation Prefix ----------", LogLevel.Debug);
				ModMonitor.Log($"[DEBUG] Route: {startingLocation} ({startingX},{startingY}) -> {endingLocation} ({endingX},{endingY})", LogLevel.Debug);

				// 🔥 メソッド内の `return` 直前にデバッグログを仕込む！
				// ModMonitor.Log($"[DEBUG] Checking return conditions in pathfindToNextScheduleLocation()", LogLevel.Debug);

				if (startingLocation == endingLocation)
				{
					ModMonitor.Log($"[DEBUG] Skipping because startingLocation == endingLocation", LogLevel.Debug);
					return true;
				}

				if (__instance == null)
				{
					ModMonitor.Log($"[ERROR] Skipping because NPC instance is NULL", LogLevel.Error);
					return true;
				}

				MethodInfo getLocationRouteMethod = AccessTools.Method(typeof(NPC), "getLocationRoute", new Type[] { typeof(string), typeof(string) });
				string[] locationsRoute = (string[])getLocationRouteMethod.Invoke(__instance, new object[] { startingLocation, endingLocation });
				string routeStr = locationsRoute != null ? string.Join(" -> ", locationsRoute) : "NULL";
				
				ModMonitor.Log($"[DEBUG] getLocationRoute Result: {routeStr}", LogLevel.Debug);

				if (locationsRoute == null || locationsRoute.Length == 0)
				{
					ModMonitor.Log($"[ERROR] Skipping because locationsRoute is NULL or EMPTY", LogLevel.Error);
					return true;
				}

				for (int i = 0; i < locationsRoute.Length - 1; i++)
				{
					GameLocation currentLocation = Game1.getLocationFromName(locationsRoute[i]);
					if (currentLocation == null)
					{
						ModMonitor.Log($"[ERROR] Skipping because currentLocation is NULL", LogLevel.Error);
						return true;
					}

					Point warpPoint = currentLocation.getWarpPointTo(locationsRoute[i + 1]);
					if (warpPoint == Point.Zero)
					{
						ModMonitor.Log($"[ERROR] Skipping because warpPoint is Zero!", LogLevel.Error);
						return true;
					}

					ModMonitor.Log($"[DEBUG] Checking if findPathForNPCSchedules is actually called", LogLevel.Debug);
					Stack<Point> debugPath = PathFindController.findPathForNPCSchedules(new Point(startingX, startingY), new Point(endingX, endingY), currentLocation, 30000);

					if (debugPath == null || debugPath.Count == 0)
					{
						ModMonitor.Log($"[ERROR] findPathForNPCSchedules was called but returned NULL or EMPTY!", LogLevel.Error);
					}
					else
					{
						string debugPathStr = string.Join(" -> ", debugPath.Select(p => $"({p.X}, {p.Y})"));
						ModMonitor.Log($"[DEBUG] findPathForNPCSchedules would return: {debugPathStr}", LogLevel.Debug);
					}
				}
				// ModMonitor.Log($"[DEBUG] pathfindToNextScheduleLocation END", LogLevel.Debug);
			}

			return true;
		}

		private static void pathfindToNextScheduleLocationPostfix(NPC __instance, string scheduleKey, string startingLocation, int startingX, int startingY, string endingLocation, int endingX, int endingY, int finalFacingDirection, string endBehavior, string endMessage, ref SchedulePathDescription __result)
		{
			if (__instance.Name == "Seiris")
			{
				ModMonitor.Log($"[DEBUG] pathfindToNextScheduleLocation Postfix ----------", LogLevel.Debug);

				MethodInfo getLocationRouteMethod = AccessTools.Method(typeof(NPC), "getLocationRoute", new Type[] { typeof(string), typeof(string) });
				MethodInfo addToStackForScheduleMethod = AccessTools.Method(typeof(NPC), "addToStackForSchedule", new Type[] { typeof(Stack<Point>), typeof(Stack<Point>) });

				Stack<Point> path = new Stack<Point>();
				Point locationStartPoint = new Point(startingX, startingY);

				if (startingLocation == "Farm" || startingLocation == "FarmHouse")
				{
					ModMonitor.Log($"[DEBUG] locationStartPoint: {locationStartPoint} ({startingX}, {startingY})", LogLevel.Debug);
				}

				if (locationStartPoint == Point.Zero)
				{
					ModMonitor.Log($"[ERROR] locationStartPoint == Point.Zero", LogLevel.Error);
					return;
				}
				// ModMonitor.Log($"[DEBUG] getLocationRoute() {string.Join(" -> ", (string[])getLocationRouteMethod.Invoke(__instance, new object[] { startingLocation, endingLocation }) ?? Array.Empty<string>())}", LogLevel.Debug);
				string[] locationsRoute = ((!startingLocation.Equals(endingLocation, StringComparison.Ordinal)) 
					? (string[])getLocationRouteMethod.Invoke(__instance, new object[] { startingLocation, endingLocation }) 
					: null);
				ModMonitor.Log($"[DEBUG] Route: {startingLocation} ({startingX},{startingY}) -> {endingLocation} ({endingX},{endingY}) detail: {string.Join(" -> ", locationsRoute ?? Array.Empty<string>())}", LogLevel.Debug);

				if (__instance == null)
				{
					ModMonitor.Log($"[ERROR] NPC == NULL", LogLevel.Error);
					return;
				}
				// if (startingLocation == endingLocation)
				// {
				// 	ModMonitor.Log($"[DEBUG] startingLocation == endingLocation", LogLevel.Debug);
				// 	return;
				// }

				if (locationsRoute != null)
				{
					for (int i = 0; i < locationsRoute.Length; i++)
					{
						string targetLocationName = locationsRoute[i];
						foreach (string activePassiveFestival in Game1.netWorldState.Value.ActivePassiveFestivals)
						{
							if (Utility.TryGetPassiveFestivalData(activePassiveFestival, out var data) && data.MapReplacements != null && data.MapReplacements.TryGetValue(targetLocationName, out var newName))
							{
								targetLocationName = newName;
								break;
							}
						}
						GameLocation currentLocation = Game1.getLocationFromName(locationsRoute[i]);
						if (currentLocation == null)
						{
							ModMonitor.Log($"[ERROR] currentLocation == null", LogLevel.Error);
							return;
						}
						if (currentLocation.Name.Equals("Trailer") && Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade"))
						{
							currentLocation = Game1.RequireLocation("Trailer_Big");
						}
						if (i < locationsRoute.Length - 1)
						{
							Point target = currentLocation.getWarpPointTo(locationsRoute[i + 1]);
							if (target == Point.Zero)
							{
								ModMonitor.Log($"[ERROR] target == Point.Zero", LogLevel.Error);
								return;
							}
							path = (Stack<Point>)addToStackForScheduleMethod.Invoke(__instance, new object[] { path, PathFindController.findPathForNPCSchedules(locationStartPoint, target, currentLocation, 30000) });
							locationStartPoint = currentLocation.getWarpPointTarget(target, __instance);
							ModMonitor.Log($"[DEBUG] *** 1 locationStartPoint: {locationStartPoint}, currentLocation: {currentLocation}, target: {target}", LogLevel.Debug);
						}
						else
						{
							ModMonitor.Log($"[DEBUG] *** 2", LogLevel.Debug);
							path = (Stack<Point>)addToStackForScheduleMethod.Invoke(__instance, new object[] { path, PathFindController.findPathForNPCSchedules(locationStartPoint, new Point(endingX, endingY), currentLocation, 30000) });
						}
					}
				}
				else if (startingLocation.Equals(endingLocation, StringComparison.Ordinal))
				{
					string targetLocationName = startingLocation;
					foreach (string activePassiveFestival2 in Game1.netWorldState.Value.ActivePassiveFestivals)
					{
						if (Utility.TryGetPassiveFestivalData(activePassiveFestival2, out var data) && data.MapReplacements != null && data.MapReplacements.TryGetValue(targetLocationName, out var newName))
						{
							targetLocationName = newName;
							break;
						}
					}
					GameLocation location = Game1.RequireLocation(targetLocationName);
					if (location.Name.Equals("Trailer") && Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade"))
					{
						location = Game1.RequireLocation("Trailer_Big");
					}
					ModMonitor.Log($"[DEBUG] *** 3", LogLevel.Debug);
					path = (Stack<Point>)addToStackForScheduleMethod.Invoke(__instance, new object[] { path, PathFindController.findPathForNPCSchedules(locationStartPoint, new Point(endingX, endingY), location, 30000) });
				}
				SchedulePathDescription scheduleDesc = new SchedulePathDescription(path, finalFacingDirection, endBehavior, endMessage, endingLocation, new Point(endingX, endingY));
				string routeStr = string.Join(" -> ", scheduleDesc.route.Select(p => $"({p.X}, {p.Y})"));
				if (scheduleDesc.route == null || scheduleDesc.route.Count == 0)
				{
					routeStr = "[empty]";
				}
				ModMonitor.Log($"[DEBUG] scheduleDesc route: {routeStr}, target: {scheduleDesc.targetLocationName} {scheduleDesc.targetTile}", LogLevel.Debug);
			}
		}

		private static void parseMasterScheduleImplPostfix(NPC __instance, string scheduleKey, string rawData, List<string> visited,  ref Dictionary<int, SchedulePathDescription> __result)
		{
			if (__instance.Name == "Seiris")
			{
				ModMonitor.Log($"[DEBUG]", LogLevel.Debug);
				ModMonitor.Log($"[DEBUG] parseMasterScheduleImpl() called for {__instance.Name} ----------", LogLevel.Debug);
	
				Dictionary<int, SchedulePathDescription> ret = new Dictionary<int, SchedulePathDescription>();
				NetVector2 defaultPosition = AccessTools.Field(typeof(NPC), "defaultPosition").GetValue(__instance) as NetVector2;
				MethodInfo parseMasterScheduleImplMethod = AccessTools.Method(typeof(NPC), "parseMasterScheduleImpl", new Type[] { typeof(string), typeof(string), typeof(List<string>) });
				MethodInfo changeScheduleMethod = AccessTools.Method(typeof(NPC), "changeScheduleForLocationAccessibility",
					new Type[] {
						typeof(string).MakeByRefType(), // 🔥 `ref string` は `MakeByRefType()` を使う！
						typeof(int).MakeByRefType(),	// 🔥 `ref int`
						typeof(int).MakeByRefType(),	// 🔥 `ref int`
						typeof(int).MakeByRefType()	 // 🔥 `ref int`
					}
				);

				if (visited.Contains<string>(scheduleKey, StringComparer.OrdinalIgnoreCase))
				{
					ModMonitor.Log($"[WARN] NPC {__instance.Name} can't load schedules because they led to an infinite loop ({string.Join(" -> ", visited)} -> {scheduleKey}).", LogLevel.Warn);
					ret = new Dictionary<int, SchedulePathDescription>();
				}
				visited.Add(scheduleKey);
				try
				{
					string[] split = NPC.SplitScheduleCommands(rawData);
					Dictionary<int, SchedulePathDescription> oneDaySchedule = new Dictionary<int, SchedulePathDescription>();
					int routesToSkip = 0;
					if (split[0].Contains("GOTO"))
					{
						string newKey = ArgUtility.SplitBySpaceAndGet(split[0], 1);
						Dictionary<string, string> allSchedules = __instance.getMasterScheduleRawData();
						if (string.Equals(newKey, "season", StringComparison.OrdinalIgnoreCase))
						{
							newKey = Game1.currentSeason;
							if (!allSchedules.ContainsKey(newKey))
							{
								newKey = "spring";
							}
						}
						try
						{
							if (allSchedules.TryGetValue(newKey, out var newScript))
							{
								ret = parseMasterScheduleImplMethod.Invoke(null, new object[] { newKey, newScript, visited }) as Dictionary<int, SchedulePathDescription>;
							}
							ModMonitor.Log($"[ERROR] Failed to load schedule '{scheduleKey}' for NPC '{__instance.Name}': GOTO references schedule '{newKey}' which doesn't exist. Falling back to 'spring'.", LogLevel.Error);
						}
						catch (Exception e)
						{
							ModMonitor.Log($"[ERROR] Failed to load schedule '{scheduleKey}' for NPC '{__instance.Name}': GOTO references schedule '{newKey}' which couldn't be parsed. Falling back to 'spring'.", LogLevel.Error);
						}
						ret = parseMasterScheduleImplMethod.Invoke(null, new object[] { "spring", __instance.getMasterScheduleEntry("spring"), visited }) as Dictionary<int, SchedulePathDescription>;
					}
					if (split[0].Contains("NOT"))
					{
						string[] commandSplit = ArgUtility.SplitBySpace(split[0]);
						if (commandSplit[1].ToLower() == "friendship")
						{
							int index = 2;
							bool conditionMet = false;
							for (; index < commandSplit.Length; index += 2)
							{
								string who = commandSplit[index];
								if (int.TryParse(commandSplit[index + 1], out var level))
								{
									foreach (Farmer allFarmer in Game1.getAllFarmers())
									{
										if (allFarmer.getFriendshipHeartLevelForNPC(who) >= level)
										{
											conditionMet = true;
											break;
										}
									}
								}
								if (conditionMet)
								{
									break;
								}
							}
							if (conditionMet)
							{
								ret = parseMasterScheduleImplMethod.Invoke(null, new object[] { "spring", __instance.getMasterScheduleEntry("spring"), visited }) as Dictionary<int, SchedulePathDescription>;
							}
							routesToSkip++;
						}
					}
					else if (split[0].Contains("MAIL"))
					{
						string mailID = ArgUtility.SplitBySpace(split[0])[1];
						routesToSkip = (!Game1.MasterPlayer.mailReceived.Contains(mailID) && !NetWorldState.checkAnywhereForWorldStateID(mailID)) ? (routesToSkip + 1) : (routesToSkip + 2);
					}
					if (split[routesToSkip].Contains("GOTO"))
					{
						string newKey = ArgUtility.SplitBySpaceAndGet(split[routesToSkip], 1);
						string text = newKey.ToLower();
						if (!(text == "season"))
						{
							if (text == "no_schedule")
							{
								__instance.followSchedule = false;
								ret = null;
							}
						}
						else
						{
							newKey = Game1.currentSeason;
						}
						ret = parseMasterScheduleImplMethod.Invoke(null, new object[] { newKey, __instance.getMasterScheduleEntry(newKey), visited }) as Dictionary<int, SchedulePathDescription>;
					}
					Point previousPosition = __instance.isMarried() ? new Point(10, 23) : new Point((int)defaultPosition.X / 64, (int)defaultPosition.Y / 64);
					string previousGameLocation = __instance.isMarried() ? "BusStop" : __instance.defaultMap.Value;
					int previousTime = 610;
					string default_map = __instance.DefaultMap;
					int default_x = (int)(defaultPosition.X / 64f);
					int default_y = (int)(defaultPosition.Y / 64f);
					bool default_map_dirty = false;
					for (int i = routesToSkip; i < split.Length; i++)
					{
						int index = 0;
						string[] newDestinationDescription = ArgUtility.SplitBySpace(split[i]);
						bool time_is_arrival_time = false;
						string time_string = newDestinationDescription[index];
						if (time_string.Length > 0 && newDestinationDescription[index][0] == 'a')
						{
							time_is_arrival_time = true;
							time_string = time_string.Substring(1);
						}
						int time = Convert.ToInt32(time_string);
						index++;
						string location = newDestinationDescription[index];
						string endOfRouteAnimation = null;
						string endOfRouteMessage = null;
						int xLocation = 0;
						int yLocation = 0;
						int localFacingDirection = 2;
						if (location == "bed")
						{
							if (__instance.isMarried())
							{
								location = "BusStop";
								xLocation = 9;
								yLocation = 23;
								localFacingDirection = 3;
							}
							else
							{
								string default_schedule = null;
								if (__instance.hasMasterScheduleEntry("default"))
								{
									default_schedule = __instance.getMasterScheduleEntry("default");
								}
								else if (__instance.hasMasterScheduleEntry("spring"))
								{
									default_schedule = __instance.getMasterScheduleEntry("spring");
								}
								if (default_schedule != null)
								{
									try
									{
										string[] last_schedule_split = ArgUtility.SplitBySpace(NPC.SplitScheduleCommands(default_schedule)[^1]);
										location = last_schedule_split[1];
										if (last_schedule_split.Length > 3)
										{
											if (!int.TryParse(last_schedule_split[2], out xLocation) || !int.TryParse(last_schedule_split[3], out yLocation))
											{
												default_schedule = null;
											}
										}
										else
										{
											default_schedule = null;
										}
									}
									catch (Exception)
									{
										default_schedule = null;
									}
								}
								if (default_schedule == null)
								{
									location = default_map;
									xLocation = default_x;
									yLocation = default_y;
								}
							}
							index++;
							Dictionary<string, string> dictionary = DataLoader.AnimationDescriptions(Game1.content);
							string sleep_behavior = __instance.name.Value.ToLower() + "_sleep";
							if (dictionary.ContainsKey(sleep_behavior))
							{
								endOfRouteAnimation = sleep_behavior;
							}
						}
						else
						{
							if (int.TryParse(location, out var _))
							{
								location = previousGameLocation;
								index--;
							}
							index++;
							xLocation = Convert.ToInt32(newDestinationDescription[index]);
							index++;
							yLocation = Convert.ToInt32(newDestinationDescription[index]);
							index++;
							try
							{
								if (newDestinationDescription.Length > index)
								{
									if (int.TryParse(newDestinationDescription[index], out localFacingDirection))
									{
										index++;
									}
									else
									{
										localFacingDirection = 2;
									}
								}
							}
							catch (Exception)
							{
								localFacingDirection = 2;
							}
						}
						if ((bool)changeScheduleMethod.Invoke(__instance, new object[] { location, xLocation, yLocation, localFacingDirection }))
						{
							string newKey = __instance.getMasterScheduleRawData().ContainsKey("default") ? "default" : "spring";
							ret = parseMasterScheduleImplMethod.Invoke(null, new object[] { newKey, __instance.getMasterScheduleEntry(newKey), visited }) as Dictionary<int, SchedulePathDescription>;
						}
						if (index < newDestinationDescription.Length)
						{
							if (newDestinationDescription[index].Length > 0 && newDestinationDescription[index][0] == '"')
							{
								endOfRouteMessage = split[i].Substring(split[i].IndexOf('"'));
							}
							else
							{
								endOfRouteAnimation = newDestinationDescription[index];
								index++;
								if (index < newDestinationDescription.Length && newDestinationDescription[index].Length > 0 && newDestinationDescription[index][0] == '"')
								{
									endOfRouteMessage = split[i].Substring(split[i].IndexOf('"')).Replace("\"", "");
								}
							}
						}
						if (time == 0)
						{
							default_map_dirty = true;
							default_map = location;
							default_x = xLocation;
							default_y = yLocation;
							previousGameLocation = location;
							previousPosition.X = xLocation;
							previousPosition.Y = yLocation;
							__instance.faceDirection(localFacingDirection);
							__instance.previousEndPoint = new Point(xLocation, yLocation);
							continue;
						}
						// ModMonitor.Log($"[DEBUG] xLocation: {xLocation}, yLocation: {yLocation}, previousPosition.X: {previousPosition.X}, previousPosition.Y: {previousPosition.Y}", LogLevel.Debug);
						SchedulePathDescription path_description = __instance.pathfindToNextScheduleLocation(scheduleKey, previousGameLocation, previousPosition.X, previousPosition.Y, location, xLocation, yLocation, localFacingDirection, endOfRouteAnimation, endOfRouteMessage);
						if (time_is_arrival_time)
						{
							int distance_traveled = 0;
							Point? last_point = null;
							foreach (Point point in path_description.route)
							{
								if (!last_point.HasValue)
								{
									last_point = point;
									continue;
								}
								if (Math.Abs(last_point.Value.X - point.X) + Math.Abs(last_point.Value.Y - point.Y) == 1)
								{
									distance_traveled += 64;
								}
								last_point = point;
							}
							int num = distance_traveled / 2;
							int ticks_per_ten_minutes = Game1.realMilliSecondsPerGameTenMinutes / 1000 * 60;
							int travel_time = (int)Math.Round((float)num / (float)ticks_per_ten_minutes) * 10;
							time = Math.Max(Utility.ConvertMinutesToTime(Utility.ConvertTimeToMinutes(time) - travel_time), previousTime);
						}
						path_description.time = time;
						oneDaySchedule.Add(time, path_description);
						previousPosition.X = xLocation;
						previousPosition.Y = yLocation;
						previousGameLocation = location;
						previousTime = time;
					}
					if (Game1.IsMasterGame && default_map_dirty)
					{
						Game1.warpCharacter(__instance, default_map, new Point(default_x, default_y));
					}
					ret = oneDaySchedule;
				}
				catch (Exception ex)
				{
					ModMonitor.Log($"[ERROR] NPC '{__instance.Name}' failed to parse master schedule '{scheduleKey}' with raw data '{rawData}'.", LogLevel.Error);
					ret = new Dictionary<int, SchedulePathDescription>();
				}
		
				// 結果確認
				if (ret != null && ret.Count > 0)
				{
					ModMonitor.Log($"[DEBUG] ret contains {ret.Count} schedule entries:", LogLevel.Debug);

					foreach (var entry in ret)
					{
						SchedulePathDescription desc = entry.Value;
						ModMonitor.Log($"[DEBUG] Time: {entry.Key} -> Location: {desc.targetLocationName} ({desc.targetTile.X}, {desc.targetTile.Y})", LogLevel.Debug);

						if (desc.route != null && desc.route.Count > 0)
						{
							string routeStr = string.Join(" -> ", desc.route.Reverse().Select(p => $"({p.X}, {p.Y})"));
							ModMonitor.Log($"[DEBUG] Route: {routeStr}", LogLevel.Debug);
						}
						else
						{
							ModMonitor.Log($"[ERROR] Route is EMPTY!", LogLevel.Error);
						}
					}
				}
				else
				{
					ModMonitor.Log($"[ERROR] ret is EMPTY or NULL!", LogLevel.Error);
				}

			}

		}

		private static bool findPathForNPCSchedulesPrefix(ref Point startPoint, Point endPoint, GameLocation location, int limit)
		{
			if (location.NameOrUniqueName == "FarmHouse" && startPoint == Point.Zero)
			{
				// ModMonitor.Log($"[DEBUG] Adjusting startPoint from {startPoint} to (3,11) in {location.NameOrUniqueName}", LogLevel.Debug);
				// startPoint = new Point(3, 11);
			}
			return true; // 🔥 `true` を返せば、元の `findPathForNPCSchedules()` も実行される！
		}

		private static void findPathForNPCSchedulesPostfix(PathFindController __instance, Point startPoint, Point endPoint, GameLocation location, int limit, ref Stack<Point> __result)
		{
			// if(location.NameOrUniqueName == "Farm" || location.NameOrUniqueName == "FarmHouse")
			// {
			// 	return;
			// }

			Stack<Point> ret = new Stack<Point>();
			sbyte[,] Directions = AccessTools.Field(typeof(PathFindController), "Directions").GetValue(__instance) as sbyte[,];
			Character? character = Game1.currentLocation?.characters?.FirstOrDefault(n => n is NPC && n.Name == "Seiris") as NPC;
			MethodInfo isPositionImpassableMethod = AccessTools.Method(typeof(PathFindController), "isPositionImpassableForNPCSchedule");
			MethodInfo getPreferenceValueForTerrainTypeMethod = AccessTools.Method(typeof(PathFindController), "getPreferenceValueForTerrainType", new Type[] { typeof(GameLocation), typeof(int), typeof(int) });

			PriorityQueue openList = new PriorityQueue();
			HashSet<int> closedList = new HashSet<int>();
			int iterations = 0;
			openList.Enqueue(new PathNode(startPoint.X, startPoint.Y, 0, null), Math.Abs(endPoint.X - startPoint.X) + Math.Abs(endPoint.Y - startPoint.Y));
			PathNode previousNode = (PathNode)openList.Peek();
			int layerWidth = location.map.Layers[0].LayerWidth;
			int layerHeight = location.map.Layers[0].LayerHeight;
			// ModMonitor.Log($"[DEBUG] startPoint = ({startPoint.X}, {startPoint.Y})", LogLevel.Debug);
			while (!openList.IsEmpty())
			{
				PathNode currentNode = openList.Dequeue();

				if (location.NameOrUniqueName == "FarmHouse")
				{
					// ModMonitor.Log($"[DEBUG] currentNode = ({currentNode.x}, {currentNode.y}), endPoint = ({endPoint.X}, {endPoint.Y})", LogLevel.Debug);
				}
				if (currentNode.x == endPoint.X && currentNode.y == endPoint.Y)
				{
					ret = PathFindController.reconstructPath(currentNode);
					if (location.NameOrUniqueName == "Farm" || location.NameOrUniqueName == "FarmHouse")
					{
						// ModMonitor.Log($"[DEBUG] ret: {string.Join(" -> ", ret.Select(p => $"({p.X}, {p.Y})"))}", LogLevel.Debug);
					}
				}
				closedList.Add(currentNode.id);
				for (int i = 0; i < 4; i++)
				{
					int neighbor_tile_x = currentNode.x + Directions[i, 0];
					int neighbor_tile_y = currentNode.y + Directions[i, 1];
					int nid = PathNode.ComputeHash(neighbor_tile_x, neighbor_tile_y);
					if (closedList.Contains(nid))
					{
						continue;
					}
					PathNode neighbor = new PathNode(neighbor_tile_x, neighbor_tile_y, currentNode);
					neighbor.g = (byte)(currentNode.g + 1);
					if ((neighbor.x == endPoint.X && neighbor.y == endPoint.Y) || (neighbor.x >= 0 && neighbor.y >= 0 && neighbor.x < layerWidth && neighbor.y < layerHeight && !(bool)isPositionImpassableMethod.Invoke(null, new object[] { location, neighbor.x, neighbor.y, character })))
					{
						int f = neighbor.g + (int)getPreferenceValueForTerrainTypeMethod.Invoke(null, new object[] { location, neighbor.x, neighbor.y }) + (Math.Abs(endPoint.X - neighbor.x) + Math.Abs(endPoint.Y - neighbor.y) + (((neighbor.x == currentNode.x && neighbor.x == previousNode.x) || (neighbor.y == currentNode.y && neighbor.y == previousNode.y)) ? (-2) : 0));
						if (!openList.Contains(neighbor, f))
						{
							openList.Enqueue(neighbor, f);
						}
					}
				}
				previousNode = currentNode;
				iterations++;
				if (iterations >= limit)
				{
					ret = null;
				}
			}
			if (ret != null && ret.Count > 0)
			{
				ModMonitor.Log($"[DEBUG] findPathForNPCSchedulesPostfix ret: {location.NameOrUniqueName} {string.Join(" -> ", ret.Select(p => $"({p.X}, {p.Y})"))}", LogLevel.Debug);
			}
			else
			{
				ModMonitor.Log($"[ERROR] findPathForNPCSchedulesPostfix ret: {location.NameOrUniqueName} No path found!", LogLevel.Error);
			}

			// if (__result != null && __result.Count > 0)
			// {
			// 	string routeStr = string.Join(" -> ", __result.Select(p => $"({p.X}, {p.Y})"));
			// 	// ModMonitor.Log($"[DEBUG] Generated Path: {routeStr}", LogLevel.Debug);
			// }
			// else
			// {
			// 	ModMonitor.Log($"[ERROR] No path found!", LogLevel.Error);
			// }
		}

		private static void getWarpPointToPostfix(GameLocation __instance, string location, Character character, Point __result)
		{
			if (__instance.NameOrUniqueName == "Farm" || __instance.NameOrUniqueName == "FarmHouse")
			{
				ModMonitor.Log($"[DEBUG] getWarpPointTo", LogLevel.Debug);
				if (__result == Point.Zero)
				{
					ModMonitor.Log($"[ERROR] getWarpPointTo({location}) returned Point.Zero in {__instance.NameOrUniqueName}!", LogLevel.Error);
				}
				else
				{
					ModMonitor.Log($"[DEBUG] getWarpPointTo({location}) -> ({__result.X}, {__result.Y}) in {__instance.NameOrUniqueName}", LogLevel.Debug);
				}
			}
		}

		private static void DebugFindPathCommand(string command, string[] args)
		{
			if (args.Length < 4)
			{
				ModMonitor.Log($"Usage: findpath <startX> <startY> <endX> <endY>", LogLevel.Info);
				return;
			}

			int startX = int.Parse(args[0]);
			int startY = int.Parse(args[1]);
			int endX = int.Parse(args[2]);
			int endY = int.Parse(args[3]);

			GameLocation currentLocation = Game1.getLocationFromName("FarmHouse");;
			Stack<Point> result = PathFindController.findPathForNPCSchedules(new Point(startX, startY), new Point(endX, endY), currentLocation, 30000);

			if (result != null && result.Count > 0)
			{
				string routeStr = string.Join(" -> ", result.Select(p => $"({p.X}, {p.Y})"));
				ModMonitor.Log($"[DEBUG] Manually called findPathForNPCSchedules() -> {routeStr}", LogLevel.Debug);
			}
			else
			{
				ModMonitor.Log($"[ERROR] Manually called findPathForNPCSchedules() but no path found!", LogLevel.Error);
			}
		}

		private static bool checkSchedulePrefix(NPC __instance)
		{
			if (__instance != null && __instance.Name == "Seiris")
			{
				ModMonitor.Log($"[DEBUG] checkSchedule() called", LogLevel.Debug);

				StackTrace stackTrace = new StackTrace();
				// ModMonitor.Log($"[DEBUG] StackTrace:\n{stackTrace}", LogLevel.Debug);
			}
			return true;
		}

		private static void DayUpdatePostfix(Farm __instance, int dayOfMonth)
		{
			ModMonitor.Log($"[DEBUG] DayUpdate() __instance = {__instance.NameOrUniqueName}", LogLevel.Debug);
			try
			{
				foreach (Building building in __instance.buildings)
				{
					if (building.GetIndoors() != null)
					{
						Point doorPoint = building.getPointForHumanDoor();
						__instance.doors[doorPoint] = building.GetIndoors().NameOrUniqueName;
						ModMonitor.Log($"[DEBUG] Added door in DayUpdate: Farm ({doorPoint.X}, {doorPoint.Y}) -> {building.GetIndoors().NameOrUniqueName}", LogLevel.Debug);
					}
				}
			}
			catch (Exception ex)
			{
				ModMonitor.Log($"[ERROR] Failed to add doors in DayUpdate: {ex}", LogLevel.Error);
			}
		}

		private static void UpdateWhenCurrentLocationPostfix(GameLocation __instance, GameTime time)
		{
			if (__instance is Farm farm)
			{
				ModMonitor.Log($"[DEBUG] UpdateWhenCurrentLocation() __instance = {__instance.NameOrUniqueName}", LogLevel.Debug);
				foreach (Building building in __instance.buildings)
				{
					if (building.GetIndoors() != null)
					{
						Point doorPoint = building.getPointForHumanDoor();
						__instance.doors[doorPoint] = building.GetIndoors().NameOrUniqueName;
						ModMonitor.Log($"[DEBUG] UpdateWhenCurrentLocation() -> Added door: Farm ({doorPoint.X}, {doorPoint.Y}) -> {building.GetIndoors().NameOrUniqueName}", LogLevel.Debug);
					}
				}
			}
		}

		// private static void loadObjectsPostfix(GameLocation __instance)
		// {
		// 	if (__instance is Farm farm)
		// 	{
		// 		ModMonitor.Log($"[DEBUG] loadObjects() __instance = {__instance.NameOrUniqueName}", LogLevel.Debug);
		// 		foreach (Building building in __instance.buildings)
		// 		{
		// 			if (building.GetIndoors() != null)
		// 			{
		// 				Point doorPoint = building.getPointForHumanDoor();
		// 				__instance.doors[doorPoint] = building.GetIndoors().NameOrUniqueName;
		// 				ModMonitor.Log($"[DEBUG] loadObjects() -> Added door: Farm ({doorPoint.X}, {doorPoint.Y}) -> {building.GetIndoors().NameOrUniqueName}", LogLevel.Debug);
		// 			}
		// 		}
		// 	}
		// }

		private static void BuildStructurePostfix(GameLocation __instance, Building building, Vector2 tileLocation, Farmer who, bool skipSafetyChecks)
		{
			try
			{
				ModMonitor.Log($"[DEBUG] Building structure: {building.buildingType.Value} at {tileLocation} in {__instance.NameOrUniqueName}", LogLevel.Debug);

				// ここで追加の処理を行う（例えば、ログ出力やNPCの移動制限解除など）
			}
			catch (Exception ex)
			{
				ModMonitor.Log($"[ERROR] Harmony Patch Failed in BuildStructurePostfix: {ex}", LogLevel.Error);
			}
		}

	}
}