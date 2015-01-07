using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FactorioClicker.UI;
using FactorioClicker.Graphics;

namespace FactorioClicker.Simulation
{
    interface ResourceSelectionRule
    {
        GridPoint Select(List<GridPoint> available, ResourceGrid resources);
    }

    class ResourceSelectionRule_First: ResourceSelectionRule
    {
        public static ResourceSelectionRule instance = new ResourceSelectionRule_First();

        public GridPoint Select(List<GridPoint> available, ResourceGrid resources)
        {
            return available[0];
        }
    }

    class ResourceSelectionRule_Most : ResourceSelectionRule
    {
        public static ResourceSelectionRule instance = new ResourceSelectionRule_Most();

        public GridPoint Select(List<GridPoint> available, ResourceGrid resources)
        {
            GridPoint result = available[0];
            int bestAmount = resources.AmountAvailableAt(result);
            for (int Idx = 1; Idx < available.Count; ++Idx)
            {
                GridPoint currentPoint = available[Idx];
                int currentAmount = resources.AmountAvailableAt(currentPoint);
                if (currentAmount > bestAmount)
                {
                    result = currentPoint;
                    bestAmount = currentAmount;
                }
            }
            return result;
        }
    }

    class ResourceSelectionRule_Least : ResourceSelectionRule
    {
        public static ResourceSelectionRule instance = new ResourceSelectionRule_Least();

        public GridPoint Select(List<GridPoint> available, ResourceGrid resources)
        {
            GridPoint result = available[0];
            int bestAmount = resources.AmountAvailableAt(result);
            for (int Idx = 1; Idx < available.Count; ++Idx)
            {
                GridPoint currentPoint = available[Idx];
                int currentAmount = resources.AmountAvailableAt(currentPoint);
                if (currentAmount < bestAmount)
                {
                    result = currentPoint;
                    bestAmount = currentAmount;
                }
            }
            return result;
        }
    }

    public class ResourceClaimTicket
    {
        public readonly ResourceGrid resources;
        public readonly GridPoint position;
        public readonly ResourceType type;
        public readonly int amount;
        public readonly bool isInput;

        public ResourceClaimTicket(ResourceGrid aResources, GridPoint aPosition, ResourceType aType, int aAmount, bool aIsInput)
        {
            resources = aResources;
            position = aPosition;
            type = aType;
            amount = aAmount;
            isInput = aIsInput;
        }

        public void DoConsume()
        {
            resources.ConsumeClaimedResource(position, type, amount);
        }

        public void Refund()
        {
            resources.RefundClaimedResource(position, type, amount);
        }
    }

    public interface ResourceTypeSpec
    {
        bool Matches(ResourceType inputType);
    }

    class ResourceTypeSpec_Equals : ResourceTypeSpec
    {
        ResourceType type;
        public ResourceTypeSpec_Equals(ResourceType aType)
        {
            type = aType;
        }

        public bool Matches(ResourceType inputType)
        {
            return type == inputType;
        }
    }

    class ResourceTypeSpec_Subtypes : ResourceTypeSpec
    {
        List<String> subtypes;
        public ResourceTypeSpec_Subtypes(List<String> aSubtypes)
        {
            subtypes = aSubtypes;
        }

        public bool Matches(ResourceType inputType)
        {
            foreach (String subtype in subtypes)
            {
                if (!inputType.subtypes.Contains(subtype))
                    return false;
            }
            return true;
        }
    }

    public enum ResourceIntakeMode
    {
        NORMAL,
        MINING,
        WELL,
        ATMOSPHERE,
    }

    public class ResourceConsumeRule
    {
        ResourceIntakeMode intakeMode;
        List<GridPoint> possiblePositions;
        public ResourceTypeSpec typeSpec;
        bool isInput;
        int amount;
        ResourceSelectionRule resourceSelectionRule;

        public ResourceConsumeRule(JSONTable template, Dictionary<String, ResourceType> resourceTypes)
        {
            JSONArray positionTemplate = template.getArray("position", null);
            if (positionTemplate != null)
            {
                possiblePositions = new List<GridPoint>() { new GridPoint(positionTemplate) };
            }
            else
            {
                possiblePositions = new List<GridPoint>();
                JSONArray anyPositionTemplate = template.getArray("anyPosition", JSONArray.empty);
                for (int Idx = 0; Idx < anyPositionTemplate.Length; ++Idx)
                {
                    possiblePositions.Add( new GridPoint(anyPositionTemplate.getArray(Idx)) );
                }
            }

            String typeName = template.getString("type", null);
            if (typeName == null || typeName == "input")
            {
                JSONArray subtypeStrings = template.getArray("subtypes", null);
                List<String> subtypes = new List<string>();
                if (subtypeStrings != null)
                {
                    for (int Idx = 0; Idx < subtypeStrings.Length; ++Idx)
                    {
                        subtypes.Add( subtypeStrings.getString(Idx) );
                    }
                }
                typeSpec = new ResourceTypeSpec_Subtypes(subtypes);
                isInput = (typeName == "input");
            }
            else
            {
                typeSpec = new ResourceTypeSpec_Equals(resourceTypes[typeName]);
            }
            amount = template.getInt("amount", 1);

            resourceSelectionRule = ResourceSelectionRule_First.instance;


            JSONArray flags = template.getArray("flags", null);
            if (flags != null)
            {
                for (int Idx = 0; Idx < flags.Length; ++Idx)
                {
                    String flag = flags.getString(Idx);
                    if (flag == "equalize")
                    {
                        resourceSelectionRule = ResourceSelectionRule_Most.instance;
                    }
                    else if (flag == "mine")
                    {
                        intakeMode = ResourceIntakeMode.MINING;
                    }
                    else if (flag == "well")
                    {
                        intakeMode = ResourceIntakeMode.WELL;
                    }
                    else if (flag == "atmosphere")
                    {
                        intakeMode = ResourceIntakeMode.ATMOSPHERE;
                    }
                }
            }
        }

        public bool CanConsume(GridPoint origin, Rotation90 rotation, ResourceGrid resources)
        {
            foreach (GridPoint position in possiblePositions)
            {
                GridPoint rotatedPosition = position.RotateBy(rotation);
                if (resources.HasResource(intakeMode, origin + rotatedPosition, typeSpec, amount))
                {
                    return true;
                }
            }

            return false;
        }

        public ResourceClaimTicket ClaimResources(GridPoint origin, Rotation90 rotation, ResourceGrid resources)
        {
            List<GridPoint> available = new List<GridPoint>();
            foreach (GridPoint position in possiblePositions)
            {
                GridPoint rotatedPosition = position.RotateBy(rotation);
                GridPoint target = origin + rotatedPosition;
                if (resources.HasResource(intakeMode, target, typeSpec, amount))
                {
                    available.Add(target);
                }
            }

            GridPoint selectedPoint = resourceSelectionRule.Select(available, resources);
            return resources.ClaimResource(intakeMode, selectedPoint, typeSpec, amount, isInput);
        }

        public GridPoint GetPrimaryPosition()
        {
            return possiblePositions[0];
        }
    }

    public enum ResourceOutputMode
    {
        NORMAL,
        EXPORT,
    }

    public class ResourceProduceRule
    {
        ResourceOutputMode outputMode = ResourceOutputMode.NORMAL;
        List<GridPoint> possiblePositions;
        ResourceType type;
        int amount;
        bool isInput;
        bool isLogistics;
        ResourceSelectionRule resourceSelectionRule;

        public ResourceProduceRule(JSONTable template, Dictionary<String, ResourceType> resourceTypes)
        {
            JSONArray positionTemplate = template.getArray("position", null);
            if (positionTemplate != null)
            {
                possiblePositions = new List<GridPoint>() { new GridPoint(template.getArray("position")) };
            }
            else
            {
                possiblePositions = new List<GridPoint>();
                JSONArray anyPositionTemplate = template.getArray("anyPosition", null);
                if (anyPositionTemplate != null)
                {
                    for (int Idx = 0; Idx < anyPositionTemplate.Length; ++Idx)
                    {
                        possiblePositions.Add(new GridPoint(anyPositionTemplate.getArray(Idx)));
                    }
                }
            }

            String typeName = template.getString("type");
            if (typeName == "input")
            {
                type = null;
                isInput = true;
                isLogistics = true;
            }
            else
            {
                type = resourceTypes[template.getString("type")];
            }
            amount = template.getInt("amount", 1);

            resourceSelectionRule = ResourceSelectionRule_First.instance;

            JSONArray flags = template.getArray("flags", null);
            if (flags != null)
            {
                for (int Idx = 0; Idx < flags.Length; ++Idx)
                {
                    String flag = flags.getString(Idx);
                    if (flag == "equalize")
                    {
                        resourceSelectionRule = ResourceSelectionRule_Least.instance;
                    }

                    if (flag == "logistics")
                    {
                        isLogistics = true;
                    }

                    if (flag == "export")
                    {
                        outputMode = ResourceOutputMode.EXPORT;
                    }
                }
            }
        }

        public bool CanProduce(GridPoint origin, Rotation90 rotation, ResourceGrid resources, ResourceType inputType)
        {
            ResourceType productionType = isInput ? inputType : type;

            foreach(GridPoint position in possiblePositions)
            {
                GridPoint rotatedPosition = position.RotateBy(rotation);
                if (resources.HasSpaceForResource(outputMode, origin + rotatedPosition, productionType, amount))
                {
                    return true;
                }
            }
            return false;
        }

        public void DoProduce(GridPoint origin, Rotation90 rotation, ResourceGrid resources, ResourceType inputType)
        {
            ResourceType productionType = isInput ? inputType : type;

            List<GridPoint> availablePositions = new List<GridPoint>();
            foreach (GridPoint position in possiblePositions)
            {
                GridPoint rotatedPosition = position.RotateBy(rotation);
                if (resources.HasSpaceForResource(outputMode, origin + rotatedPosition, productionType, amount))
                {
                    availablePositions.Add(origin + rotatedPosition);
                }
            }
            GridPoint target = resourceSelectionRule.Select(availablePositions, resources);
            resources.TryProduceResource(outputMode, target, productionType, amount, isLogistics);
        }
    }

    public class MachineType
    {
        public List<ResourceProduceRule> producesEachFrame;
        public List<ResourceConsumeRule> consumesEachFrame;
        public List<ResourceConsumeRule> consumesInitially;
        public List<ResourceProduceRule> producesFinally;
        public float duration;
        public float cooldown;
        public int powerCost;
        public int income;
        public List<GridPoint> operatorPositions;
        public int workerFeedDuration;

        public MachineType(JSONTable template, Dictionary<String, ResourceType> resourceTypes)
        {
            consumesEachFrame = new List<ResourceConsumeRule>();
            producesEachFrame = new List<ResourceProduceRule>();
            consumesInitially = new List<ResourceConsumeRule>();
            producesFinally = new List<ResourceProduceRule>();

            duration = template.getFloat("duration", 1);
            cooldown = template.getFloat("cooldown", 0);
            powerCost = template.getInt("powerCost", -template.getInt("powerProduced", 0));
            income = template.getInt("income", 0);
            workerFeedDuration = template.getInt("workerFeedDuration", 0);

            JSONArray consumesTemplate = template.getArray("consumes", null);            
            if (consumesTemplate != null)
            {
                for (int Idx = 0; Idx < consumesTemplate.Length; ++Idx)
                {
                    consumesInitially.Add(new ResourceConsumeRule(consumesTemplate.getJSON(Idx), resourceTypes));
                }
            }

            JSONArray producesTemplate = template.getArray("produces", null);
            if (producesTemplate != null)
            {
                for (int Idx = 0; Idx < producesTemplate.Length; ++Idx)
                {
                    producesFinally.Add(new ResourceProduceRule(producesTemplate.getJSON(Idx), resourceTypes));
                }
            }

            JSONArray consumesEachFrameTemplate = template.getArray("consumesEachFrame", null);
            if (consumesEachFrameTemplate != null)
            {
                for (int Idx = 0; Idx < consumesEachFrameTemplate.Length; ++Idx)
                {
                    consumesEachFrame.Add(new ResourceConsumeRule(consumesEachFrameTemplate.getJSON(Idx), resourceTypes));
                }
            }

            JSONArray producesEachFrameTemplate = template.getArray("producesEachFrame", null);
            if (producesEachFrameTemplate != null)
            {
                for (int Idx = 0; Idx < producesEachFrameTemplate.Length; ++Idx)
                {
                    producesEachFrame.Add(new ResourceProduceRule(producesEachFrameTemplate.getJSON(Idx), resourceTypes));
                }
            }

            JSONArray workerPositionsTemplate = template.getArray("workerPositions", JSONArray.empty);
            operatorPositions = new List<GridPoint>();
            foreach(JSONArray positionTemplate in workerPositionsTemplate.asJSONArrays())
            {
                operatorPositions.Add(positionTemplate.toGridPoint());
            }
        }
    }

    public class Machine
    {
        public GridItem_Building building;
        public MachineType machineType;
        public float progress;
        public bool isRunning;
        public ResourceConsumeRule starvedConsumer;
        public int timeStarved;
        public bool isOutOfPower;
        public bool isReadyToProduce;
        public bool isProductionBlocked;
        public int workerFoodDuration;
        public List<ResourceClaimTicket> resourceTickets;
        public List<FactoryWorker> operators;
        public FactoryWorker externalWorker;

        public Machine(GridItem_Building aBuilding, MachineType aMachineType)
        {
            building = aBuilding;
            machineType = aMachineType;
            progress = 0;
            resourceTickets = new List<ResourceClaimTicket>();
            operators = new List<FactoryWorker>();
        }

        public void Update(GridPoint origin, Rotation90 rotation, ResourceGrid resources)
        {
            isRunning = true;
            isOutOfPower = false;
            ResourceConsumeRule oldStarvedConsumer = starvedConsumer;
            starvedConsumer = null;
            isProductionBlocked = false;
            float TIMESTEP = 1;

            if (workerFoodDuration > 0)
            {
                workerFoodDuration--;
            }
            
            if (isReadyToProduce)
            {
                // stop running until the output hopper is clear
                isRunning = false;
                isProductionBlocked = true;
                return;
            }

            if (machineType.powerCost > 0)
            {
                if (resources.powerAvailable >= machineType.powerCost)
                {
                    resources.powerConsumed += machineType.powerCost;
                }
                else
                {
                    // not enough power to run
                    isRunning = false;
                    isOutOfPower = true;
                    return;
                }
            }

            if (progress >= machineType.duration)
            {
                progress += TIMESTEP;
                if (progress >= (machineType.duration + machineType.cooldown))
                {
                    progress = 0;
                    // and continue to the rest of the code
                }
                else
                {
                    // cooling down
                    return;
                }
            }

            List<ResourceConsumeRule> consumeList;
            if (progress == 0)
            {
                consumeList = machineType.consumesInitially;
            }
            else
            {
                consumeList = machineType.consumesEachFrame;
            }

            foreach (ResourceConsumeRule consumer in consumeList)
            {
                if (!consumer.CanConsume(origin, rotation, resources))
                {
                    isRunning = false;
                    if (oldStarvedConsumer == consumer)
                    {
                        timeStarved++;
                    }
                    else
                    {
                        timeStarved = 0;
                    }
                    starvedConsumer = consumer;
                    return;
                }
            }

            if (NeedsOperator())
            {
                isRunning = false;
                return;
            }

            // All checks passed, run the machine!
            foreach (ResourceConsumeRule consumer in consumeList)
            {
                resourceTickets.Add(consumer.ClaimResources(origin, rotation, resources));
            }

            if (machineType.powerCost < 0)
            {
                resources.powerGeneratedNextFrame += (-machineType.powerCost);
            }

            if (workerFoodDuration < machineType.workerFeedDuration)
            {
                workerFoodDuration = machineType.workerFeedDuration;
            }

            float oldProgress = progress;
            progress += TIMESTEP;

            if (progress <= machineType.duration)
            {
                isReadyToProduce = true;
            }
        }

        public void RunProduction(ResourceGrid resources)
        {
            if (!isReadyToProduce)
                return;

            ResourceType inputType = null;
            foreach (ResourceClaimTicket ticket in resourceTickets)
            {
                if (ticket.isInput)
                    inputType = ticket.type;
            }

            List<ResourceProduceRule> produceList;
            if (progress == machineType.duration)
            {
                produceList = machineType.producesFinally;
            }
            else if (progress > 0)
            {
                produceList = machineType.producesEachFrame;
            }
            else
            {
                produceList = new List<ResourceProduceRule>();
            }

            foreach (ResourceProduceRule producer in produceList)
            {
                if (!producer.CanProduce(building.machineOrigin, building.rotation, resources, inputType))
                {
                    isRunning = false;
                    return;
                }
            }

            // all checks passed, let's produce stuff!

            if (progress >= machineType.duration)
            {
                foreach (ResourceClaimTicket ticket in resourceTickets)
                {
                    ticket.DoConsume();
                }
                resourceTickets.Clear();
            }

            foreach (ResourceProduceRule producer in produceList)
            {
                producer.DoProduce(building.machineOrigin, building.rotation, resources, inputType);
            }

            Game1.instance.money += machineType.income;

            if (progress >= machineType.duration)
            {
                ReleaseOperators();
            }

            isReadyToProduce = false;
        }

        public void Reset()
        {
            foreach (ResourceClaimTicket ticket in resourceTickets)
            {
                ticket.Refund();
            }

            if (externalWorker != null)
            {
                externalWorker.SetCurrentJob(null);
            }
            ReleaseOperators();

            progress = 0;
            isRunning = false;
            isReadyToProduce = false;
            resourceTickets.Clear();
        }

        public bool NeedsOperator()
        {
            if (isProductionBlocked || isOutOfPower || starvedConsumer != null || progress == 0 || progress >= machineType.duration)
            {
                return false;
            }

            return operators.Count < machineType.operatorPositions.Count;
        }

        public GridPoint NextOperatorPosition()
        {
            return building.machineOrigin + machineType.operatorPositions[operators.Count].RotateBy(building.rotation);
        }

        public void AddOperator(FactoryWorker worker)
        {
            if (operators.Count < machineType.operatorPositions.Count)
            {
                operators.Add(worker);
                worker.operating = this;
            }
        }

        public void RemoveOperator(FactoryWorker worker)
        {
            operators.Remove(worker);
            if (worker.operating == this)
            {
                worker.operating = null;
            }
        }

        public void ReleaseOperators()
        {
            foreach (FactoryWorker worker in operators)
            {
                worker.operating = null;
                worker.currentJob = null;
            }
            operators.Clear();
        }
    }
}
