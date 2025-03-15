#pragma warning disable CS8600 // Null リテラルまたは Null の可能性がある値を Null 非許容型に変換しています。
#pragma warning disable CS8601 // Null 参照代入の可能性があります。
#pragma warning disable CS0618 // 型またはメンバーが旧型式です

using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Pathfinding;
using StardewValley.Buildings;
using Microsoft.Xna.Framework;
using xTile.Tiles;
using xTile.Layers;
using StardewValley.GameData.Buildings;
using StardewValley.Objects;

namespace ExpandNPCPaths
{
    public class ModEntry : Mod
    {
        public static IMonitor ModMonitor = null!;

        public override void Entry(IModHelper helper)
        {
            ModMonitor = Monitor;

            // Harmony.DEBUG = true;
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
            


            // FarmHouse の経路探索を拡張
            // harmony.Patch(
            //     original: AccessTools.Method(typeof(GameLocation), "isTilePassable", new[] { typeof(Vector2) }),
            //     prefix: new HarmonyMethod(typeof(ModEntry), nameof(IsTilePassable_Prefix))
            // // );
            
            // harmony.Patch(
            //     original: AccessTools.Method(typeof(NPC), "checkSchedule", new Type[] { typeof(int) }),
            //     postfix: new HarmonyMethod(typeof(ModEntry), nameof(CheckSchedulePostfix))
            // );


            // PathFindController のコンストラクタにパッチ適用
            // harmony.Patch(
            //     original: AccessTools.Constructor(typeof(PathFindController), new[] {
            //         typeof(Character), typeof(GameLocation), typeof(PathFindController.isAtEnd), 
            //         typeof(int), typeof(PathFindController.endBehavior), typeof(int), typeof(Point), typeof(bool)
            //     }),
            //     postfix: new HarmonyMethod(typeof(ModEntry), nameof(PathFindControllerPostfix))
            // );

            // harmony.Patch(
            //     original: AccessTools.Constructor(typeof(PathFindController), new[] {
            //         typeof(Character), typeof(GameLocation), typeof(Point), typeof(int)
            //     }),
            //     postfix: new HarmonyMethod(typeof(ModEntry), nameof(PathFindControllerPostfix))
            // );

            // harmony.Patch(
            //     original: AccessTools.Constructor(typeof(PathFindController), new[] { typeof(Stack<Point>), typeof(GameLocation), typeof(Character), typeof(Point) }),
            //     postfix: new HarmonyMethod(typeof(ModEntry), nameof(PathFindControllerPostfix))
            // );

            // harmony.Patch(
            //     original: AccessTools.Constructor(typeof(PathFindController), new[] { typeof(Stack<Point>), typeof(Character), typeof(GameLocation) }),
            //     postfix: new HarmonyMethod(typeof(ModEntry), nameof(PathFindControllerPostfix))
            // );


            // NPC.update() の後に PathFindController の状態をチェック
            // harmony.Patch(
            //     original: AccessTools.Method(typeof(NPC), "update", new Type[] { typeof(GameTime), typeof(GameLocation) }),
            //     postfix: new HarmonyMethod(typeof(ModEntry), nameof(UpdatePostfix))
            // );

            // harmony.Patch(
            //     original: AccessTools.Method(typeof(NPC), "pathfindToNextScheduleLocation"),
            //     postfix: new HarmonyMethod(typeof(ModEntry), nameof(PathfindToNextScheduleLocationPostfix))
            // );


            // harmony.Patch(
            //     original: AccessTools.Method(typeof(PathFindController), "findPathForNPCSchedules", new Type[] { typeof(Point), typeof(Point), typeof(GameLocation), typeof(int) }),
            //     postfix: new HarmonyMethod(typeof(ModEntry), nameof(FindPathForNPCSchedulesPostfix))
            // );

            // FarmHouse内の通行判定
            harmony.Patch(
                original: AccessTools.Method(typeof(PathFindController), "isPositionImpassableForNPCSchedule"),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(IsPositionImpassableForNPCSchedulePrefix))
            );

            // AmbiguousMatchException が出たときのシグネチャ確認用
            // foreach (var method in typeof(PathFindController).GetMethods())
            // {
            //     ModMonitor.Log($"[DEBUG] Method: {method.Name} | Parameters: {string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))}", LogLevel.Debug);
            // }


        }



        public static bool IsPositionImpassableForNPCSchedulePrefix(GameLocation loc, int x, int y, ref bool __result)
        {
            if (loc.NameOrUniqueName == "FarmHouse")
            {
                Vector2 tile = new Vector2(x, y);

                // Buildingsレイヤーのタイルを取得
                Tile buildingTile = loc.Map.GetLayer("Buildings")?.Tiles[x, y];

                // 壁の可能性があるタイルをチェック
                if (buildingTile != null)
                {
                    ModMonitor.Log($"[DEBUG] IsPositionImpassableForNPCSchedule ({x}, {y}) TileIndex = {buildingTile.TileIndex}", LogLevel.Debug);
                    if (buildingTile.TileIndex != 0)
                    {
                    // Backレイヤーの Passable プロパティを確認
                    // if (loc.doesTileHaveProperty(x, y, "Passable", "Back") == null)
                    // {
                        ModMonitor.Log($"[DEBUG] IsPositionImpassableForNPCSchedule ({x}, {y}) is Impassable because tile", LogLevel.Debug);
                        __result = true; // Passable プロパティがない場合、壁とみなす
                        return false; // Harmonyの元メソッドをスキップ
                    // }
                    }
                }

                // 家具チェック
                foreach (Furniture furniture in loc.furniture)
                {
                    if (furniture.GetBoundingBox().Contains(x * 64, y * 64))
                    {
                        ModMonitor.Log($"[DEBUG] IsPositionImpassableForNPCSchedule ({x}, {y}) is Impassable because furniture", LogLevel.Debug);
                        __result = true;
                        return false;
                    }
                }

                // オブジェクトチェック（テーブルやベッドなど）
                if (loc.objects.ContainsKey(tile) && !loc.objects[tile].isPassable())
                {
                    ModMonitor.Log($"[DEBUG] IsPositionImpassableForNPCSchedule ({x}, {y}) is Impassable because objects", LogLevel.Debug);
                    __result = true;
                    return false;
                }
            }

            return true; // 通常の処理を続行
        }


        public static void CheckSchedulePostfix(NPC __instance, int timeOfDay)
        {
            if (__instance.Name != "Seiris")
            {
                return;
            }

            ModMonitor.Log($"[DEBUG] checkSchedule called for {__instance.Name} at {timeOfDay}", LogLevel.Debug);

            if (__instance.DirectionsToNewLocation?.route != null)
            {
                ModMonitor.Log($"[DEBUG] {__instance.Name} has a precomputed route with {__instance.DirectionsToNewLocation.route.Count} steps.", LogLevel.Debug);
                ModMonitor.Log($"[DEBUG] Route Point: {string.Join(" -> ", __instance.DirectionsToNewLocation.route)}", LogLevel.Debug);
            }
            else
            {
                ModMonitor.Log($"[DEBUG] No precomputed route found for {__instance.Name}", LogLevel.Debug);
            }
        }

        // public static void PathFindControllerPostfix(PathFindController __instance, Character c, GameLocation location, Point endPoint)
        // {
        //     ModMonitor.Log($"[DEBUG] PathFindController created for {c.Name} in {location.NameOrUniqueName} towards {endPoint}", LogLevel.Debug);

        //     if (__instance.pathToEndPoint == null || __instance.pathToEndPoint.Count == 0)
        //     {
        //         ModMonitor.Log($"[DEBUG] No path found!", LogLevel.Debug);
        //     }
        //     else
        //     {
        //         ModMonitor.Log($"[DEBUG] Path length: {__instance.pathToEndPoint.Count}", LogLevel.Debug);
        //     }
        // }

        public static void UpdatePostfix(NPC __instance, GameTime time, GameLocation location)
        {
            if (__instance.Name != "Seiris")
            {
                return;
            }

            if (__instance.controller == null)
            {
                // ModMonitor.Log($"[DEBUG] {__instance.Name} has NO PathFindController", LogLevel.Debug);
            }
            else
            {
                ModMonitor.Log($"[DEBUG] {__instance.Name} PathFindController active: Path length: {__instance.controller.pathToEndPoint?.Count ?? 0}", LogLevel.Debug);

                if (__instance.controller.pathToEndPoint != null)
                {
                    ModMonitor.Log($"[DEBUG] Path Point: {string.Join(" -> ", __instance.controller.pathToEndPoint)}", LogLevel.Debug);
                }
            }
        }



        public static void FindPathPostfix(ref Stack<Point> __result, Point startPoint, Point endPoint, GameLocation location, Character character, int limit)
        {
            ModMonitor.Log($"[DEBUG] findPath() called for {character.Name}, Start: {startPoint}, End: {endPoint}, Location: {location.NameOrUniqueName}", LogLevel.Debug);
            
            if (__result == null)
            {
                ModMonitor.Log($"[DEBUG] No path found!", LogLevel.Debug);
            }
            else
            {
                ModMonitor.Log($"[DEBUG] Path length: {__result.Count}", LogLevel.Debug);
            }
        }

        public static void PathfindToNextScheduleLocationPostfix(
            ref SchedulePathDescription __result, NPC __instance,
            string scheduleKey, string startingLocation, int startingX, int startingY, 
            string endingLocation, int endingX, int endingY, int finalFacingDirection, 
            string endBehavior, string endMessage)
        {
            if (__instance.Name != "Seiris")
            {
                return;
            }
            
            ModMonitor.Log($"[DEBUG] pathfindToNextScheduleLocation() executed for {__instance.Name}", LogLevel.Debug);
            ModMonitor.Log($"[DEBUG] Start: {startingLocation} ({startingX}, {startingY}) -> End: {endingLocation} ({endingX}, {endingY})", LogLevel.Debug);

            Stack<Point> path = new Stack<Point>();
            Point locationStartPoint = new Point(startingX, startingY);

            if (locationStartPoint == Point.Zero)
            {
                ModMonitor.Log($"[ERROR] NPC {__instance.Name} has an invalid start position (0,0) in {startingLocation}!", LogLevel.Error);
                return;
            }

            // 複数のマップをまたぐ場合の経路探索
            string[] locationsRoute = (!startingLocation.Equals(endingLocation, StringComparison.Ordinal))
                ? (string[])AccessTools.Method(typeof(NPC), "getLocationRoute")
                    .Invoke(__instance, new object[] { startingLocation, endingLocation })
                : null;

            if (locationsRoute != null)
            {
                for (int i = 0; i < locationsRoute.Length; i++)
                {
                    GameLocation currentLocation = Game1.RequireLocation(locationsRoute[i]);

                    if (i < locationsRoute.Length - 1)
                    {
                        Point target = currentLocation.getWarpPointTo(locationsRoute[i + 1]);
                        path = (Stack<Point>)AccessTools.Method(typeof(NPC), "addToStackForSchedule")
                            .Invoke(__instance, new object[] {
                                path, PathFindController.findPathForNPCSchedules(locationStartPoint, target, currentLocation, 30000)
                            });

                        locationStartPoint = currentLocation.getWarpPointTarget(target, __instance);
                    }
                    else
                    {
                        path = (Stack<Point>)AccessTools.Method(typeof(NPC), "addToStackForSchedule")
                            .Invoke(__instance, new object[] {
                                path, PathFindController.findPathForNPCSchedules(locationStartPoint, new Point(endingX, endingY), currentLocation, 30000)
                            });
                    }
                }
            }
            else if (startingLocation.Equals(endingLocation, StringComparison.Ordinal))
            {
                GameLocation location = Game1.RequireLocation(startingLocation);
                path = PathFindController.findPathForNPCSchedules(locationStartPoint, new Point(endingX, endingY), location, 30000);
            }

            // 経路のログを出力
            if (path != null)
            {
                ModMonitor.Log($"[DEBUG] Generated path for {__instance.Name}, length: {path.Count}", LogLevel.Debug);
                ModMonitor.Log($"[DEBUG] Route Point: {string.Join(" -> ", __result.route)}", LogLevel.Debug);
            }
            else
            {
                ModMonitor.Log($"[DEBUG] No valid path found!", LogLevel.Debug);
            }

            __result = new SchedulePathDescription(path, finalFacingDirection, endBehavior, endMessage, endingLocation, new Point(endingX, endingY));
        }




        public static void FindPathForNPCSchedulesPostfix(
            ref Stack<Point> __result, Point startPoint, Point endPoint, GameLocation location, int limit)
        {
            ModMonitor.Log($"[DEBUG] findPathForNPCSchedules() called in {location.NameOrUniqueName} from {startPoint} to {endPoint}", LogLevel.Debug);

            if (__result == null || __result.Count == 0)
            {
                ModMonitor.Log($"[DEBUG] No valid path found!", LogLevel.Debug);
            }
            else
            {
                // ModMonitor.Log($"[DEBUG] Path length: {__result.Count}", LogLevel.Debug);
                ModMonitor.Log($"[DEBUG] Path: {string.Join(" -> ", __result)}", LogLevel.Debug);
            }
        }



        // public static void FindPathForNPCSchedulesPostfix(
        //     PathFindController __instance, ref Stack<Point> __result, Point startPoint, Point endPoint, GameLocation location, int limit)
        // {
		// 	Character? character = AccessTools.Field(typeof(PathFindController), "character").GetValue(__instance) as Character;
        //     if (character.Name != "Seiris")
        //     {
        //         return;
        //     }
                
        //     ModMonitor.Log($"[DEBUG] findPathForNPCSchedules() called: {startPoint} -> {endPoint} in {location.NameOrUniqueName}", LogLevel.Debug);
            
        //     if (__result == null || __result.Count == 0)
        //     {
        //         ModMonitor.Log($"[DEBUG] No valid path found!", LogLevel.Debug);
        //     }
        //     else
        //     {
        //         ModMonitor.Log($"[DEBUG] Path length: {__result.Count}", LogLevel.Debug);
        //         foreach (var point in __result)
        //         {
        //             ModMonitor.Log($"[DEBUG] Path Point: {point.X}, {point.Y}", LogLevel.Debug);
        //         }
        //     }
        // }

        public static void PathFindControllerPostfix(PathFindController __instance, Stack<Point> pathToEndPoint, Character c, GameLocation l)
        {
            ModMonitor.Log($"[DEBUG] PathFindController (direct path) created for {c.Name} in {l.NameOrUniqueName} with path length: {pathToEndPoint?.Count ?? 0}", LogLevel.Debug);
            
            if (pathToEndPoint != null)
            {
                foreach (var point in pathToEndPoint)
                {
                    ModMonitor.Log($"[DEBUG] Path Point: {point.X}, {point.Y}", LogLevel.Debug);
                }
            }
        }



        /// FarmHouse で NPC が経路探索できるようにする
        private static bool IsTilePassable_Prefix(GameLocation __instance, Vector2 tileLocation, ref bool __result)
        {
            if (__instance.NameOrUniqueName == "FarmHouse")
            {
                // ModMonitor.Log($"[DEBUG] isTilePassable called in FarmHouse at {tileLocation}", LogLevel.Debug);

                __result = IsFarmHouseTilePassable(__instance, tileLocation);
                return false; // 元の処理をスキップ
            }
            return true; // 元の処理を実行
        }

        /// FarmHouse 内の移動可能タイルを判定
        private static bool IsFarmHouseTilePassable(GameLocation location, Vector2 tileLocation)
        {
            // 床のタイルが通行可能かチェック
            string type = location.doesTileHaveProperty((int)tileLocation.X, (int)tileLocation.Y, "Type", "Back");
            if (type == "Stone" || type == "Wood") // 床やカーペットがあれば移動可能
            {
                return true;
            }

            // 家具がある場合、移動不可
            foreach (Furniture furniture in location.furniture)
            {
                if (Utility.doesRectangleIntersectTile(furniture.GetBoundingBox(), (int)tileLocation.X, (int)tileLocation.Y))
                {
                    return false;
                }
            }

            return true;
        }




        // public static void DebugPathFindController(
        //     PathFindController __instance, Character c, GameLocation location, Point endPoint, int finalFacingDirection)
        // {
        //     ModMonitor.Log($"[DEBUG] PathFindController created - NPC: {c.Name} | Location: {location.NameOrUniqueName} | Destination: {endPoint}", LogLevel.Debug);

        //     if (__instance.pathToEndPoint == null)
        //     {
        //         ModMonitor.Log($"[DEBUG] {c.Name}'s PathFindController has no valid path!", LogLevel.Debug);
        //     }
        // }

        // public static void UpdatePostfix(NPC __instance)
        // {
        //     if (__instance.Name != "Seiris")
        //     {
        //         return;
        //     }
            
        //     if (__instance.controller == null)
        //     {
        //         ModMonitor.Log($"[DEBUG] {__instance.Name} has NO PathFindController", LogLevel.Debug);
        //     }
        //     else
        //     {
        //         ModMonitor.Log($"[DEBUG] {__instance.Name} has a PathFindController: {__instance.controller.GetType().FullName}", LogLevel.Debug);
        //     }
        // }

        // public static void CheckSchedulePrefix(NPC __instance)
        // {
        //     if (__instance.Schedule == null || __instance.Schedule.Count == 0)
        //     {
        //         ModMonitor.Log($"[DEBUG] {__instance.Name} has NO schedule!", LogLevel.Debug);
        //     }
        //     else
        //     {
        //         ModMonitor.Log($"[DEBUG] {__instance.Name} has a schedule: {string.Join(", ", __instance.Schedule.Keys)}", LogLevel.Debug);
        //     }
        // }

        // public static bool DebugFindPath(
        //     ref Stack<Point> __result, 
        //     Point startPoint, 
        //     Point endPoint, 
        //     PathFindController.isAtEnd endPointFunction, 
        //     GameLocation location, 
        //     Character character, 
        //     int limit)
        // {
        //     ModMonitor.Log($"[DEBUG] findPath() called - Start: {startPoint} | End: {endPoint} | Location: {location.NameOrUniqueName} | NPC: {character.Name}", LogLevel.Debug);

        //     PriorityQueue openList = new PriorityQueue();
        //     HashSet<int> closedList = new HashSet<int>();

        //     openList.Enqueue(new PathNode(startPoint.X, startPoint.Y, 0, null), Math.Abs(endPoint.X - startPoint.X) + Math.Abs(endPoint.Y - startPoint.Y));

        //     while (!openList.IsEmpty())
        //     {
        //         PathNode currentNode = openList.Dequeue();
        //         if (endPointFunction(currentNode, endPoint, location, character))
        //         {
        //             __result = PathFindController.reconstructPath(currentNode);
        //             return false;
        //         }

        //         closedList.Add(currentNode.id);

        //         for (int i = 0; i < 4; i++)
        //         {
        //             // Directions を findPath() の中で取得
        //             var directionsField = AccessTools.Field(typeof(PathFindController), "Directions");
        //             sbyte[,] Directions = (sbyte[,])directionsField.GetValue(null);
                    
        //             int nx = currentNode.x + Directions[i, 0];
        //             int ny = currentNode.y + Directions[i, 1];

        //             if (location.isCollidingPosition(new Rectangle(nx * 64 + 1, ny * 64 + 1, 62, 62), Game1.viewport, false, 0, glider: false, character, pathfinding: true))
        //             {
        //                 ModMonitor.Log($"[DEBUG] findPath() blocked - {nx}, {ny}", LogLevel.Debug);
        //                 continue;
        //             }
        //         }
        //     }

        //     ModMonitor.Log($"[DEBUG] findPath() returned NULL for {character.Name}", LogLevel.Debug);
        //     __result = new Stack<Point>();
        //     return false;
        // }

        // private static void PathFindControllerConstructorPrefix(Character c, GameLocation location, PathFindController.isAtEnd endFunction, int finalFacingDirection, PathFindController.endBehavior endBehaviorFunction, int limit, Point endPoint, bool clearMarriageDialogues)
        // {
        //     ModMonitor.Log($"[DEBUG] PathFindController Constructor called for {c?.Name}", LogLevel.Debug);
        // }

        /// <summary>
        /// NPC のスケジュールチェックが実行されたことを確認する
        /// </summary>
        // private static void CheckSchedulePrefix(NPC __instance, int timeOfDay)
        // {
        //     ModMonitor.Log($"[DEBUG] checkSchedule() called at {timeOfDay} for {__instance.Name}", LogLevel.Debug);
        // }


        // /// <summary>
        // /// NPCの経路探索時に障害物を考慮する
        // /// </summary>
        // private static bool FindPathPrefix(Point startPoint, Point endPoint, PathFindController.isAtEnd endPointFunction, GameLocation location, Character character, int limit)
        // {
        //     ModMonitor.Log($"[DEBUG] findPath() called: {startPoint} -> {endPoint}, Character: {character?.Name ?? "null"}", LogLevel.Debug);
            
        //     // ここで `character` が NPC なら経路探索をカスタマイズ可能
        //     if (character is NPC npc)
        //     {
        //         ModMonitor.Log($"[DEBUG] findPath() triggered for NPC: {npc.Name}", LogLevel.Debug);
        //     }

        //     return true; // オリジナルの `findPath()` を実行
        // }
        
/// <summary>
/// NPCが「主人公が通れないもの」を回避するようにする
/// </summary>
        // private static void IsCollidingPositionPostfix(GameLocation __instance, ref bool __result, Microsoft.Xna.Framework.Rectangle position, xTile.Dimensions.Rectangle viewport, bool isFarmer, int damagesFarmer, bool glider, Character character, bool pathfinding, bool projectile, bool ignoreCharacterRequirement, bool skipCollisionEffects)
        // {
        //     if (__result) // すでに衝突判定がtrueなら、さらにNPCが通れるかチェック
        //     {
        //         if (character is NPC npc)
        //         {
        //             ModMonitor.Log($"[DEBUG]", LogLevel.Debug);
        //             ModMonitor.Log($"[DEBUG] isCollidingPosition() called for NPC: {npc.Name} at {position}", LogLevel.Debug);

        //             // 🟢 NPCが通れるかどうかをチェック
        //             if (CanNPCAvoidObstacle(__instance, position, npc))
        //             {
        //                 __result = false; // NPCは通れる
        //                 ModMonitor.Log($"[DEBUG] NPC: {npc.Name} CAN PASS through {position}", LogLevel.Debug);
        //             }
        //             else
        //             {
        //                 ModMonitor.Log($"[DEBUG] NPC: {npc.Name} CANNOT PASS through {position}", LogLevel.Debug);
        //             }
        //         }
        //     }
        // }

        // /// <summary>
        // /// NPCが「主人公が通れないもの」を避けて通れるか判定
        // /// </summary>
        // private static bool CanNPCAvoidObstacle(GameLocation location, Microsoft.Xna.Framework.Rectangle position, NPC npc)
        // {
        //     // 全キャラクターのログが流れるとコンソールが埋まってしまうので、検証中は1キャラクターだけを対象にする
        //     if (npc.Name != "Seiris")
        //     {
        //         return true;
        //     }

        //     ModMonitor.Log($"[DEBUG] Checking obstacles for NPC: {npc.Name} at {position}", LogLevel.Debug);

        //     // 1. 建物(Building) を避ける
        //     foreach (Building building in location.buildings)
        //     {
        //         if (building.intersects(position))
        //         {
        //             ModMonitor.Log($"[DEBUG] {npc.Name} COLLIDED with Building at {building.tileX.Value}, {building.tileY.Value}", LogLevel.Debug);
        //             return false; // 通れない
        //         }
        //     }

        //     // 2. 大きな障害物 (木や岩)
        //     foreach (ResourceClump clump in location.resourceClumps)
        //     {
        //         if (clump.getBoundingBox().Intersects(position))
        //         {
        //             ModMonitor.Log($"[DEBUG] {npc.Name} COLLIDED with ResourceClump at {clump.Tile.X}, {clump.Tile.Y}", LogLevel.Debug);
        //             return false; // 通れない
        //         }
        //     }

        //     // 3. 家具 (チェストなど)
        //     foreach (Furniture furniture in location.furniture)
        //     {
        //         if (furniture.IntersectsForCollision(position))
        //         {
        //             ModMonitor.Log($"[DEBUG] {npc.Name} COLLIDED with Furniture at {furniture.tileLocation.X}, {furniture.tileLocation.Y}", LogLevel.Debug);
        //             return false; // 通れない
        //         }
        //     }

        //     // 4. プレイヤーと同じ衝突判定 (タイル)
        //     Vector2 tilePosition = new Vector2(position.X / 64, position.Y / 64);
        //     if (location.IsTileOccupiedBy(tilePosition))
        //     {
        //         ModMonitor.Log($"[DEBUG] {npc.Name} COLLIDED with Tile at {tilePosition}", LogLevel.Debug);
        //         return false; // 何かに占有されている
        //     }

        //     ModMonitor.Log($"[DEBUG] {npc.Name} can pass through {position}", LogLevel.Debug);
        //     return true; // ここまでチェックして通れるならOK
        // }

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

            // ModMonitor.Log($"[DEBUG] Set Action: {location.NameOrUniqueName} {outdoorPoint} -> {buildingName} {indoorPoint}", LogLevel.Debug);
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
                TileArea = new Microsoft.Xna.Framework.Rectangle(outdoorPoint.X - building.tileX.Value, outdoorPoint.Y - building.tileY.Value, 1, 1),
                Name = "Action",
                Value = $"Warp {indoorPoint.X} {indoorPoint.Y} {buildingName}"
            });
            // ModMonitor.Log($"[DEBUG] Added Warp Action to BuildingData: {outdoorPoint} -> {buildingName} {indoorPoint}", LogLevel.Debug);
        }

    }
}