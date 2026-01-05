/*
 * Lone EFT DMA Radar
 * Brought to you by Lone (Lone DMA)
 * 
MIT License

Copyright (c) 2025 Lone DMA

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 *
*/

using Collections.Pooled;
using LoneEftDmaRadar.Misc;
using LoneEftDmaRadar.Tarkov.Unity;
using LoneEftDmaRadar.Tarkov.Unity.Structures;
using LoneEftDmaRadar.Tarkov.World.Loot;
using LoneEftDmaRadar.Tarkov.World.Player.Helpers;
using LoneEftDmaRadar.UI;
using LoneEftDmaRadar.UI.Maps;
using LoneEftDmaRadar.UI.Skia;
using VmmSharpEx.Extensions;
using VmmSharpEx.Scatter;
using static LoneEftDmaRadar.Tarkov.Unity.Structures.UnityTransform;

namespace LoneEftDmaRadar.Tarkov.World.Player
{
    /// <summary>
    /// Base class for Tarkov Players.
    /// Tarkov implements several distinct classes that implement a similar player interface.
    /// </summary>
    public abstract class AbstractPlayer : IWorldEntity, IMapEntity, IMouseoverEntity
    {
        /// <summary>
        /// Group ID for Solo Players.
        /// </summary>
        public const int SoloGroupId = -1;
        /// <summary>
        /// Group ID for Teammates of <see cref="LocalPlayer"/>.
        /// </summary>
        public const int TeammateGroupId = -100;
        public static implicit operator ulong(AbstractPlayer x) => x.Base;

        protected static EftDmaConfig Config { get; } = Program.Config;

        #region Cached Skia Paths

        private static readonly SKPath _playerPill = CreatePlayerPillBase();
        private static readonly SKPath _deathMarker = CreateDeathMarkerPath();
        private static readonly SKPaint _paintHeightPos = new SKPaint { Color = SKColors.Red, IsAntialias = true };
        private static readonly SKPaint _paintHeightNeg = new SKPaint { Color = SKColors.DeepSkyBlue, IsAntialias = true };

        private const float PP_RADIUS = 4.0f;
        private const float PP_NOSE_X = PP_RADIUS;

        private static SKPath CreatePlayerPillBase()
        {
            var path = new SKPath();
            path.AddCircle(0, 0, PP_RADIUS);
            path.Close();
            return path;
        }

        private static SKPath CreateDeathMarkerPath()
        {
            const float length = 6f;
            var path = new SKPath();

            path.MoveTo(-length, length);
            path.LineTo(length, -length);
            path.MoveTo(-length, -length);
            path.LineTo(length, length);

            return path;
        }

        #endregion

        #region Allocation

        /// <summary>
        /// Allocates a player.
        /// </summary>
        public static void Allocate(ConcurrentDictionary<ulong, AbstractPlayer> regPlayers, ulong playerBase)
        {
            try
            {
                _ = regPlayers.GetOrAdd(
                    playerBase,
                    addr => AllocateInternal(addr));
            }
            catch (Exception ex)
            {
                Logging.WriteLine($"ERROR during Player Allocation for player @ 0x{playerBase.ToString("X")}: {ex}");
            }
        }

        private static AbstractPlayer AllocateInternal(ulong playerBase)
        {
            AbstractPlayer player;
            var className = ObjectClass.ReadName(playerBase, 64);
            var isClientPlayer = className == "ClientPlayer" || className == "LocalPlayer";

            if (isClientPlayer)
                player = new ClientPlayer(playerBase);
            else
                player = new ObservedPlayer(playerBase);
            Logging.WriteLine($"Player '{player.Name}' allocated | 0x{playerBase:X}");
            return player;
        }

        private AbstractPlayer() { }

        /// <summary>
        /// Player Constructor.
        /// </summary>
        protected AbstractPlayer(ulong playerBase)
        {
            playerBase.ThrowIfInvalidUserVA(nameof(playerBase));
            Base = playerBase;
        }

        #endregion

        #region Fields / Properties
        /// <summary>
        /// Player Class Base Address
        /// </summary>
        public ulong Base { get; }

        /// <summary>
        /// True if the Player is Active (in the player list).
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Player's Group Id (Default: Solo).
        /// </summary>
        public int GroupId { get; protected set; } = SoloGroupId;

        /// <summary>
        /// Type of player unit.
        /// </summary>
        public virtual PlayerType Type { get; protected set; }

        private Vector2 _rotation;
        /// <summary>
        /// Player's Rotation in Game World.
        /// </summary>
        public Vector2 Rotation
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _rotation;
            private set
            {
                _rotation = value;
                float mapRotation = value.X - 90f;
                MapRotation = ((mapRotation % 360f) + 360f) % 360f;
            }
        }

        /// <summary>
        /// Player's Map Rotation (with 90 degree correction applied).
        /// </summary>
        public float MapRotation { get; private set; }

        /// <summary>
        /// Corpse field value.
        /// </summary>
        public ulong? Corpse { get; private set; }

        /// <summary>
        /// Player's Skeleton Root.
        /// </summary>
        public UnityTransform SkeletonRoot { get; protected set; }

        /// <summary>
        /// TRUE if critical memory reads (position/rotation) have failed.
        /// </summary>
        public bool IsError { get; set; }

        /// <summary>
        /// True if player is being focused via Right-Click (UI).
        /// </summary>
        public bool IsFocused { get; set; }

        /// <summary>
        /// Dead Player's associated loot container object.
        /// </summary>
        public LootCorpse LootObject { get; set; }
        /// <summary>
        /// Alerts for this Player Object.
        /// Used by Player History UI Interop.
        /// </summary>
        public virtual string Alerts { get; protected set; }

        #endregion

        #region Virtual Properties

        /// <summary>
        /// Player nickName.
        /// </summary>
        public virtual string Name { get; protected set; }

        /// <summary>
        /// Player's Faction.
        /// </summary>
        public virtual Enums.EPlayerSide PlayerSide { get; protected set; }

        /// <summary>
        /// Player is Human-Controlled.
        /// </summary>
        public virtual bool IsHuman { get; }

        /// <summary>
        /// MovementContext / StateContext
        /// </summary>
        public virtual ulong MovementContext { get; }

        /// <summary>
        /// Corpse field address..
        /// </summary>
        public virtual ulong CorpseAddr { get; }

        /// <summary>
        /// Player Rotation Field Address (view angles).
        /// </summary>
        public virtual ulong RotationAddress { get; }

        #endregion

        #region Boolean Getters

        /// <summary>
        /// Player is AI-Controlled.
        /// </summary>
        public bool IsAI => !IsHuman;

        /// <summary>
        /// Player is a PMC Operator.
        /// </summary>
        public bool IsPmc => PlayerSide is Enums.EPlayerSide.Usec || PlayerSide is Enums.EPlayerSide.Bear;

        /// <summary>
        /// Player is a SCAV.
        /// </summary>
        public bool IsScav => PlayerSide is Enums.EPlayerSide.Savage;

        /// <summary>
        /// Player is alive (not dead).
        /// </summary>
        public bool IsAlive => Corpse is null;

        /// <summary>
        /// True if Player is Friendly to LocalPlayer.
        /// </summary>
        public bool IsFriendly =>
            this is LocalPlayer || Type is PlayerType.Teammate;

        /// <summary>
        /// True if player is Hostile to LocalPlayer.
        /// </summary>
        public bool IsHostile => !IsFriendly;

        /// <summary>
        /// Player is Alive/Active and NOT LocalPlayer.
        /// </summary>
        public bool IsNotLocalPlayerAlive =>
            this is not LocalPlayer && IsActive && IsAlive;

        /// <summary>
        /// Player is a Hostile PMC Operator.
        /// </summary>
        public bool IsHostilePmc => IsPmc && IsHostile;

        /// <summary>
        /// Player is human-controlled (Not LocalPlayer).
        /// </summary>
        public bool IsHumanOther => IsHuman && this is not LocalPlayer;

        /// <summary>
        /// Player is AI Controlled and Alive/Active.
        /// </summary>
        public bool IsAIActive => IsAI && IsActive && IsAlive;

        /// <summary>
        /// Player is AI Controlled and Alive/Active & their AI Role is default.
        /// </summary>
        public bool IsDefaultAIActive => IsAI && Name == "defaultAI" && IsActive && IsAlive;

        /// <summary>
        /// Player is human-controlled and Active/Alive.
        /// </summary>
        public bool IsHumanActive =>
            IsHuman && IsActive && IsAlive;

        /// <summary>
        /// Player is hostile and alive/active.
        /// </summary>
        public bool IsHostileActive => IsHostile && IsActive && IsAlive;

        /// <summary>
        /// Player is human-controlled & Hostile.
        /// </summary>
        public bool IsHumanHostile => IsHuman && IsHostile;

        /// <summary>
        /// Player is human-controlled, hostile, and Active/Alive.
        /// </summary>
        public bool IsHumanHostileActive => IsHumanHostile && IsActive && IsAlive;

        /// <summary>
        /// Player is friendly to LocalPlayer (including LocalPlayer) and Active/Alive.
        /// </summary>
        public bool IsFriendlyActive => IsFriendly && IsActive && IsAlive;

        /// <summary>
        /// Player has exfil'd/left the raid.
        /// </summary>
        public bool HasExfild => !IsActive && IsAlive;

        #endregion

        #region Methods

        /// <summary>
        /// Validates the Rotation Address.
        /// </summary>
        /// <param nickName="rotationAddr">Rotation va</param>
        /// <returns>Validated rotation virtual address.</returns>
        protected static ulong ValidateRotationAddr(ulong rotationAddr)
        {
            var rotation = Memory.ReadValue<Vector2>(rotationAddr, false);
            if (!rotation.IsNormalOrZero() ||
                Math.Abs(rotation.X) > 360f ||
                Math.Abs(rotation.Y) > 90f)
                throw new ArgumentOutOfRangeException(nameof(rotationAddr));

            return rotationAddr;
        }

        /// <summary>
        /// Refreshes non-realtime player information. Call in the Registered Players Loop (T0).
        /// </summary>
        /// <param nickName="scatter"></param>
        /// <param nickName="registered"></param>
        /// <param nickName="isActiveParam"></param>
        public virtual void OnRegRefresh(VmmScatter scatter, ISet<ulong> registered, bool? isActiveParam = null)
        {
            if (isActiveParam is not bool isActive)
                isActive = registered.Contains(this);
            if (isActive)
            {
                SetAlive();
            }
            else if (IsAlive) // Not in list, but alive
            {
                scatter.PrepareReadPtr(CorpseAddr);
                scatter.Completed += (sender, x1) =>
                {
                    if (x1.ReadPtr(CorpseAddr, out var corpsePtr))
                        SetDead(corpsePtr);
                    else
                        SetExfild();
                };
            }
        }

        /// <summary>
        /// Mark player as dead.
        /// </summary>
        /// <param nickName="corpse">Corpse address.</param>
        public void SetDead(ulong corpse)
        {
            Corpse = corpse;
            IsActive = false;
        }

        /// <summary>
        /// Mark player as exfil'd.
        /// </summary>
        private void SetExfild()
        {
            Corpse = null;
            IsActive = false;
        }

        /// <summary>
        /// Mark player as alive.
        /// </summary>
        private void SetAlive()
        {
            Corpse = null;
            LootObject = null;
            IsActive = true;
        }

        /// <summary>
        /// Executed on each Realtime Loop.
        /// </summary>
        /// <param nickName="index">Scatter read index dedicated to this player.</param>
        public virtual void OnRealtimeLoop(VmmScatter scatter)
        {
            scatter.PrepareReadValue<Vector2>(RotationAddress); // Rotation
            scatter.PrepareReadArray<TrsX>(SkeletonRoot.VerticesAddr, SkeletonRoot.Count); // ESP Vertices

            scatter.Completed += (sender, s) =>
            {
                bool successRot = false;
                bool successPos = true;
                if (s.ReadValue<Vector2>(RotationAddress, out var rotation))
                    successRot = SetRotation(rotation);

                if (s.ReadPooled<TrsX>(SkeletonRoot.VerticesAddr, SkeletonRoot.Count) is IMemoryOwner<TrsX> vertices)
                {
                    using (vertices)
                    {
                        try
                        {
                            try
                            {
                                _ = SkeletonRoot.UpdatePosition(vertices.Memory.Span);
                            }
                            catch (Exception ex) // Attempt to re-allocate Transform on error
                            {
                                Logging.WriteLine($"ERROR getting Player '{Name}' SkeletonRoot Position: {ex}");
                                var transform = new UnityTransform(SkeletonRoot.TransformInternal);
                                SkeletonRoot = transform;
                            }
                        }
                        catch
                        {
                            successPos = false;
                        }
                    }
                }

                IsError = !successRot || !successPos;
            };
        }

        /// <summary>
        /// Executed on each Transform Validation Loop.
        /// </summary>
        /// <param nickName="round1">Index (round 1)</param>
        /// <param nickName="round2">Index (round 2)</param>
        public virtual void OnValidateTransforms(VmmScatter round1, VmmScatter round2)
        {
            round1.PrepareReadPtr(SkeletonRoot.TransformInternal + UnitySDK.UnityOffsets.TransformAccess_HierarchyOffset); // Bone Hierarchy
            round1.Completed += (sender, x1) =>
            {
                if (x1.ReadPtr(SkeletonRoot.TransformInternal + UnitySDK.UnityOffsets.TransformAccess_HierarchyOffset, out var tra))
                {
                    round2.PrepareReadPtr(tra + UnitySDK.UnityOffsets.Hierarchy_VerticesOffset); // Vertices Ptr
                    round2.Completed += (sender, x2) =>
                    {
                        if (x2.ReadPtr(tra + UnitySDK.UnityOffsets.Hierarchy_VerticesOffset, out var verticesPtr))
                        {
                            if (SkeletonRoot.VerticesAddr != verticesPtr) // check if any addr changed
                            {
                                Logging.WriteLine($"WARNING - SkeletonRoot Transform has changed for Player '{Name}'");
                                var transform = new UnityTransform(SkeletonRoot.TransformInternal);
                                SkeletonRoot = transform;
                            }
                        }
                    };
                }
            };
        }

        /// <summary>
        /// Set player rotation (Direction/Pitch)
        /// </summary>
        protected virtual bool SetRotation(Vector2 rotation)
        {
            try
            {
                rotation.ThrowIfAbnormalAndNotZero(nameof(rotation));
                rotation.X = rotation.X.NormalizeAngle();
                ArgumentOutOfRangeException.ThrowIfLessThan(rotation.X, 0f);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(rotation.X, 360f);
                ArgumentOutOfRangeException.ThrowIfLessThan(rotation.Y, -90f);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(rotation.Y, 90f);
                Rotation = rotation;
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion


        #region Interfaces

        public virtual ref readonly Vector3 Position => ref SkeletonRoot.Position;
        public Vector2 MouseoverPosition { get; set; }

        private ValueTuple<SKPaint, SKPaint> _paints;

        public void Draw(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            DrawInfoText(canvas, mapParams, localPlayer);
            DrawPill(canvas, mapParams, localPlayer);
        }

        public void DrawPill(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer, IEnumerable<AbstractPlayer> allPlayers = null)
        {
            try
            {
                var point = Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams);
                MouseoverPosition = new Vector2(point.X, point.Y);
                if (!IsAlive) // Player Dead -- Draw 'X' death marker and move on
                {
                    DrawDeathMarker(canvas, point);
                }
                else
                {
                    _paints = GetPaints();
                    DrawPlayerPill(canvas, localPlayer, point, allPlayers);
                }
            }
            catch (Exception ex)
            {
                Logging.WriteLine($"WARNING! Player DrawPill Error: {ex}");
            }
        }

        public void DrawInfoText(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            try
            {
                if (!IsAlive || this == localPlayer) return;

                var point = Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams);
                _paints = GetPaints();

                var height = Position.Y - localPlayer.Position.Y;
                var dist = Vector3.Distance(localPlayer.Position, Position);
                using var lines = new PooledList<(string, SKPaint)>();
                var observed = this as ObservedPlayer;
                string important = (observed is not null && observed.Equipment.CarryingImportantLoot) ?
                    "!!" : null; // Flag important loot
                string name = null;
                if (IsError)
                    name = "ERROR"; // In case POS stops updating, let us know!
                else
                    name = Name;
                string health = null; string level = null;
                if (observed is not null)
                {
                    health = observed.HealthStatus is Enums.ETagStatus.Healthy
                        ? null
                        : $" ({observed.HealthStatus})"; // Only display abnormal health status
                }
                string nameLine = $"{important}{level}{name}{health}";
                nameLine += $" ({dist:n0}m)";
                lines.Add((nameLine, null));

                string hText = $"H: {height:n0}";
                SKPaint hPaint = null;
                string arrow = "";

                if (height > 1)
                {
                    hPaint = _paintHeightPos;
                    if (height >= 8) arrow = " ▲▲▲";
                    else if (height >= 5) arrow = " ▲▲";
                    else arrow = " ▲";
                }
                else if (height < -1)
                {
                    hPaint = _paintHeightNeg;
                    if (height <= -8) arrow = " ▼▼▼";
                    else if (height <= -5) arrow = " ▼▼";
                    else arrow = " ▼";
                }

                lines.Add((hText + arrow, hPaint));

                if (Config.UI.ShowInHandsOnMap && observed?.Equipment?.InHands is not null)
                {
                    string inHands = observed.Equipment.InHands.ShortName;
                    if (!string.IsNullOrWhiteSpace(inHands))
                        lines.Add((TrimMapLabel(inHands, maxLen: 24), null));
                }

                if (observed is not null && (IsPmc || Type == PlayerType.AIBoss))
                {
                    var items = observed.Equipment.Items;
                    int headClass = 0;
                    int bodyClass = 0;

                    if (items.TryGetValue("Headwear", out var helmet))
                        headClass = helmet.ArmorClass;

                    if (items.TryGetValue("ArmorVest", out var armor))
                        bodyClass = armor.ArmorClass;

                    if (items.TryGetValue("TacticalVest", out var rig) && rig.ArmorClass > bodyClass)
                        bodyClass = rig.ArmorClass;

                    if (headClass > 0 || bodyClass > 0)
                    {
                        lines.Add(($"头{headClass}甲{bodyClass}", null));
                    }
                }

                DrawPlayerText(canvas, point, lines);
            }
            catch (Exception ex)
            {
                Logging.WriteLine($"WARNING! Player DrawInfoText Error: {ex}");
            }
        }

        private static string TrimMapLabel(string text, int maxLen)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            if (maxLen <= 1)
                return text[..1];
            return text.Length > maxLen ? text[..(maxLen - 1)] + "…" : text;
        }

        /// <summary>
        /// Draws a Player Pill on this location.
        /// </summary>
        private void DrawPlayerPill(SKCanvas canvas, LocalPlayer localPlayer, SKPoint point, IEnumerable<AbstractPlayer> allPlayers)
        {
            if (this != localPlayer && RadarWindow.MouseoverGroup is int grp && grp == GroupId)
                _paints.Item1 = SKPaints.PaintMouseoverGroup;
            // Base scale increased by 50% (0.825f -> 1.2375f) and multiplied by user config
            float scale = 1.2375f * Config.UI.PlayerScale * Config.UI.UIScale;

            canvas.Save();
            canvas.Translate(point.X, point.Y);
            canvas.Scale(scale, scale);
            canvas.RotateDegrees(MapRotation + Config.UI.MapRotation);

            SKPaints.ShapeOutline.StrokeWidth = _paints.Item1.StrokeWidth * 1.3f;
            // Draw the pill
            canvas.DrawPath(_playerPill, SKPaints.ShapeOutline); // outline
            canvas.DrawPath(_playerPill, _paints.Item1);

            var aimlineLength = Config.UI.AimLineLength;

            bool isAimingAtFriendly = this.IsFacingTarget(localPlayer, Config.UI.MaxDistance);

            if (!isAimingAtFriendly && allPlayers != null)
            {
                foreach (var other in allPlayers)
                {
                    if (other.Type == PlayerType.Teammate && this.IsFacingTarget(other, Config.UI.MaxDistance))
                    {
                        isAimingAtFriendly = true;
                        break;
                    }
                }
            }

            if (!IsFriendly &&
                !(IsAI && !Config.UI.AIAimlines) &&
                isAimingAtFriendly) // Hostile Player, check if aiming at a friendly (High Alert)
                aimlineLength = 9999;

            if (aimlineLength > 0)
            {
                // Draw aimlines thinner; avoid scaling stroke width up with UI scale.
                float invScale = 1f / Math.Max(0.001f, scale);
                using var aimOutline = SKPaints.ShapeOutline.Clone();
                aimOutline.StrokeWidth = SKPaints.ShapeOutline.StrokeWidth * invScale * 0.85f;
                using var aimPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };
                aimPaint.StrokeWidth = _paints.Item1.StrokeWidth * invScale * 0.85f;

                // Draw line from nose tip forward
                canvas.DrawLine(PP_NOSE_X, 0, PP_NOSE_X + aimlineLength, 0, aimOutline); // outline
                canvas.DrawLine(PP_NOSE_X, 0, PP_NOSE_X + aimlineLength, 0, aimPaint);
            }

            canvas.Restore();
        }

        /// <summary>
        /// Draws a Death Marker on this location.
        /// </summary>
        private static void DrawDeathMarker(SKCanvas canvas, SKPoint point)
        {
            float scale = Config.UI.UIScale;

            canvas.Save();
            canvas.Translate(point.X, point.Y);
            canvas.Scale(scale, scale);
            canvas.DrawPath(_deathMarker, SKPaints.PaintDeathMarker);
            canvas.Restore();
        }

        /// <summary>
        /// Draws Player Text on this location.
        /// </summary>
        private void DrawPlayerText(SKCanvas canvas, SKPoint point, IList<(string text, SKPaint paint)> lines)
        {
            if (RadarWindow.MouseoverGroup is int grp && grp == GroupId)
                _paints.Item2 = SKPaints.TextMouseoverGroup;
            
            // Offset below the player dot
            // Radius is 4.0f, Scale is 1.65f. 4 * 1.65 = 6.6. Add padding.
            point.Offset(0, 10f * Config.UI.UIScale + (SKFonts.UIRegular.Spacing / 2));

            foreach (var (line, paint) in lines)
            {
                if (string.IsNullOrEmpty(line?.Trim()))
                    continue;

                var textPaint = paint ?? _paints.Item2;

                canvas.DrawText(line, point, SKTextAlign.Center, SKFonts.UIRegular, SKPaints.TextOutline); // Draw outline
                canvas.DrawText(line, point, SKTextAlign.Center, SKFonts.UIRegular, textPaint); // draw line text

                point.Offset(0, SKFonts.UIRegular.Spacing);
            }
        }

        private ValueTuple<SKPaint, SKPaint> GetPaints()
        {
            if (IsFocused)
                return new ValueTuple<SKPaint, SKPaint>(SKPaints.PaintFocused, SKPaints.TextFocused);
            if (this is LocalPlayer)
                return new ValueTuple<SKPaint, SKPaint>(SKPaints.PaintLocalPlayer, SKPaints.TextLocalPlayer);
            switch (Type)
            {
                case PlayerType.Teammate:
                    return new ValueTuple<SKPaint, SKPaint>(SKPaints.PaintTeammate, SKPaints.TextTeammate);
                case PlayerType.PMC:
                    return new ValueTuple<SKPaint, SKPaint>(SKPaints.PaintPMC, SKPaints.TextPMC);
                case PlayerType.AIScav:
                    return new ValueTuple<SKPaint, SKPaint>(SKPaints.PaintScav, SKPaints.TextScav);
                case PlayerType.AIRaider:
                    return new ValueTuple<SKPaint, SKPaint>(SKPaints.PaintRaider, SKPaints.TextRaider);
                case PlayerType.AIBoss:
                    return new ValueTuple<SKPaint, SKPaint>(SKPaints.PaintBoss, SKPaints.TextBoss);
                case PlayerType.PScav:
                    return new ValueTuple<SKPaint, SKPaint>(SKPaints.PaintPScav, SKPaints.TextPScav);
                default:
                    return new ValueTuple<SKPaint, SKPaint>(SKPaints.PaintPMC, SKPaints.TextPMC);
            }
        }

        public void DrawMouseover(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            if (this == localPlayer)
                return;
            using var lines = new PooledList<string>();
            string health = null;
            var obs = this as ObservedPlayer;
            if (obs is not null)
                health = obs.HealthStatus is Enums.ETagStatus.Healthy
                    ? null
                    : $" ({obs.HealthStatus.ToString()})"; // Only display abnormal health status
            string alert = Alerts?.Trim();
            if (!string.IsNullOrEmpty(alert)) // Special Players,etc.
                lines.Add(alert);
            string group = this.GroupId == -1 ?
                null : $"G:{this.GroupId}";
            if (IsHostileActive) // Enemy Players, display information
            {
                lines.Add($"{Name}{health} {group}");
            }
            else if (!IsAlive)
            {
                lines.Add($"{Type.ToString()}:{Name} {group}");
            }
            else if (IsAIActive)
            {
                lines.Add(Name);
            }
            if (obs is not null)
            {
                // This is outside of the previous conditionals to always show equipment even if they're dead,etc.
                lines.Add($"Hands: {obs.Equipment.InHands?.ShortName ?? "<Empty>"}");
                lines.Add($"Value: {Utilities.FormatNumberKM(obs.Equipment.Value)}");
                foreach (var item in obs.Equipment.Items.OrderBy(e => e.Key))
                {
                    string important = item.Value.IsImportant ?
                        "!!" : null; // Flag important loot
                    lines.Add($"{important}{item.Key.Substring(0, 5)}: {item.Value.ShortName}");
                }
            }

            Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams).DrawMouseoverText(canvas, lines.Span);
        }

        #endregion

        #region High Alert

        /// <summary>
        /// True if Current Player is facing <paramref nickName="target"/>.
        /// </summary>
        public bool IsFacingTarget(AbstractPlayer target, float? maxDist = null)
        {
            Vector3 delta = target.Position - this.Position;

            if (maxDist is float m)
            {
                float maxDistSq = m * m;
                float distSq = Vector3.Dot(delta, delta);
                if (distSq > maxDistSq) return false;
            }

            float distance = delta.Length();
            if (distance <= 1e-6f)
                return true;

            Vector3 fwd = RotationToDirection(this.Rotation);

            float cosAngle = Vector3.Dot(fwd, delta) / distance;

            const float A = 31.3573f;
            const float B = 3.51726f;
            const float C = 0.626957f;
            const float D = 15.6948f;

            float x = MathF.Abs(C - D * distance);
            float angleDeg = A - B * MathF.Log(MathF.Max(x, 1e-6f));
            if (angleDeg < 1f) angleDeg = 1f;
            if (angleDeg > 179f) angleDeg = 179f;

            float cosThreshold = MathF.Cos(angleDeg * (MathF.PI / 180f));
            return cosAngle >= cosThreshold;

            static Vector3 RotationToDirection(Vector2 rotation)
            {
                float yaw = rotation.X * (MathF.PI / 180f);
                float pitch = rotation.Y * (MathF.PI / 180f);

                float cp = MathF.Cos(pitch);
                float sp = MathF.Sin(pitch);
                float sy = MathF.Sin(yaw);
                float cy = MathF.Cos(yaw);

                var dir = new Vector3(
                    cp * sy,
                   -sp,
                    cp * cy
                );

                float lenSq = Vector3.Dot(dir, dir);
                if (lenSq > 0f && MathF.Abs(lenSq - 1f) > 1e-4f)
                {
                    float invLen = 1f / MathF.Sqrt(lenSq);
                    dir *= invLen;
                }
                return dir;
            }
        }

        #endregion
    }
}