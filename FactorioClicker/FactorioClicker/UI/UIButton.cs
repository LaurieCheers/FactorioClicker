using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using FactorioClicker.Graphics;

namespace FactorioClicker.UI
{
    class UIButton : UIElement
    {
        bool pressed;
        bool mouseOver;
        String title;
        Vector2 titleSize;
        Rectangle rect;
        LayeredImage image;
        LayeredImage pressedImage;
        LayeredImage mouseOverImage;
        static LayeredImage defaultImage;
        static LayeredImage defaultPressedImage;
        static LayeredImage defaultMouseOverImage;
        
        public delegate void OnClickDelegate();
        public OnClickDelegate onClickDelegate;
        public JSCNCommand onClickCommand;
        public JSCNCommand isActivatedCommand;

        static void initDefaultImages(ContentManager Content)
        {
            if (defaultImage == null)
            {
                JSONTable buttonTable = JSONTable.parse("{\"layers\":[{\"texture\":\"uibutton\", \"color\":\"F4F4F4\", \"draw\":\"stretched9grid\"}]}");
                defaultImage = new LayeredImage(buttonTable, Content);
            }

            if( defaultPressedImage == null)
            {
                JSONTable pressedTable = JSONTable.parse("{\"layers\":[{\"texture\":\"uibutton\", \"color\":\"DDDDDD\", \"draw\":\"stretched9grid\", \"rotation\":180}]}");
                defaultPressedImage = new LayeredImage(pressedTable, Content);
            }

            if (defaultMouseOverImage == null)
            {
                JSONTable mouseOverTable = JSONTable.parse("{\"layers\":[{\"texture\":\"uibutton\", \"color\":\"FFFFFF\", \"draw\":\"stretched9grid\"}]}");
                defaultMouseOverImage = new LayeredImage(mouseOverTable, Content);
            }
        }

        public void addIcon(LayeredImageLayer iconLayer)
        {
            image = new LayeredImage(new LayeredImageLayer_Image(image, Rotation90.None));
            image.Add(iconLayer);
            pressedImage = new LayeredImage(new LayeredImageLayer_Image(pressedImage, Rotation90.None));
            pressedImage.Add(iconLayer);
            mouseOverImage = new LayeredImage(new LayeredImageLayer_Image(mouseOverImage, Rotation90.None));
            mouseOverImage.Add(iconLayer);
        }

        public UIButton(Texture2D aTexture, Rectangle aRect, ContentManager Content)
        {
            initDefaultImages(Content);
            title = "";
            rect = aRect;
            LayeredImageLayer imageLayer = new LayeredImageLayer_Texture(aTexture, Color.White, "fitted", 0, Rotation90.None);
            addIcon(imageLayer);
        }

        public UIButton(JSONTable template, ContentManager Content): base(template)
        {
            initDefaultImages(Content);
            Vector2 pos = template.getArray("position", null).toVector2();
            Vector2 size = template.getArray("size", null).toVector2();
            rect = pos.makeRectangle(size);

            image = template.getLayeredImage("image", defaultImage, Content);
            pressedImage = template.getLayeredImage("pressedImage", defaultPressedImage, Content);
            mouseOverImage = template.getLayeredImage("mouseOverImage", defaultMouseOverImage, Content);            

            title = template.getString("text", "");
            titleSize = Game1.font.MeasureString(title);
            
            JSONTable iconTemplate = template.getJSON("icon", null);
            if (iconTemplate != null)
            {
                addIcon(new LayeredImageLayer_Image(new LayeredImage(iconTemplate, Content), Rotation90.None));
            }

            String actionTemplate = template.getString("action", null);
            if (actionTemplate != null)
            {
                onClickCommand = JSCNCommand.parse(actionTemplate);
            }

            String activatedTemplate = template.getString("isActivated", null);
            if (activatedTemplate != null)
            {
                isActivatedCommand = JSCNCommand.parse(activatedTemplate);
            }
        }

        public UIButton(String aTitle, Rectangle aRect, ContentManager Content)
        {
            initDefaultImages(Content);

            title = aTitle;
            rect = aRect;
            image = defaultImage;
            pressedImage = defaultPressedImage;
            mouseOverImage = defaultMouseOverImage;

            titleSize = Game1.font.MeasureString(title);
        }

        public override bool HandleInput(InputState inputState, JSCNContext context)
        {
            // FIXME: handle parent offset!
            if (!rect.Contains(inputState.MousePos))
            {
                pressed = false;
                mouseOver = false;
                return false;
            }

            mouseOver = true;

            if (inputState.WasMouseLeftJustPressed())
            {
                pressed = true;
            }
            else if (pressed && inputState.WasMouseLeftJustReleased())
            {
                pressed = false;
                Clicked(context);
            }

            return true;
        }

        protected virtual void Clicked(JSCNContext context)
        {
            if (onClickDelegate != null)
            {
                onClickDelegate();
            }
            else if( onClickCommand != null )
            {
                onClickCommand.Evaluate(context);
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (pressed || (isActivatedCommand != null && ((bool)isActivatedCommand.Evaluate(Game1.instance.uiManager)) == true))
            {
                pressedImage.Draw(spriteBatch, rect);
            }
            else if (mouseOver)
            {
                mouseOverImage.Draw(spriteBatch, rect);
            }
            else
            {
                image.Draw(spriteBatch, rect);
            }

            spriteBatch.DrawString(Game1.font, title, new Vector2(rect.X + (int)((rect.Width - titleSize.X) / 2), rect.Y + (int)((rect.Height - titleSize.Y) / 2)), Color.Black);
        }

        public override Rectangle GetBounds()
        {
            return rect;
        }

        public override void SetBounds(Rectangle aRect)
        {
            rect = aRect;
        }
    }
}
