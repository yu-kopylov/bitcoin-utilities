CREATE TABLE Headers
(
	Height			INTEGER NOT NULL,
	Hash			BLOB NOT NULL,
	IsReversible	INTEGER NOT NULL,

	PRIMARY KEY (Height)
) WITHOUT ROWID;

CREATE UNIQUE INDEX UX_Headers_Hash ON Headers(Hash);
CREATE INDEX IX_Headers_IsReversible ON Headers(Height) where IsReversible!=0;

CREATE TABLE UnspentOutputs
(
	TxHash			BLOB NOT NULL,
	OutputIndex		INTEGER NOT NULL,
	Value			INTEGER NOT NULL,
	PubkeyScript	BLOB NOT NULL,

	PRIMARY KEY (TxHash, OutputIndex)
) WITHOUT ROWID;

CREATE TABLE OutputOperations
(
	Height			INTEGER NOT NULL,
	OperationNumber	INTEGER NOT NULL,
	Spent			INTEGER NOT NULL,
	TxHash			BLOB NOT NULL,
	OutputIndex		INTEGER NOT NULL,
	Value			INTEGER NOT NULL,
	PubkeyScript	BLOB NOT NULL,

	PRIMARY KEY (Height, OperationNumber)
) WITHOUT ROWID;
