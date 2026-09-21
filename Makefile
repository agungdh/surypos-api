PROJECT := src/SuryPos.Api/SuryPos.Api.csproj
LAUNCH_PROFILE := http

.DEFAULT_GOAL := help

.PHONY: help start watch dev run build restore clean

help: ## Tampilkan daftar perintah yang tersedia (default)
	@echo "Usage: make [target]"
	@echo ""
	@echo "Targets:"
	@grep -E '^[a-zA-Z0-9_-]+:.*?## ' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "  %-10s %s\n", $$1, $$2}'

start: ## Jalankan API dengan dotnet watch (hot reload)
	dotnet watch run --project $(PROJECT) --launch-profile $(LAUNCH_PROFILE)

watch: start ## Alias dari start

dev: start ## Alias dari start

run: ## Jalankan API tanpa watch
	dotnet run --project $(PROJECT) --launch-profile $(LAUNCH_PROFILE)

build: ## Build solution
	dotnet build

restore: ## Restore dependencies
	dotnet restore

clean: ## Bersihkan hasil build
	dotnet clean
