using System.Collections.Generic;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace Hearthstone
{
    [BepInPlugin(Hearthstone.PluginGUID, "Hearthstone Revived", "1.0.2")]
    [BepInDependency(Jotunn.Main.ModGuid)]
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
            item1 = Config.Bind<string>("General", "RecipeItem1", "BoneFragments",
                new ConfigDescription("Recipe item 1 - leave blank for none", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            item1.SettingChanged += SettingsChanged;
            item2 = Config.Bind<string>("General", "RecipeItem2", "Coins",
                new ConfigDescription("Recipe item 2 - leave blank for none", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            item2.SettingChanged += SettingsChanged;
            item3 = Config.Bind<string>("General", "RecipeItem3", "Crystal",
                new ConfigDescription("Recipe item 3 - leave blank for none", null,
                               new ConfigurationManagerAttributes { IsAdminOnly = true }));
            item3.SettingChanged += SettingsChanged;

            itemCost1 = Config.Bind<int>("General", "itemCost1", 10,
                new ConfigDescription("Amount of item 1 required to craft the Hearthstone", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
            itemCost1.SettingChanged += SettingsChanged;
            itemCost2 = Config.Bind<int>("General", "itemCost2", 30, new ConfigDescription("Amount of item 2 required to craft the Hearthstone", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
            itemCost2.SettingChanged += SettingsChanged;
            itemCost3 = Config.Bind<int>("General", "itemCost3", 3, new ConfigDescription("Amount of item 3 required to craft the Hearthstone", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
            itemCost3.SettingChanged += SettingsChanged;

            writeDebugOutput = Config.Bind<bool>("Debug", "writeDebugOutput", true, new ConfigDescription("Write debug output?", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
            writeDebugOutput.SettingChanged += SettingsChanged;
            allowTeleportWithoutRestriction = Config.Bind<bool>("General", "allowTeleportWithoutRestriction", false,
                new ConfigDescription("Allow teleport without restriction", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            allowTeleportWithoutRestriction.SettingChanged += SettingsChanged;

            Debug(Assembly.GetExecutingAssembly().GetType().FullName + " has awakened");

            harmony.PatchAll();

            LoadAssets();

            SynchronizationManager.OnConfigurationSynchronized += SynchronizationManager_OnConfigurationSynchronized;
            SynchronizationManager.OnAdminStatusChanged += SynchronizationManager_OnAdminStatusChanged; ;
        }

        private void SettingsChanged(object sender, System.EventArgs e)
        {
            AddOrUpdateHearthStoneRecipe();
        }

        private void SynchronizationManager_OnAdminStatusChanged()
        {
            Debug(SynchronizationManager.Instance.PlayerIsAdmin ? "No Admin anymore" : "You are admin");
        }

        private void SynchronizationManager_OnConfigurationSynchronized(object sender, ConfigurationSynchronizationEventArgs e)
        {
            Debug(e.InitialSynchronization ? "Recieved Initial Config" : "Updated Config");
            AddOrUpdateHearthStoneRecipe();
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
            PrefabManager.OnVanillaPrefabsAvailable += AddOrUpdateHearthStoneRecipe;
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

        private void AddOrUpdateHearthStoneRecipe()
        {
            var spritePath = Path.Combine(BaseDir, "Assets");
            var var1 = AssetUtils.LoadSpriteFromFile(Path.Combine(spritePath, "heart.png"));
            var tex = AssetUtils.LoadTexture(Path.Combine(spritePath, "heart.png"));

            /// check if updating the recipe is enough:
            if (ItemManager.Instance != null && ItemManager.Instance.GetItem(sharedName) is CustomItem item && item.Recipe != null)
            {
                var rqs = new List<Piece.Requirement>();
                
                // Helperfunction to check for correct name of item, name existence and amount
                void AddRQ(string name, int amount)
                {
                    if (!string.IsNullOrEmpty(name) && amount > 0)
                    {
                        // for efficiency this would be a better solution: ObjectDB.instance.GetItemPrefab(name.GetStableHashCode()).GetComponent<ItemDrop>()
                        // but as the update is only when the config gets updated. Thanks to Margmas!
                        var itemDrop = ObjectDB.instance == null ? null : ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Material, name).FirstOrDefault();
                        if (itemDrop != null)
                        {
                            rqs.Add(new Piece.Requirement
                            {
                                m_amount = amount,
                                m_resItem = itemDrop
                            });
                        }
                    }
                }

                AddRQ(item1.Value, itemCost1.Value);
                AddRQ(item2.Value, itemCost2.Value);
                AddRQ(item3.Value, itemCost3.Value);
                
                item.Recipe.Recipe.m_resources = rqs.ToArray();

                return;
            }

            // does the same as AddRQ, but this time as RequirementConfig.
            RequirementConfig[] buildRequirementConfigs()
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

            ItemConfig shieldConfig = new ItemConfig
            {
                Name = sharedName,
                Description = ".. and Dorothy clicked her heels together twice .. ",
                Requirements = buildRequirementConfigs(),
                Icons = new Sprite[] { var1 },
                StyleTex = tex
            };

            var customItem = new CustomItem($"{sharedName}", "YagluthDrop", shieldConfig);
            customItem.ItemDrop.m_itemData.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Consumable;

            ItemManager.Instance.AddItem(customItem);

            PrefabManager.OnVanillaPrefabsAvailable -= AddOrUpdateHearthStoneRecipe;
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
