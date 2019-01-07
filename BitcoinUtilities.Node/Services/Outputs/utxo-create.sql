CREATE TABLE Headers
(
	Id				INTEGER PRIMARY KEY NOT NULL,
	Hash			BLOB NOT NULL,
	Height			INTEGER NOT NULL,
	IsReversible	INTEGER NOT NULL
);

CREATE UNIQUE INDEX UX_Headers_Hash ON Headers(Hash);
CREATE UNIQUE INDEX UX_Headers_Height ON Headers(Height);

CREATE TABLE UnspentOutputs
(
	OutputPoint		BLOB PRIMARY KEY NOT NULL,
	Height			INTEGER NOT NULL,
	Value			INTEGER NOT NULL,
	Script			BLOB NOT NULL
) WITHOUT ROWID;

CREATE INDEX IX_UnspentOutputs_Height ON UnspentOutputs(Height);

CREATE TABLE SpentOutputs
(
	Id				INTEGER PRIMARY KEY NOT NULL,
	OutputPoint		BLOB NOT NULL,
	Height			INTEGER NOT NULL,
	Value			INTEGER NOT NULL,
	Script			BLOB NOT NULL,
	SpentHeight		INTEGER NOT NULL
);

CREATE INDEX IX_SpentOutputs_SpentHeight ON SpentOutputs(SpentHeight);
