CREATE TABLE IF NOT EXISTS "Orders"(
	"Id" varchar(255) PRIMARY KEY,
    "Product" varchar(255),
    "Total" decimal(10,2),
    "AmountPaid" decimal(10,2),
    "Currency" varchar(255)
);