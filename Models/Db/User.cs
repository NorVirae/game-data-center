namespace game_data_center.Models.Db
{
    public class User
    {
        public string Id { get; set; }
        public string PlayfabId{ get; set; }
        public string PlayDisplayName { get; set;}
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set;}
    }
}
