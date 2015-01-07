using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FactorioClicker.UI;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using FactorioClicker.Graphics;

namespace FactorioClicker.Simulation
{
    public class Notification_ResourceProduced : Notification
    {
        public int amount;
        public ResourceType resourceType;

        public Notification_ResourceProduced(ResourceType aResourceType, int aAmount)
        {
            amount = aAmount;
            resourceType = aResourceType;
        }
    }

    public struct ResourceGridEntry
    {
        public ResourceType type;
        public int amount;
        public int claimedAmount;

        public int totalAmount { get { return amount+claimedAmount; } }

        public bool HasResource(ResourceTypeSpec typeSpec, int requiredAmount)
        {
            if (type == null)
                return false;

            if (typeSpec != null && !typeSpec.Matches(type))
                return false;

            if (amount < requiredAmount)
                return false;

            return true;
        }

        public void Draw(SpriteBatch spriteBatch, Rectangle rect)
        {
            if (type != null)
            {
                type.image.Draw(spriteBatch, rect, Rotation90.None);
                bool debugClaimed = false;

                if (rect.Width >= 32)
                {
                    if (debugClaimed)
                    {
                        spriteBatch.DrawStringJustified(UITextAlignment.RIGHT, Game1.font, Convert.ToString(amount), new Vector2(rect.Right - 5, rect.Center.Y), Color.White);
                        spriteBatch.DrawStringJustified(UITextAlignment.RIGHT, Game1.font, Convert.ToString(claimedAmount), new Vector2(rect.Right - 5, rect.Top + 5), Color.Yellow);
                    }
                    else
                    {
                        spriteBatch.DrawStringJustified(UITextAlignment.RIGHT, Game1.font, Convert.ToString(amount + claimedAmount), new Vector2(rect.Right - 5, rect.Center.Y), Color.White);
                    }
                }
            }
        }
    }

    public class ResourceGrid
    {
        GridSize size;
        public ResourceGridEntry[,] cells;
        ResourceGrid parentGrid;
        GridPoint parentPosition;
        public int powerGenerated;
        public int powerGeneratedNextFrame;
        public int powerConsumed;
        public int powerStored;
        public int powerStoredMax;
        public int powerMaxAllTime;

        public ResourceGrid(GridSize aSize, ResourceGrid aParentGrid, GridPoint aParentPosition)
        {
            size = aSize;
            cells = new ResourceGridEntry[size.Width, size.Height];
            parentGrid = aParentGrid;
            parentPosition = aParentPosition;
        }

        public int powerAvailable { get { return powerGenerated + powerStored - powerConsumed; } }

        public void UpdatePower(int initialPowerGeneratedNextFrame, int newStoredMax, int newConsumedMax)
        {
            powerStoredMax = newStoredMax;

            if (powerGenerated + powerStoredMax > powerMaxAllTime)
            {
                powerMaxAllTime = powerGenerated + powerStoredMax;
            }
            
            if (newConsumedMax > powerMaxAllTime)
            {
                powerMaxAllTime = newConsumedMax;
            }

            if (powerGenerated > powerConsumed)
            {
                int powerStoreAmount = (int)((powerGenerated - powerConsumed) * 0.01f);
                if (powerStoreAmount < 1)
                    powerStoreAmount = 1;

                powerStored += powerStoreAmount;

                if (powerStored > powerStoredMax)
                {
                    powerStored = powerStoredMax;
                }
            }
            else
            {
                powerStored -= (powerConsumed - powerGenerated);

                if (powerStored < 0)
                {
                    powerStored = 0;
                }
            }

            powerGenerated = powerGeneratedNextFrame;
            powerGeneratedNextFrame = initialPowerGeneratedNextFrame;
            powerConsumed = 0;
        }

        public bool IsInBounds(GridPoint position)
        {
            if (position.X < 0 || position.Y < 0 || position.X >= size.Width || position.Y >= size.Height)
                return false;

            return true;
        }

        public int AmountAvailableAt(GridPoint position)
        {
            if (!IsInBounds(position))
            {
                return 0;
            }

            return cells[position.X, position.Y].amount;
        }

        public int TotalAmountAt(GridPoint position)
        {
            if (!IsInBounds(position))
            {
                return 0;
            }

            return cells[position.X, position.Y].totalAmount;
        }

        public bool HasResource(ResourceIntakeMode intakeMode, GridPoint position, ResourceTypeSpec typeSpec, int amount)
        {
            switch(intakeMode)
            {
                case ResourceIntakeMode.MINING:
                    return parentGrid.HasResource(ResourceIntakeMode.NORMAL, parentPosition, typeSpec, amount);

                case ResourceIntakeMode.NORMAL:
                    if (!IsInBounds(position))
                    {
                        return false;
                    }
                    ResourceGridEntry entry = cells[position.X, position.Y];
                    return entry.HasResource(typeSpec, amount);
            }
            return false;
        }

        public bool HasResource(GridPoint position, ResourceType type, int amount)
        {
            if (!IsInBounds(position))
            {
                return false;
            }
            ResourceGridEntry entry = cells[position.X, position.Y];
            if( entry.type != type )
                return false;

            if(entry.amount < amount )
                return false;

            return true;
        }

        public ResourceClaimTicket ClaimResource(ResourceIntakeMode intakeMode, GridPoint position, ResourceTypeSpec typeSpec, int amount, bool isInput)
        {
            if (!HasResource(intakeMode, position, typeSpec, amount))
            {
                return null;
            }

            if (intakeMode == ResourceIntakeMode.MINING)
            {
                return parentGrid.ClaimResource(ResourceIntakeMode.NORMAL, parentPosition, typeSpec, amount, isInput);
            }
            ResourceGridEntry newEntry = cells[position.X, position.Y];
            newEntry.amount -= amount;
            newEntry.claimedAmount += amount;
            cells[position.X, position.Y] = newEntry;
            return new ResourceClaimTicket(this, position, newEntry.type, amount, isInput);
        }

        public void ConsumeClaimedResource(GridPoint position, ResourceType type, int amount)
        {
            ResourceGridEntry newEntry = cells[position.X, position.Y];

            if (newEntry.claimedAmount < amount)
                return; // this shouldn't happen!

            newEntry.claimedAmount -= amount;
            if (newEntry.amount == 0 && newEntry.claimedAmount == 0)
            {
                newEntry.type = null;
            }
            cells[position.X, position.Y] = newEntry;
        }

        public void RefundClaimedResource(GridPoint position, ResourceType type, int amount)
        {
            ResourceGridEntry newEntry = cells[position.X, position.Y];

            if (newEntry.claimedAmount < amount)
                return; // this shouldn't happen!

            newEntry.claimedAmount -= amount;
            newEntry.amount += amount;
            cells[position.X, position.Y] = newEntry;
        }

        public bool HasSpaceForResource(ResourceOutputMode outputMode, GridPoint position, ResourceType type, int amount)
        {
            if (outputMode == ResourceOutputMode.EXPORT)
            {
                return parentGrid.HasSpaceForResource(ResourceOutputMode.NORMAL, parentPosition, type, amount);
            }

            if (!IsInBounds(position))
                return false;

            ResourceGridEntry entry = cells[position.X, position.Y];
            if (entry.type == null || entry.amount == 0)
                return true;

            if (entry.type == type && (entry.amount + entry.claimedAmount) < type.maxAmount)
                return true;

            return false;
        }

        public bool TryProduceResource(ResourceOutputMode outputMode, GridPoint position, ResourceType type, int amount, bool isLogistics)
        {
            if (outputMode == ResourceOutputMode.EXPORT)
            {
                return parentGrid.TryProduceResource(ResourceOutputMode.NORMAL, parentPosition, type, amount, isLogistics);
            }

            ResourceGridEntry entry = cells[position.X, position.Y];
            if (entry.type == null || entry.amount == 0)
            {
                entry.type = type;
                entry.amount = 0;
            }

            if ( entry.type != type || entry.amount >= type.maxAmount )
            {
                return false;
            }

            entry.amount += amount;
            if(entry.amount > type.maxAmount)
            {
                entry.amount = type.maxAmount;
            }
            cells[position.X, position.Y] = entry;

            if (!isLogistics)
            {
                NotificationManager.instance.Notify(new Notification_ResourceProduced(type, amount));
            }
            return true;
        }

        public void DrawCell(GridPoint pos, Rectangle rect, SpriteBatch spriteBatch)
        {
            cells[pos.X, pos.Y].Draw(spriteBatch, rect);
        }
    }
}
