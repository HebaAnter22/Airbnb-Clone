# Airbnb-Clone

## Overview
This project is a full-stack clone of Airbnb, designed to replicate core functionalities such as property listings, bookings, and user authentication. It leverages modern web development technologies to provide a scalable and responsive platform for users to browse, book, and manage properties.


## Features
![Screenshot 2025-04-04 181708](https://github.com/user-attachments/assets/5203afd8-a013-499d-a405-6b437d1bbc41)

- User Authentication: Register, log in, and manage user profiles securely.
- Property Listings: Browse, search, and filter available properties with details like location, price, and amenities.
- Booking System: Reserve properties for specific dates with real-time availability checks.
- Payment: Users can pay for bookings using Stripe payment gateway for testing.
- Reviews: Guests can add reviews and feedback for every booking.
- Responsive Design: Seamless experience across desktop and mobile devices.
- Admin Dashboard: Manage properties and bookings (for authorized users).

  
## Technologies
- Backend: ASP.NET Core Web API (C#)
- Database: SQL Server
- Frontend: Angular (TypeScript)
- ORM: Entity Framework Core (Code First approach)
- Authentication: JWT (JSON Web Tokens), OAuth
- Styling: CSS/SCSS with Angular Material or Bootstrap
- Tools: Visual Studio, SQL Server Management Studio (SSMS), Node.js, Angular CLI

  
## High-Level Architecture
![Screenshot 2025-04-04 181733](https://github.com/user-attachments/assets/3c624ef4-78cf-445f-99ad-3044482b545c)


## Database Design
![Airbnb](https://github.com/user-attachments/assets/8c3b34a3-5a1f-4039-af47-f6c83542d41c)


## File Structure
```
AirbnbClone/
|
├── AirbnbClone.API/
|    ├──Proprties
|    ├── Controllers/
|    ├── Models/
|    ├── Services/
|    ├── Data/
|    ├── DTOs/
|    ├── Middleware/
|    ├── API.http
|    └── appsettings.json
|
├── AirbnbClone.Client/
|   ├── src/
|   │   ├── app/
|   │   │   ├── core/ (services, interceptors, guards)
|   │   │   ├── modules/
|   │   │   │   ├── properties/
|   │   │   │   ├── bookings/
|   │   │   │   ├── auth/
|   │   │   │   └── shared/
|   │   │   ├── models/
|   │   │   └── app.component.*
|   │   ├── assets/
|   │   └── environments/
|
```


# Installation

### Backend Setup
1. Clone the repository:
   ```bash
   git clone https://github.com/HebaAnter22/Airbnb-Clone.git
   cd AirbnbClone/AirbnbClone.API
2- Update appsettings.json with your SQL Server connection string
```Json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Jwt": {
    "Key": "your-secret-key",
    "Issuer": "your-issuer",
    "Audience": "your-audience"
  }
}
```
3- Restore dependencies and apply migrations
```bash
  dotnet restore
  dotnet ef database update
```
4- dotnet run
```bash
  dotnet run
```

# Frontedn Setup
1- Navigate to the client folder
```bash
  cd AirbnbClone/AirbnbClone.Client
```
2- Install dependencies
```bash
  npm install
```
3- Run the Angular app
```bash
ng serve
```


# Usage
* Start the backend API (dotnet run in AirbnbClone.API).
* Start the frontend (ng serve in AirbnbClone.Client).
* Open your browser to http://localhost:4200 and explore the app.

  
# Conclusion
This Airbnb clone demonstrates a robust full-stack application using ASP.NET Core, SQL Server, and Angular. It’s designed to be extensible, allowing for additional features like payment integration (e.g., Stripe), reviews, or advanced search filters. Contributions and feedback are welcome!
