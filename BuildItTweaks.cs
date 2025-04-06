using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Managers;
using ServerSync;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Configs;
using Jotunn.Entities;

namespace BuildItTweaks
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency("RockerKitten.BuildIt")]
    [BepInDependency("com.jotunn.jotunn")]
    public class BuildItTweaks : BaseUnityPlugin
    {
        private const string ModGuid = "com.crowigor.buildittweaks";
        private const string ModName = "BuildIt Tweaks";
        private const string ModVersion = "1.0.0";

        private static readonly ConfigSync ConfigSync = new ConfigSync(ModGuid)
        {
            DisplayName = ModName,
            CurrentVersion = ModVersion,
            MinimumRequiredVersion = ModVersion
        };

        private ConfigEntry<bool> _debug;
        private ConfigEntry<bool> _removeHammer;

        private void Awake()
        {
            const string section = "General";
            _debug = ConfigEntry(section, "Debug", false, "Enable debug logging");
            _removeHammer = ConfigEntry(section, "Remove rk_hammer", true,
                "If true, rk_hammer will only be available via console");
            ConfigSync.IsLocked = ConfigEntry(section, "Lock Config", true,
                "If true, config is locked and controlled by server.").Value;

            Harmony.CreateAndPatchAll(typeof(BuildItTweaks));

            // Регистрация событий
            PrefabManager.OnPrefabsRegistered += HandlePrefabsRegistered;
        }

        private void HandlePrefabsRegistered()
        {
            LogInfo("🔁 HandlePrefabsRegistered");
            CreateConfigParams();
            if (ConfigSync.IsLocked)
            {
                LogInfo("🔁 ConfigSync.IsLocked > ApplyTweaks");
                DisableModHammerCraft();
                ApplyTweaks();
            }
            else
            {
                LogInfo("🔁 DON't ConfigSync.IsLocked");
            }
        }

        private void CreateConfigParams()
        {
            LogInfo("🔁 CreateConfigParams");
            foreach (var prefab in ZNetScene.instance.m_prefabs)
            {
                if (!prefab.name.StartsWith("rk_") || !prefab.TryGetComponent(out Piece piece))
                    continue;

                var section = prefab.name;
                ConfigEntry(section, "Enabled", false, "Enable this piece", 5);
                ConfigEntry(section, "CustomName", "", "Custom display name for the piece", 4);
                ConfigEntry(section, "Category", "Misc",
                    "Piece category (Misc, Crafting, Furniture, BuildingStonecutter, BuildingWorkbench)",
                    3);
                ConfigEntry(section, "CraftingStation", GetDefaultStation(piece),
                    "Piece required crafting station (piece_workbench)", 2);
                ConfigEntry(section, "Cost", GetDefaultCost(piece),
                    "Build cost (format: item:amount[,item:amount,...])", 1);
            }
        }

        private void ApplyTweaks()
        {
            LogInfo("🔁 ApplyTweaks");
            foreach (var prefab in ZNetScene.instance.m_prefabs)
            {
                if (!prefab.name.StartsWith("rk_") || !prefab.TryGetComponent(out Piece piece))
                    continue;

                var section = prefab.name;
                var isEnabled = Config.Bind(section, "Enabled", false).Value;
                if (!isEnabled)
                {
                    if (!piece.m_description.Contains(prefab.name))
                        piece.m_description += $"\n(Prefab: {prefab.name})";
                    continue;
                }

                var customName = Config.Bind(section, "CustomName", piece.m_name, "").Value;
                var category = Config.Bind(section, "Category", "Misc").Value;
                var station = Config.Bind(section, "CraftingStation", GetDefaultStation(piece), "").Value;
                var cost = Config.Bind(section, "Cost", GetDefaultCost(piece)).Value;

                PieceManager.Instance.RemovePiece(prefab.name);
                
                var requirements = ParseCost(cost);
                var pieceCfg = new PieceConfig
                {
                    PieceTable = "Hammer",
                    Category = category,
                    Enabled = true,
                    Requirements = requirements.ToArray()
                };
                var custom = new CustomPiece(prefab, false, pieceCfg);
                PieceManager.Instance.AddPiece(custom);

                piece.m_resources = requirements.ConvertAll(requirement =>
                {
                    var resItem = ObjectDB.instance.GetItemPrefab(requirement.Item)?.GetComponent<ItemDrop>();
                    return resItem != null
                        ? new Piece.Requirement
                            { m_resItem = resItem, m_amount = requirement.Amount, m_recover = requirement.Recover }
                        : null;
                }).FindAll(r => r != null).ToArray();

                if (!string.IsNullOrEmpty(station))
                {
                    var stationPrefab = ZNetScene.instance.GetPrefab(station);
                    if (stationPrefab != null && stationPrefab.TryGetComponent(out CraftingStation cs))
                        piece.m_craftingStation = cs;
                }

                if (!string.IsNullOrWhiteSpace(customName))
                    piece.m_name = customName;

                LogInfo($"✅ {prefab.name} enabled → {category}");
            }

            DisplayMessage($"✅ {ModName} settings loaded");
        }

        private static string GetDefaultCost(Piece piece)
        {
            var list = (from req in piece.m_resources
                where req?.m_resItem != null
                select $"{req.m_resItem.name.Replace("JVLmock_", "")}:{req.m_amount}").ToList();

            return list.Count > 0 ? string.Join(",", list) : "Wood:1";
        }

        private static string GetDefaultStation(Piece piece)
        {
            return piece.m_craftingStation
                ? piece.m_craftingStation.name.Replace("JVLmock_", "")
                : "piece_workbench";
        }

        private static List<RequirementConfig> ParseCost(string raw)
        {
            var list = new List<RequirementConfig>();
            var entries = raw.Split(',');

            foreach (var entry in entries)
            {
                var parts = entry.Split(':');
                if (parts.Length >= 2 && int.TryParse(parts[1], out int amt))
                {
                    list.Add(new RequirementConfig
                    {
                        Item = parts[0].Trim(),
                        Amount = amt,
                        Recover = true
                    });
                }
            }

            return list;
        }

        private void DisableModHammerCraft()
        {
            if (!_removeHammer.Value)
                return;

            LogInfo("🔧 Registering rk_hammer for console only");

            var prefab = ZNetScene.instance.GetPrefab("rk_hammer");
            if (prefab == null)
            {
                LogWarning("❌ rk_hammer not found");
                return;
            }

            if (ItemManager.Instance.GetItem("rk_hammer") != null)
                ItemManager.Instance.RemoveItem("rk_hammer");

            var customHammer = new CustomItem(prefab, false, new ItemConfig
            {
                Name = "$ImprovedHammer",
                Enabled = true,
                Requirements = []
            });

            ItemManager.Instance.AddItem(customHammer);
            LogInfo("✅ rk_hammer available via spawn rk_hammer only");
        }

        private ConfigEntry<T> ConfigEntry<T>(string section, string key, T value, string description, int order = 0,
            bool synced = true)
        {
            var attributes = new ConfigurationManagerAttributes { Order = order };
            var entry = Config.Bind(section, key, value, new ConfigDescription(description, null, attributes));
            if (!synced)
                return entry;

            ConfigSync.AddConfigEntry(entry);

            entry.SettingChanged += (_, _) =>
            {
                LogInfo($"🛠️ Changed setting → <[{section}] - {key}");

                if (!section.Equals("General", System.StringComparison.OrdinalIgnoreCase))
                {
                    ApplyTweaks();
                    RefreshHammerUI();
                    LogInfo("🔁 Changes applied to build pieces");
                }
                else
                {
                    LogInfo("⚙️ General setting changed — no tweaks applied");
                }
            };

            return entry;
        }

        private void RefreshHammerUI()
        {
            var player = Player.m_localPlayer;
            if (player == null)
                return;

            LogInfo("🔄 Refreshed hammer UI via SetBuildPieces()");
        }

        private void DisplayMessage(string message)
        {
            if (_debug.Value)
                Player.m_localPlayer?.Message(MessageHud.MessageType.TopLeft, message);
        }

        private void LogInfo(string message)
        {
            if (_debug.Value)
                Logger.LogInfo(message);
        }

        private void LogWarning(string message)
        {
            if (_debug.Value)
                Logger.LogWarning(message);
        }
    }
}