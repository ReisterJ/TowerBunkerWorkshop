using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TBW
{
    public abstract class Upgrade_Utility
    {
       
        public ResearchProjectDef research;

        public string Upgrade_desc;
        
    }

    public class Upgrade_BioReactor : Upgrade_Utility
    {
        public float powerOutputPerPawnMultiplier;

        public float powerOutputMultiplier;

        public float powerOutputPerPawnOffset;

        public float powerOutputOffset;
    }
}
