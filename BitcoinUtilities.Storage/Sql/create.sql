---- Transaction Hashes Schema -------------------------------------------

CREATE TABLE txhash.TransactionHashes
(
    Id              INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
	Hash			BINARY NOT NULL,
	SemiHash		INTEGER NOT NULL
);

CREATE INDEX txhash.IX_TransactionHashes_SemiHash ON TransactionHashes(SemiHash);

---- Binary Data Schema --------------------------------------------------

CREATE TABLE bin.BinaryData
(
    Id              INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
	Data			BINARY NOT NULL
);

---- Address Schema ------------------------------------------------------

CREATE TABLE addr.Addresses
(
    Id              INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
	Value			TEXT NOT NULL,
	SemiHash		INTEGER NOT NULL
);

CREATE INDEX addr.IX_Addresses_SemiHash ON Addresses(SemiHash);

---- Main Schema ---------------------------------------------------------

CREATE TABLE Blocks
(
    Id			INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    Height		INTEGER NOT NULL,
    Hash		BINARY NOT NULL,
    Header		BINARY NOT NULL
);

CREATE UNIQUE INDEX UX_Blocks_Hash ON Blocks(Hash);
CREATE INDEX IX_Blocks_Height ON Blocks(Height);

CREATE TABLE Transactions
(
    Id              INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    BlockId         INTEGER NOT NULL,
    NumberInBlock   INTEGER NOT NULL,
    HashId          INTEGER NOT NULL,
    FOREIGN KEY (BlockId) REFERENCES Blocks(Id)
);

CREATE INDEX IX_Transactions_BlockId ON Transactions(BlockId);
CREATE INDEX IX_Transactions_HashId ON Transactions(HashId);

CREATE TABLE TransactionInputs
(
    Id					INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    TransactionId		INTEGER NOT NULL,
	NumberInTransaction	INTEGER NOT NULL,
    SignatureScriptId	INTEGER NOT NULL,
    Sequence			INTEGER NOT NULL,
	OutputHashId		INTEGER NOT NULL,
	OutputIndex			INTEGER NOT NULL,
    FOREIGN KEY (TransactionId) REFERENCES Transactions
);

CREATE INDEX IX_TransactionInputs_TransactionId ON TransactionInputs(TransactionId);
CREATE INDEX IX_TransactionInputs_OutputHashId ON TransactionInputs(OutputHashId);

CREATE TABLE TransactionOutputs
(
    Id                  INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    TransactionId       INTEGER NOT NULL,
	NumberInTransaction	INTEGER NOT NULL,
    Value               INTEGER NOT NULL,
    PubkeyScriptId      INTEGER NOT NULL,
    AddressId           INTEGER NULL,
    FOREIGN KEY (TransactionId) REFERENCES Transactions
);

CREATE INDEX IX_TransactionOutputs_TransactionId ON TransactionOutputs(TransactionId);
CREATE INDEX IX_TransactionOutputs_AddressId ON TransactionOutputs(AddressId);
