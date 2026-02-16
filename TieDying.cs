using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Machines;

namespace VoL {
    internal static class TieDying {

        private static RenderTarget2D? new_shirt_texture;
        private static RenderTarget2D? new_pants_texture;

        public static void EditShirts(IAssetData asset) {
            var editor = asset.AsDictionary<string, StardewValley.GameData.Shirts.ShirtData>();
            new_shirt_texture = new RenderTarget2D(Game1.game1.GraphicsDevice, 256, ((editor.Data.Keys.Count/16)+1)*32);
            Game1.game1.GraphicsDevice.SetRenderTarget(new_shirt_texture);
            Game1.game1.GraphicsDevice.Clear(Color.Transparent);
            Texture2D tie_dye_pattern = ModEntry.instance.Helper.ModContent.Load<Texture2D>("assets/tiedyeshirt.png");
            int n = 0;
            string[] keys_copy = new string[editor.Data.Keys.Count];
            editor.Data.Keys.CopyTo(keys_copy, 0);
            foreach (string shirt_name in keys_copy) {
                StardewValley.GameData.Shirts.ShirtData new_shirt = new StardewValley.GameData.Shirts.ShirtData();
                new_shirt.Name = $"Tie-Dyed {editor.Data[shirt_name].Name}";
                new_shirt.DisplayName = $"Tie-Dyed {editor.Data[shirt_name].DisplayName}";
                new_shirt.Description = editor.Data[shirt_name].Description;
                new_shirt.Price = editor.Data[shirt_name].Price;
                new_shirt.Texture = $"Mods/{ModEntry.instance.ModManifest.UniqueID}/tiedyedshirts";
                new_shirt.SpriteIndex = n;
                new_shirt.DefaultColor = editor.Data[shirt_name].DefaultColor;
                new_shirt.CanBeDyed = editor.Data[shirt_name].CanBeDyed;
                new_shirt.IsPrismatic = editor.Data[shirt_name].IsPrismatic;
                new_shirt.HasSleeves = editor.Data[shirt_name].HasSleeves;
                new_shirt.CanChooseDuringCharacterCustomization = false;
                new_shirt.CustomFields = editor.Data[shirt_name].CustomFields;
                editor.Data[$"{ModEntry.instance.ModManifest.UniqueID}_TieDyed_(S){shirt_name}"] = new_shirt;
                // generate images here
                Texture2D old_shirt_texture_sheet = ModEntry.instance.Helper.GameContent.Load<Texture2D>(editor.Data[shirt_name].Texture??"Characters/Farmer/shirts");
                int old_shirt_index = editor.Data[shirt_name].SpriteIndex;
                SpriteBatch sb = new SpriteBatch(Game1.game1.GraphicsDevice);
                sb.Begin();
                sb.Draw(old_shirt_texture_sheet, ShirtTextureBounds(n,false), ShirtTextureBounds(old_shirt_index,false), Color.White);
                sb.Draw(old_shirt_texture_sheet, ShirtTextureBounds(n,true), ShirtTextureBounds(old_shirt_index,true), Color.White);
                sb.End();
                BlendState bs = new BlendState();
                bs.ColorSourceBlend = Blend.DestinationAlpha;
                bs.AlphaSourceBlend = Blend.Zero;
                bs.ColorDestinationBlend = Blend.InverseSourceAlpha;
                bs.AlphaDestinationBlend = Blend.One;
                sb.Begin(blendState : bs);
                sb.Draw(tie_dye_pattern, ShirtTextureBounds(n,false), ShirtTextureBounds(0,false), Color.White);
                sb.Draw(tie_dye_pattern, ShirtTextureBounds(n,true), ShirtTextureBounds(0,false), Color.White);
                sb.End();
                n += 1;
            }
            using (var fileStream = File.Create("/tmp/testshirts.png")) {
                new_shirt_texture.SaveAsPng(fileStream, new_shirt_texture.Width, new_shirt_texture.Height);
            }
            ModEntry.instance.Helper.GameContent.InvalidateCache($"Mods/{ModEntry.instance.ModManifest.UniqueID}/tiedyedshirts");
        }

        public static void EditPants(IAssetData asset) {
            var editor = asset.AsDictionary<string, StardewValley.GameData.Pants.PantsData>();
            new_pants_texture = new RenderTarget2D(Game1.game1.GraphicsDevice, 1920, ((editor.Data.Keys.Count/10)+1)*688);
            Game1.game1.GraphicsDevice.SetRenderTarget(new_pants_texture);
            Game1.game1.GraphicsDevice.Clear(Color.Transparent);
            Texture2D tie_dye_pattern = ModEntry.instance.Helper.ModContent.Load<Texture2D>("assets/tiedyepants.png");
            int n = 0;
            string[] keys_copy = new string[editor.Data.Keys.Count];
            editor.Data.Keys.CopyTo(keys_copy, 0);
            foreach (string pants_name in keys_copy) {
                StardewValley.GameData.Pants.PantsData new_pants = new StardewValley.GameData.Pants.PantsData();
                new_pants.Name = $"Tie-Dyed {editor.Data[pants_name].Name}";
                new_pants.DisplayName = $"Tie-Dyed {editor.Data[pants_name].DisplayName}";
                new_pants.Description = editor.Data[pants_name].Description;
                new_pants.Price = editor.Data[pants_name].Price;
                new_pants.Texture = $"Mods/{ModEntry.instance.ModManifest.UniqueID}/tiedyedpants";
                new_pants.SpriteIndex = n;
                new_pants.DefaultColor = editor.Data[pants_name].DefaultColor;
                new_pants.CanBeDyed = editor.Data[pants_name].CanBeDyed;
                new_pants.IsPrismatic = editor.Data[pants_name].IsPrismatic;
                new_pants.CanChooseDuringCharacterCustomization = false;
                new_pants.CustomFields = editor.Data[pants_name].CustomFields;
                editor.Data[$"{ModEntry.instance.ModManifest.UniqueID}_TieDyed_(P){pants_name}"] = new_pants;
                // generate images here
                Texture2D old_pants_texture_sheet = ModEntry.instance.Helper.GameContent.Load<Texture2D>(editor.Data[pants_name].Texture??"Characters/Farmer/pants");
                int old_pants_index = editor.Data[pants_name].SpriteIndex;
                SpriteBatch sb = new SpriteBatch(Game1.game1.GraphicsDevice);
                sb.Begin();
                sb.Draw(old_pants_texture_sheet, PantsTextureBounds(n), PantsTextureBounds(old_pants_index), Color.White);
                sb.End();
                BlendState bs = new BlendState();
                bs.ColorSourceBlend = Blend.DestinationAlpha;
                bs.AlphaSourceBlend = Blend.Zero;
                bs.ColorDestinationBlend = Blend.InverseSourceAlpha;
                bs.AlphaDestinationBlend = Blend.One;
                sb.Begin(blendState : bs);
                sb.Draw(tie_dye_pattern, PantsTextureBounds(n), PantsTextureBounds(0), Color.White);
                sb.End();
                n += 1;
            }
            using (var fileStream = File.Create("/tmp/testpants.png")) {
                new_pants_texture.SaveAsPng(fileStream, new_pants_texture.Width, new_pants_texture.Height);
            }
            ModEntry.instance.Helper.GameContent.InvalidateCache($"Mods/{ModEntry.instance.ModManifest.UniqueID}/tiedyedpants");
        }

        private static Rectangle ShirtTextureBounds(int index, bool dyemask) {
			return new Rectangle((index%16)*8 + (dyemask?128:0),
								 (index/16)*32, // integer division always rounds down
								 8,
								 32);
		}

        private static Rectangle PantsTextureBounds(int index) {
			return new Rectangle((index%10)*192,
								 (index/10)*688, // integer division always rounds down
								 192,
								 688);
		}

        public static Texture2D LoadShirts() {
            if (new_shirt_texture == null) {
				ModEntry.instance.Helper.GameContent.InvalidateCache("Data/Shirts");
			}
			return new_shirt_texture??(new Texture2D(Game1.game1.GraphicsDevice,1,1));
        }

        public static Texture2D LoadPants() {
            if (new_pants_texture == null) {
				ModEntry.instance.Helper.GameContent.InvalidateCache("Data/Pants");
			}
			return new_pants_texture??(new Texture2D(Game1.game1.GraphicsDevice,1,1));
        }

        public static Item DyeTubGetOutput(StardewValley.Object machine, Item inputItem, bool probe, MachineItemOutput outputData, Farmer player, out int? overrideMinutesUntilReady) {
            overrideMinutesUntilReady = null;
            var dyed_item = new StardewValley.Objects.Clothing($"{ModEntry.instance.ModManifest.UniqueID}_TieDyed_{inputItem.QualifiedItemId}");
            StardewValley.Objects.Clothing? item_as_clothing = inputItem as StardewValley.Objects.Clothing;
            if (item_as_clothing != null) {
                dyed_item.clothesColor.Value = item_as_clothing.clothesColor.Value;
            }
            return (dyed_item);
        }

        public static Item BleachTubGetOutput(StardewValley.Object machine, Item inputItem, bool probe, MachineItemOutput outputData, Farmer player, out int? overrideMinutesUntilReady) {
            overrideMinutesUntilReady = null;
            string id = inputItem.ItemId.Remove(0, $"{ModEntry.instance.ModManifest.UniqueID}_TieDyed_(X)".Length);
            return (new StardewValley.Objects.Clothing(id));
        }
    }
}