using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using FactorioClicker.Graphics;

namespace FactorioClicker.UI
{
    class UIBubble : UIContainer
    {
        UIElement target;
        LayeredImage background;
        LayeredImage tail;

        public UIBubble(UIElement aTarget, ContentManager Content) : base(null, 5)
        {
            target = aTarget;
            background = new LayeredImage(JSONTable.parse("{\"layers\":[{\"texture\":\"bubbleframe\", \"color\":\"FF0000\", \"draw\":\"stretched9grid\"}]}"), Content);
            tail = new LayeredImage(JSONTable.parse("{\"layers\":[{\"texture\":\"bubblearrow\", \"color\":\"FF0000\"}]}"), Content);
        }

        public override void Add(UIElement element)
        {
            base.Add(element);
            UpdatePosition();
        }

        public Vector2 GetAnchorPosition()
        {
            Rectangle targetBounds = target.GetBounds();
            return new Vector2(targetBounds.Center.X, targetBounds.Top);
        }

        public void UpdatePosition()
        {
            Vector2 anchor = GetAnchorPosition();
            Rectangle localBounds = GetBounds();

            Vector2 newOffset = new Vector2(anchor.X - localBounds.Width / 2, anchor.Y - localBounds.Height);
            SetBounds(new Rectangle((int)newOffset.X, (int)newOffset.Y, localBounds.Width, localBounds.Height));
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Rectangle bounds = GetBounds();
            background.Draw(spriteBatch, bounds);
            tail.Draw(spriteBatch, new Rectangle(bounds.X + bounds.Width/2 - 4, bounds.Y + bounds.Height - 4, 8, 8));
            base.Draw(spriteBatch);
        }
    }
}
