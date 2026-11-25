using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System;
using System.Collections.Generic;

namespace ZukoYLilaMod
{
    public class CompanionCat : NPC
    {
        private readonly float followDistance;
        private readonly float moveSpeed;
        private readonly Vector2 offsetPreference;
        private int animationTimer;
        private static readonly Random rng = new Random();

        public bool PettedToday { get; set; } = false;

        // Constructor sin parámetros requerido por el motor de red (Netcode) de Stardew Valley
        public CompanionCat()
            : base(
                sprite: new AnimatedSprite("Animals\\cat", 0, 32, 32),
                position: Vector2.Zero,
                defaultMap: "Farm",
                facingDir: 2,
                name: "Zuko",
                portrait: null,
                eventActor: false
            )
        {
            this.followDistance = 64f;
            this.moveSpeed = 4.2f;
            this.offsetPreference = new Vector2(-48f, 24f);
            this.HideShadow = false;
            this.Breather = false;
            this.farmerPassesThrough = true;
            this.collidesWithOtherCharacters.Value = false;
        }

        public CompanionCat(string name, string spriteAssetName, float followDistance, float moveSpeed, Vector2 offsetPreference)
            : base(
                sprite: new AnimatedSprite(spriteAssetName, 0, 32, 32),
                position: Vector2.Zero,
                defaultMap: "Farm",
                facingDir: 2,
                name: name,
                portrait: null,
                eventActor: false
            )
        {
            this.followDistance = followDistance;
            this.moveSpeed = moveSpeed;
            this.offsetPreference = offsetPreference;

            this.HideShadow = false;
            this.Breather = false;
            this.farmerPassesThrough = true;
            this.collidesWithOtherCharacters.Value = false;
        }

        public void WarpToPlayer(Farmer? player)
        {
            if (player?.currentLocation == null)
                return;

            this.currentLocation = player.currentLocation;
            Vector2 spawnPos = player.Position + this.offsetPreference;
            this.Position = spawnPos;
            this.SetCatFacing(player.FacingDirection, isMoving: false, Game1.currentGameTime);
        }

        public void UpdateFollow(Farmer? player, GameTime time)
        {
            if (player?.currentLocation == null)
                return;

            if (this.currentLocation != player.currentLocation)
            {
                this.WarpToPlayer(player);
                return;
            }

            // Actualiza saltos y emotes activos en cada tick
            this.updateEmote(time);
            if (this.yJumpOffset != 0 || this.yJumpVelocity != 0f)
            {
                this.yJumpOffset += (int)this.yJumpVelocity;
                this.yJumpVelocity -= 0.5f;
                if (this.yJumpOffset >= 0)
                {
                    this.yJumpOffset = 0;
                    this.yJumpVelocity = 0f;
                }
            }

            Vector2 targetPos = player.Position + this.offsetPreference;
            Vector2 diff = targetPos - this.Position;
            float distance = diff.Length();

            if (distance > 650f)
            {
                this.Position = targetPos;
                this.SetCatFacing(player.FacingDirection, isMoving: false, time);
                return;
            }

            if (distance > this.followDistance)
            {
                diff.Normalize();
                float currentSpeed = distance > 220f ? this.moveSpeed * 1.45f : this.moveSpeed;
                this.Position += diff * currentSpeed;

                this.SetCatFacing(player.FacingDirection, isMoving: true, time);
            }
            else
            {
                this.SetCatFacing(player.FacingDirection, isMoving: false, time);
            }
        }

        public void DrawCompanion(SpriteBatch b)
        {
            if (this.currentLocation == null || this.currentLocation != Game1.currentLocation || this.Sprite == null)
                return;

            Vector2 screenPos = Game1.GlobalToLocal(Game1.viewport, this.Position + new Vector2(0f, this.yJumpOffset));
            float layerDepth = Math.Max(0f, (this.Position.Y + 64f) / 10000f);

            // Sombra suave debajo del gatito
            b.Draw(
                Game1.shadowTexture,
                Game1.GlobalToLocal(Game1.viewport, this.Position + new Vector2(32f, 58f)),
                Game1.shadowTexture.Bounds,
                Color.White * 0.6f,
                0f,
                new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y),
                3.2f,
                SpriteEffects.None,
                layerDepth - 0.0001f
            );

            // Sprite animado del gato
            b.Draw(
                this.Sprite.Texture,
                screenPos,
                this.Sprite.SourceRect,
                Color.White,
                0f,
                Vector2.Zero,
                4f,
                SpriteEffects.None,
                layerDepth
            );

            // Dibuja el globo de corazón (emote) si está activo
            if (this.IsEmoting)
            {
                Vector2 emotePos = screenPos + new Vector2(16f, -36f);
                b.Draw(
                    Game1.emoteSpriteSheet,
                    emotePos,
                    new Rectangle(this.CurrentEmoteIndex * 16 % Game1.emoteSpriteSheet.Width, this.CurrentEmoteIndex * 16 / Game1.emoteSpriteSheet.Width * 16, 16, 16),
                    Color.White,
                    0f,
                    Vector2.Zero,
                    4f,
                    SpriteEffects.None,
                    layerDepth + 0.001f
                );
            }
        }

        public bool TryPet(ref int friendshipPoints)
        {
            if (this.currentLocation == null)
                return false;

            this.yJumpVelocity = 6f;
            this.yJumpOffset = -1;
            this.currentLocation.playSound("cat");
            this.doEmote(20);

            if (!this.PettedToday)
            {
                this.PettedToday = true;
                friendshipPoints = Math.Min(2500, friendshipPoints + 45);
            }

            int hearts = friendshipPoints / 250;
            string heartBar = new string('♥', hearts) + new string('♡', 10 - hearts);
            Game1.addHUDMessage(new HUDMessage($"{this.Name}: {heartBar} ({hearts}/10)", HUDMessage.newQuest_type));
            return true;
        }

        public void DropGift(string qualifiedItemId, List<string> customPhrases)
        {
            if (this.currentLocation == null)
                return;

            Item gift = ItemRegistry.Create(qualifiedItemId);
            string chosenPhrase = customPhrases[rng.Next(customPhrases.Count)];

            Game1.createItemDebris(
                item: gift,
                pixelOrigin: this.Position,
                direction: rng.Next(4),
                location: this.currentLocation
            );

            this.yJumpVelocity = 6f;
            this.yJumpOffset = -1;
            this.currentLocation.playSound("cat");
            this.doEmote(20);
            Game1.addHUDMessage(new HUDMessage(chosenPhrase, HUDMessage.newQuest_type));
        }

        private void SetCatFacing(int playerFacingDirection, bool isMoving, GameTime time)
        {
            this.FacingDirection = playerFacingDirection;

            int spriteRow = playerFacingDirection switch
            {
                2 => 0,
                1 => 1,
                0 => 2,
                3 => 3,
                _ => 0
            };

            int baseFrame = spriteRow * 4;

            if (!isMoving)
            {
                this.animationTimer = 0;
                this.Sprite.CurrentFrame = baseFrame;
                return;
            }

            this.animationTimer += (int)time.ElapsedGameTime.TotalMilliseconds;
            if (this.animationTimer >= 130)
            {
                this.animationTimer = 0;
                int currentOffset = (this.Sprite.CurrentFrame - baseFrame + 1) % 4;
                if (currentOffset < 0) currentOffset = 0;
                this.Sprite.CurrentFrame = baseFrame + currentOffset;
            }
        }
    }
}