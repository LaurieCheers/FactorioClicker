using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FactorioClicker.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FactorioClicker.UI
{
    class UIImage : UIElement
    {
        LayeredImage image;
        Rectangle rect;

        public UIImage(JSONTable template, ContentManager Content)
            : base(template)
        {
            image = new LayeredImage(template.getJSON("image"), Content);
            Vector2 pos = template.getArray("position").toVector2();
            Vector2 size = template.getArray("size").toVector2();
            rect = new Rectangle((int)pos.X, (int)pos.Y, (int)size.X, (int)size.Y);
        }

        public override bool HandleInput(InputState inputState, JSCNContext context)
        {
            return false;
        }

        public override Rectangle GetBounds()
        {
            return rect;
        }

        public override void SetBounds(Rectangle aRect)
        {
            rect = aRect;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            image.Draw(spriteBatch, rect);
        }
    }
}
