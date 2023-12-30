using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Zeepkist.RandomZeep.Patches
{
    [HarmonyPatch(typeof(MainMenuUI), "Awake")]
    public static class MainMenuUi_Awake
    {
        public static void Postfix()
        {
            Debug.Log($"Main menu loaded, run plugin startup!");
            Plugin.OnStartup();
        }
    }
}
