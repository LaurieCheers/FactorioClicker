using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using FactorioClicker.Graphics;

namespace FactorioClicker.Simulation
{
    public class GridItemType
    {
        public String name;
        public String displayName;
        public LayeredImage image;
        public GridSize gridSize;
        public bool canRotate;

        public GridItemType(JSONTable template, ContentManager content):
            this(template.getString("name", null), template, content)
        {
        }

        public GridItemType(string aName, JSONTable template, ContentManager content)
        {
            name = aName;
            displayName = template.getString("displayName", "<anonymous>");
            image = new LayeredImage(template, content);
            gridSize = new GridSize(template.getArray("size", null), GridSize.Unit);
            canRotate = template.getBool("canRotate", false);
        }
    }

    public class GridItem
    {
        public GridItemType itemType;
        public Grid container;
        public GridPoint gridPosition;
        public Rotation90 rotation;
        public bool canMove;

        public GridSize gridSize { get { return itemType.gridSize.RotateBy(rotation); } }

        public GridItem(GridItemType aItemType, GridPoint aGridPosition, Rotation90 aRotation, bool aCanMove)
        {
            itemType = aItemType;
            gridPosition = aGridPosition;
            rotation = aRotation;
            canMove = aCanMove;
        }

        public GridItem(JSONTable template, ContentManager Content)
        {
            gridPosition = new GridPoint(template.getArray("position", null));
            itemType = new GridItemType(template, Content);
        }

        public GridItem(string name, JSONTable template, ContentManager Content)
        {
            gridPosition = new GridPoint(template.getArray("position", null));
            itemType = new GridItemType(name, template, Content);
        }

        public virtual void Draw(SpriteBatch spriteBatch, Rectangle rect)
        {
            itemType.image.Draw(spriteBatch, rect, rotation);
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 position, float scale)
        {
            Draw(spriteBatch, position.makeRectangle(new Vector2(gridSize.Width * scale, gridSize.Height * scale)));
        }

        public virtual GridItem Clone()
        {
            return new GridItem(itemType, gridPosition, rotation, canMove);
        }

        public virtual void PrepareToMove()
        {
        }

        public virtual void Delete()
        {
            container.Remove(this);
        }

        public void RotateTo(Rotation90 aRotation)
        {
            if (!itemType.canRotate)
                return;

            Grid g = container;
            g.Remove(this);
            rotation = aRotation;
            g.Add(this);
        }

        public GridPoint LocalToGlobalPosition(GridPoint offset)
        {
            return gridPosition + offset.RotateBy(rotation);
        }

        public GridPoint GlobalToLocalPosition(GridPoint position)
        {
            return (position - gridPosition).RotateBy(rotation.invert());
        }
    }
}
