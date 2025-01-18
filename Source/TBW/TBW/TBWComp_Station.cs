using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TBW
{
    public class TBWComp_Station : ThingComp
    {
        public TBWCompProperties_Station Props
        {
            get
            {
                return (TBWCompProperties_Station)this.props;
            }
        }
    }

    public class TBWCompProperties_Station : CompProperties 
    {
        public TBWCompProperties_Station() {
            this.compClass = typeof(TBWComp_Station);
        }
        public int maxPawnNum;
        public bool isDefensive;
    }
}
