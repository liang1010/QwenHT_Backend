# QwenHT Backend

This is the backend API for the QwenHT Identity Management System built with ASP.NET Core and ASP.NET Core Identity.

## Features

- ASP.NET Core Identity for authentication and authorization
- User management with roles (Admin, Supervisor, User, Guest)
- JWT token generation and validation
- API endpoints for user and role management
- Support for Google OAuth integration

## Prerequisites

- .NET 8 SDK
- SQL Server or SQL Server Express LocalDB
- Node.js and npm (for running the Angular app separately)

## Setup

1. Navigate to the project directory
2. Run the following command to restore packages:
   ```
   dotnet restore
   ```
3. Update the connection string in `appsettings.json` if needed
4. Run migrations to create the database:
   ```
   dotnet ef database update
   ```
5. Run the application:
   ```
   dotnet run
   ```

## API Endpoints

- `POST /api/account/login` - User login
- `POST /api/account/register` - User registration
- `GET /api/users` - Get all users (Admin only)
- `GET /api/users/{id}` - Get specific user (Admin only)
- `PUT /api/users/{id}` - Update user (Admin only)
- `DELETE /api/users/{id}` - Delete user (Admin only)
- `GET /api/roles` - Get all roles (Admin only)
- `POST /api/roles` - Create role (Admin only)
- `DELETE /api/roles/{id}` - Delete role (Admin only)

## Default Users

On first run, the application creates:
- Admin user: `admin@qwenht.com` with password `P@ssw0rd!2025`
- Supervisor user: `supervisor@qwenht.com` with password `P@ssw0rd!2025`

## Configuration

- JWT secret, issuer, and audience are configured in `appsettings.json`
- Google OAuth credentials can be added to `appsettings.json` under `Authentication:Google`

## Development

The backend serves as a standalone API server that can be used with any frontend client. The Angular frontend is in a separate project.