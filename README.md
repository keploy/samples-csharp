# Sample CRUD application

## Installing Keploy
```bash
curl -O https://raw.githubusercontent.com/keploy/keploy/main/keploy.sh && source keploy.sh
```

### Running in record mode
```bash
keploy record -c "dotnet run"
```

Now, since we have our application up and running, let's perform few cURL requests :-

1. POST Request :-
```sh
curl -kv -X POST -H "Content-Type: application/json" -d '{"name":"John Doe","age":30}' http://localhost:5249/api/user
```

2. GET Request :-
```sh
curl -k http://localhost:5249/api/user
```

3. DELETE Request :-
```sh
curl -k -X DELETE http://localhost:5249/api/user/1
```

and voila, we have our testcases generated.

### Running in test-mode

```sh
keploy test -c "dotnet run"
```

We can see that we have got our test-report generated and present in `testReports` under Keploy folder.