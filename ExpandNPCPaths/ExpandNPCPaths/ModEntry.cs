#pragma warning disable CS0618 // 型またはメンバーが旧型式です
#pragma warning disable CS8600 // Null リテラルまたは Null の可能性がある値を Null 非許容型に変換しています。
#pragma warning disable CS8602 // null 参照の可能性があるものの逆参照です。
#pragma warning disable CS8601

using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Pathfinding;
using StardewValley.Buildings;
using Microsoft.Xna.Framework;
using xTile.Tiles;
using xTile.Layers;
using StardewValley.GameData.Buildings;
using StardewValley.Locations;
using StardewValley.Objects;

// TODO:
// attemptCountってもしかしてローカル変数でもよかったりする？
// 結婚後の挙動

namespace ExpandNPCPaths
{

    // public class ModConfig
    // {
    //     public bool EnableInitialBeds { get; set; } = false;
    //     public List<InitialBedEntry> InitialBeds { get; set; } = new List<InitialBedEntry>();
    // }

    public class ModEntry : Mod
    {
        public static IMonitor ModMonitor = null!;
        // private static ModConfig Config;

        private static int attemptCount = 0; // 経路探索の試行回数
        private static bool useVanillaCollisionLogic = false;

        public override void Entry(IModHelper helper)
        {
            ModMonitor = Monitor;

            // Harmony.DEBUG = true;
            var harmony = new Harmony(ModManifest.UniqueID);

            // config.jsonを読み込む
            // Config = helper.ReadConfig<ModConfig>();

            // *** NPCの行動範囲の拡張 ***
            // NPCBarrier属性を削除、Farmの建物のドア定義をセット(OnValueAddedフックを予約)
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

            // *** NPCの経路探索の変更 ***
            // 障害物を避けて歩くよう経路探索を変更(歩き始める前の経路探索の変更)
            harmony.Patch(
                original: AccessTools.Method(typeof(PathFindController), "isPositionImpassableForNPCSchedule"),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(IsNPCPathObstructed))
            );

            // IsNPCPathObstructed()で経路が見つからなかった場合はバニラの経路探索ロジックを使用するよう振り分ける
            harmony.Patch(
                original: AccessTools.Method(typeof(PathFindController), "findPathForNPCSchedules", new Type[] { typeof(Point), typeof(Point), typeof(GameLocation), typeof(int), typeof(Character) }),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(DecideNPCPathfindingMethod))
            );

            // NPCが障害物にぶつかった際に経路を再探索(歩き始めた後に障害物にぶつかった時に経路探索を再実行)
            harmony.Patch(
                original: AccessTools.Method(typeof(NPC), "MovePosition"),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(HandleNPCObstacle))
            );

            // *** 配偶者ハードコード対応 ***
            // 配偶者がダブルベッドの位置から開始するよう変更
            // harmony.Patch(
            //     original: AccessTools.Method(typeof(PathFindController), "handleWarps"),
            //     postfix: new HarmonyMethod(typeof(ModEntry), nameof(HandleWarpsPostfix))
            // );

            // harmony.Patch(
            //     original: AccessTools.Method(typeof(FarmHouse), "getEntryLocation"),
            //     postfix: new HarmonyMethod(typeof(ModEntry), nameof(getEntryLocationPostfix))
            // );

            harmony.Patch(
                original: AccessTools.Method(typeof(NPC), "parseMasterScheduleImpl"),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(ParseMasterScheduleImplPostfix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(NPC), "pathfindToNextScheduleLocation"),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(PathfindToNextScheduleLocationPostfix))
            );

            // for debug
            harmony.Patch(
                original: AccessTools.Method(typeof(NPC), "checkSchedule"),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(CheckSchedulePrefix))
            );
            
        }
        

        public static void CheckSchedulePrefix(NPC __instance)
        {
            if (__instance.Name != "Seiris")
            {
                return;
            }
            // ModMonitor.Log($"[DEBUG] {__instance.Name} checking schedule at {Game1.timeOfDay}. Available keys: {string.Join(", ", __instance.Schedule.Keys)}", LogLevel.Debug);

            if (__instance.Schedule == null || __instance.Schedule.Count == 0)
            {
                ModMonitor.Log($"[DEBUG] {__instance.Name} has NO schedule at {Game1.timeOfDay}", LogLevel.Debug);
            }
            else
            {
                ModMonitor.Log($"[DEBUG] {__instance.Name} has a schedule: {string.Join(", ", __instance.Schedule.Keys)}", LogLevel.Debug);
            }
        }

        // parseMasterScheduleImpl の `"bed"` の処理を修正し、最初のエントリーのみに `previousPosition` を適用
        private static void ParseMasterScheduleImplPostfix(NPC __instance, string scheduleKey, string rawData, List<string> visited, ref Dictionary<int, SchedulePathDescription> __result)
        {
            if (__instance.Name != "Seiris")
            {
                return;
            }
            ModMonitor.Log($"[DEBUG] ParseMasterScheduleImplPostfix() start", LogLevel.Debug);
            ModMonitor.Log($"[DEBUG] {__instance.Name} ScheduleKey: {scheduleKey}", LogLevel.Debug);

            if (__instance.controller == null)
            {
                ModMonitor.Log($"[DEBUG] {__instance.Name} controller is NULL after schedule update!", LogLevel.Warn);
            }

            if (!__instance.isMarried() || __result == null || __result.Count == 0)
            {
                return;
            }

            FarmHouse farmHouse = Utility.getHomeOfFarmer(__instance.getSpouse());
            if (farmHouse == null)
            {
                return;
            }

            Point? bedSpot = GetSpouseBedSpot(farmHouse);
            if (!bedSpot.HasValue)
            {
                return;
            }

            // その日のスケジュールの最初のエントリーを取得（最も早い時刻のもの）
            int firstKey = __result.Keys.Min();

            ModMonitor.Log($"[DEBUG] __result[firstKey] key: {firstKey}, time: {__result[firstKey].time}, targetLocation: {__result[firstKey].targetLocationName} {__result[firstKey].targetTile}, route: {string.Join(" -> ", __result[firstKey].route)}", LogLevel.Debug);

            Point firstSchedulePoint = __result[firstKey].route.Peek();

            // 最初のスケジュールだけを "bed" に修正
            if (firstSchedulePoint.X == 10 && firstSchedulePoint.Y == 23)
            {
                ModMonitor.Log($"[DEBUG] {__instance.Name} のスケジュール開始位置を BusStop ではなく Bed {bedSpot.Value} に変更", LogLevel.Debug);

                __result[firstKey] = __instance.pathfindToNextScheduleLocation(
                    scheduleKey,
                    farmHouse.NameOrUniqueName,
                    // bedSpot.Value.X,
                    // bedSpot.Value.Y,
                    farmHouse.getKitchenStandingSpot().X,
                    farmHouse.getKitchenStandingSpot().Y,
                    __result[firstKey].targetLocationName,
                    __result[firstKey].targetTile.X,
                    __result[firstKey].targetTile.Y,
                    __result[firstKey].facingDirection,
                    __result[firstKey].endOfRouteBehavior,
                    __result[firstKey].endOfRouteMessage
                );

            }

            foreach (var entry in __result)
            {
                entry.Value.time = entry.Key;
                ModMonitor.Log($"[DEBUG] {__instance.Name} Schedule Time: {entry.Key}({entry.Value.time}), Target: {entry.Value.targetLocationName} {entry.Value.targetTile}, Route: {string.Join(" -> ", entry.Value.route.ToArray())}", LogLevel.Debug);
            }

            bool success = __instance.TryLoadSchedule(scheduleKey, __result);
            
            ModMonitor.Log($"[DEBUG] {__instance.Name} のスケジュール更新: {success}, {__instance.Schedule.Count} 件のエントリー", LogLevel.Debug);
        }

        // 配偶者のダブルベッドの位置を取得
        private static Point? GetSpouseBedSpot(FarmHouse farmHouse)
        {
            foreach (Furniture furniture in farmHouse.furniture)
            {
                if (furniture is BedFurniture bed && bed.bedType == BedFurniture.BedType.Double)
                {
                    return bed.GetBedSpot();
                }
            }
            return null; // ダブルベッドが見つからなかった場合
        }

        // private static void HandleWarpsPostfix(PathFindController __instance, Rectangle position)
        // {
        //     Character? character = AccessTools.Field(typeof(PathFindController), "character").GetValue(__instance) as Character;
        //     if (character.Name != "Seiris")
        //     {
        //         return;
        //     }
        //                     ModMonitor.Log($"[DEBUG] HandleWarpsPostfix() start", LogLevel.Debug);

        //     if (character is NPC spouse && spouse.isMarried() && spouse.followSchedule)
        //     {
        //                     ModMonitor.Log($"[DEBUG] character is NPC spouse && spouse.isMarried() && spouse.followSchedule", LogLevel.Debug);

        //         GameLocation gameLocation = __instance.location;

        //         if (gameLocation is FarmHouse farmHouse)
        //         {
        //                     ModMonitor.Log($"[DEBUG] HandleWarpsPostfix: gameLocation is FarmHouse", LogLevel.Debug);

        //             // ダブルベッドの位置を取得
        //             foreach (Furniture furniture in farmHouse.furniture)
        //             {
        //                 if (furniture is BedFurniture bed && bed.bedType == BedFurniture.BedType.Double)
        //                 {
        //                     // BusStop (10,23) にワープする処理を上書きし、ダブルベッド の座標に変更
        //                     Point bedPoint = bed.GetBedSpot();
		//                     Warp w = __instance.location.isCollidingWithWarpOrDoor(position, character);
        //                     w = new Warp(w.X, w.Y, gameLocation.NameOrUniqueName, bedPoint.X, bedPoint.Y, flipFarmer: false);
        //                     ModMonitor.Log($"[DEBUG] {spouse.Name} のワープ先を {gameLocation.NameOrUniqueName} {bedPoint} に変更しました。", LogLevel.Debug);
        //                     return;
        //                 }
        //             }
        //                     ModMonitor.Log($"[DEBUG] HandleWarpsPostfix: ダブルベッドが見つかりませんでした", LogLevel.Debug);

        //             return; // ダブルベッドが見つからなかった場合
        //         }
        //     }
        // }

        public static void PathfindToNextScheduleLocationPostfix(NPC __instance, string scheduleKey, string startingLocation, int startingX, int startingY, string endingLocation, int endingX, int endingY, int finalFacingDirection, string endBehavior, string endMessage, ref SchedulePathDescription __result)
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
                // ModMonitor.Log($"[DEBUG] Generated path for {__instance.Name}, length: {path.Count}", LogLevel.Debug);
                ModMonitor.Log($"[DEBUG] pathfindToNextScheduleLocation() pathToEndpoint: {string.Join(" -> ", __result.route)}", LogLevel.Debug);
            }
            else
            {
                ModMonitor.Log($"[DEBUG] No valid path found!", LogLevel.Debug);
            }

            __result = new SchedulePathDescription(path, finalFacingDirection, endBehavior, endMessage, endingLocation, new Point(endingX, endingY));
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

        private static void DecideNPCPathfindingMethod(Point startPoint, Point endPoint, GameLocation location, int limit, Character npc, ref Stack<Point> __result)
        {
            // すでに経路が見つかっている場合はそのまま
            if (__result != null && __result.Count > 0)
            {
                if (__result != null && npc != null && npc.Name == "Seiris")
                {
                    ModMonitor.Log($"[DEBUG] findPathForNPCSchedules {location.NameOrUniqueName} {startPoint} -> {endPoint}, __result: {string.Join(" -> ", __result.Select(p => $"({p.X},{p.Y})"))}", LogLevel.Debug);
                }
                attemptCount = 0; // 成功したので試行回数リセット
                return;
            }
            
            // すでにバニラのロジックを適用した場合は、無限ループ防止のために抜ける
            if (attemptCount >= 1)
            {
                // ModMonitor.Log($"[WARN] 経路探索失敗（Mod & バニラどちらも到達不能）: NPCは目的地に到達できない", LogLevel.Warn);
                if (__result != null && npc != null && npc.Name == "Seiris")
                {
                    ModMonitor.Log($"[DEBUG] findPathForNPCSchedules {location.NameOrUniqueName} {startPoint} -> {endPoint}, __result: {string.Join(" -> ", __result.Select(p => $"({p.X},{p.Y})"))}", LogLevel.Debug);
                }
                return;
            }

            // 試行回数を加算（1回目の失敗）
            attemptCount++;

            // ModMonitor.Log($"[DEBUG] 初回経路探索失敗: バニラのisPositionImpassableForNPCScheduleを適用して再探索", LogLevel.Debug);

            // バニラのロジックを適用して再試行
            useVanillaCollisionLogic = true;
            __result = PathFindController.findPathForNPCSchedules(startPoint, endPoint, location, limit);
            useVanillaCollisionLogic = false;
            
            if (__result == null || __result.Count == 0)
            {
                // ModMonitor.Log($"[DEBUG] バニラのロジックでも経路が見つからず: NPCは目的地に到達できない", LogLevel.Debug);
            }
            else
            {
                // ModMonitor.Log($"[DEBUG] バニラのロジックで経路発見: NPCは目的地に向かう", LogLevel.Debug);
            }

            if (__result != null && npc != null && npc.Name == "Seiris")
            {
                ModMonitor.Log($"[DEBUG] findPathForNPCSchedules {location.NameOrUniqueName} {startPoint} -> {endPoint}, __result: {string.Join(" -> ", __result.Select(p => $"({p.X},{p.Y})"))}", LogLevel.Debug);
            }

            // 再試行が終わったらリセット（次回の探索に影響しないように）
            attemptCount = 0;
        }

        private static bool IsNPCPathObstructed(GameLocation loc, int x, int y, ref bool __result)
        {
            // 障害物を避けて経路探索
            if (useVanillaCollisionLogic)
            {
                // バニラのロジックを適用
                return true;
            }

            // Mod のロジックで判定
            // isCollidingPosition()にcharacterを渡す必要があるが、PathFindControllerのcharacterを取得できないためNPCの一般値としてAbigailを使用
            NPC character = Game1.getCharacterFromName("Abigail");
            // Rectangleの大きさは、PathFindController.findPath()内のisCollidingPosition()での判定と同様に、1マスより僅かに小さくしておく
            bool isBlocked = loc.isCollidingPosition(new Rectangle(x * 64 + 1, y * 64 + 1, 62, 62), Game1.viewport, false, 0, false, character, true);

            if (isBlocked)
            {
                __result = true;
                return false; // 経路探索を止める
            }

            return true; // 経路探索を続行
        }

        private static void HandleNPCObstacle(NPC __instance, GameTime time, xTile.Dimensions.Rectangle viewport, GameLocation currentLocation)
        {
            if (__instance.Name != "Seiris")
            {
                return;
            }

            // 現在のマップを取得
            if (currentLocation == null)
            {
                // nullなことないのでは？
                ModMonitor.Log($"[DEBUG] HandleNPCObstacle location == null", LogLevel.Debug);
                return;
            }

            // 現在の位置で衝突をチェック
            Rectangle bbox = __instance.GetBoundingBox();
            bool isBlocked = currentLocation.isCollidingPosition(bbox, Game1.viewport, false, 0, false, __instance, true);

            if (!isBlocked)
            {
                return; // 進行可能なら何もしない
            }

            // ModMonitor.Log($"[DEBUG] {__instance.Name} is blocked at {__instance.TilePoint}. Repathing...", LogLevel.Debug);

            // 現在時刻
            int currentTime = Game1.timeOfDay;

            // NPCの次の移動先
            Point? npcNextTarget = (__instance.controller?.pathToEndPoint != null && __instance.controller.pathToEndPoint.Count > 0)
                ? __instance.controller.pathToEndPoint.Last()
                : (Point?)null;

            if (npcNextTarget == null)
            {
                ModMonitor.Log($"[ERROR] {__instance.Name}: pathToEndPoint is null, cannot determine next target.", LogLevel.Error);
                return;
            }

            // `Schedule` 辞書から現在時刻に最も近い「実行中のスケジュール」を取得
            SchedulePathDescription scheduleDescription = null;

            if (__instance.Schedule != null && __instance.Schedule.Count > 0)
            {
                foreach (int timeKey in __instance.Schedule.Keys
                    .Where(t => t <= currentTime) // 現在時刻より前のスケジュールのみ
                    .OrderByDescending(t => t)) // 過去に向かって新しい順に並べる
                {
                    if (__instance.Schedule.TryGetValue(timeKey, out var candidateSchedule))
                    {
                        // ModMonitor.Log($"[DEBUG] candidateSchedule: {candidateSchedule.targetTile}, npcNextTarget: {npcNextTarget.Value}", LogLevel.Debug);

                        // 目的地のタイルが合致するかチェック
                        if (candidateSchedule.targetTile == npcNextTarget.Value)
                        {
                            scheduleDescription = candidateSchedule;
                            // ModMonitor.Log($"[DEBUG] Found matching schedule for {__instance.Name} at time {timeKey}.", LogLevel.Debug);
                            break;
                        }
                    }
                }
            }

            if (scheduleDescription != null)
            {
                // SchedulePathDescription から目的地の情報を取得
                string scheduleKey = __instance.ScheduleKey ?? "default";
                string startingLocation = __instance.currentLocation.NameOrUniqueName;
                // Peekにするとめり込んでから歩き始める可能性がある…
                int startingX = __instance.controller.pathToEndPoint.Peek().X;
                int startingY = __instance.controller.pathToEndPoint.Peek().Y;
                // int startingX = __instance.TilePoint.X;
                // int startingY = __instance.TilePoint.Y;
                string endingLocation = scheduleDescription.targetLocationName;
                int endingX = scheduleDescription.targetTile.X;
                int endingY = scheduleDescription.targetTile.Y;
                string endBehavior = scheduleDescription.endOfRouteBehavior;
                string endMessage = scheduleDescription.endOfRouteMessage;
                int finalFacingDirection = scheduleDescription.facingDirection;

                // ModMonitor.Log($"[DEBUG] Repathing {currentLocation.NameOrUniqueName} {__instance.TilePoint} -> {endingLocation} {scheduleDescription.targetTile}", LogLevel.Debug);

                // 経路探索を再実行
                SchedulePathDescription newPath = __instance.pathfindToNextScheduleLocation(
                    scheduleKey, startingLocation, startingX, startingY,
                    endingLocation, endingX, endingY,
                    finalFacingDirection, endBehavior, endMessage
                );

                // ModMonitor.Log($"[DEBUG] Old path: {string.Join(" -> ", __instance.controller.pathToEndPoint)}", LogLevel.Debug);
                // ModMonitor.Log($"[DEBUG] New path: {string.Join(" -> ", newPath.route)}", LogLevel.Debug);
                
                if (newPath.route != null && newPath.route.Count > 0)
                {
                    // 経路が見つかった場合は適用
                    ModMonitor.Log($"[DEBUG] Applying new path for {__instance.Name}. New path: {string.Join(" -> ", newPath.route)}", LogLevel.Debug);
                    __instance.controller = new PathFindController(newPath.route, __instance, currentLocation);
                }
                else
                {
                    // 経路が見つからなかった場合、最終手段として立ち止まる
                    // 立ち止まらなくてもいいかも　バニラの処理になげるべきかな？
                    ModMonitor.Log($"[DEBUG] No valid path found for {__instance.Name}. Stopping movement.", LogLevel.Warn);
                    __instance.Halt();
                }
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