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
            return;

#if DEBUG
            //Log.Message(text);
#endif
        }

        public static void ifDebugLog(object obj)
        {
            return;
#if DEBUG
            //Log.Message(obj);
#endif
        }
        public static void ifDebugLog(object obj, bool output)
        {
            if (!output) return;
#if DEBUG
            Log.Message(obj);
#endif
        }

        public enum HoldPawnOffsetTags
        {
            skills,
            gender,
            age,
            bodysize,
            conciousness,
            nutrition
        }
    }
}
