using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;

namespace ZukoYLilaMod
{
    public class ModEntry : Mod
    {
        private CompanionCat? zuko;
        private CompanionCat? lila;
        private readonly CatEventManager eventManager = new CatEventManager();
        private readonly GiftCatalog giftCatalog = new GiftCatalog();
        private CatProgressionData sharedData = new CatProgressionData();

        private int zukoGiftsToday = 0;
        private int lilaGiftsToday = 0;
        private int lastZukoGiftTime = -999;
        private int lastLilaGiftTime = -999;
        private bool isFirstDayAfterLoad = true;
        private readonly Random random = new Random();

        private ClickableTextureComponent? inventoryCatButton;

        private const int DailyPetGain = 45;
        private const int MissedDayPenalty = DailyPetGain * 10; // 450 puntos
        private const string SyncStateMessage = "SyncSharedCatState";
        private const string PetRequestMessage = "RequestPetCat";

        private readonly List<string> zukoPhrases = new()
        {
            "El zuko le robo a la lila esto y te lo entrego",
            "El ñom acaba de encontrar algo",
            "Ñom sabe que no es muy cariñoso pero para demostrar su amor te trajo esto"
        };

        private readonly List<string> lilaPhrases = new()
        {
            "La loca acaba de pelear con otros gatos, les quito esto",
            "La loca encontro esto en el techo",
            "La loca perdio su collar, para que no te enojes te trajo esto"
        };

        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.Saving += this.OnSaving;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.GameLoop.TimeChanged += this.OnTimeChanged;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.Player.Warped += this.OnPlayerWarped;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Display.RenderedActiveMenu += this.OnRenderedActiveMenu;
            helper.Events.Display.RenderedWorld += this.OnRenderedWorld;
            helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
            helper.Events.Multiplayer.PeerConnected += this.OnPeerConnected;
            helper.Events.Multiplayer.PeerDisconnected += this.OnPeerDisconnected;
            helper.Events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;

            helper.ConsoleCommands.Add("zuko_regalo", "Hace que Zuko y Lila suelten un objeto ahora mismo.", this.OnTestGiftCommand);
            helper.ConsoleCommands.Add("zuko_corazones", "Sube a 10 corazones el cariño con Zuko y Lila.", this.OnMaxHeartsCommand);
        }

        private void EnsureCatsInitialized()
        {
            if (this.zuko == null)
            {
                this.zuko = new CompanionCat(
                    name: "Zuko",
                    spriteAssetName: "Animals\\cat",
                    followDistance: 64f,
                    moveSpeed: 4.2f,
                    offsetPreference: new Vector2(-48f, 24f)
                );
            }

            if (this.lila == null)
            {
                this.lila = new CompanionCat(
                    name: "Lila",
                    spriteAssetName: "Animals\\cat1",
                    followDistance: 84f,
                    moveSpeed: 3.9f,
                    offsetPreference: new Vector2(48f, 32f)
                );
            }
        }

        private Farmer GetAssignedFarmer(long ownerId)
        {
            if (ownerId != 0)
            {
                foreach (Farmer farmer in Game1.getOnlineFarmers())
                {
                    if (farmer != null && farmer.UniqueMultiplayerID == ownerId && farmer.currentLocation != null)
                        return farmer;
                }
            }
            return Game1.MasterPlayer;
        }

        private void BroadcastSharedState()
        {
            if (Context.IsMultiplayer)
            {
                this.Helper.Multiplayer.SendMessage(
                    message: this.sharedData,
                    messageType: SyncStateMessage,
                    modIDs: new[] { this.ModManifest.UniqueID }
                );
            }
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            this.giftCatalog.BuildCatalog();
            this.isFirstDayAfterLoad = true;

            if (Context.IsMainPlayer)
            {
                this.sharedData = this.Helper.Data.ReadSaveData<CatProgressionData>("Shercris.ZukoYLila.Progression")
                                  ?? new CatProgressionData();

                if (this.sharedData.ZukoOwnerId == 0)
                    this.sharedData.ZukoOwnerId = Game1.MasterPlayer.UniqueMultiplayerID;
                if (this.sharedData.LilaOwnerId == 0)
                    this.sharedData.LilaOwnerId = Game1.MasterPlayer.UniqueMultiplayerID;
            }
            else
            {
                this.sharedData = new CatProgressionData();
            }

            this.EnsureCatsInitialized();
            this.zuko!.WarpToPlayer(this.GetAssignedFarmer(this.sharedData.ZukoOwnerId));
            this.lila!.WarpToPlayer(this.GetAssignedFarmer(this.sharedData.LilaOwnerId));
        }

        private void OnPeerConnected(object? sender, PeerConnectedEventArgs e)
        {
            if (Context.IsMainPlayer)
            {
                this.Helper.Multiplayer.SendMessage(
                    message: this.sharedData,
                    messageType: SyncStateMessage,
                    modIDs: new[] { this.ModManifest.UniqueID },
                    playerIDs: new[] { e.Peer.PlayerID }
                );
            }
        }

        private void OnPeerDisconnected(object? sender, PeerDisconnectedEventArgs e)
        {
            if (Context.IsMainPlayer)
            {
                bool changed = false;
                if (this.sharedData.ZukoOwnerId == e.Peer.PlayerID)
                {
                    this.sharedData.ZukoOwnerId = Game1.MasterPlayer.UniqueMultiplayerID;
                    changed = true;
                }
                if (this.sharedData.LilaOwnerId == e.Peer.PlayerID)
                {
                    this.sharedData.LilaOwnerId = Game1.MasterPlayer.UniqueMultiplayerID;
                    changed = true;
                }

                if (changed)
                {
                    this.BroadcastSharedState();
                }
            }
        }

        private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID != this.ModManifest.UniqueID)
                return;

            if (e.Type == SyncStateMessage)
            {
                this.sharedData = e.ReadAs<CatProgressionData>();
                this.EnsureCatsInitialized();

                if (this.zuko != null)
                {
                    this.zuko.PettedToday = this.sharedData.ZukoPettedToday;
                    this.zuko.WarpToPlayer(this.GetAssignedFarmer(this.sharedData.ZukoOwnerId));
                }
                if (this.lila != null)
                {
                    this.lila.PettedToday = this.sharedData.LilaPettedToday;
                    this.lila.WarpToPlayer(this.GetAssignedFarmer(this.sharedData.LilaOwnerId));
                }
            }
            else if (e.Type == PetRequestMessage && Context.IsMainPlayer)
            {
                string catName = e.ReadAs<string>();
                this.ApplySharedPet(catName);
            }
        }

        private void ApplySharedPet(string catName)
        {
            this.EnsureCatsInitialized();

            if (catName == "Zuko" && this.zuko != null)
            {
                this.zuko.PettedToday = this.sharedData.ZukoPettedToday;
                int pts = this.sharedData.ZukoFriendshipPoints;
                this.zuko.TryPet(ref pts);
                this.sharedData.ZukoFriendshipPoints = pts;
                this.sharedData.ZukoPettedToday = this.zuko.PettedToday;
            }
            else if (catName == "Lila" && this.lila != null)
            {
                this.lila.PettedToday = this.sharedData.LilaPettedToday;
                int pts = this.sharedData.LilaFriendshipPoints;
                this.lila.TryPet(ref pts);
                this.sharedData.LilaFriendshipPoints = pts;
                this.sharedData.LilaPettedToday = this.lila.PettedToday;
            }

            this.BroadcastSharedState();
        }

        private void OnSaving(object? sender, SavingEventArgs e)
        {
            if (Context.IsMainPlayer)
            {
                this.Helper.Data.WriteSaveData("Shercris.ZukoYLila.Progression", this.sharedData);
            }
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            this.EnsureCatsInitialized();

            if (Context.IsMainPlayer)
            {
                if (!this.isFirstDayAfterLoad)
                {
                    if (!this.sharedData.ZukoPettedToday && this.sharedData.ZukoFriendshipPoints > 0)
                    {
                        this.sharedData.ZukoFriendshipPoints = Math.Max(0, this.sharedData.ZukoFriendshipPoints - MissedDayPenalty);
                        Game1.addHUDMessage(new HUDMessage("Zuko se sintió ignorado ayer y perdió cariño...", HUDMessage.error_type));
                    }

                    if (!this.sharedData.LilaPettedToday && this.sharedData.LilaFriendshipPoints > 0)
                    {
                        this.sharedData.LilaFriendshipPoints = Math.Max(0, this.sharedData.LilaFriendshipPoints - MissedDayPenalty);
                        Game1.addHUDMessage(new HUDMessage("Lila se sintió ignorada ayer y perdió cariño...", HUDMessage.error_type));
                    }
                }

                this.sharedData.ZukoPettedToday = false;
                this.sharedData.LilaPettedToday = false;
                this.BroadcastSharedState();
            }

            this.isFirstDayAfterLoad = false;
            this.zukoGiftsToday = 0;
            this.lilaGiftsToday = 0;
            this.lastZukoGiftTime = -999;
            this.lastLilaGiftTime = -999;

            if (this.zuko != null)
            {
                this.zuko.PettedToday = false;
                this.zuko.WarpToPlayer(this.GetAssignedFarmer(this.sharedData.ZukoOwnerId));
            }

            if (this.lila != null)
            {
                this.lila.PettedToday = false;
                this.lila.WarpToPlayer(this.GetAssignedFarmer(this.sharedData.LilaOwnerId));
            }
        }

        private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.currentLocation == null)
                return;

            this.zuko?.DrawCompanion(e.SpriteBatch);
            this.lila?.DrawCompanion(e.SpriteBatch);
        }

        private Rectangle GetInventoryCatButtonBounds(GameMenu menu)
        {
            int btnX = Math.Max(16, menu.xPositionOnScreen - 76);
            int btnY = Math.Max(16, menu.yPositionOnScreen + 96);
            return new Rectangle(btnX, btnY, 64, 64);
        }

        private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
        {
            if (Game1.activeClickableMenu is GameMenu menu)
            {
                Rectangle bounds = this.GetInventoryCatButtonBounds(menu);

                this.inventoryCatButton = new ClickableTextureComponent(
                    bounds: bounds,
                    texture: Game1.mouseCursors,
                    sourceRect: new Rectangle(162, 440, 16, 16),
                    scale: 4f
                )
                {
                    hoverText = Context.IsMainPlayer ? "Asignar a Zuko y Lila" : "Ver estado de Zuko y Lila"
                };

                IClickableMenu.drawTextureBox(
                    e.SpriteBatch,
                    Game1.mouseCursors,
                    new Rectangle(384, 396, 15, 15),
                    bounds.X - 4,
                    bounds.Y - 4,
                    bounds.Width + 8,
                    bounds.Height + 8,
                    Color.White,
                    4f,
                    drawShadow: true
                );

                this.inventoryCatButton.draw(e.SpriteBatch);

                int mx = Game1.getMouseX(true);
                int my = Game1.getMouseY(true);
                if (this.inventoryCatButton.containsPoint(mx, my))
                {
                    IClickableMenu.drawHoverText(e.SpriteBatch, this.inventoryCatButton.hoverText, Game1.smallFont);
                }

                menu.drawMouse(e.SpriteBatch);
            }
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            // 1. Clic izquierdo (o botón A en mando) sobre el botón de Zuko y Lila en el inventario
            if ((e.Button == SButton.MouseLeft || e.Button.IsUseToolButton()) && Game1.activeClickableMenu is GameMenu menu)
            {
                Rectangle bounds = this.GetInventoryCatButtonBounds(menu);
                Rectangle hitBox = new Rectangle(bounds.X - 8, bounds.Y - 8, bounds.Width + 16, bounds.Height + 16);

                Vector2 scaledCursor = e.Cursor.GetScaledScreenPixels();
                int uiMouseX = Game1.getMouseX(true);
                int uiMouseY = Game1.getMouseY(true);

                if (hitBox.Contains((int)scaledCursor.X, (int)scaledCursor.Y) || hitBox.Contains(uiMouseX, uiMouseY))
                {
                    Game1.playSound("bigSelect");
                    Game1.activeClickableMenu = new CatAssignMenu(this.sharedData, this.AssignCatToPlayer);
                    this.Helper.Input.Suppress(e.Button);
                    return;
                }
            }

            // 2. Clic derecho para acariciar a Zuko o Lila en el mundo
            if (!Context.IsPlayerFree || Game1.eventUp || Game1.player == null || Game1.currentLocation == null)
                return;

            // No permitir hacer clic sobre los gatos en las minas (MineShaft incluye Minas normales y Caverna Calavera) ni en el Volcán
            if (Game1.currentLocation is StardewValley.Locations.MineShaft ||
                Game1.currentLocation is StardewValley.Locations.VolcanoDungeon)
                return;

            if (e.Button.IsActionButton())
            {
                Vector2 cursorPos = e.Cursor.AbsolutePixels;

                if (this.zuko != null && this.zuko.currentLocation == Game1.currentLocation)
                {
                    if (Vector2.Distance(cursorPos, this.zuko.Position + new Vector2(32f, 32f)) < 64f &&
                        Vector2.Distance(Game1.player.Position, this.zuko.Position) < 160f)
                    {
                        this.RequestPet("Zuko");
                        this.Helper.Input.Suppress(e.Button);
                        return;
                    }
                }

                if (this.lila != null && this.lila.currentLocation == Game1.currentLocation)
                {
                    if (Vector2.Distance(cursorPos, this.lila.Position + new Vector2(32f, 32f)) < 64f &&
                        Vector2.Distance(Game1.player.Position, this.lila.Position) < 160f)
                    {
                        this.RequestPet("Lila");
                        this.Helper.Input.Suppress(e.Button);
                        return;
                    }
                }
            }
        }

        private void RequestPet(string catName)
        {
            if (Context.IsMainPlayer)
            {
                this.ApplySharedPet(catName);
            }
            else
            {
                if (catName == "Zuko" && this.zuko != null)
                {
                    int previewPts = this.sharedData.ZukoFriendshipPoints;
                    this.zuko.TryPet(ref previewPts);
                }
                else if (catName == "Lila" && this.lila != null)
                {
                    int previewPts = this.sharedData.LilaFriendshipPoints;
                    this.lila.TryPet(ref previewPts);
                }

                this.Helper.Multiplayer.SendMessage(
                    message: catName,
                    messageType: PetRequestMessage,
                    modIDs: new[] { this.ModManifest.UniqueID },
                    playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID }
                );
            }
        }

        private void AssignCatToPlayer(string catName, long targetPlayerId)
        {
            if (!Context.IsMainPlayer)
                return;

            if (catName == "Zuko")
            {
                this.sharedData.ZukoOwnerId = targetPlayerId;
                this.zuko?.WarpToPlayer(this.GetAssignedFarmer(targetPlayerId));
            }
            else if (catName == "Lila")
            {
                this.sharedData.LilaOwnerId = targetPlayerId;
                this.lila?.WarpToPlayer(this.GetAssignedFarmer(targetPlayerId));
            }

            this.BroadcastSharedState();
        }

        private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.eventUp || Game1.currentLocation == null || Game1.player == null)
                return;

            if (!Game1.currentLocation.IsOutdoors)
                return;

            long myId = Game1.player.UniqueMultiplayerID;
            long zukoOwner = this.sharedData.ZukoOwnerId == 0 ? Game1.MasterPlayer.UniqueMultiplayerID : this.sharedData.ZukoOwnerId;
            long lilaOwner = this.sharedData.LilaOwnerId == 0 ? Game1.MasterPlayer.UniqueMultiplayerID : this.sharedData.LilaOwnerId;

            bool zukoCooldownReady = (e.NewTime - this.lastZukoGiftTime) >= 300;
            bool lilaCooldownReady = (e.NewTime - this.lastLilaGiftTime) >= 300;

            int zukoHearts = this.sharedData.ZukoFriendshipPoints / 250;
            int lilaHearts = this.sharedData.LilaFriendshipPoints / 250;

            if (zukoOwner == myId && this.zuko != null && this.zukoGiftsToday < 3 && zukoCooldownReady && this.random.NextDouble() < 0.18)
            {
                string itemId = this.giftCatalog.GetRandomItemByHearts(zukoHearts, this.random);
                this.zuko.DropGift(itemId, this.zukoPhrases);
                this.zukoGiftsToday++;
                this.lastZukoGiftTime = e.NewTime;
                this.sharedData.TotalGiftsReceived++;
                this.BroadcastSharedState();
            }
            else if (lilaOwner == myId && this.lila != null && this.lilaGiftsToday < 3 && lilaCooldownReady && this.random.NextDouble() < 0.18)
            {
                string itemId = this.giftCatalog.GetRandomItemByHearts(lilaHearts, this.random);
                this.lila.DropGift(itemId, this.lilaPhrases);
                this.lilaGiftsToday++;
                this.lastLilaGiftTime = e.NewTime;
                this.sharedData.TotalGiftsReceived++;
                this.BroadcastSharedState();
            }
        }

        private void OnPlayerWarped(object? sender, WarpedEventArgs e)
        {
            if (!e.IsLocalPlayer || !Context.IsWorldReady || e.Player == null || e.NewLocation == null)
                return;

            try
            {
                this.EnsureCatsInitialized();

                long zukoOwner = this.sharedData.ZukoOwnerId == 0 ? Game1.MasterPlayer.UniqueMultiplayerID : this.sharedData.ZukoOwnerId;
                long lilaOwner = this.sharedData.LilaOwnerId == 0 ? Game1.MasterPlayer.UniqueMultiplayerID : this.sharedData.LilaOwnerId;

                if (zukoOwner == e.Player.UniqueMultiplayerID)
                    this.zuko?.WarpToPlayer(e.Player);

                if (lilaOwner == e.Player.UniqueMultiplayerID)
                    this.lila?.WarpToPlayer(e.Player);

                if (Context.IsMainPlayer)
                {
                    this.eventManager.CheckAndTriggerEvents(e.NewLocation, this.sharedData.TotalGiftsReceived);
                }
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"Error controlado en Warped: {ex.Message}", LogLevel.Warn);
            }
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.paused || Game1.activeClickableMenu != null)
                return;

            if (Game1.eventUp)
                return;

            this.EnsureCatsInitialized();

            Farmer zukoFarmer = this.GetAssignedFarmer(this.sharedData.ZukoOwnerId);
            Farmer lilaFarmer = this.GetAssignedFarmer(this.sharedData.LilaOwnerId);

            this.zuko?.UpdateFollow(zukoFarmer, Game1.currentGameTime);
            this.lila?.UpdateFollow(lilaFarmer, Game1.currentGameTime);
        }

        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            this.zuko = null;
            this.lila = null;
            this.sharedData = new CatProgressionData();
        }

        private void OnTestGiftCommand(string command, string[] args)
        {
            if (!Context.IsWorldReady)
                return;

            int zukoHearts = this.sharedData.ZukoFriendshipPoints / 250;
            int lilaHearts = this.sharedData.LilaFriendshipPoints / 250;

            string zukoItem = this.giftCatalog.GetRandomItemByHearts(zukoHearts, this.random);
            string lilaItem = this.giftCatalog.GetRandomItemByHearts(lilaHearts, this.random);

            this.zuko?.DropGift(zukoItem, this.zukoPhrases);
            this.lila?.DropGift(lilaItem, this.lilaPhrases);
            this.sharedData.TotalGiftsReceived += 2;
            this.BroadcastSharedState();
        }

        private void OnMaxHeartsCommand(string command, string[] args)
        {
            if (!Context.IsWorldReady)
                return;

            this.sharedData.ZukoFriendshipPoints = 2500;
            this.sharedData.LilaFriendshipPoints = 2500;
            this.BroadcastSharedState();
            this.Monitor.Log("¡Cariño compartido con Zuko y Lila subido a 10/10 corazones (2500 pts)!", LogLevel.Info);
        }
    }
}