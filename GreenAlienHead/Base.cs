using BepInEx;
using RoR2;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Mono.Cecil;
using UnityEngine;
using System.Collections.Generic;
using BepInEx.Bootstrap;
using System;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;

namespace GreenAlienHead
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [R2APISubmoduleDependency(nameof(LanguageAPI))]
    [BepInPlugin("com.Borbo.GreenAlienHead", "Yeah Thats Right Alien Head Is A Green Item Now", "4.0.0")]
    public class Base : BaseUnityPlugin
    {
        public static AssetBundle iconBundle = LoadAssetBundle(Properties.Resources.gah);
        public static AssetBundle LoadAssetBundle(Byte[] resourceBytes)
        {
            if (resourceBytes == null) throw new ArgumentNullException(nameof(resourceBytes));
            return AssetBundle.LoadFromMemory(resourceBytes);
        }

        static ItemDef alienHeadItemDef;
        static Sprite greenAlienHeadSprite;
        internal static ConfigFile CustomConfigFile { get; set; }
        public static ConfigEntry<float> CooldownReduction { get; set; }

        private float alienHeadNewCooldownFraction = 0.85f;
        private ItemTier headNewTier = ItemTier.Tier2;
        private bool isLoaded(string modguid)
        {
            foreach (KeyValuePair<string, PluginInfo> keyValuePair in Chainloader.PluginInfos)
            {
                string key = keyValuePair.Key;
                PluginInfo value = keyValuePair.Value;
                bool flag = key == modguid;
                if (flag)
                {
                    return true;
                }
            }
            return false;
        }

        public void Awake()
        {
            CustomConfigFile = new ConfigFile(Paths.ConfigPath + "\\GreenAlienHead.cfg", true);

            CooldownReduction = CustomConfigFile.Bind<float>(
                "Green Alien Head",
                "Set Cooldown Reduction",
                (1 - alienHeadNewCooldownFraction) * 100,
                "Change the cooldown reduction PERCENT of alien head."
                );
            alienHeadNewCooldownFraction = 1 - (CooldownReduction.Value / 100);

            greenAlienHeadSprite = iconBundle.LoadAsset<Sprite>("Assets/greenalienhead.png");

            IL.RoR2.CharacterBody.RecalculateStats += NerfAlienHeadCdr;

            alienHeadItemDef = LegacyResourcesAPI.Load<ItemDef>("RoR2/Base/AlienHead/AlienHead.asset");
            /*if (alienHeadItemDef != null)
            {
                Debug.Log("dasgsethnbsrt");
                alienHeadItemDef.tier = headNewTier;

                if(headNewTier == ItemTier.Tier2)
                    alienHeadItemDef.pickupIconSprite = greenAlienHeadSprite;
            }
            else
            {
                Debug.Log("Whadda haell....");
                //RoR2Application.onLoad += GAH;
            }*/

            On.RoR2.ItemCatalog.SetItemDefs += Gah2;
            

            LanguageAPI.Add("ITEM_ALIENHEAD_DESC",
                $"<style=cIsUtility>Reduce skill cooldowns</style> by <style=cIsUtility>{CooldownReduction.Value}%</style> <style=cStack>(+{CooldownReduction.Value}% per stack)</style>.");

            Debug.Log($"Green Alien Head Initialized! Cooldowns should now be multiplied by {alienHeadNewCooldownFraction} per stack.");
        }

        private void Gah2(On.RoR2.ItemCatalog.orig_SetItemDefs orig, ItemDef[] newItemDefs)
        {
            GAH();
            orig(newItemDefs);
        }

        void GAH()
        {
            RoR2Content.Items.AlienHead._itemTierDef = ItemTierCatalog.GetItemTierDef(headNewTier);
            if (headNewTier == ItemTier.Tier2)
                RoR2Content.Items.AlienHead.pickupIconSprite = greenAlienHeadSprite;
        }

        private void NerfAlienHeadCdr(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int alienHeadLocation = 15;
            c.GotoNext(MoveType.Before,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "AlienHead"),
                x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GetItemCount)),
                x => x.MatchStloc(out alienHeadLocation)
                );

            c.GotoNext(MoveType.Before,
                x => x.MatchLdloc(alienHeadLocation)
                );
            c.Index -= 8;
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, alienHeadNewCooldownFraction);
        }
    }
}
