using game_data_center.Models;
using game_data_center.Models.Db;
using game_data_center.Models.Request;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using PlayFab;
using PlayFab.AdminModels;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Assuming you have a MongoDB connection string set up in your appsettings.json or another configuration source
//var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDB");
var mongoConnectionString = "mongodb+srv://virae:C%40list5r@fischela.xme4w.mongodb.net/?retryWrites=true&w=majority";

var mongoClient = new MongoClient(mongoConnectionString);
var database = mongoClient.GetDatabase("AmadiohaChat"); // Replace with your actual database name
var chatMessagesCollection = database.GetCollection<ChatMessage>("ChatMessages"); // Replace with your actual collection name


app.MapPost("/sendFriendRequest", async ([FromBody] FriendRequest data) =>
{
    Console.WriteLine(JsonSerializer.Serialize(data) + "TAKE A LOOK");

    string recipientPlayFabId = data.recipientId;
    Friend Sender = new Friend { DisplayName = data.newFriend.DisplayName, PlayfabId = data.newFriend.PlayfabId };

    // Set your PlayFab Title ID and Developer Secret Key
    PlayFabSettings.staticSettings.TitleId = "10F24";
    PlayFabSettings.staticSettings.DeveloperSecretKey = "Z85MNMYUMKHD8HA66JTIFT1UKSOWGBUWETZABX6CJ7O7UWQDCM";

    // First, get the existing friend requests data for the recipient
    var getDataRequest = new GetUserDataRequest
    {
        PlayFabId = recipientPlayFabId,
        Keys = new List<string> { "friendRequests" }
    };

    Console.WriteLine("GotIN");

    var getDataResult = await PlayFabAdminAPI.GetUserDataAsync(getDataRequest);

    // Initialize the list of friend requests
    var friendRequests = new List<Friend>();

    if (getDataResult.Result.Data != null && getDataResult.Result.Data.ContainsKey("friendRequests"))
    {
        // Deserialize the existing list
        friendRequests = JsonSerializer.Deserialize<List<Friend>>(getDataResult.Result.Data["friendRequests"].Value);
    }

    // Append the new friend request
    friendRequests.Add(Sender);

    // Serialize the list back to JSON
    var friendRequestsJson = JsonSerializer.Serialize(friendRequests);

    // Update the friend requests data for the recipient
    var updateDataRequest = new UpdateUserDataRequest
    {
        PlayFabId = recipientPlayFabId,
        Data = new Dictionary<string, string> { { "friendRequests", friendRequestsJson } },
        Permission = UserDataPermission.Public
    };

    var updateDataResult = await PlayFabAdminAPI.UpdateUserDataAsync(updateDataRequest);

    if (updateDataResult.Error != null)
    {
        Console.WriteLine("SOme Error " + JsonSerializer.Serialize(updateDataResult.Error));

        return Results.Problem(updateDataResult.Error.ErrorMessage);
    }
    Console.WriteLine("success got here");

    return Results.Ok("Friend request sent successfully");

});


app.MapPost("/sendMessage", async ([FromBody] ChatMessageRequest messageRequest) =>
{
    var chatMessage = new ChatMessage
    {
        SenderId = messageRequest.SenderId,
        RecipientId = messageRequest.RecipientId, // This could be a UserId or a RoomId depending on your chat design
        Content = messageRequest.Content,
        Timestamp = DateTime.UtcNow // Consider storing timestamps in UTC for consistency
    };

    await chatMessagesCollection.InsertOneAsync(chatMessage);

    return Results.Ok("Message sent successfully");
});


app.MapGet("/fetchChatWithFriend", async ([FromQuery] string currentUserId, [FromQuery] string friendId, [FromQuery] DateTime? afterTimestamp) =>
{
    var filterBuilder = Builders<ChatMessage>.Filter;

    // Filter for messages sent from the current user to the friend
    var fromCurrentUserToFriendFilter = filterBuilder.And(
        filterBuilder.Eq(message => message.SenderId, currentUserId),
        filterBuilder.Eq(message => message.RecipientId, friendId)
    );

    // Filter for messages sent from the friend to the current user
    var fromFriendToCurrentUserFilter = filterBuilder.And(
        filterBuilder.Eq(message => message.SenderId, friendId),
        filterBuilder.Eq(message => message.RecipientId, currentUserId)
    );

    // Combine the two filters to capture the complete conversation
    var conversationFilter = filterBuilder.Or(fromCurrentUserToFriendFilter, fromFriendToCurrentUserFilter);

    // If an 'afterTimestamp' is provided, include it in the filter to get recent messages
    if (afterTimestamp.HasValue)
    {
        conversationFilter = filterBuilder.And(
            conversationFilter,
            filterBuilder.Gt(message => message.Timestamp, afterTimestamp.Value)
        );
    }

    var messages = await chatMessagesCollection.Find(conversationFilter)
                                               .SortBy(message => message.Timestamp)
                                               .ToListAsync();

    return Results.Ok(messages);
});

app.MapPost("/removeFriendRequest", async ([FromBody] FriendRequest data) =>
{


    var getDataRequest = new GetUserDataRequest
    {
        PlayFabId = data.recipientId,
        Keys = new List<string> { "friendRequests" }
    };

    var getDataResult = await PlayFabAdminAPI.GetUserDataAsync(getDataRequest);

    if (getDataResult.Result.Data.TryGetValue("friendRequests", out var friendRequestsData))
    {
        var friendRequests = JsonSerializer.Deserialize<List<Friend>>(friendRequestsData.Value);
        friendRequests.RemoveAll(f => f.PlayfabId == data.newFriend.PlayfabId);

        var updateDataRequest = new UpdateUserDataRequest
        {
            PlayFabId = data.recipientId,
            Data = new Dictionary<string, string> { { "friendRequests", JsonSerializer.Serialize(friendRequests) } },
            Permission = UserDataPermission.Public
        };

        await PlayFabAdminAPI.UpdateUserDataAsync(updateDataRequest);
    }

    return Results.Ok("Friend request removed successfully.");
});

app.MapPost("/removeAcceptedFriends", async ([FromBody] FriendRequest data) =>
{


    var getDataRequest = new GetUserDataRequest
    {
        PlayFabId = data.recipientId,
        Keys = new List<string> { "friendRequests" }
    };

    var getDataResult = await PlayFabAdminAPI.GetUserDataAsync(getDataRequest);

    if (getDataResult.Result.Data.TryGetValue("friendRequests", out var friendRequestsData))
    {
        var friendRequests = JsonSerializer.Deserialize<List<Friend>>(friendRequestsData.Value);
        friendRequests.RemoveAll(f => f.PlayfabId == data.newFriend.PlayfabId);

        var updateDataRequest = new UpdateUserDataRequest
        {
            PlayFabId = data.recipientId,
            Data = new Dictionary<string, string> { { "friendRequests", JsonSerializer.Serialize(friendRequests) } },
            Permission = UserDataPermission.Public
        };

        await PlayFabAdminAPI.UpdateUserDataAsync(updateDataRequest);
    }

    return Results.Ok("Friend request removed successfully.");
});





app.Run();