using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using FactorioClicker.UI;
using FactorioClicker.Simulation;
using FactorioClicker.Graphics;
using Microsoft.Xna.Framework.Content;

namespace FactorioClicker
{
    public static class Extensions
    {
        public static Rectangle getAnchoredRect(this Rectangle rect, UIAnchorSide anchorSide, Vector2 size)
        {
            float X;
            float Y;
            switch (anchorSide)
            {
                case UIAnchorSide.LEFT:
                case UIAnchorSide.LEFT_INSIDE_TOP:
                case UIAnchorSide.LEFT_INSIDE_BOTTOM:
                    X = rect.Left - size.X; break;
                case UIAnchorSide.RIGHT:
                case UIAnchorSide.RIGHT_INSIDE_TOP:
                case UIAnchorSide.RIGHT_INSIDE_BOTTOM:
                    X = rect.Right; break;
                case UIAnchorSide.INSIDE_LEFT:
                case UIAnchorSide.TOP_INSIDE_LEFT:
                case UIAnchorSide.BOTTOM_INSIDE_LEFT:
                    X = rect.Left; break;
                case UIAnchorSide.INSIDE_RIGHT:
                case UIAnchorSide.TOP_INSIDE_RIGHT:
                case UIAnchorSide.BOTTOM_INSIDE_RIGHT:
                    X = rect.Right - size.X; break;
                default: // CENTER
                    X = rect.Center.X - size.X / 2; break;
            }
            switch (anchorSide)
            {
                case UIAnchorSide.BOTTOM:
                case UIAnchorSide.BOTTOM_INSIDE_LEFT:
                case UIAnchorSide.BOTTOM_INSIDE_RIGHT:
                    Y = rect.Bottom; break;
                case UIAnchorSide.TOP:
                case UIAnchorSide.TOP_INSIDE_LEFT:
                case UIAnchorSide.TOP_INSIDE_RIGHT:
                    Y = rect.Top - size.Y; break;
                case UIAnchorSide.INSIDE_TOP:
                case UIAnchorSide.LEFT_INSIDE_TOP:
                case UIAnchorSide.RIGHT_INSIDE_TOP:
                    Y = rect.Top; break;
                case UIAnchorSide.INSIDE_BOTTOM:
                case UIAnchorSide.LEFT_INSIDE_BOTTOM:
                case UIAnchorSide.RIGHT_INSIDE_BOTTOM:
                    Y = rect.Bottom - size.Y; break;
                default: //CENTER
                    Y = rect.Center.Y - size.Y / 2; break;
            }

            return new Vector2(X, Y).makeRectangle(size);
        }

        public static TerrainType toTerrainType(this String s)
        {
            if (s == null)
            {
                return TerrainType.Space;
            }
            else
            {
                TerrainType result;
                if (Enum.TryParse<TerrainType>(s, out result))
                {
                    return result;
                }
                else
                {
                    return TerrainType.Space;
                }
            }
        }

        public static LayeredImage getLayeredImage(this JSONTable template, String key, LayeredImage defaultImage, ContentManager Content)
        {
            if (template.hasKey(key))
            {
                return new LayeredImage(template.getJSON(key), Content);
            }
            else
            {
                return defaultImage;
            }
        }

        public static UITextAlignment toAlignment(this String str)
        {
            if (str == "center")
                return UITextAlignment.CENTER;
            else if (str == "right")
                return UITextAlignment.RIGHT;
            else
                return UITextAlignment.LEFT;
        }

        public static UIVerticalAlignment toVerticalAlignment(this String str)
        {
            if (str == "center")
                return UIVerticalAlignment.CENTER;
            else if (str == "bottom")
                return UIVerticalAlignment.BOTTOM;
            else
                return UIVerticalAlignment.TOP;
        }

        public static Rectangle makeRectangle(this Vector2 position, float width, float height)
        {
            return new Rectangle((int)position.X, (int)position.Y, (int)width, (int)height);
        }

        public static Rectangle makeRectangle(this Vector2 position, Vector2 size)
        {
            return new Rectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y);
        }

        public static GridPoint toGridPoint(this JSONArray array)
        {
            if (array == null)
                return GridPoint.Zero;
            else
                return new GridPoint(array.getInt(0), array.getInt(1));
        }

        public static Vector2 toVector2(this JSONArray array)
        {
            if (array == null)
                return Vector2.Zero;
            else
                return new Vector2(array.getFloat(0), array.getFloat(1));
        }

        public static int toInt(this Rotation90 rot)
        {
            switch (rot)
            {
                case Rotation90.Rot90: return 90;
                case Rotation90.Rot180: return 180;
                case Rotation90.Rot270: return 270;
                default: return 0;
            }
        }

        public static Rotation90 getRotation(this JSONTable table, string name, Rotation90 defaultValue)
        {
            int angle = table.getInt(name, defaultValue.toInt());
            return (Rotation90)(angle/90);
        }

        public static Rotation90 rotateBy(this Rotation90 rotation, Rotation90 other)
        {
            int newRotation = (rotation.toInt() + other.toInt()) % 360;
            return (Rotation90)(newRotation / 90);
        }

        public static Rotation90 invert(this Rotation90 rotation)
        {
            int newRotation = 360 - rotation.toInt();
            return (Rotation90)(newRotation / 90);
        }

        public static Rectangle OffsetBy(this Rectangle rect, Vector2 offset)
        {
            return new Rectangle((int)(rect.X + offset.X), (int)(rect.Y + offset.Y), rect.Width, rect.Height);
        }

        public static Vector2 Size(this Texture2D texture)
        {
            return new Vector2(texture.Width, texture.Height);
        }

        public static Rectangle Expand(this Rectangle a, int padding)
        {
            return new Rectangle(a.X - padding, a.Y - padding, a.Width + padding * 2, a.Height + padding * 2);
        }

        public static Rectangle Expand(this Rectangle a, Rectangle b)
        {
            Rectangle result = a;
            if (b.X < result.X)
            {
                result.Width += (result.X - b.X);
                result.X = b.X;
            }
            if (b.Y < result.Y)
            {
                result.Height += (result.Y - b.Y);
                result.Y = b.Y;
            }
            if (b.Right > result.Right)
            {
                result.Width += b.Right - result.Right;
            }
            if (b.Bottom > result.Bottom)
            {
                result.Height += b.Bottom - result.Bottom;
            }
            return result;
        }

        public static bool Contains(this Rectangle rect, Vector2 pos)
        {
            return rect.X <= pos.X && rect.Y <= pos.Y && rect.X + rect.Width >= pos.X && rect.Y + rect.Height >= pos.Y;
        }

        public static Vector2 TopLeft(this Rectangle rect)
        {
            return new Vector2(rect.X, rect.Y);
        }

        public static Vector2 Size(this Rectangle rect)
        {
            return new Vector2(rect.Width, rect.Height);
        }

        public static Rotation90 Next(this Rotation90 rotation)
        {
            switch (rotation)
            {
                case Rotation90.None: return Rotation90.Rot90;
                case Rotation90.Rot90: return Rotation90.Rot180;
                case Rotation90.Rot180: return Rotation90.Rot270;
                default: return Rotation90.None;
            }
        }

        public static Rotation90 Prev(this Rotation90 rotation)
        {
            switch (rotation)
            {
                case Rotation90.None: return Rotation90.Rot270;
                case Rotation90.Rot270: return Rotation90.Rot180;
                case Rotation90.Rot180: return Rotation90.Rot90;
                default: return Rotation90.None;
            }
        }

        public static void DrawStringJustified(this SpriteBatch spriteBatch, UITextAlignment justify, SpriteFont font, String text, Vector2 anchor, Color color)
        {
            spriteBatch.DrawStringJustified(justify, UIVerticalAlignment.TOP, font, text, anchor, color);
        }

        public static void DrawStringJustified(this SpriteBatch spriteBatch, UITextAlignment justify, UIVerticalAlignment verticalAlign, SpriteFont font, String text, Vector2 anchor, Color color)
        {
            Vector2 textSize = font.MeasureString(text);
            float leftX;
            float topY;
            switch (justify)
            {
                case UITextAlignment.RIGHT: leftX = anchor.X - textSize.X; break;
                case UITextAlignment.CENTER: leftX = anchor.X - (textSize.X / 2); break;
                default: leftX = anchor.X; break;
            }
            switch (verticalAlign)
            {
                case UIVerticalAlignment.BOTTOM: topY = anchor.Y - textSize.Y; break;
                case UIVerticalAlignment.CENTER: topY = anchor.Y - (textSize.Y / 2); break;
                default: topY = anchor.Y; break;
            }

            spriteBatch.DrawString(font, text, new Vector2(leftX, topY), color);
        }

        public static Vector2 ToVector2(this Point p)
        {
            return new Vector2(p.X, p.Y);
        }

        public static bool IsShiftPressed(this KeyboardState keyboardState)
        {
            return keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);
        }

        public static int hexToInt(this String str)
        {
            int result = 0;
            foreach(char c in str)
            {
                if (c >= 'a' && c <= 'f')
                {
                    result = (c - 'a') + 10 + result * 16;
                }
                else if (c >= 'A' && c <= 'F')
                {
                    result = (c - 'A') + 10 + result * 16;
                }
                else if (c >= '0' && c <= '9')
                {
                    result = (c - '0') + result * 16;
                }
                else
                {
                    return 0;
                }
            }
            return result;
        }

        public static Color toColor(this String str)
        {
            if (str.Length == 6)
            {
                return new Color(str.Substring(0, 2).hexToInt(), str.Substring(2, 2).hexToInt(), str.Substring(4, 2).hexToInt());
            }
            else if (str.Length == 8)
            {
                return new Color(str.Substring(0, 2).hexToInt(), str.Substring(2, 2).hexToInt(), str.Substring(4, 2).hexToInt(), str.Substring(6, 2).hexToInt());
            }
            return Color.White;
        }
    }
}
