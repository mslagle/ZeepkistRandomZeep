using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Security.Cryptography;
using UnityEngine;
using ZeepSDK.Cosmetics;
using ZeepSDK.Racing;
using ZeepSDK.Workshop;

namespace Zeepkist.RandomZeep
{

    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("ZeepSDK")]
    public class Plugin : BaseUnityPlugin
    {
        private Harmony harmony;

        public static ConfigEntry<bool> ChangeOnGameLoad { get; private set; }
        public static ConfigEntry<bool> ChangeOnMapLoad { get; private set; }
        public static ConfigEntry<bool> ChangeOnSpawn { get; private set; }

        public static bool RandomizedAtStartup = false;

        private void Awake()
        {
            harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();

            // Plugin startup logic
            Debug.Log($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            ChangeOnGameLoad = Config.Bind<bool>("Mod", "Change zeep at game start", true);
            ChangeOnMapLoad = Config.Bind<bool>("Mod", "Change zeep at map start", true);
            ChangeOnSpawn = Config.Bind<bool>("Mod", "Change zeep at spawn", false);

            RacingApi.LevelLoaded += RacingApi_LevelLoaded;
            RacingApi.PlayerSpawned += RacingApi_PlayerSpawned;
        }

        public static void OnStartup()
        {
            Debug.Log($"OnStartup running.  Config == {ChangeOnGameLoad.Value}");
            if (ChangeOnGameLoad.Value == false)
            {
                return;
            }
            if (RandomizedAtStartup == true)
            {
                Debug.Log("Randomized at startup already occured, not randomizing!");
                return;
            }

            RandomizedAtStartup = true;
            Plugin.Randomize();
        }

        private void RacingApi_PlayerSpawned()
        {
            Debug.Log($"ChangeOnSpawn running.  Config == {ChangeOnSpawn.Value}");
            if (ChangeOnSpawn.Value == false)
            {
                return;
            }

            Randomize();
        }

        private void RacingApi_LevelLoaded()
        {
            Debug.Log($"ChangeOnMapLoad running.  Config == {ChangeOnMapLoad.Value}");
            if (ChangeOnMapLoad.Value == false)
            {
                return;
            }
            if (ChangeOnSpawn.Value == true) 
            {
                Debug.Log("Change on spawn == true, not randomizing here at it would be a duplicate randomize!");
                return;
            }

            Randomize();
        }

        public static void Randomize()
        {
            var hats = CosmeticsApi.GetUnlockedHats();
            var carts = CosmeticsApi.GetUnlockedZeepkists();
            var colors = CosmeticsApi.GetUnlockedColors();
            Debug.Log($"Randomizing zeep character with {hats.Count} hats, {carts.Count} carts, and {colors.Count} colors");

            var selectedHat = hats[RandomNumberGenerator.GetInt32(hats.Count)];
            var selectedCart = carts[RandomNumberGenerator.GetInt32(carts.Count)];
            var selectedColor = colors[RandomNumberGenerator.GetInt32(colors.Count)];
            Debug.Log($"Applying {selectedHat.name} hat, {selectedCart.name} cart, and {selectedColor.name} color...");

            PlayerManager.Instance.avontuurSoapbox = selectedCart;
            PlayerManager.Instance.avontuurColor = selectedColor;
            PlayerManager.Instance.avontuurHat = selectedHat;
            Debug.Log("Finished applying randomized cosmetics");
        }

        public void OnDestroy()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }
    }
}