#pragma warning disable AvoidNetField // Avoid Netcode types when possible
#pragma warning disable CS0162 // 到達できないコードが検出されました
#pragma warning disable CS8600 // Null リテラルまたは Null の可能性がある値を Null 非許容型に変換しています。
#pragma warning disable CS8602 // null 参照の可能性があるものの逆参照です。
#pragma warning disable CS8601 // Null 参照代入の可能性があります。
#pragma warning disable CS0618 // 型またはメンバーが旧型式です
#pragma warning disable CS0105 // 使用中のディレクティブは、以前この名前空間に使用されています
#pragma warning disable CS8605 // null の可能性がある値をボックス化解除しています。
#pragma warning disable CS8625 // null リテラルを null 非許容参照型に変換できません。

using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Pathfinding;
using StardewValley.Buildings;
using Microsoft.Xna.Framework;
using xTile.Tiles;
using xTile.Layers;
using StardewValley.GameData.Buildings;
using StardewValley.TerrainFeatures;
using StardewValley.Objects;
using System.Reflection;
using StardewValley.Locations;
using System;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Pathfinding;
using StardewValley.TerrainFeatures;
using StardewValley.Objects;
using StardewValley.Monsters;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Network;
using xTile.Tiles;
using xTile.Layers;
using xTile.Dimensions;
using xTile.ObjectModel;
using xTile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Netcode;
using Netcode.Validation;
using StardewValley.Audio;
using StardewValley.BellsAndWhistles;
using StardewValley.Buffs;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Constants;
using StardewValley.Enchantments;
using StardewValley.Extensions;
using StardewValley.GameData;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.Characters;
using StardewValley.GameData.GarbageCans;
using StardewValley.GameData.LocationContexts;
using StardewValley.GameData.Locations;
using StardewValley.GameData.Minecarts;
using StardewValley.GameData.Movies;
using StardewValley.GameData.WildTrees;
using StardewValley.Internal;
using StardewValley.Inventories;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Minigames;
using StardewValley.Mods;
using StardewValley.Monsters;
using StardewValley.Network;
using StardewValley.Network.NetEvents;
using StardewValley.Objects;
using StardewValley.Pathfinding;
using StardewValley.Projectiles;
using StardewValley.SpecialOrders;
using StardewValley.SpecialOrders.Objectives;
using StardewValley.TerrainFeatures;
using StardewValley.TokenizableStrings;
using StardewValley.Tools;
using StardewValley.Util;
using xTile;
using xTile.Dimensions;
using xTile.Layers;
using xTile.ObjectModel;
using xTile.Tiles;

// TODO:
// FarmHouse内で経路探索できるように
// 障害物を避けて経路探索できるようにしたい

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
            
            harmony.Patch(
                original: AccessTools.Method(typeof(GameLocation), "isCollidingPosition", new[] { typeof(Microsoft.Xna.Framework.Rectangle), typeof(xTile.Dimensions.Rectangle), typeof(bool), typeof(int), typeof(bool), typeof(Character), typeof(bool), typeof(bool), typeof(bool), typeof(bool) }),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(Postfix_isCollidingPosition))
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
            // foreach (var method in typeof(GameLocation).GetMethods())
            // {
            //     ModMonitor.Log($"[DEBUG] Method: {method.Name} | Parameters: {string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))}", LogLevel.Debug);
            // }


        }




        public static void Postfix_isCollidingPosition(
            GameLocation __instance,
            Microsoft.Xna.Framework.Rectangle position,
            Viewport viewport,
            bool isFarmer,
            int damagesFarmer,
            bool glider,
            Character character,
            bool pathfinding,
            bool projectile,
            bool ignoreCharacterRequirement,
            bool skipCollisionEffects,
            ref bool __result
        )
        {
            // ログ出力（デバッグ用）
            try
            {
                ModMonitor.Log($"[DEBUG] isCollidingPosition() called: x={position.Center.X / 64}, y={position.Center.Y / 64}", LogLevel.Debug);

                // isCollidingPosition のロジックをそのまま再現
                bool is_event_up = Game1.eventUp;
                if (is_event_up && Game1.CurrentEvent != null && !Game1.CurrentEvent.ignoreObjectCollisions)
                {
                    is_event_up = false;
                }

                __instance.updateMap();

                if (__instance.IsOutOfBounds(position))
                {
                    if (isFarmer && Game1.eventUp)
                    {
                        bool? flag = __instance.currentEvent?.isFestival;
                        if (flag.HasValue && flag.GetValueOrDefault() && __instance.currentEvent.checkForCollision(position, (character as Farmer) ?? Game1.player))
                        {
                            ModMonitor.Log($"[DEBUG] 衝突: イベントのフェスティバルエリア", LogLevel.Debug);
                            return;
                        }
                    }
                    ModMonitor.Log($"[DEBUG] 衝突なし: 領域外", LogLevel.Debug);
                    return;
                }

                if (character == null && !ignoreCharacterRequirement)
                {
                    ModMonitor.Log($"[DEBUG] 衝突: character が null で ignoreCharacterRequirement=false", LogLevel.Debug);
                    return;
                }

                // 位置の計算
                Vector2 nextTopRight = new Vector2(position.Right / 64, position.Top / 64);
                Vector2 nextTopLeft = new Vector2(position.Left / 64, position.Top / 64);
                Vector2 nextBottomRight = new Vector2(position.Right / 64, position.Bottom / 64);
                Vector2 nextBottomLeft = new Vector2(position.Left / 64, position.Bottom / 64);
                bool nextLargerThanTile = position.Width > 64;
                Vector2 nextBottomMid = new Vector2(position.Center.X / 64, position.Bottom / 64);
                Vector2 nextTopMid = new Vector2(position.Center.X / 64, position.Top / 64);
                BoundingBoxGroup passableTiles = null;
                Farmer farmer = character as Farmer;
                Microsoft.Xna.Framework.Rectangle? currentBounds;
                if (farmer != null)
                {
                    isFarmer = true;
                    currentBounds = farmer.GetBoundingBox();
                    passableTiles = farmer.TemporaryPassableTiles;
                }
                else
                {
                    farmer = null;
                    isFarmer = false;
                    currentBounds = null;
                }
                // 現在のキャラクターの位置を取得
                Vector2? currentTopRight = null;
                Vector2? currentTopLeft = null;
                Vector2? currentBottomRight = null;
                Vector2? currentBottomLeft = null;
                Vector2? currentBottomMid = null;
                Vector2? currentTopMid = null;
                if (currentBounds.HasValue)
                {
                    currentTopRight = new Vector2((currentBounds.Value.Right - 1) / 64, currentBounds.Value.Top / 64);
                    currentTopLeft = new Vector2(currentBounds.Value.Left / 64, currentBounds.Value.Top / 64);
                    currentBottomRight = new Vector2((currentBounds.Value.Right - 1) / 64, (currentBounds.Value.Bottom - 1) / 64);
                    currentBottomLeft = new Vector2(currentBounds.Value.Left / 64, (currentBounds.Value.Bottom - 1) / 64);
                    currentBottomMid = new Vector2(currentBounds.Value.Center.X / 64, (currentBounds.Value.Bottom - 1) / 64);
                    currentTopMid = new Vector2(currentBounds.Value.Center.X / 64, currentBounds.Value.Top / 64);
                }

                // 橋の上にいるかどうかチェック
                if (farmer?.bridge != null && farmer.onBridge.Value && position.Right >= farmer.bridge.bridgeBounds.X && position.Left <= farmer.bridge.bridgeBounds.Right)
                {
                    MethodInfo testCornersWorldMethod = AccessTools.Method(typeof(GameLocation), "_TestCornersWorld");
                    if ((bool)testCornersWorldMethod.Invoke(__instance, new object[]{position.Top, position.Bottom, position.Left, position.Right, (int x, int y) => (y > farmer.bridge.bridgeBounds.Bottom || y < farmer.bridge.bridgeBounds.Top) ? true : false}))
                    {
                        ModMonitor.Log($"[DEBUG] 衝突: プレイヤーが橋の範囲外", LogLevel.Debug);
                        return;
                    }
                    ModMonitor.Log($"[DEBUG] 衝突なし: 橋の範囲内", LogLevel.Debug);
                    return;
                }
                if (!glider)
                {
                    if (character != null && __instance.animals.FieldDict.Count > 0 && !(character is FarmAnimal))
                    {
                        foreach (FarmAnimal animal in __instance.animals.Values)
                        {
                            Microsoft.Xna.Framework.Rectangle animalBounds = animal.GetBoundingBox();
                            if (position.Intersects(animalBounds) && (!currentBounds.HasValue || !currentBounds.Value.Intersects(animalBounds)) && (passableTiles == null || !passableTiles.Intersects(position)))
                            {
                                if (!skipCollisionEffects)
                                {
                                    animal.farmerPushing();
                                }
                                ModMonitor.Log($"[DEBUG] 衝突: FarmAnimal at {animal.TilePoint}", LogLevel.Debug);
                                return;
                            }
                        }
                    }
                }
                if (__instance.buildings.Count > 0)
                {
                    foreach (Building b in __instance.buildings)
                    {
                        if (!b.intersects(position) || (currentBounds.HasValue && b.intersects(currentBounds.Value)))
                        {
                            continue;
                        }

                        if (!(character is FarmAnimal) && !(character is JunimoHarvester))
                        {
                            if (!(character is NPC))
                            {
                                ModMonitor.Log($"[DEBUG] 衝突: 建物 {b.buildingType} at {b.tileX},{b.tileY}", LogLevel.Debug);
                                return;
                            }
                            Microsoft.Xna.Framework.Rectangle door = b.getRectForHumanDoor();
                            door.Height += 64;
                            if (!door.Contains(position))
                            {
                                ModMonitor.Log($"[DEBUG] 衝突: NPCが建物のドア外に衝突", LogLevel.Debug);
                                return;
                            }
                        }
                        else
                        {
						    Microsoft.Xna.Framework.Rectangle door = b.getRectForAnimalDoor();
                            door.Height += 64;
                            if (!door.Contains(position))
                            {
                                ModMonitor.Log($"[DEBUG] 衝突: 動物が建物のドア外に衝突", LogLevel.Debug);
                                return;
                            }
						    if (character is FarmAnimal animal && !animal.CanLiveIn(b))
                            {
                                ModMonitor.Log($"[DEBUG] 衝突: 動物 {animal.Name} はこの建物に入れない", LogLevel.Debug);
                                return;
                            }
                        }
                    }
                }

                // 大きなオブジェクト（ResourceClump）との衝突判定
                if (__instance.resourceClumps.Count > 0)
                {
                    foreach (ResourceClump resourceClump in __instance.resourceClumps)
                    {
                        Microsoft.Xna.Framework.Rectangle bounds = resourceClump.getBoundingBox();

                        if (bounds.Intersects(position) && (!currentBounds.HasValue || !bounds.Intersects(currentBounds.Value)))
                        {
                            ModMonitor.Log($"[DEBUG] 衝突: ResourceClump at {bounds.Location}", LogLevel.Debug);
                            return;
                        }
                    }
                }
                if (!is_event_up && __instance.furniture.Count > 0)
                {
                    foreach (Furniture f in __instance.furniture)
                    {
                        if (f.furniture_type.Value != 12 && f.IntersectsForCollision(position) && (!currentBounds.HasValue || !f.IntersectsForCollision(currentBounds.Value)))
                        {
                            ModMonitor.Log($"[DEBUG] 衝突: Furniture {f.Name} at {f.TileLocation}", LogLevel.Debug);
                            return;
                        }
                    }
                }
                
                // 地形との衝突判定
                NetCollection<LargeTerrainFeature> netCollection = __instance.largeTerrainFeatures;
                if (netCollection != null && netCollection.Count > 0)
                {
                    foreach (LargeTerrainFeature largeTerrainFeature in __instance.largeTerrainFeatures)
                    {
                        Microsoft.Xna.Framework.Rectangle bounds = largeTerrainFeature.getBoundingBox();
                        if (bounds.Intersects(position) && (!currentBounds.HasValue || !bounds.Intersects(currentBounds.Value)))
                        {
                            ModMonitor.Log($"[DEBUG] 衝突: LargeTerrainFeature at {bounds.Location}", LogLevel.Debug);
                            return;
                        }
                    }
                }
                MethodInfo testCornersTilesMethod = AccessTools.Method(typeof(GameLocation), "_TestCornersTiles");
                if (!glider)
                {
                    if ((!is_event_up || (character != null && !isFarmer && (!pathfinding || !character.willDestroyObjectsUnderfoot))) && (bool)testCornersTilesMethod.Invoke(__instance, new object[]{nextTopRight, nextTopLeft, nextBottomRight, nextBottomLeft, nextTopMid, nextBottomMid, currentTopRight, currentTopLeft, currentBottomRight, currentBottomLeft, currentTopMid, currentBottomMid, nextLargerThanTile, delegate(Vector2 corner)
                    {
                        if (__instance.objects.TryGetValue(corner, out var value3) && value3 != null)
                        {
                            if (value3.isPassable())
                            {
                                return false;
                            }
                            Microsoft.Xna.Framework.Rectangle boundingBox = value3.GetBoundingBox();
                            if (boundingBox.Intersects(position) && (character == null || character.collideWith(value3)))
                            {
                                if (character is FarmAnimal && value3.isAnimalProduct())
                                {
                                    return false;
                                }
                                if (passableTiles != null && passableTiles.Intersects(boundingBox))
                                {
                                    return false;
                                }
                                ModMonitor.Log($"[DEBUG] 衝突: Object {value3.Name} at {corner}", LogLevel.Debug);
                                return true;
                            }
                        }
                        return false;
                    }}))
                    {
                        return;
                    }
                    testCornersTilesMethod.Invoke(__instance, new object[]{nextTopRight, nextTopLeft, nextBottomRight, nextBottomLeft, nextTopMid, nextBottomMid, null, null, null, null, null, null, nextLargerThanTile, delegate(Vector2 corner)
                    {
                        if (__instance.terrainFeatures.TryGetValue(corner, out var value2) && value2 != null && value2.getBoundingBox().Intersects(position) && !pathfinding && character != null && !skipCollisionEffects)
                        {
                            value2.doCollisionAction(position, (int)((float)character.speed + character.addedSpeed), corner, character);
                        }
                        return false;
                    }});
                    if ((bool)testCornersTilesMethod.Invoke(__instance, new object[]{nextTopRight, nextTopLeft, nextBottomRight, nextBottomLeft, nextTopMid, nextBottomMid, currentTopRight, currentTopLeft, currentBottomRight, currentBottomLeft, currentTopMid, currentBottomMid, nextLargerThanTile, (Vector2 corner) => (__instance.terrainFeatures.TryGetValue(corner, out var value) && value != null && value.getBoundingBox().Intersects(position) && !value.isPassable(character)) ? true : false}))
                    {
                        ModMonitor.Log($"[DEBUG] 衝突: TerrainFeature at corner", LogLevel.Debug);
                        return;
                    }
                }
                
                // キャラクターとの衝突判定
                if (character != null && character.hasSpecialCollisionRules() && (character.isColliding(__instance, nextTopRight) || character.isColliding(__instance, nextTopLeft) || character.isColliding(__instance, nextBottomRight) || character.isColliding(__instance, nextBottomLeft)))
                {
                    ModMonitor.Log($"[DEBUG] 衝突: {character.Name} (特殊衝突ルール)", LogLevel.Debug);
                    return;
                }
                if (((isFarmer && (__instance.currentEvent == null || __instance.currentEvent.playerControlSequence)) || (character != null && character.collidesWithOtherCharacters.Value)) && !pathfinding)
                {
                    for (int i = __instance.characters.Count - 1; i >= 0; i--)
                    {
                        NPC other = __instance.characters[i];
                        if (other != null && (character == null || !character.Equals(other)))
                        {
                            Microsoft.Xna.Framework.Rectangle bounding_box = other.GetBoundingBox();
                            
                            if (other.layingDown)
                            {
                                bounding_box.Y -= 64;
                                bounding_box.Height += 64;
                            }
                            if (bounding_box.Intersects(position) && !Game1.player.temporarilyInvincible && !skipCollisionEffects)
                            {
                                other.behaviorOnFarmerPushing();
                            }
                            if (isFarmer)
                            {
                                if (!is_event_up && !other.farmerPassesThrough && bounding_box.Intersects(position) && !Game1.player.temporarilyInvincible && Game1.player.TemporaryPassableTiles.IsEmpty() && (!other.IsMonster || (!((Monster)other).isGlider.Value && !Game1.player.GetBoundingBox().Intersects(other.GetBoundingBox()))) && !other.IsInvisible && !Game1.player.GetBoundingBox().Intersects(bounding_box))
                                {
                                    ModMonitor.Log($"[DEBUG] 衝突: NPC {other.Name} (プレイヤーと衝突)", LogLevel.Debug);
                                    return;
                                }
                            }
                            else if (bounding_box.Intersects(position))
                            {
                                ModMonitor.Log($"[DEBUG] 衝突: NPC {other.Name} (別のキャラと衝突)", LogLevel.Debug);
                                return;
                            }
                        }
                    }
                }

                // タイルの衝突判定
                Layer back_layer = __instance.map.RequireLayer("Back");
                Layer buildings_layer = __instance.map.RequireLayer("Buildings");
                Tile t;
                if (isFarmer)
                {
                    Event @event = __instance.currentEvent;
                    if (@event != null && @event.checkForCollision(position, (character as Farmer) ?? Game1.player))
                    {
                        ModMonitor.Log($"[DEBUG] 衝突: フェスティバルの衝突エリア", LogLevel.Debug);
                        return;
                    }
                }
                else
                {
                    if (!pathfinding && !(character is Monster) && damagesFarmer == 0 && !glider)
                    {
                        foreach (Farmer otherFarmer in __instance.farmers)
                        {
                            if (position.Intersects(otherFarmer.GetBoundingBox()))
                            {
                                ModMonitor.Log($"[DEBUG] 衝突: 他のプレイヤーと衝突", LogLevel.Debug);
                                return;
                            }
                        }
                    }
                    if (((bool)__instance.isFarm.Value || MineShaft.IsGeneratedLevel(__instance, out var _) || __instance is IslandLocation) && character != null && !character.Name.Contains("NPC") && !character.EventActor && !glider)
                    {
                        if ((bool)testCornersTilesMethod.Invoke(__instance, new object[]{nextTopRight, nextTopLeft, nextBottomRight, nextBottomLeft, nextTopMid, nextBottomMid, currentTopRight, currentTopLeft, currentBottomRight, currentBottomLeft, currentTopMid, currentBottomMid, nextLargerThanTile, delegate(Vector2 tile)
                        {
                            t = back_layer.Tiles[(int)tile.X, (int)tile.Y];
                            return (t != null && t.Properties.ContainsKey("NPCBarrier")) ? true : false;
                        }}))
                        {
                            ModMonitor.Log($"[DEBUG] 衝突: タイルに NPCBarrier プロパティあり", LogLevel.Debug);
                            return;
                        }
                    }
                    if (glider && !projectile)
                    {
                        ModMonitor.Log($"[DEBUG] 衝突なし: glider のためタイル判定スキップ", LogLevel.Debug);
                        return;
                    }
                }

                if (!isFarmer || !Game1.player.isRafting)
                {
                    if ((bool)testCornersTilesMethod.Invoke(__instance, new object[]{nextTopRight, nextTopLeft, nextBottomRight, nextBottomLeft, nextTopMid, nextBottomMid, currentTopRight, currentTopLeft, currentBottomRight, currentBottomLeft, currentTopMid, currentBottomMid, nextLargerThanTile, delegate(Vector2 tile)
                    {
                        t = back_layer.Tiles[(int)tile.X, (int)tile.Y];
                        return (t != null && t.Properties.ContainsKey("TemporaryBarrier")) ? true : false;
                    }}))
                    {
                        ModMonitor.Log($"[DEBUG] 衝突: TemporaryBarrier タイル", LogLevel.Debug);
                        return;
                    }
                }

                if (!isFarmer || !Game1.player.isRafting)
                {
                    if ((!(character is FarmAnimal animal) || !animal.IsActuallySwimming()) && (bool)testCornersTilesMethod.Invoke(__instance, new object[]{nextTopRight, nextTopLeft, nextBottomRight, nextBottomLeft, nextTopMid, nextBottomMid, currentTopRight, currentTopLeft, currentBottomRight, currentBottomLeft, currentTopMid, currentBottomMid, nextLargerThanTile, delegate(Vector2 tile)
                    {
                        Tile tile3 = back_layer.Tiles[(int)tile.X, (int)tile.Y];
                        if (tile3 != null)
                        {
                            bool flag3 = tile3.TileIndexProperties.ContainsKey("Passable");
                            if (!flag3)
                            {
                                flag3 = tile3.Properties.ContainsKey("Passable");
                            }
                            if (flag3)
                            {
                                if (passableTiles != null && passableTiles.Contains((int)tile.X, (int)tile.Y))
                                {
                                    return false;
                                }
                                ModMonitor.Log($"[DEBUG] 衝突: タイルは Passable ではない", LogLevel.Debug);
                                return true;
                            }
                        }
                        return false;
                    }}))
                    {
                        return;
                    }

                    if (character == null || character.shouldCollideWithBuildingLayer(__instance))
                    {
                        Tile tmp;
                        if ((bool)testCornersTilesMethod.Invoke(__instance, new object[]{nextTopRight, nextTopLeft, nextBottomRight, nextBottomLeft, nextTopMid, nextBottomMid, currentTopRight, currentTopLeft, currentBottomRight, currentBottomLeft, currentTopMid, currentBottomMid, nextLargerThanTile, delegate(Vector2 tile)
                        {
                            tmp = buildings_layer.Tiles[(int)tile.X, (int)tile.Y];
                            if (tmp != null)
                            {
                                if (projectile && __instance is VolcanoDungeon)
                                {
                                    Tile tile2 = back_layer.Tiles[(int)tile.X, (int)tile.Y];
                                    if (tile2 != null)
                                    {
                                        if (tile2.TileIndexProperties.ContainsKey("Water"))
                                        {
                                            return false;
                                        }
                                        if (tile2.Properties.ContainsKey("Water"))
                                        {
                                            return false;
                                        }
                                    }
                                }
                                bool flag2 = tmp.TileIndexProperties.ContainsKey("Shadow");
                                if (!flag2)
                                {
                                    flag2 = tmp.TileIndexProperties.ContainsKey("Passable");
                                }
                                if (!flag2)
                                {
                                    flag2 = tmp.Properties.ContainsKey("Passable");
                                }
                                if (projectile)
                                {
                                    if (!flag2)
                                    {
                                        flag2 = tmp.TileIndexProperties.ContainsKey("ProjectilePassable");
                                    }
                                    if (!flag2)
                                    {
                                        flag2 = tmp.Properties.ContainsKey("ProjectilePassable");
                                    }
                                }
                                if (!flag2 && !isFarmer)
                                {
                                    flag2 = tmp.TileIndexProperties.ContainsKey("NPCPassable");
                                }
                                if (!flag2 && !isFarmer)
                                {
                                    flag2 = tmp.Properties.ContainsKey("NPCPassable");
                                }
                                if (!flag2 && !isFarmer && character != null && character.canPassThroughActionTiles())
                                {
                                    flag2 = tmp.Properties.ContainsKey("Action");
                                }
                                if (!flag2)
                                {
                                    if (passableTiles != null && passableTiles.Contains((int)tile.X, (int)tile.Y))
                                    {
                                        return false;
                                    }
                                    ModMonitor.Log($"[DEBUG] 衝突: 建物の通行不可タイル", LogLevel.Debug);
                                    return true;
                                }
                            }
                            return false;
                        }}))
                        {
                            return;
                        }
                    }
                    if (!isFarmer && character?.controller != null && !skipCollisionEffects)
                    {
                        Point tileLocation = new Point(position.Center.X / 64, position.Bottom / 64);
                        Tile tile = buildings_layer.Tiles[tileLocation.X, tileLocation.Y];
                        if (tile != null && tile.Properties.ContainsKey("Action"))
                        {
                            __instance.openDoor(new Location(tileLocation.X, tileLocation.Y), Game1.currentLocation.Equals(__instance));
                        }
                        else
                        {
                            tileLocation = new Point(position.Center.X / 64, position.Top / 64);
                            tile = buildings_layer.Tiles[tileLocation.X, tileLocation.Y];
                            if (tile != null && tile.Properties.ContainsKey("Action"))
                            {
                                __instance.openDoor(new Location(tileLocation.X, tileLocation.Y), Game1.currentLocation.Equals(__instance));
                            }
                        }
                    }
                    return;
                }

            	if ((bool)testCornersTilesMethod.Invoke(__instance, new object[]{nextTopRight, nextTopLeft, nextBottomRight, nextBottomLeft, nextTopMid, nextBottomMid, currentTopRight, currentTopLeft, currentBottomRight, currentBottomLeft, currentTopMid, currentBottomMid, nextLargerThanTile, delegate(Vector2 tile)
                {
                    t = back_layer.Tiles[(int)tile.X, (int)tile.Y];
                    if ((!(t?.TileIndexProperties.ContainsKey("Water"))) ?? true)
                    {
                        int num = (int)tile.X;
                        int num2 = (int)tile.Y;
                        if (__instance.IsTileBlockedBy(new Vector2(num, num2)))
                        {
                            Game1.player.isRafting = false;
                            Game1.player.Position = new Vector2(num * 64, num2 * 64 - 32);
                            Game1.player.setTrajectory(0, 0);
                        }
                        ModMonitor.Log($"[DEBUG] 衝突: 水の上", LogLevel.Debug);
                        return true;
                    }
                    return false;
                }}))
                {
                    return;
                }
                ModMonitor.Log($"[DEBUG] 衝突なし: 通行可能", LogLevel.Debug);
                return;
            }
            catch (Exception ex)
            {
                ModMonitor.Log($"[ERROR] isCollidingPosition()のデバッグ中に例外が発生: {ex}", LogLevel.Error);
            }
        }


        // public static bool IsPositionImpassableForNPCSchedulePrefix(GameLocation loc, int x, int y, ref bool __result)
        // {
        //     if (loc.NameOrUniqueName == "FarmHouse")
        //     {
        //         Vector2 tile = new Vector2(x, y);

        //         // Buildingsレイヤーのタイルを取得
        //         Tile buildingTile = loc.Map.GetLayer("Buildings")?.Tiles[x, y];

        //         // 壁の可能性があるタイルをチェック
        //         if (buildingTile != null)
        //         {
        //             ModMonitor.Log($"[DEBUG] IsPositionImpassableForNPCSchedule ({x}, {y}) TileIndex = {buildingTile.TileIndex}", LogLevel.Debug);
        //             if (buildingTile.TileIndex != 0)
        //             {
        //             // Backレイヤーの Passable プロパティを確認
        //             // if (loc.doesTileHaveProperty(x, y, "Passable", "Back") == null)
        //             // {
        //                 ModMonitor.Log($"[DEBUG] IsPositionImpassableForNPCSchedule ({x}, {y}) is Impassable because tile", LogLevel.Debug);
        //                 __result = true; // Passable プロパティがない場合、壁とみなす
        //                 return false; // Harmonyの元メソッドをスキップ
        //             // }
        //             }
        //         }

        //         // 家具チェック
        //         foreach (Furniture furniture in loc.furniture)
        //         {
        //             if (furniture.GetBoundingBox().Contains(x * 64, y * 64))
        //             {
        //                 ModMonitor.Log($"[DEBUG] IsPositionImpassableForNPCSchedule ({x}, {y}) is Impassable because furniture", LogLevel.Debug);
        //                 __result = true;
        //                 return false;
        //             }
        //         }

        //         // オブジェクトチェック（テーブルやベッドなど）
        //         if (loc.objects.ContainsKey(tile) && !loc.objects[tile].isPassable())
        //         {
        //             ModMonitor.Log($"[DEBUG] IsPositionImpassableForNPCSchedule ({x}, {y}) is Impassable because objects", LogLevel.Debug);
        //             __result = true;
        //             return false;
        //         }
        //     }

        //     return true; // 通常の処理を続行
        // }


        
        public static bool IsPositionImpassableForNPCSchedulePrefix(PathFindController __instance, GameLocation loc, int x, int y, ref bool __result)
        {
            Vector2 tile = new Vector2(x, y);

            if (loc.NameOrUniqueName == "FarmHouse")
            {
                // 家具チェック（ベッドや大きな家具）
                foreach (Furniture furniture in loc.furniture)
                {
                    // if (furniture.GetBoundingBox().Intersects(new Rectangle(x * 64, y * 64, 64, 64)))
                    if (furniture.GetBoundingBox().Contains(x * 64, y * 64))
                    {
                        ModMonitor.Log($"[DEBUG] {loc.NameOrUniqueName}: Blocked by furniture at {x}, {y}", LogLevel.Debug);
                        __result = true;
                        return false;
                    }
                }

                // オブジェクトチェック（チェストなど）
                if (loc.objects.ContainsKey(tile) && !loc.objects[tile].isPassable())
                {
                    ModMonitor.Log($"[DEBUG] {loc.NameOrUniqueName}: Blocked by object at {x}, {y}", LogLevel.Debug);
                    __result = true;
                    return false;
                }

                // Character? character = null;
                NPC character = null;
                // if (__instance != null)
                // {
                    // character = AccessTools.Field(typeof(PathFindController), "character")?.GetValue(__instance) as Character;
                    // ModMonitor.Log($"[DEBUG] PathFindController is NOT null", LogLevel.Debug);
        			character = Game1.getCharacterFromName("Seiris");
                // }
                // else
                // {
                //     ModMonitor.Log($"[DEBUG] PathFindController is null", LogLevel.Debug);
                //     // character = new NPC();
                // }

                // **壁の有無をチェック**
		        // Rectangle bbox = new Rectangle(x + 8, y + 16, 16, 32);
		        // Rectangle bbox = new Rectangle(x * 64, y * 64, 64, 64);
		        Microsoft.Xna.Framework.Rectangle bbox = new Microsoft.Xna.Framework.Rectangle(x * 64 + 1, y * 64 + 1, 62, 62);

                bool ignoreCharacterRequirementValue = true;
                // ModMonitor.Log($"[DEBUG] 渡す値: ignoreCharacterRequirement={ignoreCharacterRequirementValue}", LogLevel.Debug);

                bool isBlocked = loc.isCollidingPosition(
                    bbox, Game1.viewport, false, 0, false, character, true, ignoreCharacterRequirement: ignoreCharacterRequirementValue
                );

                if (isBlocked)
                {
                    ModMonitor.Log($"[DEBUG] {loc.NameOrUniqueName}: Blocked by wall at {x}, {y}", LogLevel.Debug);
                    __result = true;
                    return false;
                }
            }

            return true; // 通常の判定を実行
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