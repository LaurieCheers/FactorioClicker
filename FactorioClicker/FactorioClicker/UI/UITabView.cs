using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;

namespace FactorioClicker.UI
{
    class UITabView: UIContainer
    {
        Dictionary<String, UIElement> tabs;
        public String selectedTabName;

        public UITabView(JSONTable template, ContentManager Content) : base(template, Content)
        {
            JSONTable tabsTemplate = template.getJSON("tabs");

            tabs = new Dictionary<string, UIElement>();
            foreach (String tabName in tabsTemplate.Keys)
            {
                tabs[tabName] = UIElement.newFromTemplate(tabsTemplate.getJSON(tabName), Content);
                ExpandToFit(tabs[tabName]);
            }

            selectedTabName = template.getString("selectedTab", tabsTemplate.Keys.First());
        }

        public override Rectangle GetBounds()
        {
            UIElement selectedTab = tabs[selectedTabName];
            if (selectedTab != null)
            {
                return base.GetBounds().Expand(selectedTab.GetBounds());
            }
            else
            {
                return base.GetBounds();
            }
        }

        public override bool HandleInput(InputState inputState, JSCNContext context)
        {
            UIElement selectedTab = tabs[selectedTabName];
            if (selectedTab != null)
            {
                bool tabHandledInput = selectedTab.HandleInput(inputState, context);
                if (tabHandledInput)
                {
                    return true;
                }
            }

            return base.HandleInput(inputState, context);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
            UIElement selectedTab = tabs[selectedTabName];
            if (selectedTab != null)
            {
                selectedTab.Draw(spriteBatch);
            }
        }
        
        public override System.Object getProperty(string aName)
        {
            if (aName == "select")
            {
                return new JSCNFunctionValue(FN_select);
            }
            else if (aName == "isSelected")
            {
                return new JSCNFunctionValue(FN_isSelected);
            }
            else
            {
                return GetNamedChild(aName);
            }
        }

        public System.Object FN_select(JSONArray parameters)
        {
            selectedTabName = parameters.getString(0);
            return null;
        }

        public System.Object FN_isSelected(JSONArray parameters)
        {
            return selectedTabName == parameters.getString(0);
        }
    }
}
