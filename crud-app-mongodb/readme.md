# **.NET 8 and MongoDB CRUD Sample API**

This repository contains a sample CRUD (Create, Read, Update, Delete) web API built with .NET 8 Minimal APIs and MongoDB. It demonstrates how to connect a .NET application to a MongoDB database and perform basic data operations.

## **Tech Stack**

* [**.NET 8**](https://dotnet.microsoft.com/en-us/download/dotnet/8.0): The core framework for building the application.  
* [**ASP.NET Core Minimal APIs**](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis): For creating concise, modern HTTP APIs.  
* [**MongoDB**](https://www.mongodb.com/): A NoSQL document database.  
* [**MongoDB .NET Driver**](https://www.mongodb.com/docs/drivers/csharp/): The official driver for interacting with MongoDB from a .NET application.

## **Prerequisites**

1. [**.NET 8 SDK**](https://dotnet.microsoft.com/en-us/download/dotnet/8.0): You must have the .NET 8 SDK installed on your machine.  
2. [**MongoDB Instance**](https://www.mongodb.com/try/download/community): You need a running MongoDB instance. The easiest way to get one for local development is by using Docker.

### **Running MongoDB with Docker**

If you have Docker installed, you can start a local MongoDB instance with the following command:

docker run \-d \--name mongodb \-p 27017:27017 mongo

This will start a MongoDB server that is accessible at mongodb://localhost:27017.

## **Configuration**

The application is configured via the appsettings.json file. The default settings assume a local MongoDB instance running on port 27017\.

{  
  "MongoDBSettings": {  
    "ConnectionURI": "mongodb://localhost:27017",  
    "DatabaseName": "PlaylistDB",  
    "CollectionName": "Playlists"  
  }  
}

If your MongoDB instance is running elsewhere, update the ConnectionURI accordingly.

## **How to Run the Application**

1. Clone this repository.  
2. Navigate to the project directory in your terminal.  
3. Run the application using the .NET CLI:  
   dotnet run

4. The API will be running on http://localhost:7XXX and http://localhost:5XXX. The exact ports will be shown in the terminal output.

## **Using the API**

Once the application is running, you can access the Swagger UI for interactive documentation and testing by navigating to http://localhost:5XXX/swagger in your browser.

### **API Endpoints**

You can also use tools like curl or Postman to interact with the API.

POST /api/playlists  
Create a new playlist.  
*Request Body:*

{  
  "username": "sample\_user",  
  "items": \["Song A", "Song B"\]  
}

*Curl Example:*

curl \-X POST \-H "Content-Type: application/json" \-d '{"username":"sample\_user","items":\["Song A", "Song B"\]}' http://localhost:5000/api/playlists \-k

GET /api/playlists  
Get all playlists.  
*Curl Example:*

curl http://localhost:5000/api/playlists \-k

GET /api/playlists/{id}  
Get a single playlist by its ID.  
*Curl Example (replace {id} with an actual ID):*

curl http://localhost:5000/api/playlists/66770f3f1e9c5c7d8a6b1c2d \-k

PUT /api/playlists/{id}  
Update an existing playlist.  
*Request Body:*

{  
  "username": "updated\_user",  
  "items": \["Song A", "Song B", "Song C"\]  
}

*Curl Example:*

curl \-X PUT \-H "Content-Type: application/json" \-d '{"username":"updated\_user","items":\["Song A", "Song B", "Song C"\]}' http://localhost:5000/api/playlists/66770f3f1e9c5c7d8a6b1c2d \-k

DELETE /api/playlists/{id}  
Delete a playlist.  
*Curl Example:*

curl \-X DELETE http://localhost:5000/api/playlists/66770f3f1e9c5c7d8a6b1c2d \-k

*(Note: The \-k or \--insecure flag in curl is used to bypass SSL certificate validation for local development.)*