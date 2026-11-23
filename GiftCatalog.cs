using StardewValley;
using StardewValley.GameData.Objects;
using System;
using System.Collections.Generic;

namespace ZukoYLilaMod
{
    public class GiftCatalog
    {
        private readonly List<string> commonItems = new();    // 0 - 2 corazones (baratos / básicos)
        private readonly List<string> uncommonItems = new();  // 3 - 5 corazones (valor medio)
        private readonly List<string> rareItems = new();      // 6 - 8 corazones (valiosos)
        private readonly List<string> epicItems = new();      // 9 - 10 corazones (tesoros / muy caros)

        // IDs específicos de objetos de progresión, llaves, teletransportes de zona o ítems especiales
        private static readonly HashSet<string> BlockedProgressionIds = new()
        {
            "73",  // Nuez dorada (Golden Walnut)
            "434", // Gota estelar (Stardrop)
            "688", // Tótem de viaje: Granja
            "689", // Tótem de viaje: Montaña
            "690", // Tótem de viaje: Playa
            "261", // Tótem de viaje: Desierto
            "886", // Tótem de viaje: Isla Jengibre
            "911", // Cetro de retorno (si existiera como objeto)
            "787", // Batería (usada para desbloquear el barco a la Isla Jengibre)
            "892", // Tótem de lluvia / warp especial
            "797", // Perla (usada en eventos/puzzles)
            "74",  // Esquirla prismática (opcional: si quieres que puedan darla a 10 corazones, quita "74" de aquí)
            "166", // Cofre del tesoro
            "279", // Caramelo mágico
            "872", // Polvo de hadas
            "417", // Baya dulce (Sweet Gem Berry - estatua del bosque)
            "460", // Colgante de sirena (matrimonio)
            "808", // Colgante fantasma (Krobus)
            "809", // Entrada de cine
            "858", // Qi Gem
            "896", // Alma de galaxia
            "910", // Fragmento radiactivo
            "909"  // Mineral radiactivo
        };

        // Palabras clave en el nombre interno o categoría para filtrar llaves y objetos de misión/zona
        private static readonly string[] BlockedKeywords = new[]
        {
            "Key", "Warp", "Totem", "Stardrop", "Walnut", "Quest", "Ticket", "Pass", "Pendant", "Trophy", "Book", "Skill"
        };

        public void BuildCatalog()
        {
            this.commonItems.Clear();
            this.uncommonItems.Clear();
            this.rareItems.Clear();
            this.epicItems.Clear();

            foreach (KeyValuePair<string, ObjectData> entry in Game1.objectData)
            {
                string id = entry.Key;
                ObjectData data = entry.Value;

                if (data == null || BlockedProgressionIds.Contains(id))
                    continue;

                // Excluir objetos que no se deben obtener normalmente o son de misión/basura/piedras/maleza
                if (data.ExcludeFromRandomSale || data.ExcludeFromShippingCollection)
                    continue;

                if (data.Type == "Quest" || data.Type == "Arch" || data.Type == "asdf" || data.Type == "Crafting")
                    continue;

                // Excluir categoría basura (-20), semillas (-74) si no quieres semillas, o minerales de geoda sin procesar
                if (data.Category == StardewValley.Object.junkCategory || data.Category == StardewValley.Object.litterCategory)
                    continue;

                // Revisar palabras clave de progresión en el nombre interno
                bool hasBlockedKeyword = false;
                foreach (string keyword in BlockedKeywords)
                {
                    if (!string.IsNullOrEmpty(data.Name) && data.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        hasBlockedKeyword = true;
                        break;
                    }
                }
                if (hasBlockedKeyword)
                    continue;

                // Clasificar por precio de venta para escalar la calidad según los corazones
                string qualifiedId = $"(O){id}";
                int price = data.Price;

                if (price <= 0)
                    continue;

                if (price <= 80)
                    this.commonItems.Add(qualifiedId);
                else if (price <= 220)
                    this.uncommonItems.Add(qualifiedId);
                else if (price <= 550)
                    this.rareItems.Add(qualifiedId);
                else
                    this.epicItems.Add(qualifiedId);
            }
        }

        public string GetRandomItemByHearts(int hearts, Random rng)
        {
            // Asegura que el catálogo esté construido
            if (this.commonItems.Count == 0)
                this.BuildCatalog();

            double roll = rng.NextDouble();

            // Según los corazones (0 a 10), aumenta la probabilidad de sacar mejores categorías
            List<string> chosenTier;

            if (hearts >= 9) // 9-10 corazones: muy alta chance de objetos raros y épicos
            {
                if (roll < 0.45 && this.epicItems.Count > 0) chosenTier = this.epicItems;
                else if (roll < 0.85 && this.rareItems.Count > 0) chosenTier = this.rareItems;
                else chosenTier = this.uncommonItems;
            }
            else if (hearts >= 6) // 6-8 corazones: buenos regalos y algunos épicos
            {
                if (roll < 0.20 && this.epicItems.Count > 0) chosenTier = this.epicItems;
                else if (roll < 0.65 && this.rareItems.Count > 0) chosenTier = this.rareItems;
                else chosenTier = this.uncommonItems;
            }
            else if (hearts >= 3) // 3-5 corazones: mezcla de comunes y poco comunes, algo raro
            {
                if (roll < 0.25 && this.rareItems.Count > 0) chosenTier = this.rareItems;
                else if (roll < 0.70 && this.uncommonItems.Count > 0) chosenTier = this.uncommonItems;
                else chosenTier = this.commonItems;
            }
            else // 0-2 corazones: objetos sencillos y algunos poco comunes
            {
                if (roll < 0.20 && this.uncommonItems.Count > 0) chosenTier = this.uncommonItems;
                else chosenTier = this.commonItems;
            }

            if (chosenTier.Count == 0)
                return "(O)18"; // Narciso de respaldo por seguridad

            return chosenTier[rng.Next(chosenTier.Count)];
        }
    }
}