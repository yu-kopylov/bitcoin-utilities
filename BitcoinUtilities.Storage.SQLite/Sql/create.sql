---- Main Schema ---------------------------------------------------------

CREATE TABLE Blocks
(
    Id					INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    Header				BLOB NOT NULL,
    Hash				BLOB NOT NULL,
    Height				INTEGER NOT NULL,
	TotalWork			REAL NOT NULL,
	HasContent			INTEGER NOT NULL,
	IsInBestHeaderChain	INTEGER NOT NULL,
	IsInBestBlockChain	INTEGER NOT NULL
);

CREATE UNIQUE INDEX UX_Blocks_Hash ON Blocks(Hash);
CREATE INDEX IX_Blocks_Height ON Blocks(Height);

---- Blocks Schema -------------------------------------------------------

CREATE TABLE blocks.BlockContents
(
    Id			INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    Hash		BLOB NOT NULL,
    Content		BLOB NOT NULL
);

CREATE UNIQUE INDEX blocks.UX_BlockContent_BlockId ON BlockContents(Hash);
