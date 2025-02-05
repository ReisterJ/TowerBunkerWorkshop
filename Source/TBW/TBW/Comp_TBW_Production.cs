using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TBW
{
    public class Comp_TBW_Production : ThingComp
    {
        public CompProperties_TBW_Production Props 
        { 
            get
            {
                return (CompProperties_TBW_Production)props;
            }        
        }
    }

    public class CompProperties_TBW_Production:CompProperties
    {
        public CompProperties_TBW_Production()
        {
            compClass = typeof(Comp_TBW_Production);
        }
        public class ProductionUpgrade
        {
            public ResearchProjectDef research;

            public float factor;
        }
        public List<ProductionUpgrade> UpgradeResearchProjs; 

        
    }
}
