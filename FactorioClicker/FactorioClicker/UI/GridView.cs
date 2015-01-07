using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FactorioClicker.Simulation;
using FactorioClicker.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace FactorioClicker.UI
{
    public class GridView : UIElement
    {
        public Grid grid;
        LayeredImage bgImage;
        public Vector2 bgPadding;
        public float scale;
        public Vector2 origin;
        bool resourcesUnder;

        public GridView(Grid aGrid, JSONTable template, ContentManager Content): base(template)
        {
            grid = aGrid;
            origin = template.getArray("position", null).toVector2();
            bgPadding = template.getArray("backgroundPadding", null).toVector2();
            scale = template.getInt("scale", 32);
            resourcesUnder = template.getBool("resourcesUnder", false);

            JSONTable gridTemplate = template.getJSON("background", null);
            if (gridTemplate != null)
            {
                bgImage = new LayeredImage(gridTemplate, Content);
            }
        }

        public override bool HandleInput(InputState inputState, JSCNContext context)
        {
            return false;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (bgImage != null)
            {
                Rectangle rect = gridToScreenRect(GridPoint.Zero, grid.size);
                bgImage.Draw(spriteBatch, (rect.TopLeft()-bgPadding).makeRectangle(rect.Size()+bgPadding*2));
/*                for (int X = 0; X < grid.size.Width; X++)
                {
                    for (int Y = 0; Y < grid.size.Width; Y++)
                    {
                        DrawImage(spriteBatch, gridImage, new GridPoint(X, Y), GridSize.Unit);
                    }
                }*/
            }
            DrawGrid(grid, spriteBatch);
        }

        public void DrawGrid(Grid aGrid, SpriteBatch spriteBatch)
        {
            if (resourcesUnder)
            {
                DrawResources(aGrid, spriteBatch);
            }

            foreach (GridItem item in aGrid.items)
            {
                DrawItem(spriteBatch, item);
            }

            if (!resourcesUnder)
            {
                DrawResources(aGrid, spriteBatch);
            }
        }

        public void DrawResources(Grid aGrid, SpriteBatch spriteBatch)
        {
            if (aGrid.resources != null)
            {
                for (int X = 0; X < aGrid.size.Width; ++X)
                {
                    for (int Y = 0; Y < aGrid.size.Height; ++Y)
                    {
                        GridPoint pos = new GridPoint(X, Y);
                        aGrid.resources.DrawCell(pos, gridToScreenRect(pos, GridSize.Unit), spriteBatch);
                    }
                }
            }
        }

        public GridItem ItemAtScreenPos(Vector2 screenPos)
        {
            return grid.ItemAtGridPos(screenToGridPos(screenPos));
        }

        public void DrawItem(SpriteBatch spriteBatch, GridItem item)
        {
            item.Draw(spriteBatch, gridToScreenRect(item.gridPosition, item.gridSize));
        }

        public void DrawImage(SpriteBatch spriteBatch, LayeredImage image, GridPoint origin, GridSize size)
        {
            image.Draw(spriteBatch, gridToScreenRect(origin, size), Rotation90.None);
        }

        public void DrawItemOffset(SpriteBatch spriteBatch, GridItem item, GridPoint gridOffset, Vector2 screenOffset)
        {
            Vector2 pos = gridToScreenPos(item.gridPosition + gridOffset) + screenOffset;
            Vector2 size = gridToScreenSize(item.gridSize);
            item.Draw(spriteBatch, new Rectangle((int)pos.X, (int)pos.Y, (int)size.X, (int)size.Y));
        }

        public Rectangle gridToScreenRect(GridPoint gridPos, GridSize gridSize)
        {
            Vector2 pos = gridToScreenPos(gridPos);
            Vector2 size = gridToScreenSize(gridSize);
            return new Rectangle((int)pos.X, (int)pos.Y, (int)size.X, (int)size.Y);
        }

        public Vector2 gridToScreenPos(GridPoint gridPos)
        {
            if (grid != null)
            {
                return new Vector2(origin.X + (grid.offset.X + gridPos.X) * scale, origin.Y + (grid.offset.Y + gridPos.Y) * scale);
            }
            else
            {
                return new Vector2(origin.X + gridPos.X * scale, origin.Y + gridPos.Y * scale);
            }
        }

        public Vector2 gridToScreenPos(Vector2 gridPos)
        {
            if (grid != null)
            {
                return new Vector2(origin.X + (grid.offset.X + gridPos.X) * scale, origin.Y + (grid.offset.Y + gridPos.Y) * scale);
            }
            else
            {
                return new Vector2(origin.X + gridPos.X * scale, origin.Y + gridPos.Y * scale);
            }
        }

        public GridPoint screenToGridPos(Vector2 screenPos)
        {
            return new GridPoint((int)((screenPos.X - origin.X) / scale) - grid.offset.X, (int)Math.Floor((screenPos.Y - origin.Y) / scale) - grid.offset.Y);
        }

        public Vector2 gridToScreenSize(GridSize gridSize)
        {
            return new Vector2(gridSize.Width * scale, gridSize.Height * scale);
        }

        public override Rectangle GetBounds()
        {
            if (grid != null)
            {
                return gridToScreenRect(GridPoint.Zero, grid.size);
            }
            else
            {
                return gridToScreenRect(GridPoint.Zero, new GridSize(10,10));
            }
        }

        public override void SetBounds(Rectangle rect)
        {
            origin = rect.TopLeft();
        }

        public virtual void OpenGrid(Grid aGrid)
        {
            grid = aGrid;
        }
    }
}
