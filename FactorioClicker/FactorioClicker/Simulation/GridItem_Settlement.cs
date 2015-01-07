using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;
using FactorioClicker.Graphics;
using FactorioClicker.UI;
using Microsoft.Xna.Framework;

namespace FactorioClicker.Simulation
{
    public class PrebuiltSettlementTemplate
    {
        String settlementName;
        GridPoint position;
        List<PrebuiltBuildingTemplate> contents;

        public PrebuiltSettlementTemplate(JSONTable template)
        {
            settlementName = template.getString("type");
            Vector2 posVec = template.getArray("position").toVector2();
            position = new GridPoint((int)posVec.X, (int)posVec.Y);

            contents = new List<PrebuiltBuildingTemplate>();
            foreach (JSONTable buildingTemplate in template.getArray("contents", JSONArray.empty).asJSONTables())
            {
                contents.Add(new PrebuiltBuildingTemplate(buildingTemplate));
            }
        }

        public void Build(MapGridView spaceView)
        {
            GridItem_Settlement settlement = spaceView.AddStation(Game1.instance.settlementTypes[settlementName], position);

            foreach (PrebuiltBuildingTemplate buildingTemplate in contents)
            {
                buildingTemplate.Build(settlement.contents);
            }
        }
    }

    public class PrebuiltBuildingTemplate
    {
        String buildingName;
        GridPoint position;

        public PrebuiltBuildingTemplate(JSONTable template)
        {
            buildingName = template.getString("type");
            Vector2 posVec = template.getArray("position").toVector2();
            position = new GridPoint((int)posVec.X, (int)posVec.Y);
        }

        public void Build(Grid grid)
        {
            GridItem_Building building = Game1.instance.buildingTypes[buildingName].Clone_Building();
            building.gridPosition = position;
            building.canMove = false;
            grid.Add(building);
        }
    }

    public class SettlementType
    {
        public GridSize contentSize;
        public int powerGenerated;
        public List<PrebuiltBuildingTemplate> prebuiltTemplates;
        public HashSet<TerrainType> buildableTerrain;

        public SettlementType(JSONTable template, ContentManager content)
        {
            contentSize = new GridSize(template.getArray("contentSize", null));
            buildableTerrain = new HashSet<TerrainType>();
            foreach (String terrainTypeName in template.getArray("terrainTypes", JSONArray.empty).asStrings())
            {
                TerrainType terrainType;
                if (Enum.TryParse<TerrainType>(terrainTypeName, out terrainType))
                {
                    buildableTerrain.Add(terrainType);
                }
            }

            powerGenerated = template.getInt("powerGenerated", 0);
            prebuiltTemplates = new List<PrebuiltBuildingTemplate>();
            foreach (JSONTable table in template.getArray("prebuilt", JSONArray.empty).asJSONTables())
            {
                prebuiltTemplates.Add(new PrebuiltBuildingTemplate(table));
            }
        }
    }

    public class GridItem_Settlement: GridItem, PseudoBuilding
    {
        public SettlementType settlementType;
        public Grid contents;
        public WorkerManager workerManager = new WorkerManager();

        // Shortages and workers tracked from the perspective of the map grid
        public List<ResourceShortage> resourceShortages { get; private set; }
        public List<OperatorShortage> operatorShortages { get; private set; }
        public List<FactoryWorkerSlot> workerSlots { get; private set; }
        public int workerFoodDuration { get; private set; }
        public GridPoint workerSpawnPosition { get { return gridPosition; } }

        public GridItem_Settlement(SettlementType aSettlementType, GridItemType aItemType, GridPoint aGridPosition, Rotation90 aRotation, bool aCanMove):
            base(aItemType, aGridPosition, aRotation, aCanMove)
        {
            settlementType = aSettlementType;
            resourceShortages = new List<ResourceShortage>();
            operatorShortages = new List<OperatorShortage>();
            workerSlots = new List<FactoryWorkerSlot>();
        }

        public GridItem_Settlement(String name, JSONTable template, ContentManager content): base(name, template, content)
        {
            settlementType = new SettlementType(template, content);
            resourceShortages = new List<ResourceShortage>();
            operatorShortages = new List<OperatorShortage>();
            workerSlots = new List<FactoryWorkerSlot>();
        }

        public void CreateGrid(ResourceGrid parentGrid, GridPoint parentPosition)
        {
            contents = new Grid(settlementType.contentSize, new ResourceGrid(settlementType.contentSize, parentGrid, parentPosition));

            foreach (PrebuiltBuildingTemplate prebuilt in settlementType.prebuiltTemplates)
            {
                prebuilt.Build(contents);
            }
        }

        public bool CanBuild(TerrainType terrainType)
        {
            return settlementType.buildableTerrain.Contains(terrainType);
        }

        public void Update()
        {
            // Update static effects of buildings (could do this on add/remove, instead?)
            int storedMax = 0;
            int consumedMax = 0;
            operatorShortages.Clear();
            resourceShortages.Clear();
            workerSlots.Clear();

            foreach(GridItem_Building building in contents.items)
            {
                storedMax += building.buildingType.powerStore;
                foreach (Machine m in building.machines)
                {
                    if (m.machineType.powerCost > 0)
                    {
                        consumedMax += m.machineType.powerCost;
                    }
                }

                foreach(FactoryWorkerSlot mapWorkerSlot in building.mapWorkerSlots)
                {
                    workerSlots.Add(mapWorkerSlot);
                }
            }
            contents.resources.UpdatePower(settlementType.powerGenerated, storedMax, consumedMax);

            // Machines produce resources
            foreach (GridItem_Building item in contents.items)
            {
                foreach (Machine m in item.machines)
                {
                    m.RunProduction(contents.resources);
                }
            }

            // Items consume resources
            foreach (GridItem_Building item in contents.items)
            {
                item.Update();
                workerManager.UpdateItem(item);

                if (item.resourceShortages.Count > 0 || item.operatorShortages.Count > 0)
                {
                    foreach (ResourceShortage shortage in item.resourceShortages)
                    {
                        resourceShortages.Add(shortage);
                    }
                }
            }

            // Update workers
            workerManager.Update(contents);
        }

        public void ResetYear()
        {
            for (int X = 0; X < contents.size.Width; X++ )
            {
                for (int Y = 0; Y < contents.size.Height; Y++)
                {
                    contents.resources.cells[X, Y].amount = 0;
                    contents.resources.cells[X, Y].claimedAmount = 0;
                    contents.resources.cells[X, Y].type = null;
                }
            }

            foreach(GridItem_Building building in contents.items)
            {
                building.ResetAllMachines();
            }

            foreach(FactoryWorker worker in workerManager.workers)
            {
                worker.carrying = null;
                worker.SetCurrentJob(null);
                worker.startPosition = worker.home.workerSpawnPosition.ToVector2();
                worker.endPosition = worker.home.workerSpawnPosition.ToVector2();
                worker.StopMoving();
            }
        }

        public override GridItem Clone()
        {
            return Clone_SpaceStation();
        }

        public GridItem_Settlement Clone_SpaceStation()
        {
            GridItem_Settlement result = new GridItem_Settlement(settlementType, itemType, gridPosition, rotation, canMove);

            return result;
        }

        public void Delete(GridItem_Building item)
        {
            workerManager.UnlistPriorityItem(item);
            item.ResetAllMachines();
            contents.Remove(item);
            foreach (FactoryWorkerSlot slot in item.workerSlots)
            {
                slot.DeleteWorker();
            }
        }
    }
}
