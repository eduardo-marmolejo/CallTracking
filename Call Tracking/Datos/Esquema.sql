-- CallTracking: esquema v1

CREATE TABLE Usuarios (
    idUsuarios INTEGER PRIMARY KEY AUTOINCREMENT,
    Nombre     TEXT NOT NULL COLLATE NOCASE CHECK (length(trim(Nombre)) > 0),
    CreateAt   TEXT NOT NULL DEFAULT (datetime('now','localtime'))
) STRICT;

CREATE TABLE Projects (
    idProject INTEGER PRIMARY KEY AUTOINCREMENT,
    Nombre    TEXT NOT NULL COLLATE NOCASE CHECK (length(trim(Nombre)) > 0),
    Compania  TEXT NOT NULL DEFAULT '',
    Direccion TEXT NOT NULL DEFAULT '',
    CreateAt  TEXT NOT NULL DEFAULT (datetime('now','localtime'))
) STRICT;

CREATE UNIQUE INDEX UX_Projects_Nombre ON Projects(Nombre);

CREATE TABLE RegistroLlamadas (
    idRegistro     INTEGER PRIMARY KEY AUTOINCREMENT,
    idProject      INTEGER NOT NULL REFERENCES Projects(idProject) ON DELETE RESTRICT,
    Fecha          TEXT NOT NULL
                   CHECK (Fecha GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]' AND date(Fecha) = Fecha),
    horaInicio     INTEGER NOT NULL CHECK (horaInicio BETWEEN 0 AND 1439),
    Duracion       INTEGER NOT NULL DEFAULT 0 CHECK (Duracion BETWEEN 0 AND 1439),
    Nombre         TEXT NOT NULL COLLATE NOCASE CHECK (length(trim(Nombre)) > 0),
    NumeroContacto TEXT NOT NULL DEFAULT '',
    Remarks        TEXT NOT NULL DEFAULT '',
    CreateAt       TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    UpdateAt       TEXT NOT NULL DEFAULT (datetime('now','localtime'))
) STRICT;

CREATE INDEX IX_RegistroLlamadas_Project_Fecha ON RegistroLlamadas(idProject, Fecha DESC);

PRAGMA user_version = 1;
