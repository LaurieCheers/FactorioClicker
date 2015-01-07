using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using FactorioClicker.UI;
using Microsoft.Xna.Framework.Graphics;

namespace FactorioClicker.Simulation
{
    public class WorkerManager
    {
        public List<FactoryWorker> workers = new List<FactoryWorker>();
        public List<PseudoBuilding> priorityQueue = new List<PseudoBuilding>();
        public HashSet<PseudoBuilding> priorityItems = new HashSet<PseudoBuilding>();
        public List<PseudoBuilding> secondaryQueue = new List<PseudoBuilding>();
        public HashSet<PseudoBuilding> secondaryItems = new HashSet<PseudoBuilding>();

        public void AddPriorityItemUnlessSecondary(PseudoBuilding item)
        {
            if (!secondaryItems.Contains(item))
            {
                AddPriorityItem(item);
            }
        }

        public void AddPriorityItem(PseudoBuilding item)
        {
            if (!priorityItems.Contains(item))
            {
                priorityQueue.Add(item);
                priorityItems.Add(item);
            }

            if (secondaryItems.Contains(item))
            {
                secondaryItems.Remove(item);
                secondaryQueue.Remove(item);
            }
        }

        public void AddSecondaryItem(PseudoBuilding item)
        {
            if (priorityItems.Contains(item))
            {
                priorityQueue.Remove(item);
                priorityItems.Remove(item);
            }

            if (!secondaryItems.Contains(item))
            {
                secondaryItems.Add(item);
                secondaryQueue.Add(item);
            }
        }

        public void UnlistPriorityItem(PseudoBuilding item)
        {
            if (priorityItems.Contains(item))
            {
                priorityQueue.Remove(item);
                priorityItems.Remove(item);
            }

            if (secondaryItems.Contains(item))
            {
                secondaryItems.Remove(item);
                secondaryQueue.Remove(item);
            }
        }

        public void Add(FactoryWorker worker)
        {
            workers.Add(worker);
        }

        public void Remove(FactoryWorker worker)
        {
            workers.Remove(worker);
        }

        public void RemoveAll(List<FactoryWorker> toRemove)
        {
            foreach (FactoryWorker worker in toRemove)
            {
                Remove(worker);
            }
        }

        public void UpdateItem(PseudoBuilding item)
        {
            if (item.resourceShortages.Count > 0 || item.operatorShortages.Count > 0)
            {
                AddPriorityItemUnlessSecondary(item);
            }
            else
            {
                UnlistPriorityItem(item);
            }
        }

        public void Update(Grid grid)
        {
            foreach (PseudoBuilding item in grid.items)
            {
                foreach(FactoryWorkerSlot slot in item.workerSlots)
                {
                    if (slot.worker == null)
                    {
                        slot.CreateWorker(grid, this, item.workerSpawnPosition);
                    }
                }
            }

            HashSet<FactoryWorker> idleWorkers = new HashSet<FactoryWorker>();
            HashSet<FactoryWorker> assignableWorkers = new HashSet<FactoryWorker>();
            foreach (FactoryWorker w in workers)
            {
                w.Update();

                if (!w.isStarving)
                {
                    if (!w.isBusy)
                    {
                        idleWorkers.Add(w);
                        assignableWorkers.Add(w);
                    }
                }
            }

            //List<GridItem_Building> makeSecondary = new List<GridItem_Building>();
            foreach (PseudoBuilding job in priorityQueue)
            {
                if (AssignJob(job, assignableWorkers, grid.resources))
                {
                    //makeSecondary.Add(job);
                }
            }

            foreach (FactoryWorker worker in assignableWorkers)
            {
                if (idleWorkers.Contains(worker))
                {
                    worker.Idle();
                }
            }
        }

        public bool AssignJob(PseudoBuilding job, HashSet<FactoryWorker> idleWorkers, ResourceGrid resources)
        {
            foreach (OperatorShortage shortage in job.operatorShortages)
            {
                if (shortage.machine.externalWorker != null)
                {
                    // this machine is already being helped; forget it
                    continue;
                }

                FactoryWorker closestWorker = null;
                float closestDistSqr = 0;
                foreach (FactoryWorker worker in idleWorkers)
                {
                    if (!worker.workerType.canOperate)
                    {
                        // ignore workers who can't operate buildings
                        continue;
                    }

                    Vector2 position = worker.position;
                    float distSqrStraight = worker.workerType.invMoveSpeedSquared * new Vector2(shortage.position.X - position.X, shortage.position.Y - position.Y).LengthSquared();
                    if (distSqrStraight == 0)
                    {
                        // found somebody right there
                        shortage.machine.AddOperator(worker);
                        idleWorkers.Remove(worker);
                        break;
                    }
                    else if (closestWorker == null || closestDistSqr < distSqrStraight)
                    {
                        closestWorker = worker;
                        closestDistSqr = distSqrStraight;
                    }
                }

                if (closestWorker != null)
                {
                    idleWorkers.Remove(closestWorker);
                    closestWorker.SetCurrentJob(shortage.machine);
                    closestWorker.HeadTowards(shortage.position.ToVector2());
                    return true;
                }
            }

            foreach (ResourceShortage shortage in job.resourceShortages)
            {
                if (shortage.machine.externalWorker != null)
                {
                    // this machine is already being helped; forget it
                    continue;
                }

                FactoryWorker closestWorkerRoundTrip = null;
                float closestDistRoundTrip = 0;

                float closestDistSqrStraight = 0;
                GridPoint destination = shortage.position;

                // is there someone already carrying this resource, so they can go straight there?
                foreach (FactoryWorker worker in idleWorkers)
                {
                    if (worker.carrying != null && shortage.typeSpec.Matches(worker.carrying))
                    {
                        Vector2 position = worker.position;
                        float distSqrStraight = worker.workerType.invMoveSpeedSquared * new Vector2(destination.X - position.X, destination.Y - position.Y).LengthSquared();
                        if (distSqrStraight == 0)
                        {
                            // this guy is right there! job search is over, just drop the resource at the consumer
                            worker.DropCarrying(shortage.typeSpec);
                            idleWorkers.Remove(worker);
                            return true;
                        }
                        if (closestWorkerRoundTrip == null || closestDistSqrStraight > distSqrStraight)
                        {
                            closestWorkerRoundTrip = worker;
                            closestDistSqrStraight = distSqrStraight;
                        }
                    }
                }

                if (closestWorkerRoundTrip != null)
                {
                    closestDistRoundTrip = (float)Math.Sqrt(closestDistSqrStraight);
                }

                Vector2 resourcePosition = Vector2.Zero;
                // is the resource lying on the ground somewhere, and is there someone available to fetch it?
                for (int X = 0; X < resources.cells.GetLength(0); X++)
                {
                    for (int Y = 0; Y < resources.cells.GetLength(1); Y++)
                    {
                        if (X == destination.X && Y == destination.Y)
                        {
                            // that resource is already at the destination
                            continue;
                        }

                        ResourceGridEntry entry = resources.cells[X, Y];
                        if (entry.amount > 0 && shortage.typeSpec.Matches(entry.type))
                        {
                            FactoryWorker closestWorker = null;
                            float closestDistSqrFromWorker = 0;
                            foreach (FactoryWorker worker in idleWorkers)
                            {
                                if (worker.carrying == null)
                                {
                                    Vector2 position = worker.position;
                                    float distSqrFromWorker = worker.workerType.invMoveSpeedSquared * new Vector2(X - position.X, Y - position.Y).LengthSquared();

                                    if (closestWorker == null || closestDistSqrFromWorker > distSqrFromWorker)
                                    {
                                        closestWorker = worker;
                                        closestDistSqrFromWorker = distSqrFromWorker;
                                    }
                                }
                            }

                            if (closestWorker != null)
                            {
                                float distReturnTrip = closestWorker.workerType.invMoveSpeed * new Vector2(X - destination.X, Y - destination.Y).Length();
                                float distRoundTrip = distReturnTrip + (float)Math.Sqrt(closestDistSqrFromWorker);
                                if (closestWorkerRoundTrip == null || closestDistRoundTrip > distRoundTrip)
                                {
                                    closestDistRoundTrip = distRoundTrip;
                                    closestWorkerRoundTrip = closestWorker;
                                    resourcePosition = new Vector2(X, Y);
                                }
                            }
                        }
                    }
                }

                if (closestWorkerRoundTrip != null)
                {
                    idleWorkers.Remove(closestWorkerRoundTrip);
                    closestWorkerRoundTrip.SetCurrentJob(shortage.machine);

                    if (closestWorkerRoundTrip.carrying != null)
                    {
                        closestWorkerRoundTrip.HeadTowards(destination.ToVector2());
                    }
                    else
                    {
                        Vector2 closestWorkerVec = closestWorkerRoundTrip.position;
                        if (closestWorkerVec.X == resourcePosition.X && closestWorkerVec.Y == resourcePosition.Y)
                        {
                            // this guy is right there! pick up the resource
                            closestWorkerRoundTrip.PickUpResource(shortage.typeSpec);
                            idleWorkers.Remove(closestWorkerRoundTrip);
                        }
                        else
                        {
                            closestWorkerRoundTrip.HeadTowards(resourcePosition);
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        public void Draw(SpriteBatch spriteBatch, GridView view)
        {
            foreach (FactoryWorker w in workers)
            {
                Vector2 gridPos = w.position;
                Vector2 workerScreenPos = view.gridToScreenPos(gridPos + new Vector2(0.5f, 0.5f));
                w.Draw(spriteBatch, workerScreenPos);
            }
        }
    }
}
