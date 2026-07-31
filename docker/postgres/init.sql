-- POSTGRES_DB ya crea ocap_db; aquí solo extensiones.
\connect ocap_db
CREATE EXTENSION IF NOT EXISTS vector;
