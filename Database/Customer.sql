SELECT 
	CustomerId,
	CompanyName
FROM 
	Customers
WHERE 
	CustomerId = @CustomerId;			