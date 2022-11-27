using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Security.Permissions;
using Jotunn.Configs;
using UnityEngine;
using BepInEx.Logging;

namespace Hearthstone
{
    [BepInPlugin(Hearthstone.PluginGUID, "Hearthstone Revived", "1.0.1")]
    [BepInProcess("valheim.exe")]
    public class Hearthstone : BaseUnityPlugin
    {
        public const string sharedName = "Hearthstone";
        public const string PluginGUID = "mennowar.mods.Hearthstone_revived";
        Harmony harmony = new Harmony(PluginGUID);

        private static ManualLogSource log = null;

        public static ConfigEntry<string> item1;
        public static ConfigEntry<string> item2;
        public static ConfigEntry<string> item3;
        public static ConfigEntry<int> itemCost1;
        public static ConfigEntry<int> itemCost2;
        public static ConfigEntry<int> itemCost3;
        public static ConfigEntry<bool> allowTeleportWithoutRestriction;
        public static ConfigEntry<bool> writeDebugOutput;

        private static string m_lastPositionString;
        private static string PositionFile
        {
            get
            {
                var dir = Jotunn.Utils.Paths.CustomItemDataFolder; // System.IO.Path.Combine(BaseDir, "Assets");
                if (!System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }

                var file = Path.Combine(dir, "HSPosition.txt");

                Debug("using Position File: " + file);

                return file;
            }
        }

        public static void Debug(string value)
        {
            if (writeDebugOutput.Value)
            {
                if (log == null)
                {
                    log = BepInEx.Logging.Logger.CreateLogSource(sharedName);
                }

                if (log != null)
                {
                    log.LogMessage(value);
                }
            }
        }

        private void Awake()
        {
            item1 = Config.Bind<string>("General", "RecipeItem1", "BoneFragments", "Recipe item 1");
            item2 = Config.Bind<string>("General", "RecipeItem2", "Coins", "Recipe item 2");
            item3 = Config.Bind<string>("General", "RecipeItem3", "Crystal", "Recipe item 3");

            itemCost1 = Config.Bind<int>("General", "itemCost1", 10, "Recipe item 1 cost");
            itemCost2 = Config.Bind<int>("General", "itemCost2", 30, "Recipe item 2 cost");
            itemCost3 = Config.Bind<int>("General", "itemCost3", 3, "Recipe item 3 cost");
            writeDebugOutput = Config.Bind<bool>("Debug", "writeDebugOutput", true, "Write Debug output");
            allowTeleportWithoutRestriction = Config.Bind<bool>("General", "allowTeleportWithoutRestriction", false, "Allow teleport without restriction");

            Debug(Assembly.GetExecutingAssembly().GetType().FullName + " has awakened");

            harmony.PatchAll();
            LoadAssets();
        }

        private void Update()
        {
            var mLocalPlayer = Player.m_localPlayer;
            if (mLocalPlayer == null)
                return;

            var hoverObject = mLocalPlayer.GetHoverObject();
            if (hoverObject == null) return;
            var componentInParent = hoverObject.GetComponentInParent<Interactable>();
            if (!IsOwner || !(componentInParent is Bed b)) return;
            if (!Input.GetKeyDown(KeyCode.P)) return;
            Hearthstone.SetHearthStonePosition();
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"You have updated your HearthStone location", 0, null);
        }

        public static bool IsOwner { get; set; }

        private void LoadAssets()
        {
            GetHearthStonePositionString();
            PrefabManager.OnVanillaPrefabsAvailable += AddClonedItems;
        }

        private static string m_BaseDir = string.Empty;
        private static string BaseDir
        {
            get
            {
                if (string.IsNullOrEmpty(m_BaseDir))
                {
                    // Get value like: file:///C:/Users/menno/AppData/Roaming/r2modmanPlus-local/Valheim/profiles/HS/BepInEx/plugins/Unknown-Hearthstone/Hearthstone.dll
                    var s = System.Reflection.Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", "");
                    var fi = new FileInfo(s);
                    m_BaseDir = fi.Directory.Name;
                }

                return m_BaseDir;
            }
        }

        private void AddClonedItems()
        {
            var spritePath = Path.Combine(BaseDir, "Assets");
            Sprite var1 = AssetUtils.LoadSpriteFromFile(Path.Combine(spritePath, "heart.png"));

            ItemConfig shieldConfig = new ItemConfig();
            shieldConfig.Name = sharedName;
            shieldConfig.Description = ".. and Dorothy clicked her heels together twice .. ";
            shieldConfig.Requirements = new[]
            {
                new RequirementConfig {
                    Amount = itemCost1.Value,
                    Item = item1.Value
                },
                new RequirementConfig {
                    Amount = itemCost2.Value,
                    Item = item2.Value
                },
                new RequirementConfig {
                    Amount = itemCost3.Value,
                    Item = item3.Value
                }
            };


            shieldConfig.Icons = new Sprite[] { var1 };
            /* shieldConfig.StyleTex = styleTex; */
            var customItem = new CustomItem($"item_{sharedName}", "YagluthDrop", shieldConfig);
            customItem.ItemDrop.m_itemData.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Consumable;
            ItemManager.Instance.AddItem(customItem);

            PrefabManager.OnVanillaPrefabsAvailable -= AddClonedItems;
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

        public static string GetHearthStonePositionString()
        {
            if (string.IsNullOrEmpty(m_lastPositionString) && System.IO.File.Exists(PositionFile))
            {
                m_lastPositionString = System.IO.File.ReadAllText(PositionFile);
            }

            return m_lastPositionString;
        }

        public static void SetHearthStonePosition()
        {
            var ci = new CultureInfo("en-US");
            var pos = $"{Player.m_localPlayer.transform.position.x.ToString(ci)}|{Player.m_localPlayer.transform.position.y.ToString(ci)}|{Player.m_localPlayer.transform.position.z.ToString(ci)}";
            System.IO.File.WriteAllText(PositionFile, pos);
            m_lastPositionString = pos;
        }
    }
}
