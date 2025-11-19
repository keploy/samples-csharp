# Sample CRUD application

This is a sample C# application that demonstrates the usage of Keploy with .NET and MongoDB.

## Get Started with Keploy

1. **Install Keploy**
```bash
curl -L https://github.com/keploy/keploy/releases/latest/download/keploy_linux_amd64.tar.gz -o keploy.tar.gz
tar -xzf keploy.tar.gz
sudo mv keploy /usr/local/bin/
sudo chmod +x /usr/local/bin/keploy
rm keploy.tar.gz

# Verify installation
keploy version
```

2. **Setup application**
```bash
git clone https://github.com/keploy/samples-csharp.git
cd samples-csharp/crud-app-mongodb
```

## Prerequisites

### Install .NET 8 SDK and Runtime

```bash
# Download and install Microsoft repository configuration
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Update package lists
sudo apt-get update

# Install .NET 8 Runtime and SDK
sudo apt-get install -y dotnet-runtime-8.0 aspnetcore-runtime-8.0 dotnet-sdk-8.0

# Verify installation
dotnet --version
```

### Install and Start MongoDB

```bash
# Import MongoDB public GPG key
wget -qO - https://www.mongodb.org/static/pgp/server-7.0.asc | sudo apt-key add -

# Create list file for MongoDB
echo "deb [ arch=amd64,arm64 ] https://repo.mongodb.org/apt/ubuntu focal/mongodb-org/7.0 multiverse" | sudo tee /etc/apt/sources.list.d/mongodb-org-7.0.list

# Update package database
sudo apt-get update

# Install MongoDB
sudo apt-get install -y mongodb-org

# Start MongoDB
sudo systemctl start mongod

# Enable MongoDB to start on boot
sudo systemctl enable mongod

# Verify MongoDB is running
sudo systemctl status mongod
```

### Configuration

Update `appsettings.json` with your MongoDB connection string (default configuration works with local MongoDB):

```json
{
  "MongoDBSettings": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "UserDb",
    "UsersCollectionName": "Users"
  }
}
```

## Running in Record Mode

```bash
sudo -E env "PATH=$PATH" keploy record   -c "dotnet run --urls=http://0.0.0.0:5001"   --proxy-port 8080   --path ./keploy-tests
```

**Note:** Keploy requires elevated permissions (sudo) to use eBPF for intercepting network calls.

Now, since we have our application up and running, let's perform a few cURL requests (in a new terminal):

### 1. POST Requests

```bash
curl -X POST -H "Content-Type: application/json" -d '{"name":"Sarthak Shnygle","age":23}' http://localhost:5067/api/users

curl -X POST -H "Content-Type: application/json" -d '{"name":"Gourav Kumar","age":22}' http://localhost:5067/api/users
```

### 2. GET Request

```bash
curl http://localhost:5067/api/users
```

### 3. GET Request by ID

```bash
# Replace {id} with actual ID from POST response
curl http://localhost:5067/api/users/{id}
```

### 4. DELETE Request

```bash
# Replace {id} with actual ID from POST response
curl -X DELETE http://localhost:5067/api/users/{id}
```

And voila, we have our test cases generated in the `./keploy-tests` directory!

### Stop Recording

Press `Ctrl+C` in the terminal where Keploy is running to stop recording.

## Run the Test Cases

Now let's run Keploy in test mode:

```bash
sudo -E env "PATH=$PATH" keploy test \
  -c "dotnet run --urls=http://0.0.0.0:5001" \
  --path ./keploy-tests \
  --delay 10
```

The `--delay 10` flag gives the .NET application 10 seconds to start before running tests.

Keploy will replay all recorded test cases and generate a test report in the `keploy-tests/reports` directory.



## Troubleshooting

### Port Already in Use

If you get an "address already in use" error:

```bash
# Kill existing dotnet processes
pkill -9 dotnet
```

### MongoDB Not Running

Ensure MongoDB is running:

```bash
sudo systemctl status mongod

# If not running, start it
sudo systemctl start mongod
```

### Permission Denied

Always use `sudo -E env "PATH=$PATH"` with Keploy commands to preserve environment variables and provide necessary permissions for eBPF.

## API Endpoints

- `GET /api/users` - Get all users
- `POST /api/users` - Create a new user
- `GET /api/users/{id}` - Get user by ID
- `PUT /api/users/{id}` - Update user by ID
- `DELETE /api/users/{id}` - Delete user by ID

## Resources

- [Keploy Documentation](https://keploy.io/docs)
- [.NET 8 Documentation](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8)
- [MongoDB .NET Driver](https://www.mongodb.com/docs/drivers/csharp/)

