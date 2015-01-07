using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using FactorioClicker.Graphics;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace FactorioClicker.Simulation
{
    public class FactoryWorkerSlot
    {
        public FactoryWorkerType workerType;
        public PseudoBuilding home;

        public WorkerManager workerManager;

        public FactoryWorker worker;

        public FactoryWorkerSlot(FactoryWorkerType aWorkerType, PseudoBuilding aHome)
        {
            workerType = aWorkerType;
            home = aHome;
        }

        public void CreateWorker(Grid grid, WorkerManager aWorkerManager, GridPoint spawnPosition)
        {
            if (worker == null)
            {
                worker = new FactoryWorker(0, grid, workerType, home, spawnPosition);
                workerManager = aWorkerManager;
                workerManager.Add(worker);
            }
        }

        public void DeleteWorker()
        {
            workerManager.Remove(worker);
            worker = null;
        }
    }

    public class FactoryWorkerType
    {
        public LayeredImage image;
        public LayeredImage starvingImage;
        public float moveSpeed;
        public bool canOperate;
        public float invMoveSpeed;

        public float invMoveSpeedSquared { get { return invMoveSpeed * invMoveSpeed; } }

        public FactoryWorkerType(JSONTable template, ContentManager Content)
        {
            image = template.getLayeredImage("image", null, Content);
            starvingImage = template.getLayeredImage("starvingImage", null, Content);
            moveSpeed = template.getFloat("moveSpeed", 1.0f);
            invMoveSpeed = 1 / moveSpeed;
            canOperate = template.getBool("canOperate", true);
        }
    }

    public class FactoryWorker
    {
        public Grid grid;
        public PseudoBuilding home;
        public Machine currentJob;
        public GridPoint lastDeliveryFrom;
        public GridPoint lastDeliveryTo;
        public ResourceTypeSpec lastDeliveryTypeSpec;
        public FactoryWorkerType workerType;
        public Vector2 startPosition;
        public Vector2 endPosition;
        public float targetDistance;
        public float distanceMoved;
        public ResourceType carrying;
        public Machine operating;
        public int workerId;

        public bool isBusy = false;

        public bool isStarving
        {
            get
            {
                return home.workerFoodDuration <= 0;
            }
        }

        public Vector2 position
        {
            get
            {
                Vector2 startVector = startPosition;
                Vector2 offsetVector = endPosition - startVector;

                if (offsetVector.LengthSquared() == 0)
                {
                    return startVector;
                }
                else
                {
                    offsetVector.Normalize();
                    return startVector + offsetVector * distanceMoved;
                }
            }
        }

        public GridPoint gridPosition
        {
            get
            {
                Vector2 pos = position;
                return new GridPoint((int)pos.X, (int)pos.Y);
            }
        }

        public FactoryWorker(int aWorkerId, Grid aGrid, FactoryWorkerType aWorkerType, PseudoBuilding aHome, GridPoint aStartPosition)
        {
            workerId = aWorkerId;
            grid = aGrid;
            workerType = aWorkerType;
            home = aHome;
            startPosition = aStartPosition.ToVector2();
            endPosition = startPosition;
            lastDeliveryFrom = aStartPosition;
            lastDeliveryTo = aStartPosition;
            targetDistance = 0;
        }

        public void Update()
        {
            isBusy = true;

            if (isStarving)
            {
                // starving - go home.

                if (operating != null)
                {
                    operating.RemoveOperator(this);
                }

                Vector2 homePosition = home.workerSpawnPosition.ToVector2();
                if (PickNewTask(home))
                {
                }
                else
                {
                    HeadTowards(home.workerSpawnPosition.ToVector2());
                }
            }

            if (operating != null)
            {
                // don't move while operating
                return;
            }

            distanceMoved += workerType.moveSpeed;
            if (distanceMoved >= targetDistance)
            {
                startPosition = endPosition;
                StopMoving();
                SetCurrentJob(null);
                isBusy = false;
//                Game1.instance.DebugLog("Worker " + workerId + " reached destination");
            }
            else if (currentJob == null || (currentJob.starvedConsumer == null && !currentJob.NeedsOperator()))
            {
                StopMoving(); // nothing to do - stay still until given another job
                SetCurrentJob(null);
                isBusy = false;
            }
        }

        public void Idle()
        {
            Vector2 currentVec = position;
            GridPoint currentPosition = gridPosition;

            if (lastDeliveryFrom.X == lastDeliveryTo.X && lastDeliveryFrom.Y == lastDeliveryTo.Y)
            {
                HeadTowards(lastDeliveryFrom.ToVector2());
            }
            else if (carrying != null)
            {
                if (lastDeliveryTypeSpec == null || lastDeliveryTypeSpec.Matches(carrying))
                {
                    if (currentVec.X == lastDeliveryTo.X && currentVec.Y == lastDeliveryTo.Y)
                    {
                        DropCarrying(lastDeliveryTypeSpec);
                    }
                    else
                    {
                        HeadTowards(lastDeliveryTo.ToVector2());
                    }
                }
                else
                {
                    StopMoving();
                }
            }
            else
            {
                if (currentVec.X == lastDeliveryFrom.X && currentVec.Y == lastDeliveryFrom.Y)
                {
                    PickUpResource(null);
                }
                else
                {
                    HeadTowards(lastDeliveryFrom.ToVector2());
                }
            }
        }

        public void SetCurrentJob(Machine newJob)
        {
            if (currentJob != null)
            {
                currentJob.externalWorker = null;
                currentJob = null;
            }

            if (newJob != null)
            {
                if (newJob.externalWorker != null)
                {
                    newJob.externalWorker.currentJob = null;
                }
                newJob.externalWorker = this;

                currentJob = newJob;
            }
        }

        public bool PickNewTask(PseudoBuilding targetJob)
        {
            if (targetJob == null || (targetJob.resourceShortages.Count == 0 && targetJob.operatorShortages.Count == 0) )
            {
                return false;
            }

            foreach (ResourceShortage shortage in targetJob.resourceShortages)
            {
                if( carrying != null && shortage.typeSpec.Matches(carrying) )
                {
                    GridPoint currentPosition = new GridPoint((int)startPosition.X, (int)startPosition.Y);
                    if (startPosition.X == shortage.position.X && startPosition.Y == shortage.position.Y)
                    {
                        // drop the resource we're carrying at the consumer
                        if (grid.resources.TryProduceResource(ResourceOutputMode.NORMAL, currentPosition, carrying, 1, true))
                        {
                            carrying = null;
                            return true;
                        }
                    }
                    else
                    {
                        // take the resource we're carrying to the consumer
                        HeadTowards(shortage.position.ToVector2());
                        SetCurrentJob(shortage.machine);
                        return true;
                    }
                }

                if (carrying == null)
                {
                    GridPoint currentPosition = new GridPoint((int)startPosition.X, (int)startPosition.Y);

                    // pick up the resource we're standing on
                    if (PickUpResource(shortage.typeSpec))
                    {
                        return true;
                    }
                    else
                    {
                        // walk to an available resource
                        // TODO: prioritize nearby resources
                        bool hasTarget = false;
                        Vector2 myPosition = startPosition;
                        Vector2 bestGridPoint = startPosition;
                        for (int X = 0; X < grid.resources.cells.GetLength(0); X++)
                        {
                            for (int Y = 0; Y < grid.resources.cells.GetLength(1); Y++)
                            {
                                ResourceGridEntry entry = grid.resources.cells[X, Y];
                                ResourceType resourceType = entry.type;
                                if (resourceType != null && entry.amount > 0)
                                {
                                    if (shortage.typeSpec.Matches(resourceType))
                                    {
                                        if( !hasTarget || (bestGridPoint - myPosition).LengthSquared() > (new Vector2(X,Y) - myPosition).LengthSquared() )
                                        {
                                            bestGridPoint = new Vector2(X, Y);
                                            hasTarget = true;
                                        }
                                    }
                                }
                            }
                        }
                        if (hasTarget)
                        {
                            HeadTowards(bestGridPoint);
                            SetCurrentJob(shortage.machine);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

/*        public bool PickNewTask(GridItem_Settlement settlement, List<GridItem_Building> priorityQueue, ResourceGrid resources)
        {
            GridItem oldJob = currentJob;

            int priorityIdx = 0;
            foreach (GridItem_Building item in priorityQueue)
            {
                if (PickNewTask(item, resources))
                {
//                    Game1.instance.DebugLog("Worker " + workerId + " accepts " + item.itemType.displayName + " (priority " + priorityIdx + " of "+priorityQueue.Count+")");
                    currentJob = item;
                    settlement.AddSecondaryItem(item);
                    return true;
                }
                else
                {
//                    Game1.instance.DebugLog("Worker " + workerId + " can't pick " + item.itemType.displayName);
                }

                priorityIdx++;
            }

            return false;
        }*/

        public void StopMoving()
        {
            HeadTowards(position);
        }

        public void HeadTowards(Vector2 aEndPosition)
        {
            startPosition = position;
            distanceMoved = 0;
            endPosition = aEndPosition;
            targetDistance = (startPosition - endPosition).Length();
        }

        public bool PickUpResource(ResourceTypeSpec typeSpec)
        {
            GridPoint currentPosition = gridPosition;
            ResourceType localResourceType = grid.resources.cells[currentPosition.X, currentPosition.Y].type;
            ResourceClaimTicket ticket = grid.resources.ClaimResource(ResourceIntakeMode.NORMAL, currentPosition, typeSpec, 1, false);
            if (ticket != null)
            {
                // standing at an available resource: pick it up
                ticket.DoConsume();
                carrying = localResourceType;
                lastDeliveryFrom = currentPosition;
                return true;
            }

            return false;
        }

        public bool DropCarrying(ResourceTypeSpec typeSpec)
        {
            GridPoint currentPosition = gridPosition;
            if (grid.resources.TryProduceResource(ResourceOutputMode.NORMAL, currentPosition, carrying, 1, true))
            {
                carrying = null;
                lastDeliveryTo = currentPosition;
                lastDeliveryTypeSpec = typeSpec;
                return true;
            }

            return false;
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 screenPos)
        {
            if (isStarving)
            {
                workerType.starvingImage.Draw(spriteBatch, (screenPos + new Vector2(-16, -32)).makeRectangle(new Vector2(32, 32)));
            }
            else
            {
                workerType.image.Draw(spriteBatch, (screenPos + new Vector2(-16, -32)).makeRectangle(new Vector2(32, 32)));
            }

            if (carrying != null)
            {
                carrying.image.Draw(spriteBatch, (screenPos + new Vector2(-12, -44)).makeRectangle(new Vector2(24, 24)));
            }

            if (operating != null)
            {
                Game1.instance.busyLightImage.Draw(spriteBatch, screenPos.makeRectangle(16, 16));
            }

            spriteBatch.DrawStringJustified(UI.UITextAlignment.CENTER, Game1.font, "" + workerId, new Vector2(screenPos.X, screenPos.Y - 24), Color.White);
        }
    }
}
