using UrlShortener.Models;

namespace UrlShortener.Helper;

public class DataMock
{
    public static readonly List<User> Users =
    [
new User { FirstName = "Olivia", LastName = "Green", Email = "olivia.green@example.com", Password = "passwordabc", PasswordSalt = "b1aebc99-1c0b-4e18-ab6d-1bb9bd380c21", hashedpass = "T0zIM3FvgIWVR3qk9W9El/5LvhPN7QFkDOG/xD5md21kXtbDqBcamra6oUcoHyA6jJAUB1cAl6wNp9n/nFY2Nw==", Roles = [ "King", "High Priest" ] },
new User { FirstName = "Ethan", LastName = "White", Email = "ethan.white@example.com", Password = "passworddef", PasswordSalt = "2c3e7bda-5e5b-4c7a-b47d-2d79f530e217", hashedpass = "Eu7kPLrTlugUUlp4tgGBDzJKEIDy3Qtz3mOtGgVMQBmRc6nEOD6JnVjDGnGyefZU4KeTl9b2/yP7W8fZhh2Pvw==", Roles = [ "Bishop" ] },
new User { FirstName = "Sophia", LastName = "Hall", Email = "sophia.hall@example.com", Password = "passwordghi", PasswordSalt = "3b2d4e4b-3c6d-4d8a-b9a5-3e14c3d7b2ff", hashedpass = "YC/eBdnEIXFm1w1MtpR7Ky3FwNaA0J5AM4dh98dV7s3rsB9ij5C0vTZ8in4tJbMIMWKCP4SlzZhV5QPwKoYBzg==", Roles = [ "High Sorcerer" ] },
new User { FirstName = "Aiden", LastName = "Adams", Email = "aiden.adams@example.com", Password = "securepass1", PasswordSalt = "4a6f2c6d-1e9b-4f9b-a5d5-4c11c6b7b3aa", hashedpass = "bwWatuu1V0USxacQR+wNqO0AfzsfhS5YLiG9oJMM38C0uHdisPKy+eQBIM19JDTngOplBrBz5X0hh9iMVOG+Wg==", Roles = [ "Cosmic Ranger" ] },
new User { FirstName = "Isabella", LastName = "Scott", Email = "isabella.scott@example.com", Password = "passwordjkl", PasswordSalt = "5b7e3e5a-0d8b-4e8c-b9c5-5a10b7c3b3ee", hashedpass = "iQRd/vAlijz0ZCJbiK3AnkL1hDUNyq1NHZFevWXaCeCBEl1HLrBpLPIQOI9VnDpfTIY2wg9V8uG4nmttSKXs7g==", Roles = [ "Ultimate Ruler" ] },
new User { FirstName = "Liam", LastName = "Turner", Email = "liam.turner@example.com", Password = "pass5678", PasswordSalt = "6c8f3f4b-0e9b-4d7b-b8a5-6e12c5d7b5ff", hashedpass = "AREZJ8izbDIlzXQD3TCFVtVrUQtYNim2za64FFDTsQrFgSUFI49pO9Eihnb2SQohMWS23gm7t3W+7OG98YX27w==", Roles = [ "Chief Joy Officer" ] },
new User { FirstName = "Charlotte", LastName = "Parker", Email = "charlotte.parker@example.com", Password = "secure456", PasswordSalt = "7d9f4d4b-1e8b-4c7b-b6a5-7c14c6b7b6ff", hashedpass = "t/aj27QWDsmX4ufqic/FKoEzaKJk0iAsJGR/+NmteMsf0xi9PrgrKl54Czyvwoxgc932xA8A99m948mS+mtNDw==", Roles = [ "King" ] },
new User { FirstName = "James", LastName = "Collins", Email = "james.collins@example.com", Password = "passwordmno", PasswordSalt = "8e1f5e5a-2d7b-4f9b-b7c5-8e16c5d7b7aa", hashedpass = "FOecfUXnVLOlNqmvmxgT7+2cBjUIpJR2AxjIXivcx/uhBTXjA6panmuBX8zEeOvqfz8ul08HmyBez9RB7c3mew==", Roles = [ "High Priest" ] },
new User { FirstName = "Isabella", LastName = "Mitchell", Email = "isabella.mitchell@example.com", Password = "pass9101", PasswordSalt = "9f2f6f6b-3c6b-4e9b-b9a5-9a18c3b7b9ff", hashedpass = "46XQbe24emuZqg9JmXOZYx+UPtV0eT0zcUoMmvXbHTWxSCvPbSrwgbIUVCEzAPRVzhnOuXsFkJCmvSCcU9IpiQ==", Roles = [ "Bishop" ] },
new User { FirstName = "William", LastName = "Carter", Email = "william.carter@example.com", Password = "pass1123", PasswordSalt = "1a3f7g7b-4b5b-4d7b-b8a5-0c19c4d7b0ff", hashedpass = "MOl5XucEsXJ0con7nhAtjJHQJYgAsw36gAJmsOxl1ro1N2OCYm0aRxCn+q5eg8pD7YcmsDm/jSbgjWKY3w6C9g==", Roles = [ "High Sorcerer" ] },
new User { FirstName = "Amelia", LastName = "Evans", Email = "amelia.evans@example.com", Password = "secure789", PasswordSalt = "2b4f8h8b-5a4b-4c9b-b7a5-1e20c4d7b1aa", hashedpass = "YAVzIeFLfiDdLidOYz7jvI7O/yXQEL5MakHTJHqprKhl8Uj3LzCu7z8cUSVyAjcUfHpYfWKTwNEu+l7qCXmR9Q==", Roles = [ "Cosmic Ranger" ] },
new User { FirstName = "Lucas", LastName = "Baker", Email = "lucas.baker@example.com", Password = "passwordpqr", PasswordSalt = "3c5f9i9b-6b3b-4d8b-b6a5-2a21c3b7b2ee", hashedpass = "p1ZW6AX5UdnBbTmCQb9QUNL4Bb1XI3a4iAouHwhscajPyjZ+HPgzTnG96zCzDT/3TJyhIn2YWn1/DEDQTk4fuw==", Roles = [ "Ultimate Ruler" ] },
new User { FirstName = "Mila", LastName = "Nelson", Email = "mila.nelson@example.com", Password = "pass3141", PasswordSalt = "4d6f0j0b-7c2b-4e7b-b5a5-3c22c2b7b3ff", hashedpass = "vaCXlhp79vk/6Bnj03zDOvbu3T5BYCJryUPwqCph/L+2P8K2+9rMvtLEwYIC98PTYVoNXDXH8prg2+d6rze3Qg==", Roles = [ "Chief Joy Officer" ] },
new User { FirstName = "Salam", LastName = "Morcos", Email = "salam.morcos+testspsd@gmail.com", Password = "oauth2.0", PasswordSalt = "n/a", hashedpass = "z4NaotajKzsCoL5Rsrjz4qwFZWdgUBWQNXAMxX8x6lC3v27aoxsSuhoFC5OPO6sEP7xkKmkiAsh1E+QJ4+7hkw==", Roles = [ "Bishop" ] }
    ];

    public static readonly List<string> Roles =
    [
        "King",
        "High Priest",
        "Bishop",
        "High Sorcerer",
        "Cosmic Ranger",
        "Ultimate Ruler",
        "Chief Joy Officer"
    ];

    public static readonly List<string> Permissions =
    [
        "CreateProduct",
        "UpdateProduct",
        "RemoveProduct",
        "ViewProductDetails",
        "ManageStock",
        "ProcessOrders",
        "HandlePayments",
        "GenerateReports",
        "AssignDuties",
        "ApproveBudgets",
        "ArrangeMeetings",
        "ManageWorkflows",
        "AccessConfidentialData",
        "GenerateFinancialReports",
        "ModerateContent"
    ];

    public static readonly List<(string Role, string Permission)> RolesPermissionsMatrix =
    [
        ("High Priest", "CreateProduct"),
        ("High Priest", "UpdateProduct"),
        ("High Priest", "RemoveProduct"),
        ("High Priest", "ViewProductDetails"),
        ("High Priest", "ManageStock"),
        ("High Priest", "ProcessOrders"),
        ("High Priest", "HandlePayments"),
        ("High Priest", "GenerateReports"),
        ("High Priest", "AssignDuties"),
        ("King", "ApproveBudgets"),
        ("King", "ArrangeMeetings"),
        ("King", "ManageWorkflows"),
        ("King", "AccessConfidentialData"),
        ("Bishop", "ArrangeMeetings"),
        ("Bishop", "AccessConfidentialData"),
        ("High Sorcerer", "GenerateFinancialReports"),
        ("High Sorcerer", "ModerateContent"),
        ("High Sorcerer", "GenerateReports"),
        ("Cosmic Ranger", "ViewProductDetails"),
        ("Cosmic Ranger", "ManageStock"),
        ("Cosmic Ranger", "ProcessOrders"),
        ("Ultimate Ruler", "ApproveBudgets"),
        ("Ultimate Ruler", "ManageWorkflows"),
        ("Chief Joy Officer", "GenerateReports"),
        ("Chief Joy Officer", "AssignDuties"),
        ("Chief Joy Officer", "GenerateFinancialReports")
    ];

    public static User? GetUserByEmail(string email)
    {
        return Users.FirstOrDefault(user => user.Email!.Equals(email, StringComparison.OrdinalIgnoreCase));
    }
}
