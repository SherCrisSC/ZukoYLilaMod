using Microsoft.Xna.Framework;
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

        public void WarpToPlayer(Farmer player)
        {
            if (player.currentLocation == null)
                return;

            if (this.currentLocation != player.currentLocation)
            {
                this.currentLocation?.characters.Remove(this);
                this.currentLocation = player.currentLocation;

                if (!player.currentLocation.characters.Contains(this))
                {
                    player.currentLocation.addCharacter(this);
                }
            }

            Vector2 spawnPos = player.Position + this.offsetPreference;
            this.Position = spawnPos;
            this.SetCatFacing(player.FacingDirection, isMoving: false, Game1.currentGameTime);
        }

        public void UpdateFollow(Farmer player, GameTime time)
        {
            if (player?.currentLocation == null)
                return;

            if (this.currentLocation != player.currentLocation)
            {
                this.WarpToPlayer(player);
                return;
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

        public bool TryPet(ref int friendshipPoints)
        {
            if (this.currentLocation == null)
                return false;

            this.jump(4f);
            this.currentLocation.playSound("cat");
            this.doEmote(20); // Emote de corazón

            if (!this.PettedToday)
            {
                this.PettedToday = true;
                // 45 puntos diarios -> 56 días (2 estaciones completas) = 2520 -> tope 2500 (10 corazones)
                friendshipPoints = Math.Min(2500, friendshipPoints + 45);
                this.SyncSocialFriendship(friendshipPoints);

                int hearts = friendshipPoints / 250;
                Game1.addHUDMessage(new HUDMessage($"¡Le hiciste cariño a {this.Name}! ({hearts}/10 corazones)", HUDMessage.newQuest_type));
                return true;
            }
            else
            {
                int hearts = friendshipPoints / 250;
                Game1.addHUDMessage(new HUDMessage($"{this.Name} ronronea feliz ({hearts}/10 corazones).", HUDMessage.achievement_type));
                return false;
            }
        }

        public void SyncSocialFriendship(int points)
        {
            if (!Game1.player.friendshipData.ContainsKey(this.Name))
            {
                Game1.player.friendshipData.Add(this.Name, new Friendship(points));
            }
            else
            {
                Game1.player.friendshipData[this.Name].Points = points;
            }
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

            this.jump(4f);
            this.currentLocation.playSound("cat");
            this.doEmote(20);
            Game1.addHUDMessage(new HUDMessage(chosenPhrase, HUDMessage.newQuest_type));
        }

        private void SetCatFacing(int playerFacingDirection, bool isMoving, GameTime time)
        {
            this.FacingDirection = playerFacingDirection;

            int spriteRow = playerFacingDirection switch
            {
                2 => 0, // Abajo
                1 => 1, // Derecha
                0 => 2, // Arriba
                3 => 3, // Izquierda
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