using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace game_data_center.Models.Db
{
    public class ChatMessage
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
       public string SenderId { get; set; }
        public string RecipientId { get; set; } // This could be a UserId or a RoomId depending on your chat design
        public string Content { get; set; }
        public DateTime Timestamp  { get; set; } // Consider storing timestamps in UTC for consistency
    }
}
