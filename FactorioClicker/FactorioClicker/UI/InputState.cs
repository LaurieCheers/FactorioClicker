using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;

namespace FactorioClicker.UI
{
    public class InputState
    {
        MouseState oldMouse;
        public MouseState mouse { get; private set; }
        KeyboardState oldKeyboard;
        public KeyboardState keyboard { get; private set; }
        public bool pauseMouse { get; private set; }

        public void Update()
        {
            oldKeyboard = keyboard;
            keyboard = Keyboard.GetState();
            if (WasKeyJustPressed(Keys.Space))
            {
                pauseMouse = !pauseMouse;
            }
            else if (IsKeyDown(Keys.Space) && pauseMouse && (WasMouseLeftJustPressed() || WasMouseRightJustPressed()))
            {
                // force an update if the user clicks
                mouse = Mouse.GetState();
            }

            if (pauseMouse)
            {
                mouse = oldMouse;
            }
            else
            {
                oldMouse = mouse;
                mouse = Mouse.GetState();
            }

            if (WasKeyJustPressed(Keys.Space))
            {
                int breakhere;
                breakhere = 1;
            }
        }

        public Vector2 MousePos { get { return new Vector2(mouse.X, mouse.Y); } }

        public bool WasMouseLeftJustPressed()
        {
            return mouse.LeftButton == ButtonState.Pressed && oldMouse.LeftButton == ButtonState.Released;
        }

        public bool WasMouseLeftJustReleased()
        {
            return mouse.LeftButton == ButtonState.Released && oldMouse.LeftButton == ButtonState.Pressed;
        }

        public bool WasMouseRightJustPressed()
        {
            return mouse.RightButton == ButtonState.Pressed && oldMouse.RightButton == ButtonState.Released;
        }

        public bool WasMouseRightJustReleased()
        {
            return mouse.RightButton == ButtonState.Released && oldMouse.RightButton == ButtonState.Pressed;
        }

        public bool WasKeyJustPressed(Keys key)
        {
            return keyboard.IsKeyDown(key) && !oldKeyboard.IsKeyDown(key);
        }

        public bool WasKeyJustReleased(Keys key)
        {
            return !keyboard.IsKeyDown(key) && oldKeyboard.IsKeyDown(key);
        }

        public bool IsKeyDown(Keys key)
        {
            return keyboard.IsKeyDown(key);
        }

        public bool IsKeyUp(Keys key)
        {
            return keyboard.IsKeyUp(key);
        }
    }
}
