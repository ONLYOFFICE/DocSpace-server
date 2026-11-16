ARG POSTGRES_VERSION
FROM postgres:${POSTGRES_VERSION}

ARG POSTGRES_VERSION
RUN apt-get update \
    && apt-get install -y --no-install-recommends postgresql-${POSTGRES_VERSION}-cron \
    && rm -rf /var/lib/apt/lists/*
