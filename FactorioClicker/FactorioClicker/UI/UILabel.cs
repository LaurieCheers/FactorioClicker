using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace FactorioClicker.UI
{
    public enum UITextAlignment
    {
        LEFT,
        RIGHT,
        CENTER,
    };

    public enum UIVerticalAlignment
    {
        TOP,
        BOTTOM,
        CENTER,
    };

    class UILabel : UIElement
    {
        String text;
        String textWithLinebreaks;
        SpriteFont font;
        Vector2 position;
        int width;
        bool fixedWidth;
        Color color;
        UITextAlignment align;
        UIVerticalAlignment verticalAlign;
        JSCNCommand dynamicText;

        public UILabel(String aText, SpriteFont aFont, Vector2 aPosition, Color aColor, UITextAlignment aAlign, UIVerticalAlignment aVerticalAlign)
        {
            text = aText;
            font = aFont;
            position = aPosition;
            color = aColor;
            align = aAlign;
            verticalAlign = aVerticalAlign;
        }

        public void SetFixedWidth(int aWidth)
        {
            fixedWidth = true;
            width = aWidth;
        }

        public UILabel(JSONTable template): base(template)
        {
            text = template.getString("text", "");
            font = Game1.font;
            position = template.getArray("position").toVector2();
            if (template.hasKey("width"))
            {
                fixedWidth = true;
                width = template.getInt("width"); 
            }
            color = template.getString("color", "000000").toColor();
            align = template.getString("align", "left").toAlignment();
            verticalAlign = template.getString("verticalAlign", "top").toVerticalAlignment();

            String dynamicTextTemplate = template.getString("dynamicText", null);
            if (dynamicTextTemplate != null)
            {
                dynamicText = JSCNCommand.parse(dynamicTextTemplate);
            }
        }

        public override bool HandleInput(InputState inputState, JSCNContext context)
        {
            return false;
        }

        public override Rectangle GetBounds()
        {
            if (fixedWidth)
            {
                if (textWithLinebreaks == null)
                {
                    GenerateTextWithLinebreaks();
                }
                Vector2 measuredSize = font.MeasureString(textWithLinebreaks);
                return new Rectangle((int)position.X, (int)position.Y, (int)width, (int)measuredSize.Y);
            }
            else
            {
                Vector2 measuredSize = font.MeasureString(text);
                return new Rectangle((int)position.X, (int)position.Y, (int)measuredSize.X, (int)measuredSize.Y);
            }
        }

        public override void SetBounds(Rectangle rect)
        {
            position = rect.TopLeft();
            width = rect.Width;
            textWithLinebreaks = null;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (dynamicText != null)
            {
                spriteBatch.DrawStringJustified(align, verticalAlign, font, (string)dynamicText.Evaluate(Game1.instance.uiManager), position, color);
            }
            else
            {
                if (textWithLinebreaks == null)
                {
                    GenerateTextWithLinebreaks();
                }
                spriteBatch.DrawStringJustified(align, verticalAlign, font, textWithLinebreaks, position, color);
            }
        }

        void GenerateTextWithLinebreaks()
        {
            if (!fixedWidth)
            {
                textWithLinebreaks = text;
                return;
            }

            Vector2 measuredSize = font.MeasureString(text);
            if (width >= measuredSize.X)
            {
                textWithLinebreaks = text;
                return;
            }

            textWithLinebreaks = "";
            int lineStartIdx = 0;
            int lastWordEndIdx = 0;
            for (int Idx = 0; Idx <= text.Length; ++Idx)
            {
                if (Idx == text.Length || text[Idx] == ' ' || text[Idx] == '\t')
                {
                    bool needsBreak = false;
                    if(lineStartIdx < lastWordEndIdx)
                    {
                        Vector2 newLineSize = font.MeasureString(text.Substring(lineStartIdx, Idx-lineStartIdx));
                        if (newLineSize.X > width)
                        {
                            needsBreak = true;
                        }
                    }

                    if( needsBreak )
                    {
                        textWithLinebreaks += text.Substring(lineStartIdx, lastWordEndIdx - lineStartIdx) + "\n";
                        lineStartIdx = lastWordEndIdx + 1;
                        lastWordEndIdx = Idx;
                    }
                    else
                    {
                        // still fits on the line; don't need to write anything yet.
                        lastWordEndIdx = Idx;
                    }
                    
                    if (Idx == text.Length && lineStartIdx < Idx )
                    {
                        // the end of the string must fit on the line
                        textWithLinebreaks += text.Substring(lineStartIdx, Idx - lineStartIdx);
                    }
                }
                else if(text[Idx] == '\n')
                {
                    textWithLinebreaks += text.Substring(lineStartIdx, Idx-lineStartIdx) + "\n";
                    lineStartIdx = Idx + 1;
                    lastWordEndIdx = Idx + 1;
                }
            }
        }
    }
}
