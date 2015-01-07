using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using FactorioClicker.Simulation;
using FactorioClicker.Graphics;

namespace FactorioClicker.UI
{
    enum SelectedPowerBar
    {
        NONE,
        GENERATED,
        CONSUMED,
        STORED
    }

    class GridEditor_SpaceStation: GridView
    {
        BuildingPalette palette;
        GridItem bandbox;
        GridItem mouseOverPaletteItem;
        GridItem_Building mouseOverGridItem;
        GridPoint bandboxOrigin;
        bool bandboxSelecting;
        HashSet<GridItem> multiSelectedItems;
        Grid draggingGrid;
        GridPoint gridDraggingOffset;
        List<GridPoint> draggingGoodMarks;
        List<GridPoint> draggingBadMarks;
        LayeredImage shadowImage;
        LayeredImage draggingGoodImage;
        LayeredImage draggingBadImage;
        LayeredImage bandboxImage;

        LayeredImage powerGeneratedImage;
        LayeredImage powerConsumedImage;
        LayeredImage powerStoredGainImage;
        LayeredImage powerStoredLossImage;
        LayeredImage powerStoredMaxImage;
        LayeredImage powerStoredHighlightImage;
        LayeredImage powerGeneratedHighlightImage;
        LayeredImage powerConsumedHighlightImage;
        LayeredImage powerNeededImage;
        Rectangle powerGeneratedRect;
        Rectangle powerConsumedRect;
        Rectangle powerStoredRect;
        Rectangle powerStoredMaxRect;
        Rectangle powerNeededRect;
        SelectedPowerBar powerBarMouseOver;

        GridItem_Settlement editingStation;

        public GridEditor_SpaceStation(JSONTable template, ContentManager Content):
            base(null, template, Content)
        {
            palette = new BuildingPalette(template.getJSON("palette"), Content);// aPalette;

            draggingGoodMarks = new List<GridPoint>();
            draggingBadMarks = new List<GridPoint>();
            multiSelectedItems = new HashSet<GridItem>();

            JSONTable shadowTable = template.getJSON("shadowImage");
            shadowImage = new LayeredImage(shadowTable, Content);

            JSONTable goodStripesTable = template.getJSON("goodStripesImage");
            draggingGoodImage = new LayeredImage(goodStripesTable, Content);

            JSONTable badStripesTable = template.getJSON("badStripesImage");
            draggingBadImage = new LayeredImage(badStripesTable, Content);

            JSONTable bandboxTemplate = template.getJSON("bandboxImage");
            bandbox = new GridItem(bandboxTemplate, Content);
            bandboxImage = new LayeredImage(bandboxTemplate, Content);

            powerConsumedImage = new LayeredImage(template.getJSON("powerConsumedImage"), Content);
            powerGeneratedImage = new LayeredImage(template.getJSON("powerGeneratedImage"), Content);
            powerStoredGainImage = new LayeredImage(template.getJSON("powerStoredGainImage"), Content);
            powerStoredLossImage = new LayeredImage(template.getJSON("powerStoredLossImage"), Content);
            powerNeededImage = new LayeredImage(template.getJSON("powerNeededImage"), Content);
            powerStoredMaxImage = new LayeredImage(template.getJSON("powerStoredMaxImage"), Content);
            powerStoredHighlightImage = new LayeredImage(template.getJSON("powerStoredHighlightImage"), Content);
            powerGeneratedHighlightImage = new LayeredImage(template.getJSON("powerGeneratedHighlightImage"), Content);
            powerConsumedHighlightImage = new LayeredImage(template.getJSON("powerConsumedHighlightImage"), Content);
        }

        public override bool HandleInput(InputState inputState, JSCNContext context)
        {
            // grid editor
            if (inputState.WasKeyJustPressed(Keys.Delete))
            {
                if (multiSelectedItems != null)
                {
                    foreach (GridItem_Building item in multiSelectedItems)
                    {
                        editingStation.Delete(item);
                    }
                    multiSelectedItems.Clear();
                }
            }
            else if (inputState.keyboard.IsKeyDown(Keys.W))
            {
                RotateSelectedTo(Rotation90.Rot270);
            }
            else if (inputState.keyboard.IsKeyDown(Keys.S))
            {
                RotateSelectedTo(Rotation90.Rot90);
            }
            else if (inputState.keyboard.IsKeyDown(Keys.A))
            {
                RotateSelectedTo(Rotation90.Rot180);
            }
            else if (inputState.keyboard.IsKeyDown(Keys.D))
            {
                RotateSelectedTo(Rotation90.None);
            }
            else if (inputState.WasKeyJustPressed(Keys.R))
            {
                RotateSelectedToNext();
            }
            else if (inputState.WasKeyJustPressed(Keys.Q))
            {
                RotateSelectedToPrev();
            }

            draggingGoodMarks.Clear();
            draggingBadMarks.Clear();

            Vector2 mousePos = inputState.MousePos;
            GridPoint mouseGridPos = screenToGridPos(mousePos);
            
            mouseOverPaletteItem = palette.ItemAtScreenPos(mousePos);
            mouseOverGridItem = (GridItem_Building)ItemAtScreenPos(mousePos);

            if (inputState.WasMouseLeftJustPressed())
            {
                if (mouseOverPaletteItem != null)
                {
                    // spawn a new item
                    draggingGrid = new Grid(mouseOverPaletteItem.gridSize, null);
                    GridItem newItem = mouseOverPaletteItem.Clone();
                    newItem.gridPosition = GridPoint.Zero;
                    draggingGrid.Add(newItem);
                    draggingGrid.offset = GridPoint.Zero;
                    multiSelectedItems.Clear();
                    multiSelectedItems.Add(newItem);
                    gridDraggingOffset = GridPoint.Zero;
                }
                else
                {
                    GridItem selectedItem = grid.ItemAtGridPos(mouseGridPos);
                    if (selectedItem != null)
                    {
                        // start dragging the existing previously selected items
                        if (multiSelectedItems.Contains(selectedItem))
                        {
                            HashSet<GridItem> newMultiSelectedItems = new HashSet<GridItem>();
                            int minX = 100000;
                            int minY = 100000;
                            int maxX = -100000;
                            int maxY = -100000;
                            foreach (GridItem item in multiSelectedItems)
                            {
                                minX = Math.Min(minX, item.gridPosition.X);
                                minY = Math.Min(minY, item.gridPosition.Y);
                                maxX = Math.Max(maxX, item.gridPosition.X + item.gridSize.Width);
                                maxY = Math.Max(maxY, item.gridPosition.Y + item.gridSize.Height);
                                if (inputState.keyboard.IsShiftPressed())
                                {
                                    newMultiSelectedItems.Add(item.Clone());
                                }
                            }
                            if (inputState.keyboard.IsShiftPressed())
                            {
                                multiSelectedItems = newMultiSelectedItems;
                            }

                            draggingGrid = new Grid(new GridSize(maxX - minX, maxY - minY), null);
                            draggingGrid.offset = new GridPoint(minX, minY);
                            gridDraggingOffset = draggingGrid.offset - mouseGridPos;
                            foreach (GridItem item in multiSelectedItems)
                            {
                                item.PrepareToMove();
                                grid.Remove(item);
                                item.gridPosition = item.gridPosition - new GridPoint(minX, minY);
                                draggingGrid.Add(item);
                            }
                        }
                        else if(selectedItem.canMove)
                        {
                            // start dragging an item
                            if (inputState.keyboard.IsShiftPressed())
                            {
                                selectedItem = selectedItem.Clone();
                            }
                            draggingGrid = new Grid(selectedItem.gridSize, null);
                            selectedItem.PrepareToMove();
                            grid.Remove(selectedItem);
                            draggingGrid.offset = selectedItem.gridPosition;
                            gridDraggingOffset = selectedItem.gridPosition - mouseGridPos;
                            selectedItem.gridPosition = GridPoint.Zero;
                            draggingGrid.Add(selectedItem);
                            multiSelectedItems.Clear();
                            multiSelectedItems.Add(selectedItem);
                        }
                    }
                    else
                    {
                        // start bandbox selecting
                        bandboxSelecting = true;
                        bandboxOrigin = mouseGridPos;
                        bandbox.gridPosition = bandboxOrigin;
                        bandbox.itemType.gridSize = GridSize.Unit; // FIXME: this breaks the GridItemType contract, maybe the bandbox should not be a gridItem?
                    }
                }
            }
            else if(inputState.mouse.LeftButton == ButtonState.Pressed)
            {
                if (bandboxSelecting)
                {
                    // dragging bandbox
                    bandbox.gridPosition = new GridPoint(Math.Min(mouseGridPos.X, bandboxOrigin.X), Math.Min(mouseGridPos.Y, bandboxOrigin.Y));
                    GridPoint maxPos = new GridPoint(Math.Max(mouseGridPos.X + 1, bandboxOrigin.X + 1), Math.Max(mouseGridPos.Y + 1, bandboxOrigin.Y + 1));
                    int Width = maxPos.X - bandbox.gridPosition.X;
                    int Height = maxPos.Y - bandbox.gridPosition.Y;
                    bandbox.itemType.gridSize = new GridSize(Width, Height);
                }

                if (draggingGrid != null)
                {
                    // update the items being dragged
                    GridPoint oldPosition = draggingGrid.offset;
                    GridPoint newPosition = mouseGridPos + gridDraggingOffset;
                    HashSet<GridPoint> blockedPoints = grid.GetBlockedPoints(draggingGrid, newPosition);
                    if (blockedPoints.Count == 0)
                    {
                        draggingGrid.offset = newPosition;
                    }
                    else
                    {
                        draggingGrid.offset = oldPosition;

                        for (int X = 0; X < draggingGrid.size.Width; X++)
                        {
                            for (int Y = 0; Y < draggingGrid.size.Height; Y++)
                            {
                                if (draggingGrid.cells[X, Y] != null)
                                {
                                    GridPoint targetPoint = new GridPoint(X + newPosition.X, Y + newPosition.Y);
                                    if (blockedPoints.Contains(targetPoint))
                                    {
                                        draggingBadMarks.Add(targetPoint);
                                    }
                                    else
                                    {
                                        draggingGoodMarks.Add(targetPoint);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // drag ended
                if (draggingGrid != null)
                {
                    grid.Add(draggingGrid, draggingGrid.offset);
                }
                else if (bandboxSelecting)
                {
                    if (!inputState.keyboard.IsShiftPressed())
                    {
                        multiSelectedItems.Clear();
                    }
                    for (int X = bandbox.gridPosition.X; X < bandbox.gridPosition.X + bandbox.gridSize.Width; ++X)
                    {
                        for (int Y = bandbox.gridPosition.Y; Y < bandbox.gridPosition.Y + bandbox.gridSize.Height; ++Y)
                        {
                            GridItem item = grid.ItemAtGridPos(new GridPoint(X, Y));
                            if (item != null && item.canMove)
                            {
                                multiSelectedItems.Add(grid.ItemAtGridPos(new GridPoint(X, Y)));
                            }
                        }
                    }
                    bandboxSelecting = false;
                }
                draggingGrid = null;
            }

            UpdatePowerBars(inputState);

            // clean up
            multiSelectedItems.RemoveWhere(hasNoGrid);

            return true;
        }

        private static bool hasNoGrid(GridItem item)
        {
            return item.container == null;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            editingStation.workerManager.Draw(spriteBatch, this);

            palette.Draw(spriteBatch);

            foreach (GridItem item in multiSelectedItems)
            {
                DrawImage(spriteBatch, bandboxImage, item.gridPosition + item.container.offset, item.gridSize);
            }

            DrawPowerBars(spriteBatch);

            if (draggingGrid != null)
            {
                foreach (GridItem item in draggingGrid.items)
                {
                    DrawImage(spriteBatch, shadowImage, item.gridPosition + draggingGrid.offset, item.gridSize);
                }
                foreach (GridItem item in draggingGrid.items)
                {
                    DrawItemOffset(spriteBatch, item, draggingGrid.offset, new Vector2(-6, -6));
                }
                foreach (GridPoint point in draggingGoodMarks)
                {
                    Vector2 screenPos = gridToScreenPos(point);
                    draggingGoodImage.Draw(spriteBatch, new Rectangle((int)screenPos.X, (int)screenPos.Y, (int)scale, (int)scale), Rotation90.None);
                }
                foreach (GridPoint point in draggingBadMarks)
                {
                    Vector2 screenPos = gridToScreenPos(point);
                    draggingBadImage.Draw(spriteBatch, new Rectangle((int)screenPos.X, (int)screenPos.Y, (int)scale, (int)scale), Rotation90.None);
                }
            }
            else if (bandboxSelecting)
            {
                DrawItem(spriteBatch, bandbox);
            }
            else if( mouseOverPaletteItem != null )
            {
                Game1.instance.DrawTooltip(spriteBatch, palette.ScreenRectFor(mouseOverPaletteItem), UIAnchorSide.RIGHT, mouseOverPaletteItem.itemType.displayName);
            }
            else if (mouseOverGridItem != null)
            {
                Game1.instance.DrawTooltip(spriteBatch, gridToScreenRect(mouseOverGridItem.gridPosition, mouseOverGridItem.gridSize), UIAnchorSide.TOP, mouseOverGridItem.itemType.displayName);
            }
        }

        void UpdatePowerBars(InputState inputState)
        {
            float powerScale = grid.size.Width * scale / (float)grid.resources.powerMaxAllTime;
            int powerGeneratedWidth = (int)(grid.resources.powerGenerated * powerScale);
            int powerConsumedWidth = (int)(grid.resources.powerConsumed * powerScale);
            int powerStoredWidth = (int)(grid.resources.powerStored * powerScale);
            int powerStoredMaxWidth = (int)(grid.resources.powerStoredMax * powerScale);

            powerGeneratedRect = new Vector2(origin.X, origin.Y - 38).makeRectangle(powerGeneratedWidth, 16);
            powerStoredRect = new Vector2(origin.X + powerGeneratedWidth, origin.Y - 38).makeRectangle(powerStoredWidth, 16);
            powerStoredMaxRect = new Vector2(origin.X + powerGeneratedWidth + powerStoredWidth, origin.Y - 38).makeRectangle(powerStoredMaxWidth - powerStoredWidth, 16);
            powerConsumedRect = new Vector2(origin.X, origin.Y - 38 + 16).makeRectangle(powerConsumedWidth, 16);

            int rhs = powerStoredMaxRect.X + powerStoredMaxRect.Width;

            if (powerGeneratedRect.Contains(inputState.MousePos))
            {
                powerBarMouseOver = SelectedPowerBar.GENERATED;
            }
            else if (powerStoredRect.Contains(inputState.MousePos))
            {
                powerBarMouseOver = SelectedPowerBar.STORED;
            }
            else if (powerConsumedRect.Contains(inputState.MousePos))
            {
                powerBarMouseOver = SelectedPowerBar.CONSUMED;
            }
            else if (powerStoredMaxRect.Contains(inputState.MousePos))
            {
                powerBarMouseOver = SelectedPowerBar.STORED;
            }
            else
            {
                powerBarMouseOver = SelectedPowerBar.NONE;
            }

            if (mouseOverGridItem != null && mouseOverGridItem.isOutOfPower)
            {
                int powerNeeded = 0;
                foreach (Machine m in mouseOverGridItem.machines)
                {
                    if (m.isOutOfPower)
                    {
                        powerNeeded += m.machineType.powerCost;
                    }
                }

                int powerNeededWidth = (int)(powerNeeded * powerScale);
                powerNeededRect = new Vector2(origin.X + powerConsumedWidth, origin.Y - 38 + 16).makeRectangle(powerNeededWidth, 16);
            }
            else
            {
                powerNeededRect = new Vector2(origin.X + powerConsumedWidth, origin.Y - 38 + 16).makeRectangle(0, 16);
            }
        }

        void DrawPowerBars(SpriteBatch spriteBatch)
        {
            powerGeneratedImage.Draw(spriteBatch, powerGeneratedRect);
            Rectangle wholePowerStoreRect = powerStoredRect.Expand(powerStoredMaxRect);
            powerStoredMaxImage.Draw(spriteBatch, wholePowerStoreRect);
            if (grid.resources.powerConsumed > grid.resources.powerGenerated)
            {
                powerStoredLossImage.Draw(spriteBatch, powerStoredRect);
            }
            else
            {
                powerStoredGainImage.Draw(spriteBatch, powerStoredRect);
            }
            powerConsumedImage.Draw(spriteBatch, powerConsumedRect);
            powerNeededImage.Draw(spriteBatch, powerNeededRect);

            if (powerBarMouseOver == SelectedPowerBar.GENERATED)
            {
                powerGeneratedHighlightImage.Draw(spriteBatch, powerGeneratedRect);
            }
            else if (powerBarMouseOver == SelectedPowerBar.CONSUMED)
            {
                powerConsumedHighlightImage.Draw(spriteBatch, powerConsumedRect);
            }
            else if (powerBarMouseOver == SelectedPowerBar.STORED)
            {
                powerStoredHighlightImage.Draw(spriteBatch, powerStoredRect.Expand(powerStoredMaxRect));
            }

            Game1.instance.powerSymbolImage.Draw(spriteBatch, new Vector2(origin.X + 5, origin.Y - 38).makeRectangle(new Vector2(32, 32)));

            if (powerBarMouseOver == SelectedPowerBar.GENERATED)
            {
                Game1.instance.DrawTooltip(spriteBatch, powerGeneratedRect, UIAnchorSide.BOTTOM, "Generated: " + grid.resources.powerGenerated);
            }
            else if (powerBarMouseOver == SelectedPowerBar.CONSUMED)
            {
                Game1.instance.DrawTooltip(spriteBatch, powerConsumedRect, UIAnchorSide.BOTTOM, "Consumed: " + grid.resources.powerConsumed);
            }
            else if (powerBarMouseOver == SelectedPowerBar.STORED)
            {
                Game1.instance.DrawTooltip(spriteBatch, powerStoredRect.Expand(powerStoredMaxRect), UIAnchorSide.BOTTOM, "Stored: " + grid.resources.powerStored + "/" + grid.resources.powerStoredMax);
            }
        }

        public void RotateSelectedTo(Rotation90 rotation)
        {
            if (multiSelectedItems != null)
            {
                foreach (GridItem item in multiSelectedItems)
                {
                    item.RotateTo(rotation);
                }
            }
        }

        public void RotateSelectedToNext()
        {
            if (multiSelectedItems != null)
            {
                foreach (GridItem item in multiSelectedItems)
                {
                    item.RotateTo(item.rotation.Next());
                }
            }
        }

        public void RotateSelectedToPrev()
        {
            if (multiSelectedItems != null)
            {
                foreach (GridItem item in multiSelectedItems)
                {
                    item.RotateTo(item.rotation.Prev());
                }
            }
        }

        public void OpenStation(GridItem_Settlement aStation)
        {
            editingStation = aStation;
            OpenGrid(aStation.contents);
            palette.FilterItemsFor(aStation);
            multiSelectedItems.Clear();
            bandboxSelecting = false;
            draggingGrid = null;
        }
    }
}
