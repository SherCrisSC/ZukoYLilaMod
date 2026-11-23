using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
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
        private CatProgressionData saveData = new CatProgressionData();

        private int zukoGiftsToday = 0;
        private int lilaGiftsToday = 0;
        private int lastZukoGiftTime = -999;
        private int lastLilaGiftTime = -999;
        private bool isFirstDayAfterLoad = true;
        private readonly Random random = new Random();

        // 45 puntos por día * 10 días = 450 puntos de penalización si pasa un día sin recibir cariño
        private const int DailyPetGain = 45;
        private const int MissedDayPenalty = DailyPetGain * 10; // 450 puntos

        // Frases personalizadas de Zuko ("El ñom")
        private readonly List<string> zukoPhrases = new()
        {
            "El zuko le robo a la lila esto y te lo entrego",
            "El ñom acaba de encontrar algo",
            "Ñom sabe que no es muy cariñoso pero para demostrar su amor te trajo esto"
        };

        // Frases personalizadas de Lila ("La loca")
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
            helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;

            helper.ConsoleCommands.Add("zuko_regalo", "Hace que Zuko y Lila suelten un objeto ahora mismo.", this.OnTestGiftCommand);
            helper.ConsoleCommands.Add("zuko_corazones", "Sube a 10 corazones el cariño con Zuko y Lila para probar regalos raros.", this.OnMaxHeartsCommand);
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            this.saveData = this.Helper.Data.ReadSaveData<CatProgressionData>("Shercris.ZukoYLila.Progression")
                            ?? new CatProgressionData();

            // Limpia cualquier rastro previo en friendshipData si quedó guardado de la prueba anterior
            Game1.player.friendshipData.Remove("Zuko");
            Game1.player.friendshipData.Remove("Lila");

            this.giftCatalog.BuildCatalog();
            this.isFirstDayAfterLoad = true;

            this.zuko = new CompanionCat(
                name: "Zuko",
                spriteAssetName: "Animals\\cat",
                followDistance: 64f,
                moveSpeed: 4.2f,
                offsetPreference: new Vector2(-48f, 24f)
            );

            this.lila = new CompanionCat(
                name: "Lila",
                spriteAssetName: "Animals\\cat1",
                followDistance: 84f,
                moveSpeed: 3.9f,
                offsetPreference: new Vector2(48f, 32f)
            );

            this.zuko.WarpToPlayer(Game1.player);
            this.lila.WarpToPlayer(Game1.player);
        }

        private void OnSaving(object? sender, SavingEventArgs e)
        {
            this.Helper.Data.WriteSaveData("Shercris.ZukoYLila.Progression", this.saveData);

            this.zuko?.currentLocation?.characters.Remove(this.zuko);
            this.lila?.currentLocation?.characters.Remove(this.lila);
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            if (!this.isFirstDayAfterLoad)
            {
                if (this.zuko != null && !this.zuko.PettedToday && this.saveData.ZukoFriendshipPoints > 0)
                {
                    this.saveData.ZukoFriendshipPoints = Math.Max(0, this.saveData.ZukoFriendshipPoints - MissedDayPenalty);
                    Game1.addHUDMessage(new HUDMessage("Zuko se sintió ignorado ayer y perdió cariño...", HUDMessage.error_type));
                }

                if (this.lila != null && !this.lila.PettedToday && this.saveData.LilaFriendshipPoints > 0)
                {
                    this.saveData.LilaFriendshipPoints = Math.Max(0, this.saveData.LilaFriendshipPoints - MissedDayPenalty);
                    Game1.addHUDMessage(new HUDMessage("Lila se sintió ignorada ayer y perdió cariño...", HUDMessage.error_type));
                }
            }

            this.isFirstDayAfterLoad = false;
            this.zukoGiftsToday = 0;
            this.lilaGiftsToday = 0;
            this.lastZukoGiftTime = -999;
            this.lastLilaGiftTime = -999;

            if (this.zuko != null)
            {
                this.zuko.PettedToday = false;
                this.zuko.WarpToPlayer(Game1.player);
            }

            if (this.lila != null)
            {
                this.lila.PettedToday = false;
                this.lila.WarpToPlayer(Game1.player);
            }
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.eventUp || Game1.activeClickableMenu != null)
                return;

            if (e.Button.IsActionButton())
            {
                Vector2 cursorPos = e.Cursor.AbsolutePixels;

                if (this.zuko != null && this.zuko.currentLocation == Game1.currentLocation)
                {
                    if (Vector2.Distance(cursorPos, this.zuko.Position + new Vector2(32f, 32f)) < 64f &&
                        Vector2.Distance(Game1.player.Position, this.zuko.Position) < 160f)
                    {
                        int points = this.saveData.ZukoFriendshipPoints;
                        this.zuko.TryPet(ref points);
                        this.saveData.ZukoFriendshipPoints = points;
                        this.Helper.Input.Suppress(e.Button);
                        return;
                    }
                }

                if (this.lila != null && this.lila.currentLocation == Game1.currentLocation)
                {
                    if (Vector2.Distance(cursorPos, this.lila.Position + new Vector2(32f, 32f)) < 64f &&
                        Vector2.Distance(Game1.player.Position, this.lila.Position) < 160f)
                    {
                        int points = this.saveData.LilaFriendshipPoints;
                        this.lila.TryPet(ref points);
                        this.saveData.LilaFriendshipPoints = points;
                        this.Helper.Input.Suppress(e.Button);
                        return;
                    }
                }
            }
        }

        private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.eventUp || Game1.currentLocation == null)
                return;

            if (!Game1.currentLocation.IsOutdoors)
                return;

            bool zukoCooldownReady = (e.NewTime - this.lastZukoGiftTime) >= 300;
            bool lilaCooldownReady = (e.NewTime - this.lastLilaGiftTime) >= 300;

            int zukoHearts = this.saveData.ZukoFriendshipPoints / 250;
            int lilaHearts = this.saveData.LilaFriendshipPoints / 250;

            if (this.zuko != null && this.zukoGiftsToday < 3 && zukoCooldownReady && this.random.NextDouble() < 0.18)
            {
                string itemId = this.giftCatalog.GetRandomItemByHearts(zukoHearts, this.random);
                this.zuko.DropGift(itemId, this.zukoPhrases);
                this.zukoGiftsToday++;
                this.lastZukoGiftTime = e.NewTime;
                this.saveData.TotalGiftsReceived++;
            }
            else if (this.lila != null && this.lilaGiftsToday < 3 && lilaCooldownReady && this.random.NextDouble() < 0.18)
            {
                string itemId = this.giftCatalog.GetRandomItemByHearts(lilaHearts, this.random);
                this.lila.DropGift(itemId, this.lilaPhrases);
                this.lilaGiftsToday++;
                this.lastLilaGiftTime = e.NewTime;
                this.saveData.TotalGiftsReceived++;
            }
        }

        private void OnPlayerWarped(object? sender, WarpedEventArgs e)
        {
            if (!e.IsLocalPlayer || !Context.IsWorldReady)
                return;

            this.zuko?.WarpToPlayer(Game1.player);
            this.lila?.WarpToPlayer(Game1.player);

            this.eventManager.CheckAndTriggerEvents(e.NewLocation, this.saveData.TotalGiftsReceived);
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.paused || Game1.activeClickableMenu != null)
                return;

            if (Game1.eventUp)
                return;

            this.zuko?.UpdateFollow(Game1.player, Game1.currentGameTime);
            this.lila?.UpdateFollow(Game1.player, Game1.currentGameTime);
        }

        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            this.zuko = null;
            this.lila = null;
        }

        private void OnTestGiftCommand(string command, string[] args)
        {
            if (!Context.IsWorldReady)
                return;

            int zukoHearts = this.saveData.ZukoFriendshipPoints / 250;
            int lilaHearts = this.saveData.LilaFriendshipPoints / 250;

            string zukoItem = this.giftCatalog.GetRandomItemByHearts(zukoHearts, this.random);
            string lilaItem = this.giftCatalog.GetRandomItemByHearts(lilaHearts, this.random);

            this.zuko?.DropGift(zukoItem, this.zukoPhrases);
            this.lila?.DropGift(lilaItem, this.lilaPhrases);
            this.saveData.TotalGiftsReceived += 2;
        }

        private void OnMaxHeartsCommand(string command, string[] args)
        {
            this.saveData.ZukoFriendshipPoints = 2500;
            this.saveData.LilaFriendshipPoints = 2500;
            this.Monitor.Log("¡Cariño con Zuko y Lila subido a 10/10 corazones (2500 pts)!", LogLevel.Info);
        }
    }
}