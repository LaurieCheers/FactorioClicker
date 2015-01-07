using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace FactorioClicker.UI
{
    public abstract class UIElement : JSCNContext
    {
        public readonly string name;
        public abstract bool HandleInput(InputState inputState, JSCNContext context);
        public abstract void Draw(SpriteBatch spriteBatch);
        public abstract Rectangle GetBounds();
        public abstract void SetBounds(Rectangle rect);

        public virtual JSCNContext getElement(String aName)
        {
            return null;
        }

        public virtual System.Object getProperty(String aName)
        {
            return null;
        }

        protected UIElement()
        {
        }

        protected UIElement(JSONTable template)
        {
            name = template.getString("id", null);
        }

        public static UIElement newFromTemplate(JSONTable template, ContentManager Content)
        {
            switch (template.getString("type"))
            {
                case "button": return new UIButton(template, Content);
                case "label": return new UILabel(template);
                case "image": return new UIImage(template, Content);
                case "container": return new UIContainer(template, Content);
                case "tabview": return new UITabView(template, Content);
                case "spacegrid": return new MapGridView(template, Content);
                case "grideditor": return new GridEditor_SpaceStation(template, Content);
                case "toolPalette": return new UIToolPalette(template, Content);
            }

            throw new ArgumentException("Invalid UIElement template of type '"+template.getString("type")+"'");
        }
    }
}
