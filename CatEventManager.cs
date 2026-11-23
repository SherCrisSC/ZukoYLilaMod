using StardewValley;
using System.Collections.Generic;

namespace ZukoYLilaMod
{
    public class CatEventManager
    {
        // IDs únicos para que el juego recuerde qué eventos ya viste
        public const string ForestEventId = "Shercris.ZukoYLila_ForestDiscovery";
        public const string BeachEventId = "Shercris.ZukoYLila_BeachTreasure";

        public void CheckAndTriggerEvents(GameLocation location, int totalGiftsReceived)
        {
            if (Game1.eventUp || location == null)
                return;

            // EVENTO 1: Al entrar al Bosque Tizón (Forest) por primera vez con Zuko y Lila
            if (location.Name == "Forest" && !Game1.player.eventsSeen.Contains(ForestEventId))
            {
                string forestEventScript =
                    "playful/90 16/farmer 90 16 2/skippable/" +
                    "pause 500/" +
                    "message \"Zuko y Lila empiezan a olfatear el aire fresco del bosque con mucha curiosidad...\"/" +
                    "emote farmer 32/" +
                    "playSound cat/" +
                    "pause 400/" +
                    "playSound cat/" +
                    "message \"¡Parece que han elegido este bosque como su lugar favorito de aventuras!\"/" +
                    "pause 500/" +
                    "end";

                location.startEvent(new Event(forestEventScript, null, ForestEventId, Game1.player));
                Game1.player.eventsSeen.Add(ForestEventId);
                return;
            }

            // EVENTO 2: Al entrar a la Playa (Beach) después de que te hayan regalado al menos 1 objeto
            if (location.Name == "Beach" && totalGiftsReceived >= 1 && !Game1.player.eventsSeen.Contains(BeachEventId))
            {
                string beachEventScript =
                    "ocean/38 15/farmer 38 15 2/skippable/" +
                    "pause 600/" +
                    "message \"Zuko corre hacia la orilla mientras Lila observa las olas con atención.\"/" +
                    "playSound cat/" +
                    "emote farmer 20/" +
                    "message \"¡Entre los dos desentierran un tesoro brillante de la arena y te lo entregan!\"/" +
                    "addItem (O)393/" + // Te regalan un Coral al terminar el evento
                    "pause 500/" +
                    "end";

                location.startEvent(new Event(beachEventScript, null, BeachEventId, Game1.player));
                Game1.player.eventsSeen.Add(BeachEventId);
            }
        }
    }
}