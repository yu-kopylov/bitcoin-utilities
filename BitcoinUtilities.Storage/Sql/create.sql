CREATE TABLE Blocks
(
    Id      INTEGER PRIMARY KEY AUTOINCREMENT,
    Hash    BINARY NOT NULL,
    Height  INTEGER NOT NULL,
    Header  BINARY NOT NULL
);

CREATE UNIQUE INDEX UX_Blocks_Hash ON Blocks(Hash);
CREATE UNIQUE INDEX UX_Blocks_Height ON Blocks(Height);

CREATE TABLE Transactions
(
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    Hash            BINARY NOT NULL,
    BlockId         INTEGER NOT NULL,
    NumberInBlock   INTEGER NOT NULL,
    FOREIGN KEY (BlockId) REFERENCES Blocks(Id)
);

CREATE UNIQUE INDEX UX_Transactions_Hash ON Transactions(Hash);

CREATE TABLE TransactionInputs
(
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    TransactionId   INTEGER NOT NULL,
    SignatureScript BINARY NOT NULL,
    Sequence        INTEGER NOT NULL,
    OutPointId      INTEGER NOT NULL,
    FOREIGN KEY (TransactionId) REFERENCES Transactions,
    FOREIGN KEY (OutPointId) REFERENCES TransactionOutput
);

CREATE TABLE TransactionOutputs
(
    Id                  INTEGER PRIMARY KEY AUTOINCREMENT,
    TransactionId       INTEGER NOT NULL,
    Value               INTEGER NOT NULL,
    PubkeyScriptType    INTEGER NOT NULL,
    PubkeyScript        BINARY NOT NULL,
    PublicKey           BINARY NOT NULL,
    FOREIGN KEY (TransactionId) REFERENCES Transactions
);

CREATE INDEX IX_TransactionOutputs_PublicKey ON TransactionOutputs(PublicKey);
