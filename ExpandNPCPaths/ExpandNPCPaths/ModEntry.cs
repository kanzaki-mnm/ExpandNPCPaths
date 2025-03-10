#pragma warning disable 8600

using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Pathfinding;
using StardewValley.Buildings;

// for debug
using Microsoft.Xna.Framework;
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
		}

		private static void updateDoorsPostfix(GameLocation __instance)
		{
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
		}

        private static void loadObjectsPostfix(GameLocation __instance)
        {
            if (__instance.NameOrUniqueName == "Farm")
            {
                // マップのすべてのタイルを走査
                foreach (Layer layer in __instance.map.Layers)
                {
                    for (int x = 0; x < layer.LayerWidth; x++)
                    {
                        for (int y = 0; y < layer.LayerHeight; y++)
                        {
                            Tile tile = layer.Tiles[x, y];
                            if (tile != null)
                            {
                                // `NPCBarrier` 属性を削除
                                if (tile.Properties.ContainsKey("NPCBarrier"))
                                {
                                    tile.Properties.Remove("NPCBarrier");
                                    ModEntry.ModMonitor.Log($"[DEBUG] Removed NPCBarrier from ({x}, {y}) in {__instance.NameOrUniqueName}", LogLevel.Debug);
                                }
                            }
                        }
                    }
                }
            }
        }

	}
}