using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace FactorioClicker.UI
{
    public class UIManager : JSCNContext
    {
        List<UIScreen> screens;
        List<UIScreen> visibleScreens;
        InputState inputState;
        Dictionary<String, JSCNContext> idElements;

        public UIManager()
        {
            screens = new List<UIScreen>();
            visibleScreens = new List<UIScreen>();
            inputState = new InputState();
            idElements = new Dictionary<String, JSCNContext>();
            idElements["ui"] = this;
        }

        public void PushScreen(UIScreen screen)
        {
            if (screen.isOpaque)
            {
                visibleScreens.Clear();
            }

            screens.Add(screen);
            visibleScreens.Add(screen);
        }

        public void PopScreen()
        {
            UIScreen last = screens.Last();
            screens.Remove(last);
            visibleScreens.Remove(last);

            if (last.isOpaque)
            {
                visibleScreens.Clear();
                int EarliestVisibleIdx = screens.Count - 1;
                while (EarliestVisibleIdx > 0 && !screens[EarliestVisibleIdx].isOpaque)
                {
                    EarliestVisibleIdx--;
                }
                for (int Idx = EarliestVisibleIdx; Idx < screens.Count; ++Idx)
                {
                    visibleScreens.Add(screens[Idx]);
                }
            }
        }

        public void Update()
        {
            inputState.Update();
            visibleScreens.Last().HandleInput(inputState, this);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (UIScreen screen in visibleScreens)
            {
                screen.Draw(spriteBatch);
            }

            if (inputState.pauseMouse)
            {
                spriteBatch.DrawString(Game1.font, "Mouse Frozen", new Vector2(300, 10), Color.White);
            }
        }

        public System.Object getProperty(string aName)
        {
            if (aName == "pushScreen")
            {
                return new JSCNFunctionValue(FN_pushScreen);
            }
            else if (aName == "popScreen")
            {
                return new JSCNFunctionValue(FN_popScreen);
            }
            else
            {
                throw new ArgumentException();
            }
        }

        System.Object FN_pushScreen(JSONArray parameters)
        {
            UIScreen screen = (UIScreen)parameters.getProperty(0);
            PushScreen(screen);
            return null;
        }

        System.Object FN_popScreen(JSONArray parameters)
        {
            PopScreen();
            return null;
        }

        public JSCNContext getElement(string aName)
        {
            if (idElements.ContainsKey(aName) )
            {
                return idElements[aName];
            }

            foreach (UIScreen screen in screens)
            {
                JSCNContext result = screen.getElement(aName);
                if (result != null)
                {
                    idElements[aName] = result; // cache for future reference
                    return result;
                }
            }

            return null;
        }
    }
}
