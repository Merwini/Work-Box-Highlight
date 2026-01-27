using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace WorkBoxHighlight;

public static class WorkTypeCache
{
    public static Dictionary<Pawn, WorkTypeDef> workTypeDefCache = new Dictionary<Pawn, WorkTypeDef>();
}
