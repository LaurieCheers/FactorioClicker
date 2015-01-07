using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using FactorioClicker.Graphics;

namespace FactorioClicker.Simulation
{
    [DebuggerDisplay("GridPoint[{X},{Y}]")]
    public struct GridPoint : IEquatable<GridPoint>
    {
        public readonly int X;
        public readonly int Y;
        public static GridPoint Zero = new GridPoint(0, 0);

        public GridPoint(int aX, int aY)
        {
            X = aX;
            Y = aY;
        }

        public GridPoint(JSONArray template, GridPoint fallback)
        {
            if (template == null)
            {
                X = fallback.X;
                Y = fallback.Y;
            }
            else
            {
                X = template.getInt(0);
                Y = template.getInt(1);
            }
        }

        public GridPoint(JSONArray template): this(template, GridPoint.Zero)
        {
        }

        public static GridPoint operator +(GridPoint a, GridPoint b)
        {
            return new GridPoint(a.X + b.X, a.Y + b.Y);
        }

        public static GridPoint operator -(GridPoint a, GridPoint b)
        {
            return new GridPoint(a.X - b.X, a.Y - b.Y);
        }

        public bool Equals(GridPoint p)
        {
            return X == p.X && Y == p.Y;
        }

        public override int GetHashCode()
        {
            return 10000 * X + Y;
        }

        public GridPoint RotateBy(Rotation90 rotation)
        {
            switch (rotation)
            {
                case Rotation90.Rot90: return new GridPoint(-Y, X);
                case Rotation90.Rot180: return new GridPoint(-X, -Y);
                case Rotation90.Rot270: return new GridPoint(Y, -X);
                default: return this;
            }
        }

        public GridPoint[] Adjacent4
        {
            get
            {
                return new GridPoint[]
                {
                    new GridPoint(X,Y-1),
                    new GridPoint(X, Y+1),
                    new GridPoint(X-1,Y),
                    new GridPoint(X+1, Y)
                };
            }
        }

        public Vector2 ToVector2()
        {
            return new Vector2(X, Y);
        }
    }

    [DebuggerDisplay("GridSize[{Width},{Height}]")]
    public struct GridSize : IEquatable<GridSize>
    {
        public readonly int Width;
        public readonly int Height;
        public static GridSize Zero = new GridSize(0, 0);
        public static GridSize Unit = new GridSize(1, 1);

        public GridSize(int aWidth, int aHeight)
        {
            Width = aWidth;
            Height = aHeight;
        }

        public GridSize(JSONArray sizes, GridSize fallback)
        {
            if (sizes == null)
            {
                Width = fallback.Width;
                Height = fallback.Height;
            }
            else
            {
                Width = sizes.getInt(0);
                Height = sizes.getInt(1);
            }
        }

        public GridSize(JSONArray sizes): this(sizes, GridSize.Zero)
        {
        }

        public bool Equals(GridSize s)
        {
            return Width == s.Width && Height == s.Height;
        }

        public override int GetHashCode()
        {
            return 10000 * Width + Height;
        }

        public GridSize RotateBy(Rotation90 rotation)
        {
            switch (rotation)
            {
                case Rotation90.Rot90:
                case Rotation90.Rot270:
                    return new GridSize(Height, Width);
                default:
                    return this;
            }
        }
    }

    public class Grid
    {
        public ResourceGrid resources;
        public List<GridItem> items;
        public GridItem[,] cells { get; private set; }
        public GridSize size { get; private set; }
        public GridPoint offset; // for subgrids that are offset relative to a parent grid

        public Grid(GridSize aSize, ResourceGrid aResources)
        {
            size = aSize;
            cells = new GridItem[size.Width, size.Height];
            offset = GridPoint.Zero;
            items = new List<GridItem>();
            resources = aResources;
        }

        public bool Add(GridItem item)
        {
            if (!CanPlaceItem(item))
                return false;

            Stamp(item);
            item.container = this;
            items.Add(item);
            return true;
        }

        public void Remove(GridItem item)
        {
            if (item.container != this)
                return;

            Unstamp(item);
            item.container = null;
            items.Remove(item);
        }

        public GridItem ItemAtGridPos(GridPoint gridPos)
        {
            if (gridPos.X >= 0 && gridPos.Y >= 0 && gridPos.X < size.Width && gridPos.Y < size.Height)
            {
                return cells[gridPos.X, gridPos.Y];
            }
            else
            {
                return null;
            }
        }

        public bool IsOutOfBounds(GridPoint point)
        {
            return point.X < 0 || point.Y < 0 || point.X >= size.Width || point.Y >= size.Height;
        }

        public bool CanPlaceItem(GridItem item)
        {
            int maxX = item.gridPosition.X + item.gridSize.Width;
            int maxY = item.gridPosition.Y + item.gridSize.Height;
            if (item.gridPosition.X < 0 || item.gridPosition.Y < 0 || maxX > size.Width || maxY > size.Height)
            {
                return false;
            }
            for (int x = item.gridPosition.X; x < maxX; x++)
            {
                for (int y = item.gridPosition.Y; y < maxY; y++)
                {
                    GridItem target = cells[x,y];
                    if (target != null && target != item)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public void Unstamp(GridItem item)
        {
            Stamp(item, null);
        }

        public void Stamp(GridItem item)
        {
            Stamp(item, item);
        }

        void Stamp(GridItem area, GridItem value)
        {
            int maxX = area.gridPosition.X + area.gridSize.Width;
            int maxY = area.gridPosition.Y + area.gridSize.Height;
            for (int x = area.gridPosition.X; x < maxX; x++)
            {
                for (int y = area.gridPosition.Y; y < maxY; y++)
                {
                    cells[x, y] = value;
                }
            }
        }

        public HashSet<GridPoint> GetBlockedPoints(Grid overlay, GridPoint offset)
        {
            HashSet<GridPoint> result = new HashSet<GridPoint>();
            for (int X = 0; X < overlay.size.Width; X++)
            {
                for (int Y = 0; Y < overlay.size.Height; Y++)
                {
                    GridItem myItem = overlay.cells[X, Y];
                    if (myItem != null)
                    {
                        GridPoint targetPoint = new GridPoint(X+offset.X, Y+offset.Y);
                        if( IsOutOfBounds( targetPoint ) || cells[targetPoint.X, targetPoint.Y] != null )
                        {
                            result.Add(targetPoint);
                        }
                    }
                }
            }

            return result;
        }

        public void Add(Grid grid, GridPoint offset)
        {
            GridItem[] items = new GridItem[grid.items.Count];
            grid.items.CopyTo(items);
            foreach (GridItem item in items)
            {
                grid.Remove(item);
                item.gridPosition = item.gridPosition + offset;
                Add(item);
            }
        }
    }
}
