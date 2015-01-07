using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FactorioClicker.Simulation
{
    public class Notification_ResearchComplete: Notification
    {
        public string unlockName;

        public Notification_ResearchComplete(string aUnlockName)
        {
            unlockName = aUnlockName;
        }
    }

    public class ProductionTracker
    {
        public int yearTotal { get; private set; }
/*        int[] productionPerFrame;
        int[] rollingTotals;
        int currentFrameIndex;
        int framesToAverage;
        float averageDenominator;
 * */
        int currentFrameProduction = 0;
/*        int currentTotal = 0;
        int recordTotal = 0; // as in world record
 
        public float recordRate { get { return recordTotal * averageDenominator; } }
        public float currentRate { get { return currentTotal * averageDenominator; } }
        */

        public ProductionTracker()
        {
            yearTotal = 0;
 /*           framesToAverage = 120;

            productionPerFrame = new int[framesToAverage];
            rollingTotals = new int[60];
            currentFrameIndex = 0;

            averageDenominator = 60.0f / framesToAverage;
  * */
        }

        public void Update()
        {
            yearTotal += currentFrameProduction;
            currentFrameProduction = 0;
            /*            for (int Idx = 0; Idx < rollingTotals.Length; ++Idx)
                        {
                            rollingTotals[Idx] -= productionPerFrame[(currentFrameIndex+Idx)%productionPerFrame.Length];
                        }

                        productionPerFrame[currentFrameIndex] = currentFrameProduction;

                        currentTotal = 0;
                        for (int Idx = 0; Idx < rollingTotals.Length; ++Idx)
                        {
                            rollingTotals[Idx] += productionPerFrame[(currentFrameIndex + productionPerFrame.Length + Idx - rollingTotals.Length) % productionPerFrame.Length];

                            if (currentTotal < rollingTotals[Idx])
                            {
                                currentTotal = rollingTotals[Idx];
                            }
                        }

                        if (currentTotal > recordTotal)
                        {
                            recordTotal = currentTotal;
                        }

                        currentFrameProduction = 0;
                        currentFrameIndex = (currentFrameIndex+1)%productionPerFrame.Length;
             * */
        }

        public void OnProduced(int amount)
        {
            currentFrameProduction += amount;
        }

        public void OnYearEnd()
        {
            yearTotal = 0;
        }
    }

    public class ResearchRule
    {
        string unlockName;
        public bool unlocked { get; private set; }
        ResearchManager manager;

        public ResearchRule(string aUnlockName, ResearchManager aManager)
        {
            unlockName = aUnlockName;
            unlocked = false;
            manager = aManager;

            if (unlockName != null)
            {
                manager.LockBuilding(unlockName);
            }
        }

        public static ResearchRule newFromTemplate(JSONTable template, ResearchManager manager, Dictionary<string, ResourceType> resourceTypes)
        {
            String ruleType = template.getString("type");
            switch (ruleType)
            {
                case "resource":
                    return new ResearchRule_Resource(template, manager, resourceTypes);
            }

            return null;
        }

        public virtual void OnYearEnd()
        {
        }

        public void Unlock()
        {
            if (unlocked)
                return;

            unlocked = true;
            manager.UnlockBuilding(unlockName);
            NotificationManager.instance.Notify(new Notification_ResearchComplete(unlockName));
        }
    }

    public class ResearchRule_Resource: ResearchRule
    {
        ProductionTracker tracker;
        float amount;

        public ResearchRule_Resource(JSONTable template, ResearchManager manager, Dictionary<string, ResourceType> resourceTypes):
            base(template.getString("unlockBuilding", null), manager)
        {
            tracker = manager.GetProductionTracker(resourceTypes[template.getString("resourceType")]);
            amount = template.getFloat("amount");
        }

        public override void OnYearEnd()
        {
            if (tracker != null && tracker.yearTotal >= amount)
            {
                Unlock();
            }
        }
    }

    public class ResearchManager: Notifiable<Notification_ResourceProduced>
    {
        Dictionary<ResourceType, ProductionTracker> trackers;
        List<ResearchRule> researchRules;
        HashSet<string> unavailableResearch;

        public ResearchManager(JSONArray template, Dictionary<string, ResourceType> resourceTypes)
        {
            unavailableResearch = new HashSet<string>();
            researchRules = new List<ResearchRule>();
            trackers = new Dictionary<ResourceType, ProductionTracker>();
            NotificationManager.instance.AddNotification<Notification_ResourceProduced>(this);

            foreach (JSONTable researchRuleTemplate in template.asJSONTables())
            {
                researchRules.Add(ResearchRule.newFromTemplate(researchRuleTemplate, this, resourceTypes));
            }
        }

        public ProductionTracker GetProductionTracker(ResourceType resourceType)
        {
            ProductionTracker tracker;
            if( !trackers.ContainsKey(resourceType) )
            {
                tracker = new ProductionTracker();
                trackers[resourceType] = tracker;
            }
            else
            {
                tracker = trackers[resourceType];
            }
            return tracker;
        }

        public void UnlockBuilding(string name)
        {
            unavailableResearch.Remove(name);
        }

        public void LockBuilding(string name)
        {
            unavailableResearch.Add(name);
        }

        public bool IsResearched(string name)
        {
            return !unavailableResearch.Contains(name);
        }

        public void Update()
        {
            foreach (ProductionTracker tracker in trackers.Values)
            {
                tracker.Update();
            }
        }

        public void OnYearEnd()
        {
            //NB: order matters here - rules must be resolved before trackers get reset
            foreach (ResearchRule rule in researchRules)
            {
                rule.OnYearEnd();
            }

            foreach (ProductionTracker tracker in trackers.Values)
            {
                tracker.OnYearEnd();
            }
        }

/*        public float GetProductionRate(ResourceType type)
        {
            if (trackers.ContainsKey(type))
            {
                return trackers[type].currentRate;
            }
            else
            {
                return 0;
            }
        }*/

        public void Notify(Notification_ResourceProduced note)
        {
            if (trackers.ContainsKey(note.resourceType))
            {
                trackers[note.resourceType].OnProduced(note.amount);
            }
        }
    }
}
