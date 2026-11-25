namespace ZukoYLilaMod
{
    public class CatProgressionData
    {
        // Corazones compartidos entre todos los jugadores (250 pts = 1 corazón, 2500 = 10 corazones)
        public int ZukoFriendshipPoints { get; set; } = 0;
        public int LilaFriendshipPoints { get; set; } = 0;
        public int TotalGiftsReceived { get; set; } = 0;

        // ID del jugador que actualmente tiene asignado a cada gato (0 = Jugador Principal / Host)
        public long ZukoOwnerId { get; set; } = 0;
        public long LilaOwnerId { get; set; } = 0;

        // Control diario compartido para evitar sumar puntos dobles el mismo día
        public bool ZukoPettedToday { get; set; } = false;
        public bool LilaPettedToday { get; set; } = false;
    }
}