using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ZukoYLilaMod
{
    public class CatAssignMenu : IClickableMenu
    {
        private readonly CatProgressionData sharedData;
        private readonly Action<string, long> onAssignCat;

        private readonly List<(ClickableComponent Button, string CatName, long TargetPlayerId, string Label)> assignButtons = new();

        public CatAssignMenu(CatProgressionData sharedData, Action<string, long> onAssignCat)
            : base(
                x: (Game1.uiViewport.Width - 640) / 2,
                y: (Game1.uiViewport.Height - 480) / 2,
                width: 640,
                height: 480,
                showUpperRightCloseButton: true
            )
        {
            this.sharedData = sharedData;
            this.onAssignCat = onAssignCat;
            this.RebuildButtons();
        }

        private void RebuildButtons()
        {
            this.assignButtons.Clear();
            var farmers = Game1.getOnlineFarmers().Where(f => f != null).ToList();

            int startY = this.yPositionOnScreen + 185;
            int rowHeight = 64;

            for (int i = 0; i < farmers.Count; i++)
            {
                Farmer farmer = farmers[i];
                int rowY = startY + i * rowHeight;
                string farmerName = string.IsNullOrWhiteSpace(farmer.Name) ? $"Granjero {i + 1}" : farmer.Name;

                var zukoRect = new Rectangle(this.xPositionOnScreen + 240, rowY, 175, 44);
                var lilaRect = new Rectangle(this.xPositionOnScreen + 425, rowY, 175, 44);

                this.assignButtons.Add((
                    new ClickableComponent(zukoRect, $"Zuko_{farmer.UniqueMultiplayerID}"),
                    "Zuko",
                    farmer.UniqueMultiplayerID,
                    farmerName
                ));

                this.assignButtons.Add((
                    new ClickableComponent(lilaRect, $"Lila_{farmer.UniqueMultiplayerID}"),
                    "Lila",
                    farmer.UniqueMultiplayerID,
                    farmerName
                ));
            }
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            foreach (var entry in this.assignButtons)
            {
                if (entry.Button.containsPoint(x, y))
                {
                    Game1.playSound("coin");
                    this.onAssignCat(entry.CatName, entry.TargetPlayerId);
                    return;
                }
            }
        }

        public override void draw(SpriteBatch b)
        {
            // Fondo oscuro translúcido
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.65f);

            // Caja de diálogo nativa de Stardew Valley
            Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, speaker: false, drawOnlyBox: true);

            // Título
            string title = "Zuko y Lila - Compañeros";
            Vector2 titleSize = Game1.dialogueFont.MeasureString(title);
            Utility.drawTextWithShadow(
                b,
                title,
                Game1.dialogueFont,
                new Vector2(this.xPositionOnScreen + (this.width - titleSize.X) / 2f, this.yPositionOnScreen + 36),
                Game1.textColor
            );

            // Corazones compartidos y dueños actuales
            int zukoHearts = this.sharedData.ZukoFriendshipPoints / 250;
            int lilaHearts = this.sharedData.LilaFriendshipPoints / 250;

            string zukoOwnerName = this.GetFarmerName(this.sharedData.ZukoOwnerId);
            string lilaOwnerName = this.GetFarmerName(this.sharedData.LilaOwnerId);

            string zukoStatus = $"Zuko: {zukoHearts}/10 corazones  |  Con: {zukoOwnerName}";
            string lilaStatus = $"Lila: {lilaHearts}/10 corazones  |  Con: {lilaOwnerName}";

            Utility.drawTextWithShadow(b, zukoStatus, Game1.smallFont, new Vector2(this.xPositionOnScreen + 48, this.yPositionOnScreen + 96), Color.DarkSlateBlue);
            Utility.drawTextWithShadow(b, lilaStatus, Game1.smallFont, new Vector2(this.xPositionOnScreen + 48, this.yPositionOnScreen + 128), Color.DarkMagenta);

            // Lista de jugadores y botones de entrega
            var farmers = Game1.getOnlineFarmers().Where(f => f != null).ToList();
            int startY = this.yPositionOnScreen + 185;
            int rowHeight = 64;

            for (int i = 0; i < farmers.Count; i++)
            {
                Farmer farmer = farmers[i];
                int rowY = startY + i * rowHeight;
                string farmerName = string.IsNullOrWhiteSpace(farmer.Name) ? $"Granjero {i + 1}" : farmer.Name;

                Utility.drawTextWithShadow(
                    b,
                    farmerName,
                    Game1.dialogueFont,
                    new Vector2(this.xPositionOnScreen + 48, rowY + 6),
                    Game1.textColor
                );
            }

            foreach (var entry in this.assignButtons)
            {
                long currentOwner = entry.CatName == "Zuko" ? this.sharedData.ZukoOwnerId : this.sharedData.LilaOwnerId;
                if (currentOwner == 0)
                    currentOwner = Game1.MasterPlayer.UniqueMultiplayerID;

                bool isAssignedHere = currentOwner == entry.TargetPlayerId;
                bool isHovered = entry.Button.containsPoint(Game1.getMouseX(true), Game1.getMouseY(true));

                IClickableMenu.drawTextureBox(
                    b,
                    Game1.mouseCursors,
                    new Rectangle(432, 439, 9, 9),
                    entry.Button.bounds.X,
                    entry.Button.bounds.Y,
                    entry.Button.bounds.Width,
                    entry.Button.bounds.Height,
                    isAssignedHere ? Color.LightGreen : (isHovered ? Color.Wheat : Color.White),
                    4f,
                    drawShadow: false
                );

                string btnText = isAssignedHere ? $"✓ Con {entry.CatName}" : $"Dar a {entry.CatName}";
                Vector2 textSize = Game1.smallFont.MeasureString(btnText);
                Utility.drawTextWithShadow(
                    b,
                    btnText,
                    Game1.smallFont,
                    new Vector2(
                        entry.Button.bounds.X + (entry.Button.bounds.Width - textSize.X) / 2f,
                        entry.Button.bounds.Y + (entry.Button.bounds.Height - textSize.Y) / 2f
                    ),
                    Game1.textColor
                );
            }

            base.draw(b);
            this.drawMouse(b);
        }

        private string GetFarmerName(long ownerId)
        {
            long targetId = ownerId == 0 ? Game1.MasterPlayer.UniqueMultiplayerID : ownerId;
            foreach (Farmer farmer in Game1.getOnlineFarmers())
            {
                if (farmer != null && farmer.UniqueMultiplayerID == targetId && !string.IsNullOrWhiteSpace(farmer.Name))
                    return farmer.Name;
            }
            return "Anfitrión";
        }
    }
}