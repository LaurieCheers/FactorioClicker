using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using FactorioClicker.Graphics;

namespace FactorioClicker.UI
{
    class UIToolPalette : UIContainer
    {
        String contextName;
        MapGridView spaceGrid;

        public UIToolPalette(JSONTable template, ContentManager Content): base(template,Content)
        {
            contextName = template.getString("context");
        }

        public override bool HandleInput(InputState inputState, JSCNContext context)
        {
            if (spaceGrid == null)
            {
                JSCNContext contextElement = context.getElement(contextName);
                if (contextElement != null)
                {
                    spaceGrid = (MapGridView)contextElement;

                    Rectangle bounds = GetBounds();
                    int y = 10;
                    foreach (SpaceGridTool tool in spaceGrid.toolPalette.Values)
                    {
                        UIButton button = new UIButton("", new Rectangle(0, y, 32, 32), Game1.instance.Content);
                        button.addIcon(new LayeredImageLayer_Image(tool.buttonIcon, Rotation90.None));
                        button.onClickDelegate = tool.SelectTool;
                        Add(button);

                        UILabel label = new UILabel(tool.displayName, Game1.font, new Vector2(37, y + 16), Color.Black, UITextAlignment.LEFT, UIVerticalAlignment.CENTER);
                        label.SetFixedWidth(bounds.Width - 37);
                        Add(label);

                        y += 42;
                    }
                }
            }

            return base.HandleInput(inputState, context);
        }
    }
}
