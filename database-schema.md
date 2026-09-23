# Digital Wallet & Expense Management - Database Schema

PostgreSQL

## users
| Column | Type | Key |
|---|---|---|
| id | uuid | PK |
| name | varchar(200) | |
| email | varchar(320) | UNIQUE |
| account_no | varchar(10) | UNIQUE |
| password_hash | text | |
| role | varchar(20) | |
| created_at | timestamptz | |
| updated_at | timestamptz | |

## currencies
| Column | Type | Key |
|---|---|---|
| id | uuid | PK |
| code | varchar(3) | UNIQUE |
| name | varchar(100) | |
| symbol | varchar(10) | |
| decimal_places | int | |
| status | varchar(20) | |
| created_at | timestamptz | |
| updated_at | timestamptz | |

## wallets
| Column | Type | Key |
|---|---|---|
| id | uuid | PK |
| user_id | uuid | FK → users.id |
| currency_id | uuid | FK → currencies.id |
| balance | numeric(18,4) | |
| status | varchar(20) | |
| created_at | timestamptz | |
| updated_at | timestamptz | |

## transactions
| Column | Type | Key |
|---|---|---|
| id | uuid | PK |
| user_id | uuid | FK → users.id |
| currency_id | uuid | FK → currencies.id |
| type | varchar(20) | |
| amount | numeric(18,4) | |
| status | varchar(20) | |
| reference | varchar(50) | UNIQUE |
| note | varchar(500) | |
| failure_code | varchar(50) | |
| failure_reason | text | |
| created_at | timestamptz | |
| updated_at | timestamptz | |

## wallet_logs
| Column | Type | Key |
|---|---|---|
| id | uuid | PK |
| wallet_id | uuid | FK → wallets.id |
| transaction_id | uuid | FK → transactions.id |
| direction | varchar(10) | |
| amount | numeric(18,4) | |
| balance_before | numeric(18,4) | |
| balance_after | numeric(18,4) | |
| created_at | timestamptz | |

## bank_transfers
| Column | Type | Key |
|---|---|---|
| transaction_id | uuid | PK, FK → transactions.id |
| bank_code | varchar(20) | |
| bank_reference | varchar(100) | |

## p2p_transfers
| Column | Type | Key |
|---|---|---|
| transaction_id | uuid | PK, FK → transactions.id |
| receiver_wallet_id | uuid | FK → wallets.id |

## expense_categories
| Column | Type | Key |
|---|---|---|
| id | uuid | PK |
| name | varchar(100) | UNIQUE |
| description | varchar(500) | |
| status | varchar(20) | |
| created_at | timestamptz | |
| updated_at | timestamptz | |

## expenses
| Column | Type | Key |
|---|---|---|
| transaction_id | uuid | PK, FK → transactions.id |
| category_id | uuid | FK → expense_categories.id |
| expense_date | date | |

## refresh_tokens
| Column | Type | Key |
|---|---|---|
| id | uuid | PK |
| user_id | uuid | FK → users.id |
| token_hash | varchar(64) | UNIQUE |
| expires_at | timestamptz | |
| revoked_at | timestamptz | |
| replaced_by_token_hash | varchar(64) | |
| created_at | timestamptz | |
| updated_at | timestamptz | |
