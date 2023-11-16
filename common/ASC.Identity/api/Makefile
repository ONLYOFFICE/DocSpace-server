.DEFAULT_GOAL := help

.PHONY: help
help:
	@grep -E "^[a-z-]+: #" $(MAKEFILE_LIST) | \
		awk 'BEGIN {FS = ": # "}; {printf "%s: %s\n", $$1, $$2}'

.PHONY: dev
dev: #          Start Spring Boot server
	./mvnw spring-boot:run

.PHONY: lint
lint: #         Lint the source code for style and check for types.
	@flake8
	@mypy .

.PHONY: load-dev
load-dev: #     Development load testing
	@artillery run -e development ./artillery.yml