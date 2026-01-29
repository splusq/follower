.PHONY: build run test clean restore

build:
	dotnet build

run:
	dotnet run --project Follower

test:
	dotnet test

clean:
	dotnet clean
	rm -rf Follower/bin Follower/obj
	rm -rf Follower.Tests/bin Follower.Tests/obj

restore:
	dotnet restore
