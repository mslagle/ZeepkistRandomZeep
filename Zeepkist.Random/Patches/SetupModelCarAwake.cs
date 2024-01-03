using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zeepkist.RandomZeep.Patches
{
    [HarmonyPatch(typeof(SetupModelCar), "Awake")]
    public static class SetupModelCar_Awake
    {
        public static void Postfix(SetupModelCar __instance)
        {
            Plugin.modelCar = __instance;
        }
    }
}
