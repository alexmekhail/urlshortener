# URLShortener

A simple and secure URL shortener API written in C#.

## Overview

This project allows authenticated users to shorten long URLs via a RESTful API. Built with ASP.NET Core, it stores URL mappings in Azure SQL or Azure Database services and includes mock data support and basic authentication for development and testing.

## Features

- 🔒 Basic authentication to ensure only authorized users can access endpoints  
- 🌐 RESTful API for creating and retrieving shortened URLs  
- ☁️ Integration with Azure SQL / Azure Database for storage  
- 🛠️ Sample `.http` files for testing with Postman or REST Client  
- 📦 Mock data included for ease of development  

## Repository Structure

/
├── Controllers/ # API controllers
├── Helper/ # Utility and helper classes
├── Migrations/ # Entity Framework database migrations
├── Models/ # URL mapping models
├── Properties/ # Project configuration files
├── Security/ # Authentication and authorization logic
├── bin/ # Compiled binaries
├── obj/ # Object files
├── Program.cs # Entry point of the application
├── ShortUrl.cs # Model or service class for URL mapping
├── appsettings.json # Application settings
├── appsettings.Development.json # Dev-specific configuration
├── urlshortener.csproj # Project definition file
├── urlshortener.sln # Visual Studio solution file
└── urlshortener.http # HTTP client scripts for testing

markdown
Copy code

## Getting Started

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download)  
- Azure SQL Database or Azure Database for setup  
- (Optional) HTTP client like Postman or VS Code REST Client  

### Setup Instructions

1. **Clone the repository**:
   ```bash
   git clone https://github.com/alexmekhail/urlshortener.git
Navigate to the project directory:

bash
Copy code
cd urlshortener
Configure database connection
Update appsettings.json with your Azure database connection string.

Apply EF migrations (if using Entity Framework):

bash
Copy code
dotnet ef database update
Run the application:

bash
Copy code
dotnet run
Test the API
Use the provided urlshortener.http file, Postman, or VS Code REST Client to send requests.

Usage Examples
Create a short URL
Send a POST request with a long URL (requires basic authentication).

Retrieve a shortened URL
Send a GET request with the generated short code to be redirected to the original URL.

Example request (with Postman or REST Client):

http
Copy code
POST https://localhost:5001/api/url
Authorization: Basic <base64encoded-credentials>
Content-Type: application/json

{
  "longUrl": "https://example.com/some/very/long/link"
}
Example response:

json
Copy code
{
  "shortUrl": "https://short.ly/abc123",
  "longUrl": "https://example.com/some/very/long/link"
}
Contributing
Contributions are welcome!

Fork the repository

Create a feature branch (git checkout -b feature/my-feature)

Commit your changes (git commit -m 'Add new feature')

Push to the branch (git push origin feature/my-feature)

Open a Pull Request
