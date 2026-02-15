using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using Netcode;
using System.Runtime.Versioning;
using System.Reflection;
using System.Net.Mime;
using System.Xml.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks.Dataflow;

namespace VoL {
	internal sealed class ModEntry : Mod {

		public Dictionary<string,string> ConsumeVerbs = new Dictionary<string,string> {
			{"silverfish.VoLInternal_Joint", "Smoke"},
			{"silverfish.VoLInternal_Acid", "Drop"},
			{"silverfish.VoLInternal_MDMA", "Take"},
		};
		string current_verb = "Eat";
		MethodInfo? rendertarget_getdata = null;
		MethodInfo? rendertarget_setdata = null;
		Microsoft.Xna.Framework.Color[] screen_buffer = new Microsoft.Xna.Framework.Color[1];
		Microsoft.Xna.Framework.Color[] fractal_buffer = new Microsoft.Xna.Framework.Color[1];
		Random rand = new Random();
		RenderTarget2D new_shirt_texture;
	
		public static ModEntry instance;

		public override void Entry(IModHelper helper) {
			instance = this; // assumes ModEntry is instantiated exactly once, or weird things will happen
			helper.Events.Input.ButtonPressed += this.OnButtonPressed;
			helper.Events.Content.AssetRequested += this.OnAssetRequested;
			helper.Events.GameLoop.DayStarted += this.OnDayStarted;
			helper.Events.Display.RenderedWorld += this.OnRenderedWorld;
			helper.Events.Display.WindowResized += this.OnWindowResized;
			RefreshBuffers(Game1.defaultResolutionX, Game1.defaultResolutionY);
			// retrieve references to render_target.GetData and render_target.SetData
			// custom reflection because we need to disambiguate accessing an overloaded function
			Type? texture_type = Game1.graphics.GraphicsDevice.Textures[0].GetType();
			MethodInfo[] methodInfos = typeof(Microsoft.Xna.Framework.Graphics.Texture2D).GetMethods();
			foreach (MethodInfo m in methodInfos) {
				// we want the ones with one parameter, ie. the array of pixel data
				if (m.Name == "GetData" && m.GetParameters().Count() == 1) {
					this.rendertarget_getdata = m.MakeGenericMethod(typeof(Microsoft.Xna.Framework.Color));
				}
				if (m.Name == "SetData" && m.GetParameters().Count() == 1) {
					this.rendertarget_setdata = m.MakeGenericMethod(typeof(Microsoft.Xna.Framework.Color));
				}
			}
			if (this.rendertarget_getdata == null) {
				this.Monitor.Log("unable to fetch rendertarget.GetData", LogLevel.Warn);
			}
			if (this.rendertarget_setdata == null) {
				this.Monitor.Log("unable to fetch rendertarget.SetData", LogLevel.Warn);
			}
		}

		private void OnButtonPressed(object? sender, ButtonPressedEventArgs e) {
			if (e.Button == SButton.MouseRight) {
				if (Game1.player.ActiveObject != null && Game1.player.ActiveObject is StardewValley.Object) {
					this.Helper.GameContent.InvalidateCache("Strings/StringsFromCSFiles");
				}
			}
		}

		private void OnAssetRequested(object? sender, AssetRequestedEventArgs e) {
        	if (e.NameWithoutLocale.IsEquivalentTo("Strings/StringsFromCSFiles")) {
				if (Game1.player != null && Game1.player.ActiveObject != null && Game1.player.ActiveObject is StardewValley.Object && ConsumeVerbs.ContainsKey(Game1.player.ActiveObject.ItemId)) {
					current_verb = ConsumeVerbs[Game1.player.ActiveObject.ItemId];
				} else {
					current_verb = "Eat";
				}
            	e.Edit(asset => {
                	var editor = asset.AsDictionary<string, string>();
                	editor.Data["Game1.cs.3160"] = $"{current_verb} {{0}}?"; // confusingly, in c# format strings {{}} means literal {}
            	});
        	} else if (e.NameWithoutLocale.IsEquivalentTo("Data/Locations")) {
				e.Edit(asset => {
                	var editor = asset.AsDictionary<string, StardewValley.GameData.Locations.LocationData>();
					var mushroom_foragedata_spring = new StardewValley.GameData.Locations.SpawnForageData();
					mushroom_foragedata_spring.Id = $"{this.ModManifest.UniqueID}_mushroomforagedata";
					mushroom_foragedata_spring.ItemId = "silverfish.VoLInternal_Mushroom";
					mushroom_foragedata_spring.Season = StardewValley.Season.Spring;
					var mushroom_foragedata_fall = new StardewValley.GameData.Locations.SpawnForageData();
					mushroom_foragedata_fall.Id = $"{this.ModManifest.UniqueID}_mushroomforagedata";
					mushroom_foragedata_fall.ItemId = "silverfish.VoLInternal_Mushroom";
					mushroom_foragedata_fall.Season = StardewValley.Season.Fall;
                	editor.Data["Woods"].Forage.Add(mushroom_foragedata_spring);
                	editor.Data["Woods"].Forage.Add(mushroom_foragedata_fall);
            	});
			} else if (e.NameWithoutLocale.IsEquivalentTo("Data/Shirts")) {
				e.Edit(asset => {
					var editor = asset.AsDictionary<string, StardewValley.GameData.Shirts.ShirtData>();
					this.new_shirt_texture = new RenderTarget2D(Game1.game1.GraphicsDevice, 256, ((editor.Data.Keys.Count/16)+1)*32);
					Game1.game1.GraphicsDevice.SetRenderTarget(this.new_shirt_texture);
					Game1.game1.GraphicsDevice.Clear(Color.Transparent);
					using (var fileStream = File.Create("/tmp/testblankshirts.png")) {
					    this.new_shirt_texture.SaveAsPng(fileStream, this.new_shirt_texture.Width, this.new_shirt_texture.Height);
					}
					Texture2D tie_dye_pattern = this.Helper.ModContent.Load<Texture2D>("assets/tiedye.png");
					int n = 0;
					string[] keys_copy = new string[editor.Data.Keys.Count];
					editor.Data.Keys.CopyTo(keys_copy, 0);
					foreach (string shirt_name in keys_copy) {
						StardewValley.GameData.Shirts.ShirtData new_shirt = new StardewValley.GameData.Shirts.ShirtData();
						new_shirt.Name = $"Tie-Dyed {editor.Data[shirt_name].Name}";
						new_shirt.DisplayName = $"Tie-Dyed {editor.Data[shirt_name].DisplayName}";
						new_shirt.Description = editor.Data[shirt_name].Description;
						new_shirt.Price = editor.Data[shirt_name].Price;
						new_shirt.Texture = $"Mods/{this.ModManifest.UniqueID}/tiedyedshirts";
						new_shirt.SpriteIndex = n;
						new_shirt.DefaultColor = editor.Data[shirt_name].DefaultColor;
						new_shirt.CanBeDyed = editor.Data[shirt_name].CanBeDyed;
						new_shirt.IsPrismatic = editor.Data[shirt_name].IsPrismatic;
						new_shirt.HasSleeves = editor.Data[shirt_name].HasSleeves;
						new_shirt.CanChooseDuringCharacterCustomization = false;
						new_shirt.CustomFields = editor.Data[shirt_name].CustomFields;
						editor.Data[$"{this.ModManifest.UniqueID}_TieDyed_{shirt_name}"] = new_shirt;
						// generate images here
						Texture2D old_shirt_texture_sheet = this.Helper.GameContent.Load<Texture2D>(editor.Data[shirt_name].Texture??"Characters/Farmer/shirts");
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
					    this.new_shirt_texture.SaveAsPng(fileStream, this.new_shirt_texture.Width, this.new_shirt_texture.Height);
					}
					this.Helper.GameContent.InvalidateCache($"Mods/{this.ModManifest.UniqueID}/tiedyedshirts");
				}, AssetEditPriority.Late); // shirts added by mods that take effect after this one will not be tie-dyable
			} else if (e.NameWithoutLocale.IsEquivalentTo($"Mods/{this.ModManifest.UniqueID}/tiedyedshirts")) {
				e.LoadFrom(() => {
					if (this.new_shirt_texture == null) {
						this.Helper.GameContent.InvalidateCache("Data/Shirts");
					}
					return this.new_shirt_texture;
				}, AssetLoadPriority.Medium);
			}
    	}

		private Rectangle ShirtTextureBounds(int index, bool dyemask) {
			return new Rectangle((index%16)*8 + (dyemask?128:0),
								 (index/16)*32, // integer division always rounds down
								 8,
								 32);
		}

		private void OnDayStarted(object? sender, DayStartedEventArgs e) {
			// we only actually need to do these things at the start of the first day each session, but there doesn't seem to be an event for that
			// can't do it on entry or OnGameLaunched because Game1.player hasn't been initialised yet
			
			// attach maybePerformSmokeAnimation to eatAnimation
			var reflected_eatanimationevent = this.Helper.Reflection.GetField<NetEvent1Field<StardewValley.Object, NetRef<StardewValley.Object>>>(Game1.player, "eatAnimationEvent").GetValue();
			// invoked after performEatAnimation, allowing it to cancel the previous animation and substitute its own
			reflected_eatanimationevent.onEvent += maybePerformSmokeAnimation;

			Game1.player.addItemToInventory(new StardewValley.Objects.Clothing($"{this.ModManifest.UniqueID}_TieDyed_1133"));
		}

		private bool currently_tripping = false;
		private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e) {
			if (Game1.player.buffs.IsApplied($"silverfish.VoLInternal_Tripping")) {
				if (!currently_tripping) {
					currently_tripping = true;
					RefreshFractal();
				}
				if (this.rendertarget_getdata != null && this.rendertarget_setdata != null) {
					try {
						this.rendertarget_getdata.Invoke(Game1.game1.screen, new [] {this.screen_buffer});		
						this.TrippingShader();
						this.rendertarget_setdata.Invoke(Game1.game1.screen, new [] {this.screen_buffer});
					} catch (System.Reflection.TargetInvocationException) {
						this.Monitor.Log("frame size mismatch due to window resizing, will update next frame", LogLevel.Debug);
					}
				}
			} else {
				if (currently_tripping) {
					currently_tripping = false;
				}
			}
			if (Game1.player.buffs.IsApplied($"silverfish.VoLInternal_Rolling")) {
				if (this.rendertarget_getdata != null && this.rendertarget_setdata != null) {
					try {
						this.rendertarget_getdata.Invoke(Game1.game1.screen, new [] {this.screen_buffer});		
						this.RollingShader();
						this.rendertarget_setdata.Invoke(Game1.game1.screen, new [] {this.screen_buffer});
					} catch (System.Reflection.TargetInvocationException) {
						this.Monitor.Log("frame size mismatch due to window resizing, will update next frame", LogLevel.Debug);
					}
				}
			}
		}

		private void OnWindowResized(object? sender, WindowResizedEventArgs e) {
			RefreshBuffers(e.NewSize.X, e.NewSize.Y);
		}

		private void RefreshBuffers(int width, int height) {
			screen_buffer = new Microsoft.Xna.Framework.Color[width * height];
			fractal_buffer = new Microsoft.Xna.Framework.Color[width * height];
			RefreshFractal();
		}

		private void RefreshFractal() {
			int width = Game1.game1.screen.Width;
			int height = Game1.game1.screen.Height;
			int n = this.rand.Next(1,6);
			Texture2D fractal_texture = Texture2D.FromStream(Game1.graphics.GraphicsDevice, File.OpenRead(Path.Combine(this.Helper.DirectoryPath, "assets", $"fractal{n}.png")));
			RenderTarget2D fractal_scaled = new RenderTarget2D(Game1.game1.GraphicsDevice, width, height);
			SpriteBatch sb = new SpriteBatch(Game1.game1.GraphicsDevice);
			Game1.game1.GraphicsDevice.SetRenderTarget(fractal_scaled);
			Rectangle targetrect = new Rectangle(0,0,width,height);
			sb.Begin();
			sb.Draw(fractal_texture, targetrect, Color.White);
			sb.End();
			fractal_scaled.GetData(fractal_buffer);
		}

		private void maybePerformSmokeAnimation(StardewValley.Object item) {
			if (ConsumeVerbs.ContainsKey(item.ItemId) && ConsumeVerbs[item.ItemId] == "Smoke") {
				if (Game1.player.isEmoteAnimating) {
		            Game1.player.EndEmoteAnimation();
        		}

		        if (!Game1.player.IsLocalPlayer) {
		            Game1.player.itemToEat = item;
        		}

		        // skip over the eating animation
				Game1.player.FarmerSprite.currentAnimationIndex = Game1.player.FarmerSprite.CurrentAnimation.Count - 1;
				StardewValley.Utility.addSmokePuff(Game1.currentLocation, Game1.player.Position + new Microsoft.Xna.Framework.Vector2(20f,-60f), 0);
				StardewValley.Utility.addSmokePuff(Game1.currentLocation, Game1.player.Position + new Microsoft.Xna.Framework.Vector2(10f,-63f), 5);
				StardewValley.Utility.addSmokePuff(Game1.currentLocation, Game1.player.Position + new Microsoft.Xna.Framework.Vector2(25f,-56f), 10);
        		Game1.player.isEating = true;
			}
		}

		private int t = 0;
		private int angle = 0;
		private int target_angle = 0;
		private double intensity = 0;

		private void TrippingShader() {
			int time_total = Game1.player.buffs.AppliedBuffs[$"silverfish.VoLInternal_Tripping"].totalMillisecondsDuration;
			int time_left = Game1.player.buffs.AppliedBuffs[$"silverfish.VoLInternal_Tripping"].millisecondsDuration;
			if (time_left > time_total*0.66) {
				this.intensity = 1-((float)(time_left-(time_total*0.66))/(time_total*0.33));
			} else {
				this.intensity = (float)(time_left)/(time_total*0.66);
			}
			intensity = Math.Clamp(intensity, 0, 1); // seems to wander outside sometimes
			t += 2;
			if (angle > target_angle) {
				angle -= 1;
			} else if (angle < target_angle) {
				angle += 1;
			} else if (angle == target_angle) {
				target_angle = this.rand.Next(0, (int)(intensity * 360));
				
			}
			double fractal_angle = 360.0 * ((1.0+Math.Sin((float)t/300.0))/2.0);
			Parallel.For(0, this.screen_buffer.Count(), i => {
				double H = 0;
				double S = 0;
				double L = 0;
				StardewValley.Utility.RGBtoHSL(this.screen_buffer[i].R, this.screen_buffer[i].G, this.screen_buffer[i].B, out H, out S, out L);
				H = (H + angle) % 360;
				L = Lerp(L, ((float)this.fractal_buffer[i].R)/255.0, intensity * (1.0-(Math.Abs(H - fractal_angle)/360.0)));				
				int R = 0;
				int G = 0;
				int B = 0;
				StardewValley.Utility.HSLtoRGB(H, S, L, out R, out G, out B);
				this.screen_buffer[i].R = (byte)R;
				this.screen_buffer[i].G = (byte)G;
				this.screen_buffer[i].B = (byte)B;
			});
		}

		private void RollingShader() {
			int time_total = Game1.player.buffs.AppliedBuffs[$"silverfish.VoLInternal_Rolling"].totalMillisecondsDuration;
			int time_left = Game1.player.buffs.AppliedBuffs[$"silverfish.VoLInternal_Rolling"].millisecondsDuration;
			if (time_left > time_total-15000) {
				intensity = 1-(((float)time_left-((float)time_total-15000.0))/15000.0);
			} else if (time_left < 15000) {
				intensity = (float)time_left/15000.0;
			} else {
				intensity = 1;
			}
			Parallel.For(0, this.screen_buffer.Count(), i => {
				double R = (double)this.screen_buffer[i].R;
				double G = (double)this.screen_buffer[i].G;
				double B = (double)this.screen_buffer[i].B;
				// if the pixel is mostly blue, pull it towards cyan, otherwise towards pink
				if (B > R+1 && B > G+1) { // +1 to account for floating point errors because we need to be sure plain black will go towards pink
					R = Lerp(R, 0, Lerp(0,0.3,intensity));
					G = Lerp(G, 255, Lerp(0,0.3,intensity));
					B = Lerp(B, 255, Lerp(0,0.3,intensity));
				} else {
					R = Lerp(R, 255, Lerp(0,0.2,intensity));
					G = Lerp(G, 200, Lerp(0,0.2,intensity));
					B = Lerp(B, 200, Lerp(0,0.2,intensity));
				}
				this.screen_buffer[i].R = (byte)R;
				this.screen_buffer[i].G = (byte)G;
				this.screen_buffer[i].B = (byte)B;
			});
			if (intensity == 1 && this.rand.Next(0,250) == 0) {
				Game1.player.doEmote(20);
			}
			if (intensity == 1 && this.rand.Next(0,150) == 0) {
				Game1.currentLocation.temporarySprites.AddRange(StardewValley.Utility.sparkleWithinArea(
					new Microsoft.Xna.Framework.Rectangle(Game1.viewport.X, Game1.viewport.Y, Game1.viewport.Width, Game1.viewport.Height),
					5,
					new Microsoft.Xna.Framework.Color(255,200,200)
				));
			}
		}

		private double Lerp(double A, double B, double amount) {
			return A + (B - A) * amount;
		}
		private void Log(string s) {
			this.Monitor.Log(s, LogLevel.Debug);
		}
	}
}