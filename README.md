# AcxiomCRM setup
1. Install .NET 8 SDK + SQL Server LocalDB (comes with Visual Studio).
2. dotnet tool install --global dotnet-ef
3. cd AcxiomCRM ; dotnet restore
4. dotnet ef migrations add Initial ; dotnet ef database update
5. dotnet run  -> open https://localhost:5001 (or URL shown)
6. Login: admin@acxiom.com / Admin@123   manager@acxiom.com / Admin@123   (Register creates SalesExecutive)
