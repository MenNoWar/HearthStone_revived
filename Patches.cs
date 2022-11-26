using HarmonyLib;
using System;
using UnityEngine;

namespace Hearthstone
{
    class Patches
    {
        [HarmonyPatch(typeof(Player), "ConsumeItem")]
        public static class ConsumePatch
        {
            private static bool Prefix(ItemDrop.ItemData item)
            {
                var name = item.m_shared.m_name;
                if (string.IsNullOrEmpty(name))
                    return false;
                
                if (name == Hearthstone.sharedName)
                {
                    if (!Player.m_localPlayer.IsTeleportable() && !Hearthstone.allowTeleportWithoutRestriction.Value)
                    {
                        Player.m_localPlayer.Message(MessageHud.MessageType.Center, "You can't teleport carrying those items");
                        return false;
                    }

                    Vector3 teleportPosition = Hearthstone.GetHearthStonePosition();

                    if (teleportPosition == Vector3.zero)
                    {
                        Player.m_localPlayer.Message(MessageHud.MessageType.Center, "You need to set hearthstone spawn point");
                        return false;
                    }

                    Player.m_localPlayer.TeleportTo(teleportPosition, Player.m_localPlayer.transform.rotation, true);
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Bed), "GetHoverText")]
        static class Bed_GetHoverText_Patch
        {
            static void Postfix(Bed __instance, ref string __result, ZNetView ___m_nview)
            {
                if (Player.m_localPlayer == null)
                {
                    Hearthstone.IsOwner = false;
                    return;
                }

                if (___m_nview.GetZDO().GetLong("owner", 0L) == Player.m_localPlayer.GetPlayerID() || __instance.IsCurrent())
                {
                    Hearthstone.IsOwner = true;
                    __result += $"\n[<color=yellow><b>P</b></color>] Set hearthstone";
                }
                else
                {
                    Hearthstone.IsOwner = false;
                }
            }
        }       
    }
}
