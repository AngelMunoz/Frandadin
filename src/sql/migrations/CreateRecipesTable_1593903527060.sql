-- ---------- MIGRONDI:UP:1593903527060 --------------
-- Write your Up migrations here
CREATE TABLE IF NOT EXISTS recipes (
  id SERIAL PRIMARY KEY,
  title VARCHAR(100) NOT NULL CHECK (title <> ''),
  imageurl VARCHAR(255) DEFAULT '',
  description TEXT DEFAULT '',
  notes VARCHAR(240) DEFAULT '',
  userid INT REFERENCES users (id)
);
CREATE TABLE IF NOT EXISTS ingredients (
  id SERIAL PRIMARY KEY,
  name varchar(100) NOT NULL CHECK(name <> ''),
  quantity varchar(30) NOT NULL CHECK(quantity <> ''),
  recipeid INT REFERENCES recipes (id)
);
CREATE TABLE IF NOT EXISTS recipesteps (
  id SERIAL PRIMARY KEY,
  steporder INT default 1,
  imageurl VARCHAR(255) DEFAULT '',
  directions TEXT NOT NULL CHECK(directions <> ''),
  recipeid INT REFERENCES recipes (id)
);
-- ---------- MIGRONDI:DOWN:1593903527060 --------------
-- Write how to revert the migration here
DROP TABLE IF EXISTS ingredients;
DROP TABLE IF EXISTS recipestep;
DROP TABLE IF EXISTS recipes;