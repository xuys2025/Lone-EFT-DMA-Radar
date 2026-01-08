using Collections.Pooled;
using SkiaSharp;
using LoneEftDmaRadar.Tarkov.Unity;
using LoneEftDmaRadar.Tarkov.World.Player;
using LoneEftDmaRadar.UI.Maps;
using LoneEftDmaRadar.UI.Skia;

namespace LoneEftDmaRadar.Tarkov.World.Quests
{
    /// <summary>
    /// Wraps a Mouseoverable Quest Location marker onto the Map GUI.
    /// </summary>
    public sealed class QuestLocation : IWorldEntity, IMapEntity, IMouseoverEntity
    {
        /// <summary>
        /// Name of this quest.
        /// </summary>
        public string Name { get; }
        public QuestObjectiveType Type { get; }
        public Vector2 MouseoverPosition { get; set; }

        private readonly Vector3 _position;
        public ref readonly Vector3 Position => ref _position;

        public QuestLocation(string questID, string target, Vector3 position)
        {
            QuestObjectiveType foundType = QuestObjectiveType.Unknown;
            // Resolve quest name and objective type (if available).
            if (TarkovDataManager.TaskData.TryGetValue(questID, out var q))
            {
                Name = q.Name ?? target;

                // Attempt to find the objective that corresponds to 'target' and extract its Type.
                try
                {
                    if (q.Objectives is not null)
                    {
                        var obj = q.Objectives.FirstOrDefault(o =>
                            !string.IsNullOrEmpty(o.Id) && string.Equals(o.Id, target, StringComparison.OrdinalIgnoreCase)
                            || (o.MarkerItem?.Id is not null && string.Equals(o.MarkerItem.Id, target, StringComparison.OrdinalIgnoreCase))
                            || (o.Zones?.Any(z => string.Equals(z.Id, target, StringComparison.OrdinalIgnoreCase)) == true)
                        );
                        foundType = obj?.Type ?? QuestObjectiveType.Unknown;
                    }
                }
                catch
                {
                    // Swallow any unexpected structure errors
                }

                Type = foundType;
            }
            else
            {
                // Fallback when TaskData doesn't contain the quest
                Name = target;
                Type = QuestObjectiveType.Unknown;
            }

            _position = position;
        }

        public void Draw(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            var point = Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams);
            MouseoverPosition = new Vector2(point.X, point.Y);

            float scale = Program.Config.UI.UIScale;
            // Radius 4.5f (slightly smaller than player dot ~5.0f)
            float radius = 4.5f * scale; 

            using var paint = new SKPaint
            {
                Color = SKColors.Green,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            using var outline = new SKPaint
            {
                Color = SKColors.Black,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1f * scale,
                IsAntialias = true
            };

            canvas.DrawCircle(point, radius, paint);
            canvas.DrawCircle(point, radius, outline);
            
            if (Program.Config.UI.AlwaysShowMapLabels)
            {
                var font = SKFonts.UIRegular;
                float yOffset = radius + (2f * scale) + font.Size;
                
                // Line 1: Name
                canvas.DrawText(Name, new SKPoint(point.X, point.Y + yOffset), SKTextAlign.Center, font, SKPaints.TextMouseover);
                
                // Line 2: Type & Height
                float heightDiff = Position.Y - localPlayer.Position.Y;
                string distinctHeight = heightDiff > 0 ? $"+{heightDiff:F1}" : $"{heightDiff:F1}";
                string infoText = $"{Type} ({distinctHeight}m)";
                
                canvas.DrawText(infoText, new SKPoint(point.X, point.Y + yOffset + font.Spacing), SKTextAlign.Center, font, SKPaints.TextMouseover);
            }
        }

        public void DrawMouseover(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            using var lines = new PooledList<string>();
            lines.Add(Name);
            lines.Add($"Type: {Type.ToString()}");
            Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams).DrawMouseoverText(canvas, lines.Span);
        }
    }
}