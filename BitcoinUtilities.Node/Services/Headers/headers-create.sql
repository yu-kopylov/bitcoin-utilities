﻿CREATE TABLE Headers
(
	Id					INTEGER PRIMARY KEY NOT NULL,
	Header				BLOB NOT NULL,
	Hash				BLOB NOT NULL,
	Height				INTEGER NOT NULL,
	TotalWork			REAL NOT NULL
);

CREATE UNIQUE INDEX UX_Headers_Hash ON Headers(Hash);
CREATE UNIQUE INDEX UX_Headers_Height ON Headers(Height);
