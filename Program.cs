using game_data_center.Models;
using game_data_center.Models.Request;
using Microsoft.AspNetCore.Mvc;
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



app.MapPost("/sendFriendRequest", async ([FromBody] FriendRequest data) =>
{
    Console.WriteLine(JsonSerializer.Serialize(data) + "TAKE A LOOK");

    string recipientPlayFabId = data.recipientId;
    Friend Sender = new Friend { DisplayName = data.newFriend.DisplayName, PlayfabId = data.newFriend.PlayfabId } ;

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


app.Run();

