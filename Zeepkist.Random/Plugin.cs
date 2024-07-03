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
            var cosmetics = RandomizeCosmetics();

            // Reload during the main menu, online, or offline
            if (modelCar != null && modelCar.soapbox != null)
            {
                modelCar.DoCarSetup(PlayerManager.Instance.adventureCosmetics, true, false, false);
            }

            // Reload only during online - backup call only
            else if (PlayerManager.Instance?.currentMaster?.carSetups?.FirstOrDefault<SetupCar>() != null)
            {
                PlayerManager.Instance.currentMaster.carSetups.FirstOrDefault<SetupCar>(x => x.tag == modelCar.tag).SetupSoapbox(PlayerManager.Instance.adventureCosmetics);
                PlayerManager.Instance.currentMaster.carSetups.FirstOrDefault<SetupCar>(x => x.tag == modelCar.tag).SetupCharacter(PlayerManager.Instance.adventureCosmetics);
            }

            // Send a packet to update in multiplayer
            if (ZeepkistNetwork.IsConnectedToGame)
            {


                PlayerCosmeticsPacket packet = new PlayerCosmeticsPacket();
                packet.cosmetics = PlayerManager.Instance.adventureCosmetics.GetIDs();
                packet.chatColor = Color.HSVToRGB(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
                ZeepkistNetwork.NetworkClient?.SendPacket<PlayerCosmeticsPacket>(packet);
            }

            Debug.Log("Finished applying randomized cosmetics");
        }

        public static CosmeticsV16 RandomizeCosmetics()
        {
            PlayerManager.Instance.messenger.Log("Randomizing zeep", 1.0f);

            var carts = CosmeticWardrobe.Instance.unlockedZeepkist.Values.Cast<Object_Soapbox>().ToList();
            var hats = CosmeticWardrobe.Instance.unlockedHat.Values.Cast<HatValues>().ToList();
            var colors = CosmeticWardrobe.Instance.unlockedColor.Values.Cast<CosmeticColor>().ToList();
            var glasses = CosmeticWardrobe.Instance.unlockedGlasses.Values.Cast<HatValues>().ToList();
            var wheels = CosmeticWardrobe.Instance.unlockedWheel.Values.Cast<Object_Wheel>().ToList();
            var gliders = CosmeticWardrobe.Instance.unlockedParaglider.Values.Cast<Object_Paraglider>().ToList();
            var horns = CosmeticWardrobe.Instance.unlockedHorn.Values.Cast<Object_Horn>().ToList();
            Debug.Log($"Randomizing with {carts.Count} carts, {hats.Count} hats, {colors.Count} colors, {glasses.Count} glasses, {wheels.Count} wheels, {gliders.Count} paragliders, {horns.Count} horns.");

            var selectedCart = carts[RandomNumberGenerator.GetInt32(carts.Count)];
            var selectedHat = hats[RandomNumberGenerator.GetInt32(hats.Count)];
            var selectedGlasses = glasses[RandomNumberGenerator.GetInt32(glasses.Count)];
            var selectedGlider = gliders[RandomNumberGenerator.GetInt32(gliders.Count)];
            var selectedHorn = horns[RandomNumberGenerator.GetInt32(horns.Count)];
            Debug.Log($"Applying {selectedCart.name} cart, {selectedHat.name} hat, {selectedGlasses.name} glasses, {selectedGlider.name} glider, and {selectedHorn.name} horn...");

            var selectedColorLeftLeg = colors[RandomNumberGenerator.GetInt32(colors.Count)];
            var selectedColorRightLeg = colors[RandomNumberGenerator.GetInt32(colors.Count)];
            var selectedColorLeftArm = colors[RandomNumberGenerator.GetInt32(colors.Count)];
            var selectedColorRightArm = colors[RandomNumberGenerator.GetInt32(colors.Count)];
            var selectedColorBody = colors[RandomNumberGenerator.GetInt32(colors.Count)];
            Debug.Log($"Applying {selectedColorLeftLeg.name} left leg, {selectedColorRightLeg.name} right leg, {selectedColorLeftArm.name} left arm, {selectedColorRightArm.name} right arm, and {selectedColorBody.name} body...");

            var selectedFrontWheels = wheels[RandomNumberGenerator.GetInt32(wheels.Count)];
            var selectedRearWheels = wheels[RandomNumberGenerator.GetInt32(wheels.Count)];
            Debug.Log($"Applying {selectedFrontWheels.name} front wheels, and {selectedRearWheels.name} rear wheels...");

            PlayerManager.Instance.adventureCosmetics.zeepkist = selectedCart;
            PlayerManager.Instance.adventureCosmetics.hat = selectedHat;
            PlayerManager.Instance.adventureCosmetics.glasses = selectedGlasses;
            PlayerManager.Instance.adventureCosmetics.paraglider = selectedGlider;
            PlayerManager.Instance.adventureCosmetics.horn = selectedHorn;

            PlayerManager.Instance.adventureCosmetics.color_body = selectedColorBody;
            PlayerManager.Instance.adventureCosmetics.color_leftArm = selectedColorLeftArm;
            PlayerManager.Instance.adventureCosmetics.color_rightArm = selectedColorRightArm;
            PlayerManager.Instance.adventureCosmetics.color_leftLeg = selectedColorLeftLeg;
            PlayerManager.Instance.adventureCosmetics.color_rightLeg = selectedColorRightLeg;

            PlayerManager.Instance.adventureCosmetics.frontwheels = selectedFrontWheels;
            PlayerManager.Instance.adventureCosmetics.rearwheels = selectedRearWheels;

            return PlayerManager.Instance.adventureCosmetics;
        }

        public void OnDestroy()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }
    }
}