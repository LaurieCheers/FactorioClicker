using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace FactorioClicker.UI
{
    public class UIScreen : UIContainer
    {
        public UIScreen()
        {
            isOpaque = false;
        }

        public UIScreen(bool aIsOpaque)
        {
            isOpaque = aIsOpaque;
        }

        public UIScreen(JSONTable template, ContentManager Content)
        {
            isOpaque = template.getBool("opaque", false);
            JSONArray elementTemplates = template.getArray("elements", JSONArray.empty);
            foreach (JSONTable elementTemplate in elementTemplates.asJSONTables())
            {
                Add(UIElement.newFromTemplate(elementTemplate, Content));
            }
        }

        public bool isOpaque { get; private set; }
    }
}
