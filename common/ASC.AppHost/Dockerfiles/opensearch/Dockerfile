ARG OPENSEARCH_VERSION
FROM opensearchproject/opensearch:${OPENSEARCH_VERSION}

RUN /usr/share/opensearch/bin/opensearch-plugin install --batch ingest-attachment
