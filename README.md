# MyLittleUrlAPI
MyLittleURLAPI

## Build

docker-compose build

## Run Container

docker run -d -p **32980:80** --name mylittleurlapi

## Browse

Use the port number specified above

_[GET]_: http://localhost:32980/api/littleurl

_[GET]_: http://localhost:32980/api/littleurl/abc

_[POST]_: http://localhost:32980/api/littleurl
{with application JSON}
