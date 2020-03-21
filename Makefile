SHELL := /bin/bash

DC := docker-compose -f docker-compose.yml -f docker-compose.custom.yml
DC_RUN := ${DC} run --rm

include config.env
export

.PHONY: confirmation
confirmation:
	@echo -n 'Are you sure? [y|N] ' && read ans && [ $$ans == y ]

.PHONY: cmd
cmd:
	@echo 'Docker-Compose command:'
	@echo '${DC}'

.PHONY: mondodump mongoimport
mongodump:
	@echo 'Dumping MongoDB to ./mongo.zip'
	${DC} up -d mongo
	docker exec $(shell ${DC} ps -q mongo) mongodump --uri 'mongodb://${MONGO_INITDB_ROOT_USERNAME}:${MONGO_INITDB_ROOT_PASSWORD}@mongo' --archive --gzip > mongo.zip

mongoimport: confirmation
	test -f mongo.zip
	@echo 'Replacing database with contents from file ./mongo.zip...'
	${DC} up -d mongo
	cat mongo.zip | docker exec --interactive $(shell ${DC} ps -q mongo) mongorestore --uri 'mongodb://${MONGO_INITDB_ROOT_USERNAME}:${MONGO_INITDB_ROOT_PASSWORD}@mongo' --archive --gzip --drop --preserveUUID
	@echo 'Import completed.'

.PHONY: up
up:
	${DC} up -d api
	${DC} ps
	@echo
	@echo 'Service is now up'

.PHONY: ps
ps:
	${DC} ps

.PHONY: rs
rs:
	${DC} restart

.PHONY: rebuild
rebuild:
	${DC} rm -sf api
	${DC} build api
	${DC} up -d api

.PHONY: stop
stop:
	${DC} stop

.PHONY: rm
rm:
	${DC} rm -fs

.PHONY: logs
logs:
	docker logs -f $(shell ${DC} ps -q api)
