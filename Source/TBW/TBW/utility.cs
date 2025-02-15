using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace TBW
{
    public static class utility
    {
        public static void ifDebugLog(string text)
        {
#if DEBUG
            Log.Message(text);
#endif
        }

        public static void ifDebugLog(object obj)
        {
#if DEBUG
            Log.Message(obj);
#endif
        }

        
    }
}
