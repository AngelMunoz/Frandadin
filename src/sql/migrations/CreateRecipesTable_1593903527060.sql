-- ---------- MIGRONDI:UP:1593903527060 --------------
-- Write your Up migrations here
CREATE TABLE IF NOT EXISTS recipes (
  id SERIAL PRIMARY KEY,
  title VARCHAR(100) NOT NULL CHECK (title <> ''),
  imageUrl VARCHAR(255) DEFAULT '',
  description TEXT DEFAULT '',
  notes VARCHAR(240) DEFAULT '',
  userId INT REFERENCES users (id)
);
CREATE TABLE IF NOT EXISTS ingredients (
  id SERIAL PRIMARY KEY,
  name varchar(100) NOT NULL CHECK(name <> ''),
  quantity varchar(30) NOT NULL CHECK(quantity <> ''),
  recipeId INT REFERENCES recipes (id)
);
CREATE TABLE IF NOT EXISTS recipestep (
  id SERIAL PRIMARY KEY,
  stepOrder INT default 1,
  imageUrl VARCHAR(255) DEFAULT '',
  directions TEXT NOT NULL CHECK(directions <> ''),
  recipeId INT REFERENCES recipes (id)
);
-- ---------- MIGRONDI:DOWN:1593903527060 --------------
-- Write how to revert the migration here
DROP TABLE IF EXISTS ingredients;
DROP TABLE IF EXISTS recipestep;
DROP TABLE IF EXISTS recipes;