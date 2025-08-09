using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace Jetpack.DebugCode
{
    // Will make the player visible / invisible.  Got code from here (Jenix106):
    // https://www.nexusmods.com/bladeandsorcery/mods/8366
    public static class PlayerVisibility
    {
        public static void MakeInvisible()
        {
            var playerCreature = Player.local.creature;
            if (playerCreature == null)
                return;

            // Set player to "hidden" state (makes invisible to the camera)
            playerCreature.hidden = true;

            // Hide renderers and items
            foreach (var renderer in playerCreature.renderers)
            {
                if (renderer.splitRenderer != null)
                    ((Renderer)renderer.splitRenderer).enabled = false;
                else if (renderer.renderer != null)
                    ((Renderer)renderer.renderer).enabled = false;
            }

            playerCreature.HideItemsInHolders(true);
        }

        public static void MakeVisible()
        {
            var playerCreature = Player.local.creature;
            if (playerCreature == null)
                return;

            // Restore "visible" state
            playerCreature.hidden = false;

            // Restore renderers and items
            foreach (var renderer in playerCreature.renderers)
            {
                if (renderer.splitRenderer != null)
                    ((Renderer)renderer.splitRenderer).enabled = true;
                else if (renderer.renderer != null)
                    ((Renderer)renderer.renderer).enabled = true;
            }

            playerCreature.HideItemsInHolders(false);
        }
    }
}
