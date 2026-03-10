.PHONY: up down logs db-shell redis-shell ps build clean

up:
	docker compose up -d

down:
	docker compose down

logs:
	docker compose logs -f

ps:
	docker compose ps

build:
	docker compose build

db-shell:
	docker compose exec postgres psql -U $${POSTGRES_USER:-vnstock_user} -d $${POSTGRES_DB:-vnstock}

redis-shell:
	docker compose exec redis redis-cli

clean:
	docker compose down -v --remove-orphans

api-logs:
	docker compose logs -f api

vnstock-logs:
	docker compose logs -f vnstock-service
