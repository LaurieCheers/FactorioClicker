using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using FactorioClicker.Simulation;
using Microsoft.Xna.Framework.Content;
using FactorioClicker.Graphics;

namespace FactorioClicker.UI
{
    class BuildingPalette: Notifiable<Notification_ResearchComplete>
    {
        List<GridItem_Building> filteredItems;
        Vector2 origin;
        float scale;
        float spacing;
        GridSize maxDisplaySize;
        GridItem_Settlement currentStation;

        public BuildingPalette(Vector2 aOrigin, float aScale, float aSpacing)
        {
            origin = aOrigin;
            scale = aScale;
            spacing = aSpacing;

            NotificationManager.instance.AddNotification<Notification_ResearchComplete>(this);
        }

        public BuildingPalette(JSONTable template, ContentManager Content)
        {
            origin = template.getArray("position", null).toVector2();
            scale = template.getInt("scale");
            spacing = template.getInt("spacing", 0);
            maxDisplaySize = new GridSize(template.getArray("maxDisplaySize", null), GridSize.Zero);

            NotificationManager.instance.AddNotification<Notification_ResearchComplete>(this);
        }

        public void FilterItemsFor(GridItem_Settlement station)
        {
            currentStation = station;
            filteredItems = new List<GridItem_Building>();
            foreach (GridItem_Building currentItem in Game1.instance.buildingTypes.Values)
            {
                if (currentItem.CanPlaceIn(station) && Game1.instance.researchManager.IsResearched(currentItem.itemType.name))
                {
                    filteredItems.Add(currentItem);
                }
            }
        }

        public GridItem ItemAtScreenPos(Vector2 screenPos)
        {
            float itemY = origin.Y;
            foreach (GridItem item in filteredItems)
            {
                Vector2 size = ClampedSizeFor(item.gridSize);
                if (origin.X <= screenPos.X && origin.X + size.X >= screenPos.X)
                {
                    if (itemY <= screenPos.Y && itemY + size.Y >= screenPos.Y)
                    {
                        return item;
                    }
                }
                itemY += size.Y + spacing;
            }

            return null;
        }

        public Rectangle ScreenRectFor(GridItem targetItem)
        {
            Vector2 itemSize = ClampedSizeFor(targetItem.gridSize);
            float itemY = origin.Y;
            foreach (GridItem item in filteredItems)
            {
                if (item == targetItem)
                {
                    return new Vector2(origin.X, itemY).makeRectangle(itemSize);
                }
                else
                {
                    Vector2 size = ClampedSizeFor(item.gridSize);
                    itemY += size.Y + spacing;
                }
            }

            return new Vector2(0, 0).makeRectangle(itemSize);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            float itemY = origin.Y;
            foreach (GridItem item in filteredItems)
            {
                Vector2 size = ClampedSizeFor(item.gridSize);
                item.itemType.image.Draw(spriteBatch, new Vector2(origin.X, itemY).makeRectangle(size), Rotation90.None);
                itemY += size.Y + spacing;
            }
        }

        public Vector2 ClampedSizeFor(GridSize size)
        {
            int W = size.Width;
            int H = size.Height;
            if( maxDisplaySize.Width != 0 && maxDisplaySize.Width < W )
            {
                W = maxDisplaySize.Width;
            }
            if( maxDisplaySize.Height != 0 && maxDisplaySize.Height < H )
            {
                H = maxDisplaySize.Height;
            }

            return new Vector2(W*scale, H*scale);
        }

        public void Notify(Notification_ResearchComplete note)
        {
            if (currentStation != null)
            {
                FilterItemsFor(currentStation);
            }
        }
    }
}
