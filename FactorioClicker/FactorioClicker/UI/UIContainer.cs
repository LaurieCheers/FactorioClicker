using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using FactorioClicker.Graphics;
using Microsoft.Xna.Framework.Content;

namespace FactorioClicker.UI
{
    class UIAnchor
    {
        Vector2 offset;
        Vector2 size;

        // X and Y range from 0 to 1, signifying where within the parent rectangle this point is anchored
        Vector2 parentAlignment;
        Vector2 internalAlignment;

        public UIAnchor(Vector2 aOffset, Vector2 aSize)
        {
            offset = aOffset;
            size = aSize;
            parentAlignment = Vector2.Zero;
            internalAlignment = Vector2.Zero;
        }

        public UIAnchor(Vector2 aOffset, Vector2 aSize, Vector2 aParentAlignment, Vector2 aInternalAlignment)
        {
            offset = aOffset;
            size = aSize;
            parentAlignment = aParentAlignment;
            internalAlignment = aInternalAlignment;
        }

        public UIAnchor(Vector2 aOffset, Vector2 aSize, Vector2 aAlignment)
        {
            offset = aOffset;
            size = aSize;
            parentAlignment = aAlignment;
            internalAlignment = aAlignment;
        }

        public static UIAnchor Default(Vector2 aSize)
        {
            return new UIAnchor(Vector2.Zero, aSize, new Vector2(0.0f, 0.0f));
        }
        public static UIAnchor TopLeftAligned(Vector2 aOffset, Vector2 aSize)
        {
            return new UIAnchor(aOffset, aSize, new Vector2(0.0f, 0.0f));
        }
        public static UIAnchor TopRightAligned(Vector2 aOffset, Vector2 aSize)
        {
            return new UIAnchor(aOffset, aSize, new Vector2(1.0f, 0.0f));
        }
        public static UIAnchor BottomLeftAligned(Vector2 aOffset, Vector2 aSize)
        {
            return new UIAnchor(aOffset, aSize, new Vector2(0.0f, 1.0f));
        }
        public static UIAnchor BottomRightAligned(Vector2 aOffset, Vector2 aSize)
        {
            return new UIAnchor(aOffset, aSize, new Vector2(1.0f, 1.0f));
        }

        public static UIAnchor LeftAligned(Vector2 aOffset, Vector2 aSize)
        {
            return new UIAnchor(aOffset, aSize, new Vector2(0.0f, 0.5f));
        }
        public static UIAnchor RightAligned(Vector2 aOffset, Vector2 aSize)
        {
            return new UIAnchor(aOffset, aSize, new Vector2(1.0f, 0.5f));
        }
        public static UIAnchor TopAligned(Vector2 aOffset, Vector2 aSize)
        {
            return new UIAnchor(aOffset, aSize, new Vector2(0.5f, 0.0f));
        }
        public static UIAnchor BottomAligned(Vector2 aOffset, Vector2 aSize)
        {
            return new UIAnchor(aOffset, aSize, new Vector2(0.5f, 1.0f));
        }

        public Vector2 GetPosition(Rectangle parentRect)
        {
            return parentRect.TopLeft() + parentRect.Size() * parentAlignment + offset;
        }

        public Rectangle GetBounds(Rectangle parentRect)
        {
            Vector2 basePosition = GetPosition(parentRect);
            return new Rectangle(
                (int)(basePosition.X - size.X * internalAlignment.X),
                (int)(basePosition.Y - size.Y * internalAlignment.Y),
                (int)size.X,
                (int)size.Y
            );
        }
    }

    struct UIAnchoredElement
    {
        public readonly UIElement element;
        public readonly UIAnchor anchor;

        public UIAnchoredElement(UIElement aElement)
        {
            element = aElement;
            anchor = UIAnchor.Default(element.GetBounds().Size());
        }

        public void UpdateAnchor(Rectangle parentRect)
        {
            element.SetBounds( anchor.GetBounds(parentRect) );
        }
    }

    public class UIContainer : UIElement, JSCNContext
    {
        LayeredImage image;
        List<UIAnchoredElement> elements;
        Rectangle bounds;
        Vector2 contentOffset;
        UIElement lastInputHandler;
        int padding;

        public UIContainer()
        {
            image = null;
            elements = new List<UIAnchoredElement>();
            contentOffset = Vector2.Zero;
        }

        public UIContainer(LayeredImage aImage, int aPadding)
        {
            image = aImage;
            padding = aPadding;
            elements = new List<UIAnchoredElement>();
            contentOffset = Vector2.Zero;
        }

        public UIContainer(JSONTable template, ContentManager Content) : base(template)
        {
            JSONTable imageTemplate = template.getJSON("image", null);
            if (imageTemplate != null)
            {
                image = new LayeredImage(imageTemplate, Content);
            }

            padding = template.getInt("padding", 0);
            elements = new List<UIAnchoredElement>();

            contentOffset = template.getArray("position", null).toVector2();

            Vector2 size = template.getArray("size", null).toVector2();
            bounds = contentOffset.makeRectangle(size);

            foreach (JSONTable elementTemplate in template.getArray("elements", JSONArray.empty).asJSONTables())
            {
                Add( UIElement.newFromTemplate(elementTemplate, Content) );
            }
        }

        public void ExpandToFit(UIElement e)
        {
            Rectangle eBounds = e.GetBounds().OffsetBy(contentOffset);
            e.SetBounds(eBounds);

            Rectangle paddedEBounds = eBounds.Expand(padding);
            Vector2 topLeft = new Vector2(contentOffset.X - padding, contentOffset.Y - padding);
            Rectangle addedBounds = new Rectangle((int)topLeft.X, (int)topLeft.Y, (int)(paddedEBounds.Right - topLeft.X), (int)(paddedEBounds.Bottom - topLeft.Y));

            bounds = bounds.Expand(addedBounds);
        }

        public virtual void Add(UIElement e)
        {
            ExpandToFit(e);
            elements.Add(new UIAnchoredElement(e));
        }

        void SetContentOffset(Vector2 newOffset)
        {
            Vector2 delta = newOffset - contentOffset;

            foreach (UIAnchoredElement anchored in elements)
            {
                Rectangle oldBounds = anchored.element.GetBounds();
                anchored.element.SetBounds(oldBounds.OffsetBy(delta));
            }

            contentOffset = newOffset;
        }

        public override bool HandleInput(InputState inputState, JSCNContext context)
        {
            if (inputState.mouse.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Released &&
                !inputState.WasMouseLeftJustReleased())
            {
                lastInputHandler = null;
            }

            if (lastInputHandler != null && lastInputHandler.HandleInput(inputState, context))
            {
                return true;
            }

            lastInputHandler = null;

            for (int Idx = elements.Count - 1; Idx >= 0; --Idx)
            {
                UIElement currentElement = elements[Idx].element;
                if (currentElement.HandleInput(inputState, context))
                {
                    lastInputHandler = currentElement;
                    return true;
                }
            }

            if (image != null)
            {
                if (bounds.Contains(inputState.MousePos))
                {
                    return true;
                }
            }

            return false;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (image != null)
            {
                image.Draw(spriteBatch, bounds);
            }

            foreach (UIAnchoredElement anchored in elements)
            {
                anchored.element.Draw(spriteBatch);
            }
        }

        public override Rectangle GetBounds()
        {
            return bounds;
        }

        public override void SetBounds(Rectangle newBounds)
        {
            SetContentOffset(newBounds.TopLeft() + new Vector2(padding, padding));
            bounds = newBounds;
        }

        public virtual JSCNContext GetNamedChild(string aName)
        {
            if (name == aName)
            {
                return this;
            }

            foreach (UIAnchoredElement anchor in elements)
            {
                UIElement element = anchor.element;
                if (element.name == aName)
                {
                    return element;
                }
                else
                {
                    if (element is UIContainer)
                    {
                        JSCNContext result = ((UIContainer)element).GetNamedChild(aName);
                        if (result != null)
                        {
                            return result;
                        }
                    }
                }
            }

            return null;
        }

        // JSCNContext api
        public override JSCNContext getElement(string aName)
        {
            return GetNamedChild(aName);
        }

        public override System.Object getProperty(string aName)
        {
            return null;
        }
    }
}
