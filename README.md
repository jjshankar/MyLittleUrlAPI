# MyLittleUrlAPI
URL Shortener experiment - running in a docker container.

This expermiental REST API uses:
* .NET Core 2.0
* In-memory database 

## Build
 `$ docker-compose build`

## Run Container
 `$ docker run -d -p 32980:80 --name mylittleurlapi`

## Browse
Use the port number specified above

_[GET]_: http://localhost:32980/api/littleurl

_[GET]_: http://localhost:32980/api/littleurl/abc

_[POST]_: http://localhost:32980/api/littleurl
{with application JSON}
