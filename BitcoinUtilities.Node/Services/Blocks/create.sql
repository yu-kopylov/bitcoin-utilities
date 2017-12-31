﻿CREATE TABLE Blocks
(
    Id					INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    Hash				BLOB NOT NULL,
    Size				INTEGER NOT NULL,
    Received			TEXT NOT NULL,
    Requested			BIT NOT NULL,
    Content				BLOB NOT NULL
);

CREATE UNIQUE INDEX UX_Blocks_Hash ON Blocks(Hash);
CREATE INDEX UX_Blocks_Received ON Blocks(Requested, Received);

CREATE TABLE BlockRequests
(
    Id					INTEGER PRIMARY KEY NOT NULL,
    Token				TEXT NOT NULL,
	Hash				BLOB NOT NULL
);

CREATE UNIQUE INDEX UX_BlockRequests_HashAndToken ON BlockRequests(Hash, Token);
