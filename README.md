# MyLittleUrlAPI
URL Shortener experiment - running in a docker container.

This expermiental REST API uses:
* .NET Core 2.0
* MongoDb in a container 

## Build
```
$ docker-compose build
```

## Run Mongo (with networking)
Create a custom network with 'bridge' driver

```
$ docker network create --driver bridge mylittleurl-net
```

Use the name *`mymongodb`* for your mongo container
  * Optionally expose port 27017 to the host for access to mongo

```
$ docker run -d -p=0.0.0.0:32779:27017 --name=mymongodb --network=mylittleurl-net mongo:latest
```

## Run API Container (with networking)
Ensure the network name is the same.

```
$ docker run -d -p=0.0.0.0:32780:80 --name=mylittleurlapi --network=mylittleurl-net mylittleurlapi:latest
```

## Browse/Postman
Use the port number specified above

_[GET]_: `http://localhost:32780/api/littleurl`

_[GET]_: `http://localhost:32780/api/littleurl/abc`

_[POST]_: `http://localhost:32780/api/littleurl`
{with application JSON}
