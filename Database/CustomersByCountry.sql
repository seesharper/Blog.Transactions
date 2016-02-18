SELECT 
	CustomerId,
	CompanyName
FROM 
	Customers
WHERE 
	Country = @Country;			