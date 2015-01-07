using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FactorioClicker.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace FactorioClicker.Simulation
{
    public class BuildingType
    {
        public List<MachineType> machineTypes;
        public HashSet<String> stationTypes;
        public int powerStore;
        public int numWorkers;
        public int numMapWorkers;
        public FactoryWorkerType workerType;
        public bool workersNeedFood;
        public int initialWorkerFeedDuration;

        public BuildingType(JSONTable template, ContentManager content)
        {
            Dictionary<String, ResourceType> resourceTypes = Game1.instance.resourceTypes;

            machineTypes = new List<MachineType>();
            JSONArray machinesTemplate = template.getArray("processes", null);
            if (machinesTemplate != null)
            {
                for (int Idx = 0; Idx < machinesTemplate.Length; ++Idx)
                {
                    machineTypes.Add(new MachineType(machinesTemplate.getJSON(Idx), resourceTypes));
                }
            }
            else
            {
                machineTypes.Add(new MachineType(template, resourceTypes));
            }

            if (template.hasKey("stationTypes"))
            {
                stationTypes = new HashSet<string>();
                foreach (String stationName in template.getArray("stationTypes", JSONArray.empty).asStrings())
                {
                    stationTypes.Add(stationName);
                }
            }

            if (template.hasKey("initialWorkerFeedDuration"))
            {
                initialWorkerFeedDuration = template.getInt("initialWorkerFeedDuration");
                workersNeedFood = true;
            }
            else
            {
                initialWorkerFeedDuration = 1;
                workersNeedFood = false;
            }
            powerStore = template.getInt("powerStore", 0);
            numWorkers = template.getInt("workers", 0);
            numMapWorkers = template.getInt("mapWorkers", 0);
            if (numWorkers > 0 || numMapWorkers > 0)
            {
                workerType = Game1.instance.workerTypes[template.getString("workerType", "normal")];
            }
        }
    }

    public class ResourceShortage
    {
        public Machine machine { get; private set; }
        public ResourceTypeSpec typeSpec { get; private set; }
        public int amount { get; private set; }
        public GridPoint position { get; private set; }

        public ResourceShortage(Machine aMachine, ResourceTypeSpec aTypeSpec, int aAmount, GridPoint aPosition)
        {
            machine = aMachine;
            typeSpec = aTypeSpec;
            amount = aAmount;
            position = aPosition;
        }
    }

    public class OperatorShortage
    {
        public Machine machine { get; private set; }
        public GridPoint position { get; private set; }

        public OperatorShortage(Machine aMachine, GridPoint aPosition)
        {
            machine = aMachine;
            position = aPosition;
        }
    }

    public interface PseudoBuilding
    {
        List<ResourceShortage> resourceShortages { get; }
        List<OperatorShortage> operatorShortages { get; }
        List<FactoryWorkerSlot> workerSlots { get; }
        int workerFoodDuration { get; }
        GridPoint workerSpawnPosition { get; }
    }

    public class GridItem_Building : GridItem, PseudoBuilding
    {
        public BuildingType buildingType;
        public List<Machine> machines;
        public bool isOutOfPower;
//        public bool isOutOfResources;
        public bool wasOutOfResourcesLastFrame;
//        public bool isReadyForOperator;
        public List<FactoryWorkerSlot> mapWorkerSlots { get; private set; }

        public int workerFoodDuration { get; private set; }
        public List<ResourceShortage> resourceShortages { get; private set; }
        public List<OperatorShortage> operatorShortages { get; private set; }
        public List<FactoryWorkerSlot> workerSlots { get; private set; }
        public GridPoint workerSpawnPosition { get { return gridPosition; } }

        public GridItem_Building(BuildingType aBuildingType, GridItemType aItemType, GridPoint aGridPosition, Rotation90 aRotation, bool aCanMove) :
            base(aItemType, aGridPosition, aRotation, aCanMove)
        {
            buildingType = aBuildingType;
            machines = new List<Machine>();
            workerFoodDuration = buildingType.initialWorkerFeedDuration;
            resourceShortages = new List<ResourceShortage>();
            operatorShortages = new List<OperatorShortage>();

            foreach (MachineType mt in buildingType.machineTypes)
            {
                machines.Add(new Machine(this, mt));
            }

            init();
        }

        public GridItem_Building(String name, JSONTable template, ContentManager content): base(name, template, content)
        {
            buildingType = new BuildingType(template, content);
            canMove = true;
            init();
        }

        private void init()
        {
            resourceShortages = new List<ResourceShortage>();
            operatorShortages = new List<OperatorShortage>();
            workerSlots = new List<FactoryWorkerSlot>();
            for (int Idx = 0; Idx < buildingType.numWorkers; ++Idx)
            {
                workerSlots.Add(new FactoryWorkerSlot(buildingType.workerType, this));
            }

            mapWorkerSlots = new List<FactoryWorkerSlot>();
            for (int Idx = 0; Idx < buildingType.numMapWorkers; ++Idx)
            {
                mapWorkerSlots.Add(new FactoryWorkerSlot(buildingType.workerType, this));
            }
        }

        public GridPoint machineOrigin
        {
            get
            {
                switch (rotation)
                {
                    case Rotation90.Rot90: return new GridPoint(gridPosition.X + itemType.gridSize.Height - 1, gridPosition.Y);
                    case Rotation90.Rot180: return new GridPoint(gridPosition.X + itemType.gridSize.Width - 1, gridPosition.Y + itemType.gridSize.Height - 1);
                    case Rotation90.Rot270: return new GridPoint(gridPosition.X, gridPosition.Y + itemType.gridSize.Width - 1);
                    default: return gridPosition;
                }
            }
        }


        public bool CanPlaceIn(GridItem_Settlement station)
        {
            if (buildingType.stationTypes != null && !buildingType.stationTypes.Contains(station.itemType.name))
            {
                return false;
            }

            // other restrictions?

            return true;
        }

        public override GridItem Clone()
        {
            return Clone_Building();
        }

        public GridItem_Building Clone_Building()
        {
            return new GridItem_Building(buildingType, itemType, gridPosition, rotation, canMove);
        }

        public override void PrepareToMove()
        {
            ResetAllMachines();
        }

        public override void Delete()
        {
            ResetAllMachines();
            container.Remove(this);
        }

        public virtual void Update()
        {
            isOutOfPower = false;
            resourceShortages.Clear();
            operatorShortages.Clear();

            if (workerFoodDuration > 0 && buildingType.workersNeedFood)
            {
                workerFoodDuration--;
            }

            if (machines != null)
            {
                foreach (Machine m in machines)
                {
                    m.Update(machineOrigin, rotation, container.resources);

                    if (workerFoodDuration < m.workerFoodDuration)
                    {
                        workerFoodDuration = m.workerFoodDuration;
                    }

                    if (!m.isReadyToProduce)
                    {
                        if (m.isOutOfPower)
                        {
                            isOutOfPower = true;
                        }
                        if (m.starvedConsumer != null)
                        {
                            resourceShortages.Add(new ResourceShortage(m, m.starvedConsumer.typeSpec, 1, LocalToGlobalPosition(m.starvedConsumer.GetPrimaryPosition())));
                        }
                        if (m.NeedsOperator())
                        {
                            operatorShortages.Add(new OperatorShortage(m, gridPosition));
                        }
                    }
                }
            }
        }

        public void ResetAllMachines()
        {
            if (container != null && container.resources != null)
            {
                foreach (Machine m in machines)
                {
                    m.Reset();
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch, Rectangle rect)
        {
            base.Draw(spriteBatch, rect);

            if (isOutOfPower)
            {
                Rectangle powerSymbolRect = (rect.Center.ToVector2() - new Vector2(16,16)).makeRectangle(32,32);
                Game1.instance.powerSymbolImage.Draw(spriteBatch, powerSymbolRect);
            }
        }
    }
}
