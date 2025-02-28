using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Timers;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using static System.Net.Mime.MediaTypeNames;

namespace Hearthstone
{
    [BepInPlugin(Hearthstone.PluginGUID, "Hearthstone Revived", "1.0.7")]
    [BepInDependency(Jotunn.Main.ModGuid, "2.23.2")]
    public class Hearthstone : BaseUnityPlugin
    {
        public const string sharedName = "Hearthstone";
        public const string PluginGUID = "mennowar.mods.Hearthstone_revived";
        Harmony harmony = new Harmony(PluginGUID);

        public static ConfigEntry<string> item1;
        public static ConfigEntry<string> item2;
        public static ConfigEntry<string> item3;
        public static ConfigEntry<int> itemCost1;
        public static ConfigEntry<int> itemCost2;
        public static ConfigEntry<int> itemCost3;
        public static ConfigEntry<bool> allowTeleportWithoutRestriction;
        public static ConfigEntry<bool> writeDebugOutput;
        public static bool IsOwner { get; set; } // gets updated from Patches.cs, indicates whether the targeted bed is owned by the current player instance id

        private static CustomItem m_hearthStoneItem;
        private static string m_lastPositionString;

        private static string PositionFile
        {
            get
            {
                var dir = Jotunn.Utils.Paths.CustomItemDataFolder; // System.IO.Path.Combine(BaseDir, "Assets");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var file = Path.Combine(dir, "HSPosition.txt");

                Debug("using Position File: " + file);

                return file;
            }
        }

        public static void Debug(string value)
        {
            if (writeDebugOutput.Value)
                Jotunn.Logger.LogMessage(value);
        }

        private void Awake()
        {
            Config.SaveOnConfigSet = true;
            Config.SettingChanged += (s, ea) => GenerateRecipe();

            item1 = Config.Bind<string>("General", "RecipeItem1", "Stone",
                new ConfigDescription("Recipe item 1 - leave blank for none", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            itemCost1 = Config.Bind<int>("General", "itemCost1", 2,
                new ConfigDescription("Amount of item 1 required to craft the Hearthstone", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            item2 = Config.Bind<string>("General", "RecipeItem2", "Coins",
                new ConfigDescription("Recipe item 2 - leave blank for none", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            itemCost2 = Config.Bind<int>("General", "itemCost2", 10,
                new ConfigDescription("Amount of item 2 required to craft the Hearthstone", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            item3 = Config.Bind<string>("General", "RecipeItem3", "Carrot",
                new ConfigDescription("Recipe item 3 - leave blank for none", null,
                               new ConfigurationManagerAttributes { IsAdminOnly = true }));

            itemCost3 = Config.Bind<int>("General", "itemCost3", 5,
                new ConfigDescription("Amount of item 3 required to craft the Hearthstone", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));


            writeDebugOutput = Config.Bind<bool>("Debug", "writeDebugOutput", true, new ConfigDescription("Write debug output?", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            allowTeleportWithoutRestriction = Config.Bind<bool>("General", "allowTeleportWithoutRestriction", false,
                new ConfigDescription("Allow teleport without restriction", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            harmony.PatchAll();

            GetHearthStonePositionString();

            PrefabManager.OnVanillaPrefabsAvailable += AddOrUpdateHearthStoneItem; // create the item on start, recipe follows later
            ItemManager.OnItemsRegistered += ItemManager_OnItemsRegistered;          // at this point it should be safe to create the configurable recipe

            SynchronizationManager.OnConfigurationSynchronized += SynchronizationManager_OnConfigurationSynchronized;
            SynchronizationManager.OnAdminStatusChanged += SynchronizationManager_OnAdminStatusChanged;
        }

        private void SynchronizationManager_OnAdminStatusChanged()
        {
            Debug(SynchronizationManager.Instance.PlayerIsAdmin ? "No Admin anymore" : "You are admin");
        }

        private void SynchronizationManager_OnConfigurationSynchronized(object sender, ConfigurationSynchronizationEventArgs e)
        {
            Debug(e.InitialSynchronization ? "Recieved Initial Config" : "Updated Config");
            GenerateRecipe();
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private void Update()
        {
            var mLocalPlayer = Player.m_localPlayer;
            if (mLocalPlayer == null)
                return;

            var hoverObject = mLocalPlayer.GetHoverObject();
            if (hoverObject == null) return;

            var componentInParent = hoverObject.GetComponentInParent<Interactable>();
            if (!IsOwner || !(componentInParent is Bed b) || !Input.GetKeyDown(KeyCode.P)) return;

            Hearthstone.SetHearthStonePosition();
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"You have updated your HearthStone location", 0, null);
        }

        private void ItemManager_OnItemsRegistered()
        {
            ItemManager.OnItemsRegistered -= ItemManager_OnItemsRegistered;
            GenerateRecipe();
        }

        private void GenerateRecipe()
        {
            if (m_hearthStoneItem == null || !ObjectDB.instance.m_items.Any())
                return;

            if (m_hearthStoneItem.Recipe == null)
            {
                Recipe m_Recipe = ScriptableObject.CreateInstance<Recipe>();
                m_Recipe.name = $"Recipe_{sharedName}";
                m_Recipe.m_item = m_hearthStoneItem.ItemDrop;

                var hsRecipe = new CustomRecipe(m_Recipe, true, true)
                {
                    FixRequirementReferences = true
                };

                m_hearthStoneItem.Recipe = hsRecipe;
            }

            var rqs = new List<Piece.Requirement>();

            // Helperfunction to check for correct name of item, name existence and amount
            void AddRq(ConfigEntry<string> item, ConfigEntry<int> amount)
            {
                try
                {
                    if (item != null && !string.IsNullOrEmpty(item.Value) && amount.Value > 0)
                    {

                        var itemDrop = ObjectDB.instance.GetItemPrefab(item.Value).GetComponent<ItemDrop>(); // Thanks to Margmas!
                        if (itemDrop != null)
                        {
                            rqs.Add(new Piece.Requirement
                            {
                                m_amount = amount.Value,
                                m_resItem = itemDrop
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug(ex.Message);
                }
            }

            AddRq(item1, itemCost1);
            AddRq(item2, itemCost2);
            AddRq(item3, itemCost3);

            if (m_hearthStoneItem.Recipe != null && m_hearthStoneItem.Recipe.Recipe != null)
                m_hearthStoneItem.Recipe.Recipe.m_resources = rqs.ToArray();
        }

        private void AddOrUpdateHearthStoneItem()
        {
            if (m_hearthStoneItem == null)
            {
                var bundle = AssetUtils.LoadAssetBundleFromResources("hs", typeof(Hearthstone).Assembly);

                if (bundle == null)
                {
                    Jotunn.Logger.LogWarning("Bundle could not be loaded!");

                    return;
                }

                const string prefabName = "Assets/_Custom/Hearthstone.prefab";
                var prefab = bundle.LoadAsset<GameObject>(prefabName);

                if (prefab == null)
                {
                    Jotunn.Logger.LogWarning("Prefab could not be loaded!");

                    return;
                }

                var drop = prefab.GetComponent<ItemDrop>();
                var shared = drop.m_itemData.m_shared;

                if (shared == null)
                {
                    Jotunn.Logger.LogWarning("Shared ItemData could not be loaded!");

                    return;
                }

                ItemConfig shieldConfig = new ItemConfig
                {
                    Name = shared.m_name,
                    Requirements = BuildRequirementConfigs(),
                    Icons = shared.m_icons,
                    Description = shared.m_description
                };

                m_hearthStoneItem = new CustomItem(bundle, prefabName, true, shieldConfig);
                ItemManager.Instance.AddItem(m_hearthStoneItem);

                GenerateRecipe();
            }
        }

        static RequirementConfig[] BuildRequirementConfigs()
        {
            var list = new List<RequirementConfig>();
            if (itemCost1.Value > 0 && !string.IsNullOrEmpty(item1.Value))
            {
                list.Add(new RequirementConfig
                {
                    Amount = itemCost1.Value,
                    Item = item1.Value
                });
            }

            if (itemCost2.Value > 0 && !string.IsNullOrEmpty(item2.Value))
            {
                list.Add(new RequirementConfig
                {
                    Amount = itemCost2.Value,
                    Item = item2.Value
                });
            }

            if (itemCost3.Value > 0 && !string.IsNullOrEmpty(item3.Value))
            {
                list.Add(new RequirementConfig
                {
                    Amount = itemCost3.Value,
                    Item = item3.Value
                });
            }

            return list.ToArray();
        }

        public static Vector3 GetHearthStonePosition()
        {
            var ci = new CultureInfo("en-US");
            var result = Vector3.zero;

            var s = GetHearthStonePositionString();
            if (string.IsNullOrEmpty(s))
                return result;

            var arr = s.Split('|');
            try
            {
                result = new Vector3(float.Parse(arr[0], ci), float.Parse(arr[1], ci), float.Parse(arr[2], ci));
            }
            catch
            {
                result = Vector3.zero;
            }

            return result;
        }

        private static string GetHearthStonePositionString()
        {
            if (string.IsNullOrEmpty(m_lastPositionString) && System.IO.File.Exists(PositionFile))
            {
                m_lastPositionString = System.IO.File.ReadAllText(PositionFile);
            }

            return m_lastPositionString;
        }

        private static void SetHearthStonePosition()
        {
            var ci = new CultureInfo("en-US");
            var pos = $"{Player.m_localPlayer.transform.position.x.ToString(ci)}|{Player.m_localPlayer.transform.position.y.ToString(ci)}|{Player.m_localPlayer.transform.position.z.ToString(ci)}";
            File.WriteAllText(PositionFile, pos);
            m_lastPositionString = pos;
        }
    }
}
