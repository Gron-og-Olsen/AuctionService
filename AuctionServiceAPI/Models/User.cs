using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.String)] // Ensures the Guid is stored as a string in MongoDB
    public Guid Id { get; set; }

    public string Username { get; set; }
    public string Password { get; set; }
    public string First_Name { get; set; }
    public string Last_Name { get; set; }
    public string Email { get; set; }
    public string Address { get; set; }
    public string City { get; set; }
    public int PostalCode { get; set; }
    public string Country { get; set; }
    public string Role { get; set; }
    public string Phone { get; set; }
}
