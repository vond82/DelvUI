using Dalamud.Interface;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using DelvUI.Config;
using DelvUI.Enums;
using DelvUI.Interface.GeneralElements;
using ImGuiNET;
using ImGuiScene;
using Lumina.Excel;
using System;
using System.Numerics;

namespace DelvUI.Helpers
{
    public enum GradientDirection
    {
        None,
        Right,
        Left,
        Up,
        Down,
        CenteredHorizonal
    }

    public static class DrawHelper
    {
        private static uint[] ColorArray(PluginConfigColor color, GradientDirection gradientDirection)
        {
            return gradientDirection switch
            {
                GradientDirection.None => new[] { color.Base, color.Base, color.Base, color.Base },
                GradientDirection.Right => new[] { color.TopGradient, color.BottomGradient, color.BottomGradient, color.TopGradient },
                GradientDirection.Left => new[] { color.BottomGradient, color.TopGradient, color.TopGradient, color.BottomGradient },
                GradientDirection.Up => new[] { color.BottomGradient, color.BottomGradient, color.TopGradient, color.TopGradient },
                _ => new[] { color.TopGradient, color.TopGradient, color.BottomGradient, color.BottomGradient }
            };
        }
        
        private static Vector2 GetBarTextureUV1Vector(Vector2 size, int textureWidth, int textureHeight, BarTextureDrawMode drawMode)
        {
            if (drawMode == BarTextureDrawMode.Stretch) { return new Vector2(1); }

            float x = drawMode == BarTextureDrawMode.RepeatVertical ? 1 : (float)size.X / textureWidth;
            float y = drawMode == BarTextureDrawMode.RepeatHorizontal ? 1 : (float)size.Y / textureHeight;

            return new Vector2(x, y);
        }

        public static void DrawBarTexture(Vector2 position, Vector2 size, PluginConfigColor color, string? name, BarTextureDrawMode drawMode, ImDrawListPtr drawList)
        {
            IDalamudTextureWrap? texture = BarTexturesManager.Instance?.GetBarTexture(name);
            if (texture == null)
            {
                DrawGradientFilledRect(position, size, color, drawList);
                return;
            }

            Vector2 uv0 = new Vector2(0);
            Vector2 uv1 = GetBarTextureUV1Vector(size, texture.Width, texture.Height, drawMode);

            drawList.AddImage(texture.ImGuiHandle, position, position + size, uv0, uv1, color.Base);
        }

        public static void DrawGradientFilledRect(Vector2 position, Vector2 size, PluginConfigColor color, ImDrawListPtr drawList)
        {
            GradientDirection gradientDirection = ConfigurationManager.Instance.GradientDirection;
            DrawGradientFilledRect(position, size, color, drawList, gradientDirection);
        }

        public static void DrawGradientFilledRect(Vector2 position, Vector2 size, PluginConfigColor color, ImDrawListPtr drawList, GradientDirection gradientDirection = GradientDirection.Down)
        {
            uint[]? colorArray = ColorArray(color, gradientDirection);

            if (gradientDirection == GradientDirection.CenteredHorizonal)
            {
                Vector2 halfSize = new(size.X, size.Y / 2f);
                drawList.AddRectFilledMultiColor(
                    position, position + halfSize,
                    colorArray[0], colorArray[1], colorArray[2], colorArray[3]
                );

                Vector2 pos = position + new Vector2(0, halfSize.Y);
                drawList.AddRectFilledMultiColor(
                    pos, pos + halfSize,
                    colorArray[3], colorArray[2], colorArray[1], colorArray[0]
                );
            }
            else
            {
                drawList.AddRectFilledMultiColor(
                    position, position + size,
                    colorArray[0], colorArray[1], colorArray[2], colorArray[3]
                );
            }
        }

        public static void DrawOutlinedText(string text, Vector2 pos, ImDrawListPtr drawList, int thickness = 1)
        {
            DrawOutlinedText(text, pos, 0xFFFFFFFF, 0xFF000000, drawList, thickness);
        }

        public static void DrawOutlinedText(string text, Vector2 pos, uint color, uint outlineColor, ImDrawListPtr drawList, int thickness = 1)
        {
            // outline
            for (int i = 1; i < thickness + 1; i++)
            {
                drawList.AddText(new Vector2(pos.X - i, pos.Y + i), outlineColor, text);
                drawList.AddText(new Vector2(pos.X, pos.Y + i), outlineColor, text);
                drawList.AddText(new Vector2(pos.X + i, pos.Y + i), outlineColor, text);
                drawList.AddText(new Vector2(pos.X - i, pos.Y), outlineColor, text);
                drawList.AddText(new Vector2(pos.X + i, pos.Y), outlineColor, text);
                drawList.AddText(new Vector2(pos.X - i, pos.Y - i), outlineColor, text);
                drawList.AddText(new Vector2(pos.X, pos.Y - i), outlineColor, text);
                drawList.AddText(new Vector2(pos.X + i, pos.Y - i), outlineColor, text);
            }

            // text
            drawList.AddText(new Vector2(pos.X, pos.Y), color, text);
        }

        public static void DrawShadowText(string text, Vector2 pos, uint color, uint shadowColor, ImDrawListPtr drawList, int offset = 1, int thickness = 1)
        {
            // TODO: Add parameter to allow to choose a direction

            // Shadow
            for (int i = 0; i < thickness; i++)
            {
                drawList.AddText(new Vector2(pos.X + i + offset, pos.Y  + i + offset), shadowColor, text);
            }

            // Text
            drawList.AddText(new Vector2(pos.X, pos.Y), color, text);
        }

        public static void DrawIcon<T>(dynamic row, Vector2 position, Vector2 size, bool drawBorder, bool cropIcon, int stackCount = 1) where T : ExcelRow
        {
            IDalamudTextureWrap texture = TexturesHelper.GetTexture<T>(row, (uint)Math.Max(0, stackCount - 1));
            if (texture == null) { return; }

            (Vector2 uv0, Vector2 uv1) = GetTexCoordinates(texture, size, cropIcon);

            ImGui.SetCursorPos(position);
            ImGui.Image(texture.ImGuiHandle, size, uv0, uv1);

            if (drawBorder)
            {
                ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                drawList.AddRect(position, position + size, 0xFF000000);
            }
        }

        public static void DrawIcon<T>(ImDrawListPtr drawList, dynamic row, Vector2 position, Vector2 size, bool drawBorder, bool cropIcon, int stackCount = 1) where T : ExcelRow
        {
            IDalamudTextureWrap texture = TexturesHelper.GetTexture<T>(row, (uint)Math.Max(0, stackCount - 1));
            if (texture == null) { return; }

            (Vector2 uv0, Vector2 uv1) = GetTexCoordinates(texture, size, cropIcon);

            drawList.AddImage(texture.ImGuiHandle, position, position + size, uv0, uv1);

            if (drawBorder)
            {
                drawList.AddRect(position, position + size, 0xFF000000);
            }
        }

        public static void DrawIcon(uint iconId, Vector2 position, Vector2 size, bool drawBorder, ImDrawListPtr drawList)
        {
            DrawIcon(iconId, position, size, drawBorder, 0xFFFFFFFF, drawList);
        }

        public static void DrawIcon(uint iconId, Vector2 position, Vector2 size, bool drawBorder, float alpha, ImDrawListPtr drawList)
        {
            uint a = (uint)(alpha * 255);
            uint color = 0xFFFFFF + (a << 24);
            DrawIcon(iconId, position, size, drawBorder, color, drawList);
        }


        public static void DrawIcon(uint iconId, Vector2 position, Vector2 size, bool drawBorder, uint color, ImDrawListPtr drawList)
        {
            IDalamudTextureWrap? texture = TexturesHelper.GetTextureFromIconId(iconId);
            if (texture == null) { return; }

            drawList.AddImage(texture.ImGuiHandle, position, position + size, Vector2.Zero, Vector2.One, color);

            if (drawBorder)
            {
                drawList.AddRect(position, position + size, 0xFF000000);
            }
        }

        public static (Vector2, Vector2) GetTexCoordinates(IDalamudTextureWrap texture, Vector2 size, bool cropIcon = true)
        {
            if (texture == null)
            {
                return (Vector2.Zero, Vector2.Zero);
            }

            // Status = 24x32, show from 2,7 until 22,26
            //show from 0,0 until 24,32 for uncropped status icon

            float uv0x = cropIcon ? 4f : 1f;
            float uv0y = cropIcon ? 14f : 1f;

            float uv1x = cropIcon ? 4f : 1f;
            float uv1y = cropIcon ? 12f : 1f;

            Vector2 uv0 = new(uv0x / texture.Width, uv0y / texture.Height);
            Vector2 uv1 = new(1f - uv1x / texture.Width, 1f - uv1y / texture.Height);

            return (uv0, uv1);
        }

        public static void DrawIconCooldown(Vector2 position, Vector2 size, float elapsed, float total, ImDrawListPtr drawList)
        {
            float completion = elapsed / total;
            int segments = (int)Math.Ceiling(completion * 4);

            Vector2 center = position + size / 2;
            
            //Define vertices for top, left, bottom, and right points relative to the center.
            Vector2[] vertices =
            [
                center with {Y = center.Y - size.Y}, // Top
                center with {X = center.X - size.X}, // Left
                center with {Y = center.Y + size.Y}, // Bottom
                center with {X = center.X + size.X}  // Right
            ];
            
            ImGui.PushClipRect(position, position + size, false);
            for (int i = 0; i < segments; i++)
            {
                Vector2 v2 = vertices[i % 4];
                Vector2 v3 = vertices[(i + 1) % 4];
                
                
                if (i == segments - 1)
                {   // If drawing the last segment, adjust the second vertex based on the cooldown.
                    float angle = 2 * MathF.PI * (1 - completion);
                    float cos = MathF.Cos(angle);
                    float sin = MathF.Sin(angle);

                    v3 = center + Vector2.Multiply(new Vector2(sin,-cos), size);
                }

                drawList.AddTriangleFilled(center, v3, v2, 0xCC000000);
            }
            ImGui.PopClipRect();
        }
        
        public static void DrawOvershield(float shield, Vector2 cursorPos, Vector2 barSize, float height, bool useRatioForHeight, PluginConfigColor color, ImDrawListPtr drawList)
        {
            if (shield == 0) { return; }

            float h = useRatioForHeight ? barSize.Y / 100 * height : height;

            DrawGradientFilledRect(cursorPos, new Vector2(Math.Max(1, barSize.X * shield), h), color, drawList);
        }

        public static void DrawShield(float shield, float hp, Vector2 cursorPos, Vector2 barSize, float height, bool useRatioForHeight, PluginConfigColor color, ImDrawListPtr drawList)
        {
            if (shield == 0) { return; }

            // on full hp just draw overshield
            if (hp == 1)
            {
                DrawOvershield(shield, cursorPos, barSize, height, useRatioForHeight, color, drawList);
                return;
            }

            // hp portion
            float h = useRatioForHeight ? barSize.Y / 100 * Math.Min(100, height) : height;
            float missingHPRatio = 1 - hp;
            float s = Math.Min(shield, missingHPRatio);
            Vector2 shieldStartPos = cursorPos + new Vector2(Math.Max(1, barSize.X * hp), 0);
            DrawGradientFilledRect(shieldStartPos, new Vector2(Math.Max(1, barSize.X * s), barSize.Y), color, drawList);

            // overshield
            shield -= s;
            if (shield <= 0) { return; }

            DrawGradientFilledRect(cursorPos, new Vector2(Math.Max(1, barSize.X * shield), h), color, drawList);
        }

        public static void DrawInWindow(string name, Vector2 pos, Vector2 size, Action<ImDrawListPtr> drawAction)
        {
            bool needsInput = InputsHelper.Instance?.IsProxyEnabled == true ? false : true;
            DrawInWindow(name, pos, size, needsInput, drawAction);
        }

        public static void DrawInWindow(string name, Vector2 pos, Vector2 size, bool needsInput, Action<ImDrawListPtr> drawAction)
        {
            const ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoTitleBar |
                                                 ImGuiWindowFlags.NoScrollbar |
                                                 ImGuiWindowFlags.NoBackground |
                                                 ImGuiWindowFlags.NoMove |
                                                 ImGuiWindowFlags.NoResize;

            DrawInWindow(name, pos, size, needsInput, false, windowFlags, drawAction);
        }

        public static void DrawInWindow(
            string name,
            Vector2 pos,
            Vector2 size,
            bool needsInput,
            bool needsWindow,
            ImGuiWindowFlags windowFlags,
            Action<ImDrawListPtr> drawAction)
        {

            if (!ClipRectsHelper.Instance.Enabled || ClipRectsHelper.Instance.Mode == WindowClippingMode.Performance)
            {
                drawAction(ImGui.GetWindowDrawList());
                return;
            }

            windowFlags |= ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoBringToFrontOnFocus;

            if (!needsInput)
            {
                windowFlags |= ImGuiWindowFlags.NoInputs;
            }

            ClipRect? clipRect = ClipRectsHelper.Instance.GetClipRectForArea(pos, size);

            // no clipping needed
            if (!ClipRectsHelper.Instance.Enabled || !clipRect.HasValue)
            {
                ImDrawListPtr drawList = ImGui.GetWindowDrawList();

                if (!needsInput && !needsWindow)
                {
                    drawAction(drawList);
                    return;
                }

                ImGui.SetNextWindowPos(pos);
                ImGui.SetNextWindowSize(size);

                bool begin = ImGui.Begin(name, windowFlags);
                if (!begin)
                {
                    ImGui.End();
                    return;
                }

                drawAction(drawList);

                ImGui.End();
            }

            // clip around game's window
            else
            {
                // hide instead of clip?
                if (ClipRectsHelper.Instance.Mode == WindowClippingMode.Hide) { return; }

                ImGuiWindowFlags flags = windowFlags;
                if (needsInput && clipRect.Value.Contains(ImGui.GetMousePos()))
                {
                    flags |= ImGuiWindowFlags.NoInputs;
                }

                ClipRect[] invertedClipRects = ClipRectsHelper.GetInvertedClipRects(clipRect.Value);
                for (int i = 0; i < invertedClipRects.Length; i++)
                {
                    ImGui.SetNextWindowPos(pos);
                    ImGui.SetNextWindowSize(size);
                    ImGuiHelpers.ForceNextWindowMainViewport();

                    bool begin = ImGui.Begin(name + "_" + i, flags);
                    if (!begin)
                    {
                        ImGui.End();
                        continue;
                    }

                    ImGui.PushClipRect(invertedClipRects[i].Min, invertedClipRects[i].Max, false);
                    drawAction(ImGui.GetWindowDrawList());
                    ImGui.PopClipRect();

                    ImGui.End();
                }
            }
        }
    }
}
