using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using ZeepkistClient;
using ZeepkistNetworking;
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
        public static ConfigEntry<KeyCode> RandomizeKey { get; private set; }

        public static SetupModelCar modelCar { get; set; }
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
            RandomizeKey = Config.Bind<KeyCode>("Mod", "Change zeep during race", KeyCode.L);

            RacingApi.LevelLoaded += RacingApi_LevelLoaded;
            RacingApi.PlayerSpawned += RacingApi_PlayerSpawned;
        }

        private void Update()
        {
            if (Input.GetKeyDown(Plugin.RandomizeKey.Value))
            {
                Debug.Log("Pressed the randomized key, randomizing now!");
                Randomize();
            }
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
            PlayerManager.Instance.messenger.Log("Randomizing zeep", 1.0f);

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

            // Reload during the main menu, online, or offline
            if (modelCar != null && modelCar.soapbox != null)
            {
                modelCar.DoCarSetup(selectedCart, selectedHat, selectedColor, true, false, false);
            }
            // Reload only duriing online - backup call only
            else if (PlayerManager.Instance?.currentMaster?.carSetups?.FirstOrDefault<SetupCar>() != null)
            {
                PlayerManager.Instance.currentMaster.carSetups.FirstOrDefault<SetupCar>().SetupSoapbox(selectedCart);
                PlayerManager.Instance.currentMaster.carSetups.FirstOrDefault<SetupCar>().SetupCharacter(selectedHat, selectedColor);
            }

            // Send a packet to update in multiplayer
            if (ZeepkistNetwork.IsConnectedToGame)
            {
                PlayerCosmeticsPacket packet = new PlayerCosmeticsPacket();
                packet.ZeepkistID = PlayerManager.Instance.objectsList.wardrobe.GetZeepkistUnlocked(PlayerManager.Instance.avontuurSoapbox.GetCompleteID()).GetCompleteID();
                packet.HatID = PlayerManager.Instance.objectsList.wardrobe.GetHatUnlocked(PlayerManager.Instance.avontuurHat.GetCompleteID()).GetCompleteID();
                packet.ColorID = PlayerManager.Instance.objectsList.wardrobe.GetColorUnlocked(PlayerManager.Instance.avontuurColor.GetCompleteID()).GetCompleteID();
                ZeepkistNetwork.NetworkClient?.SendPacket<PlayerCosmeticsPacket>(packet);
            }

            Debug.Log("Finished applying randomized cosmetics");
        }

        public void OnDestroy()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }
    }
}