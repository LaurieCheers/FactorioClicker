using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace FactorioClicker.Graphics
{
    interface IDrawMode
    {
        void Draw(SpriteBatch spriteBatch, Rectangle rect, Texture2D texture, Color color, Rotation90 rotation);
    }

    class DrawMode_Fixed : IDrawMode
    {
        public void Draw(SpriteBatch spriteBatch, Rectangle rect, Texture2D texture, Color color, Rotation90 rotation)
        {
            spriteBatch.Draw(texture, rect.TopLeft(), color);
        }
    }

    class DrawMode_Fitted : IDrawMode
    {
        public void Draw(SpriteBatch spriteBatch, Rectangle rect, Texture2D texture, Color color, Rotation90 rotation)
        {
            float textureAspect = texture.Width / (float)texture.Height;
            float rectAspect = rect.Width / (float)rect.Height;

            float scale;
            if (textureAspect > rectAspect)
            {
                // fit width
                scale = rect.Width / (float)texture.Width;
            }
            else
            {
                scale = rect.Height / (float)texture.Height;
            }

            Rectangle drawRect = new Rectangle((int)(rect.X + 0.5f*(rect.Width - texture.Width*scale)), (int)(rect.Y + 0.5f*(rect.Height - texture.Height*scale)), (int)(texture.Width*scale), (int)(texture.Height*scale));
            spriteBatch.Draw(texture, drawRect, color);
        }
    }

    class DrawMode_Stretched: IDrawMode
    {
        public void Draw(SpriteBatch spriteBatch, Rectangle rect, Texture2D texture, Color color, Rotation90 rotation)
        {
            float rot = 0.0f;
            int rotWidth = rect.Width;
            int rotHeight = rect.Height;
            if (rotation == Rotation90.None)
            {
                spriteBatch.Draw(texture, rect, color);
                return;
            }

            if (rotation == Rotation90.Rot90)
            {
                rot = (float)(Math.PI * 0.5);
                rotWidth = rect.Height;
                rotHeight = rect.Width;
            }
            else if (rotation == Rotation90.Rot180)
            {
                rot = (float)Math.PI;
            }
            else if (rotation == Rotation90.Rot270)
            {
                rot = (float)(Math.PI * 1.5);
                rotWidth = rect.Height;
                rotHeight = rect.Width;
            }

            int halfWidth = rect.Width / 2;
            int halfHeight = rect.Height / 2;

            Rectangle rotRect = new Rectangle((int)(rect.X + halfWidth), (int)(rect.Y + halfHeight), rotWidth, rotHeight);

            // origin would be <texture.Width/2, texture.Height/2>, if halfWidth and halfHeight weren't rounded
            Vector2 origin = new Vector2(texture.Width * (halfWidth / (float)rect.Width), texture.Height * (halfHeight/(float)rect.Height));

            spriteBatch.Draw(texture, rotRect, null, color, rot, origin, SpriteEffects.None, 0);
        }
    }
    class DrawMode_Tiled : IDrawMode
    {
        public void Draw(SpriteBatch spriteBatch, Rectangle rect, Texture2D texture, Color color, Rotation90 rotation)
        {
            for (int X = rect.X; X < rect.X + rect.Width; X += texture.Width)
            {
                for (int Y = rect.Y; Y < rect.Y + rect.Height; Y += texture.Height)
                {
                    spriteBatch.Draw(texture, new Vector2(X, Y), color);
                }
            }
        }
    }
    
    class DrawMode_Stretch9Grid : IDrawMode
    {
        public void Draw(SpriteBatch spriteBatch, Rectangle rect, Texture2D texture, Color color, Rotation90 rotation)
        {
            int fragmentW = texture.Width / 4;
            int fragmentH = texture.Height / 4;
            int rightEdgeX = rect.X + rect.Width - fragmentW;
            int bottomEdgeY = rect.Y + rect.Height - fragmentH;
            // TL, top, TR
            spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, fragmentW, fragmentH),
                new Rectangle(0,0, fragmentW, fragmentH), color);
            spriteBatch.Draw(texture, new Rectangle(rect.X+fragmentW, rect.Y, rect.Width-fragmentW*2, fragmentH),
                new Rectangle(fragmentW,0, fragmentW*2, fragmentH), color);
            spriteBatch.Draw(texture, new Rectangle(rightEdgeX, rect.Y, fragmentW, fragmentH),
                new Rectangle(fragmentW*3,0, fragmentW, fragmentH), color);

            // left, center, right
            spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y+fragmentH, fragmentW, rect.Height-fragmentH*2),
                new Rectangle(0,fragmentH, fragmentW, fragmentH*2), color);
            spriteBatch.Draw(texture, new Rectangle(rect.X+fragmentW, rect.Y+fragmentH, rect.Width-fragmentW*2, rect.Height-fragmentH*2),
                new Rectangle(fragmentW,fragmentH, fragmentW*2, fragmentH*2), color);
            spriteBatch.Draw(texture, new Rectangle(rightEdgeX, rect.Y+fragmentH, fragmentW, rect.Height-fragmentH*2),
                new Rectangle(fragmentW*3,fragmentH, fragmentW, fragmentH*2), color);

            // BL, bottom, BR
            spriteBatch.Draw(texture, new Rectangle(rect.X, bottomEdgeY, fragmentW, fragmentH),
                new Rectangle(0,fragmentH*3, fragmentW, fragmentH), color);
            spriteBatch.Draw(texture, new Rectangle(rect.X+fragmentW, bottomEdgeY, rect.Width-fragmentW*2, fragmentH),
                new Rectangle(fragmentW,fragmentH*3, fragmentW*2, fragmentH), color);
            spriteBatch.Draw(texture, new Rectangle(rightEdgeX, bottomEdgeY, fragmentW, fragmentH),
                new Rectangle(fragmentW*3,fragmentH*3, fragmentW, fragmentH), color);
        }
    }

    class DrawMode_Tiled9Grid : IDrawMode
    {
        public void Draw(SpriteBatch spriteBatch, Rectangle rect, Texture2D texture, Color color, Rotation90 rotation)
        {
            // man, this is fiddly
            int fragmentW = texture.Width / 4;
            int fragmentH = texture.Height / 4;
            int rightEdgeX = rect.X + rect.Width - fragmentW;
            int bottomEdgeY = rect.Y + rect.Height - fragmentH;
            int X;
            int Y = rect.Y + fragmentH;
            for (X = rect.X + fragmentW; X <= rect.X + rect.Width - fragmentW * 3; X += fragmentW * 2)
            {
                // top
                spriteBatch.Draw(texture, new Rectangle(X, rect.Y, fragmentW * 2, fragmentH),
                    new Rectangle(fragmentW, 0, fragmentW * 2, fragmentH), color);
                // middles
                for (Y = rect.Y + fragmentH; Y <= rect.Y + rect.Height - fragmentH*3; Y += fragmentH * 2)
                {
                    spriteBatch.Draw(texture, new Rectangle(X,Y,fragmentW*2, fragmentH*2),
                        new Rectangle(fragmentW, fragmentH, fragmentW*2, fragmentH*2), color);
                }
                // bottom gap-fill
                if (Y < bottomEdgeY)
                {
                    int fillY = bottomEdgeY - Y;
                    spriteBatch.Draw(texture, new Rectangle(X, Y, fragmentW * 2, fillY),
                        new Rectangle(fragmentW, fragmentH, fragmentW * 2, fillY), color);
                }
                // bottom
                spriteBatch.Draw(texture, new Rectangle(X, bottomEdgeY, fragmentW * 2, fragmentH),
                    new Rectangle(fragmentW, fragmentH*3, fragmentW * 2, fragmentH), color);
            }

            int finalX = X;
            int finalY = Y;
            int fillW = rightEdgeX - finalX;
            int fillH = bottomEdgeY - finalY;

            // bottom-right corner gap fill
            if (fillW > 0 && fillH > 0)
            {
                spriteBatch.Draw(texture, new Rectangle(finalX, finalY, fillW, fillH),
                    new Rectangle(fragmentW, fragmentH, fillW, fillH), color);
            }

            // edge gap fill
            if (fillW > 0)
            {
                // top
                spriteBatch.Draw(texture, new Rectangle(finalX, rect.Y, fillW, fragmentH),
                    new Rectangle(fragmentW, 0, fillW, fragmentH), color);
                // bottom
                spriteBatch.Draw(texture, new Rectangle(finalX, bottomEdgeY, fillW, fragmentH),
                    new Rectangle(fragmentW, fragmentH * 3, fillW, fragmentH), color);
            }
            if (fillH > 0)
            {
                // left
                spriteBatch.Draw(texture, new Rectangle(rect.X, finalY, fragmentW, fillH),
                    new Rectangle(0, fragmentH, fragmentW, fillH), color);
                // right 
                spriteBatch.Draw(texture, new Rectangle(rightEdgeX, finalY, fragmentW, fillH),
                    new Rectangle(fragmentW*3, fragmentH, fragmentW, fillH), color);
            }

            for (Y = rect.Y + fragmentH; Y <= rect.Y + rect.Height - fragmentH * 3; Y += fragmentH * 2)
            {
                // left
                spriteBatch.Draw(texture, new Rectangle(rect.X, Y, fragmentW, fragmentH * 2),
                    new Rectangle(0, fragmentH, fragmentW, fragmentH * 2), color);
                // right
                spriteBatch.Draw(texture, new Rectangle(rightEdgeX, Y, fragmentW, fragmentH * 2),
                    new Rectangle(fragmentW*3, fragmentH, fragmentW, fragmentH * 2), color);
                // right gap-fill
                spriteBatch.Draw(texture, new Rectangle(finalX, Y, fillW, fragmentH*2),
                    new Rectangle(fragmentW, fragmentH, fillW, fragmentH*2), color);
            }

            spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, fragmentW, fragmentH),
                new Rectangle(0, 0, fragmentW, fragmentH), color);
            spriteBatch.Draw(texture, new Rectangle(rightEdgeX, bottomEdgeY, fragmentW, fragmentH),
                new Rectangle(fragmentW*3, fragmentH*3, fragmentW, fragmentH), color);
            spriteBatch.Draw(texture, new Rectangle(rect.X, bottomEdgeY, fragmentW, fragmentH),
                new Rectangle(0, fragmentH*3, fragmentW, fragmentH), color);
            spriteBatch.Draw(texture, new Rectangle(rightEdgeX, rect.Y, fragmentW, fragmentH),
                new Rectangle(fragmentW*3, 0, fragmentW, fragmentH), color);
        }
    }

    public interface LayeredImageLayer
    {
        void Draw(SpriteBatch spriteBatch, Rectangle rect, Rotation90 rotation);
    }

    public class LayeredImageLayer_Texture: LayeredImageLayer
    {
        Texture2D texture;
        Color color;
        IDrawMode drawMode;
        int padding;
        Vector2 offset;
        Rotation90 rotation;
        bool modifiesRect;
        static Dictionary<String, IDrawMode> drawModes = new Dictionary<string, IDrawMode> {
            {"default", new DrawMode_Stretched()},
            {"stretched", new DrawMode_Stretched()},
            {"fixed", new DrawMode_Fixed()},
            {"fitted", new DrawMode_Fitted()},
            {"tiled", new DrawMode_Tiled()},
            {"tiled9grid", new DrawMode_Tiled9Grid()},
            {"stretched9grid", new DrawMode_Stretch9Grid()}
        };

        public LayeredImageLayer_Texture(Texture2D aTexture, Color aColor, String aDrawMode, int aPadding, Rotation90 aRotation)
        {
            texture = aTexture;
            color = aColor;
            drawMode = drawModes[aDrawMode];
            padding = aPadding;
            rotation = aRotation;
            offset = Vector2.Zero;

            modifiesRect = (padding != 0 || offset.X != 0 || offset.Y != 0);
        }

        public LayeredImageLayer_Texture(JSONTable template, ContentManager content)
        {
            texture = content.Load<Texture2D>(template.getString("texture", "white"));
            color = template.getString("color", "FFFFFF").toColor();
            drawMode = drawModes[template.getString("draw", "default")];
            padding = template.getInt("padding", 0);
            offset = template.getArray("offset", null).toVector2();
            rotation = template.getRotation("rotation", Rotation90.None);

            modifiesRect = (padding != 0 || offset.X != 0 || offset.Y != 0);
        }

        public void Draw(SpriteBatch spriteBatch, Rectangle rect, Rotation90 aRotation)
        {
            if (modifiesRect)
            {
                drawMode.Draw(spriteBatch, new Rectangle(rect.X + (int)offset.X - padding, rect.Y + (int)offset.Y - padding, rect.Width + padding * 2, rect.Height + padding * 2), texture, color, rotation.rotateBy(aRotation));
            }
            else
            {
                drawMode.Draw(spriteBatch, rect, texture, color, rotation.rotateBy(aRotation));
            }
        }
    }

    public class LayeredImageLayer_Image : LayeredImageLayer
    {
        LayeredImage image;
        Rotation90 rotation;

        public LayeredImageLayer_Image(LayeredImage aImage, Rotation90 aRotation)
        {
            image = aImage;
            rotation = aRotation;
        }

        public void Draw(SpriteBatch spriteBatch, Rectangle rect, Rotation90 aRotation)
        {
            image.Draw(spriteBatch, rect, rotation.rotateBy(aRotation));
        }
    }


    public class LayeredImage
    {
        List<LayeredImageLayer> layers;
        public LayeredImage()
        {
            layers = new List<LayeredImageLayer>();
        }

        public LayeredImage(LayeredImageLayer layer)
        {
            layers = new List<LayeredImageLayer>();
            layers.Add(layer);
        }

        public LayeredImage(JSONTable template, ContentManager content)
        {
            layers = new List<LayeredImageLayer>();

            JSONArray layerTemplate = template.getArray("layers", null);
            if (layerTemplate != null)
            {
                for (int Idx = 0; Idx < layerTemplate.Length; ++Idx)
                {
                    layers.Add(new LayeredImageLayer_Texture(layerTemplate.getJSON(Idx), content));
                }
            }
            else
            {
                layers.Add(new LayeredImageLayer_Texture(template, content));
            }
        }

        public void Add(LayeredImageLayer layer)
        {
            layers.Add(layer);
        }

        public void Add(LayeredImage image)
        {
            layers.Add(new LayeredImageLayer_Image(image, Rotation90.None));
        }

        public void Draw(SpriteBatch spriteBatch, Rectangle rect)
        {
            Draw(spriteBatch, rect, Rotation90.None);
        }

        public void Draw(SpriteBatch spriteBatch, Rectangle rect, Rotation90 rotation)
        {
            foreach(LayeredImageLayer curLayer in layers)
            {
                curLayer.Draw(spriteBatch, rect, rotation);
            }
        }
    }
}
