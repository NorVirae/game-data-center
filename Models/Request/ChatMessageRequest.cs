namespace game_data_center.Models.Request
{
    public class ChatMessageRequest
    {
        public string SenderId { get; set; }
        public string RecipientId { get; set; } // This could be a UserId or a RoomId depending on your chat design
        public string Content { get; set; }
    }
}
