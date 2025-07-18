using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TBW
{
    public class Comp_TBW_Consumption: ThingComp
    {
        protected float stuffConsumptionPerDayBase;

        protected bool PowerOn;

        protected float stuffConsumptionPerDayMultiplier = 1f;

        //protected List<ConsumptionType> consumptionList ;

        public List<ConsumptionWorker> consumptionWorkerList = new List<ConsumptionWorker>();

        protected bool Operational;

        protected Comp_TBW_MultiPawnsHolder cachedCompMultiPawnsHolder;

        public CompProperties_TBW_Consumption Props
        {
            get
            {
                return (CompProperties_TBW_Consumption)this.props;
            }
        }

        public void InitConsumptionWorker()
        {
            foreach (ConsumptionType consumptionType in Props.consumptionList)
            {
                ConsumptionWorker worker = new ConsumptionWorker();
                worker.consumptionType = consumptionType;
                worker.currentFuel = 0;
                consumptionWorkerList.Add(worker);
            }
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            cachedCompMultiPawnsHolder = parent.TryGetComp<Comp_TBW_MultiPawnsHolder>();
            InitConsumptionWorker();
            PowerOn = parent.GetComp<CompPowerTrader>()?.PowerOn ?? false;
        }
        public override void CompTick()
        {
            base.CompTick();
            
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

        }

        public bool ShouldWork(ConsumptionWorker consumptionWorker)
        {
            if(consumptionWorker.consumptionType.critical && ( consumptionWorker.currentFuel <= 0))
            {
                //Operational = false;
                return false;
            }
            return true;

        }
        public void Process()
        {
            foreach ( ConsumptionWorker consumptionWorker in consumptionWorkerList )
            {
                if (ShouldWork(consumptionWorker))
                {
                    ConsumptionType consumptionType = consumptionWorker.consumptionType;
                    float amount = consumptionType.stuffConsumptionPerDay;
                    if (consumptionType.rateModifiedByPawnNum)
                    {
                        amount *= GetConsumptionMultiplier() * consumptionType.consumptionMultiplierPerPawn;
                    }
                    consumptionWorker.ConsumeProcess(amount);
                }
                else { return; }
            }
        }

       

        public float GetConsumptionMultiplier()
        {

            if( null != cachedCompMultiPawnsHolder )
            {
                return stuffConsumptionPerDayMultiplier * cachedCompMultiPawnsHolder.currentPawnNum;
            }
            return stuffConsumptionPerDayMultiplier;
        }
    }
    public class CompProperties_TBW_Consumption : CompProperties
    {
        public CompProperties_TBW_Consumption()
        {
            this.compClass = typeof(Comp_TBW_Consumption);
        }
        
        public List<ConsumptionType> consumptionList = new List<ConsumptionType>();

        public bool autoRefuel = true;

        public bool consumeOnlyWhenUsed;

        public bool consumeOnlyWhenPowered;

        public float consumptionMultiplier = 1f;

        public ThingFilter filter;

        
    }
    public class ConsumptionType
    {
        public ThingDef stuffDef;

        public float consumptionMultiplierPerPawn;

        public float stuffConsumptionPerDay;

        public bool critical;

        public float maxCapacity;

        public bool rateModifiedByPawnNum = false;
        public ConsumptionType Copy()
        {
            ConsumptionType copy = new ConsumptionType()
            {
                stuffDef = this.stuffDef,
                consumptionMultiplierPerPawn = this.consumptionMultiplierPerPawn,
                stuffConsumptionPerDay = this.stuffConsumptionPerDay,
                critical = this.critical    
            };
            return copy;
        }
    }
    public class ConsumptionWorker
    {
        public ConsumptionType consumptionType;

        public float currentFuel;

        public void Refuel(float amout)
        {
            if (currentFuel + amout > consumptionType.maxCapacity)
            {
                currentFuel = consumptionType.maxCapacity;
            }
            else
            {
                if(currentFuel + amout > currentFuel)
                {
                    currentFuel = currentFuel + amout;
                }
            }
        }
        public virtual void ConsumeProcess(float amout)
        {
            currentFuel = ( ( ( currentFuel - amout )  > 0) && ( ( currentFuel - amout ) < currentFuel )
                ? currentFuel - amout : 0f);
        }

    }
}
