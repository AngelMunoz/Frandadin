-- ---------- MIGRONDI:UP:1593897487211 --------------
-- Write your Up migrations here

CREATE TABLE IF NOT EXISTS users (
  id SERIAL PRIMARY KEY,
  name VARCHAR(100) NOT NULL CHECK (name <> ''),
  lastname VARCHAR(100) NOT NULL CHECK (lastName<> ''),
  email VARCHAR(100) NOT NULL CHECK (email <> ''),
  password VARCHAR(100) NOT NULL CHECK (password <> ''),
  UNIQUE(email)
);

-- ---------- MIGRONDI:DOWN:1593897487211 --------------
-- Write how to revert the migration here
DROP TABLE IF EXISTS users;
